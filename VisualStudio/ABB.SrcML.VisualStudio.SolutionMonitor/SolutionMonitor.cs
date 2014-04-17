using ABB.SrcML.Utilities;
using EnvDTE;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Thread = System.Threading.Thread;

namespace ABB.SrcML.VisualStudio.SolutionMonitor {

    /// <summary>
    /// This class implements three Visual Studio basic IDE interfaces: (1)
    /// IVsTrackProjectDocumentsEvents2: Notifies clients of changes made to project files or
    /// directories. Methods of this interface were implemented to handle file creation and deletion
    /// events in Visual Studio envinronment. (2) IVsRunningDocTableEvents: Implements methods that
    /// fire in response to changes to documents in the Running Document Table (RDT). Methods of
    /// this interface were implemented to handle file change events in Visual Studio envinronment.
    /// (3) IVsSolutionEvents: Listening interface that monitors any notifications of changes to the
    /// solution. Methods of this interface were implemented to handle project
    /// creation/change/deletion events in Visual Studio envinronment. This class also extends
    /// AbstractFileMonitor so that SrcMLService can subscribe events that are raised from solution
    /// monitor.
    /// </summary>
    public class SolutionMonitor : AbstractFileMonitor, IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents, IVsSolutionEvents {
        /*
        public string FullFolderPath { get; set; }
        */

        /// <summary>
        /// OpenSolution: The solution to be monitored.
        /// </summary>
        private readonly SolutionWrapper OpenSolution;

        /// <summary>
        /// List of all "monitored" files.
        /// </summary>
        private List<string> AllMonitoredFiles;

        private uint cookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// IVsRunningDocumentTable: Manages the set of currently open documents in the environment.
        /// DocumentTableItemId is used in registering/unregistering events.
        /// </summary>
        private IVsRunningDocumentTable DocumentTable;

        private uint DocumentTableItemId;

        /// <summary>
        /// A bool flag indicating whether Solution Monitor is about to stop monitoring.
        /// </summary>
        private bool isAboutToStopMonitoring = false;

        private uint PdwCookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// IVsTrackProjectDocumentsEvents2: Used by projects to query the environment for
        /// permission to add, remove, or rename a file or directory in a solution. pdwCookie is
        /// used in registering/unregistering events.
        /// </summary>
        private IVsTrackProjectDocuments2 ProjectDocumentsTracker;

        /// <summary>
        /// IVsSolution: Provides top-level manipulation or maintenance of the solution.
        /// cookie is used in registering/unregistering events.
        /// </summary>
        private IVsSolution solution;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="openSolution"></param>
        /// <param name="baseDirectory"></param>
        /// <param name="defaultArchive"></param>
        /// <param name="otherArchives"></param>
        public SolutionMonitor(SolutionWrapper openSolution, string baseDirectory, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : base(baseDirectory, defaultArchive, otherArchives) {
            this.OpenSolution = openSolution;
        }

        #region AbstractFileMonitor Members

        public override IEnumerable<string> EnumerateMonitoredFiles() {
            return GetMonitoredFiles(null);
        }

        /// <summary>
        /// Start monitoring the solution.
        /// </summary>
        public override void StartMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("======= SolutionMonitor: START MONITORING =======");
            RegisterSolutionEvents();
            RegisterTrackProjectDocumentsEvents2();
            RegisterRunningDocumentTableEvents();
        }

        /// <summary>
        /// Stop monitoring the solution.
        /// </summary>
        public override void StopMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("======= SolutionMonitor: STOP MONITORING =======");
            isAboutToStopMonitoring = true;

            UnregisterRunningDocumentTableEvents();
            UnregisterTrackProjectDocumentsEvents2();
            UnregisterSolutionEvents();

            base.StopMonitoring();
        }

        #endregion AbstractFileMonitor Members

        /// <summary>
        /// Get all "monitored" files in this solution.
        /// TODO: exclude directories? worker?
        /// </summary>
        /// <returns></returns>
        public List<string> GetMonitoredFiles(BackgroundWorker worker) {
            AllMonitoredFiles = new List<string>();
            WalkSolutionTree(worker);
            NumberOfAllMonitoredFiles = AllMonitoredFiles.Count;    // For progress bar purpose
            return AllMonitoredFiles;
        }

        /// <summary>
        /// Get a list of all files in the Running Docuement Table.
        /// </summary>
        /// <returns></returns>
        public List<string> GetRDTFiles() {
            List<string> list = new List<string>();
            IEnumRunningDocuments documents;
            if(DocumentTable != null) {
                DocumentTable.GetRunningDocumentsEnum(out documents);
                uint[] docCookie = new uint[1];
                uint fetched;
                while((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched)) {
                    uint flags;
                    uint editLocks;
                    uint readLocks;
                    string moniker;
                    IVsHierarchy docHierarchy;
                    uint docId;
                    IntPtr docData = IntPtr.Zero;
                    DocumentTable.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData);
                    list.Add(moniker);
                }
            }
            return list;
        }

        /// <summary>
        /// Register RunningDocumentTable events.
        /// </summary>
        public void RegisterRunningDocumentTableEvents() {
            DocumentTable = (IVsRunningDocumentTable) Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            if(DocumentTable != null) {
                int hr = DocumentTable.AdviseRunningDocTableEvents(this, out DocumentTableItemId);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Register Solution events.
        /// </summary>
        public void RegisterSolutionEvents() {
            //SrcMLFileLogger.DefaultLogger.Info("SolutionMonitor: RegisterSolutionEvents()");
            solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if(solution != null) {
                int hr = this.solution.AdviseSolutionEvents(this, out cookie);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Register TrackProjectDocuments2 events.
        /// </summary>
        public void RegisterTrackProjectDocumentsEvents2() {
            ProjectDocumentsTracker = Package.GetGlobalService(typeof(SVsTrackProjectDocuments)) as IVsTrackProjectDocuments2;
            if(ProjectDocumentsTracker != null) {
                int hr = ProjectDocumentsTracker.AdviseTrackProjectDocumentsEvents(this, out PdwCookie);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Respond to events of file creation/change/deletion in the solution in Visual Studio
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="oldFilePath"></param>
        /// <param name="type"></param>
        public void RespondToVSFileChangedEvent(string filePath, string oldFilePath, FileEventType type) {
            //SrcMLFileLogger.DefaultLogger.Info("SolutionMonitor.RespondToVSFileChangedEvent(): filePath = " + filePath + ", oldFilePath = " + oldFilePath + ", type = " + type);
            switch(type) {
                case FileEventType.FileAdded:
                    AddFile(filePath);
                    break;

                case FileEventType.FileChanged:
                    AddFile(filePath);
                    break;

                case FileEventType.FileDeleted:
                    DeleteFile(filePath);
                    break;

                case FileEventType.FileRenamed:  // actually not used
                    DeleteFile(oldFilePath);
                    AddFile(filePath);
                    break;
            }
        }

        /// <summary>
        /// Save a specific file in the Running Docuement Table.
        /// </summary>
        /// <param name="fileName"></param>
        public void saveRDTFile(string fileName) {
            IEnumRunningDocuments documents;
            if(DocumentTable != null) {
                DocumentTable.GetRunningDocumentsEnum(out documents);
                uint[] docCookie = new uint[1];
                uint fetched;
                while((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched)) {
                    uint flags;
                    uint editLocks;
                    uint readLocks;
                    string moniker;
                    IVsHierarchy docHierarchy;
                    uint docId;
                    IntPtr docData = IntPtr.Zero;
                    DocumentTable.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData);
                    if(fileName.Equals(moniker)) {
                        DocumentTable.SaveDocuments((uint) __VSRDTSAVEOPTIONS.RDTSAVEOPT_ForceSave, null, 0, docCookie[0]);
                        //FileLogger.DefaultLogger.Info("IVsRunningDocumentTable.SaveDocuments() DONE. [" + moniker + "]");
                    }
                }
            }
        }

        /// <summary>
        /// Unregister RunningDocumentTable events.
        /// </summary>
        public void UnregisterRunningDocumentTableEvents() {
            if(DocumentTable != null) {
                int hr = DocumentTable.UnadviseRunningDocTableEvents(DocumentTableItemId);
                ErrorHandler.Succeeded(hr);
            }
        }

        /// <summary>
        /// Unregister Solution events.
        /// </summary>
        public void UnregisterSolutionEvents() {
            //SrcMLFileLogger.DefaultLogger.Info("SolutionMonitor: UnregisterSolutionEvents()");
            if(cookie != VSConstants.VSCOOKIE_NIL && solution != null) {
                int hr = solution.UnadviseSolutionEvents(cookie);
                ErrorHandler.Succeeded(hr);
                cookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        /// <summary>
        /// Unregister TrackProjectDocuments2 events.
        /// </summary>
        public void UnregisterTrackProjectDocumentsEvents2() {
            if(PdwCookie != VSConstants.VSCOOKIE_NIL && ProjectDocumentsTracker != null) {
                int hr = ProjectDocumentsTracker.UnadviseTrackProjectDocumentsEvents(PdwCookie);
                ErrorHandler.Succeeded(hr);
                PdwCookie = VSConstants.VSCOOKIE_NIL;
            }
        }

        /// <summary>
        /// Handle file creation/deletion cases. The way these parameters work is: rgFirstIndices
        /// contains a list of the starting index into the changeProjectItems array for each project
        /// listed in the changedProjects list
        /// Example: if you get two projects, then rgFirstIndices should have two elements, the
        ///          first element is probably zero since rgFirstIndices would start at zero.
        /// Then item two in the rgFirstIndices array is where in the changeProjectItems list that
        /// the second project's changed items reside.
        /// TODO: may process files in parallel
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private int OnNotifyFileAddRemove(int cProjects,
                                        int cFiles,
                                        IVsProject[] rgpProjects,
                                        int[] rgFirstIndices,
                                        string[] rgpszMkDocuments,
                                        FileEventType type) {
            int projItemIndex = 0;
            for(int changeProjIndex = 0; changeProjIndex < cProjects; changeProjIndex++) {
                int endProjectIndex = ((changeProjIndex + 1) == cProjects) ? rgpszMkDocuments.Length : rgFirstIndices[changeProjIndex + 1];
                for(; projItemIndex < endProjectIndex; projItemIndex++) {
                    if(rgpProjects[changeProjIndex] != null) {
                        RespondToVSFileChangedEvent(rgpszMkDocuments[projItemIndex], null, type);
                    }
                }
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Process the children items of a project item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessChildren(ProjectItem item, BackgroundWorker worker, List<string> list) {
            if(item != null && item.ProjectItems != null) {
                ProcessItems(item.ProjectItems.GetEnumerator(), worker, list);
            } else {
                var proj = item.SubProject as Project;
                ProcessProject(proj, worker, list);
            }
        }

        /// <summary>
        /// Process a single project item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessItem(ProjectItem item, BackgroundWorker worker, List<string> list) {
            ProcessSingleFile(item, worker, list);
            ProcessChildren(item, worker, list);
        }

        /// <summary>
        /// Recursively process project items.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="worker"></param>
        private void ProcessItems(IEnumerator items, BackgroundWorker worker, List<string> list) {
            while(items.MoveNext()) {
                if(worker != null && worker.CancellationPending) {
                    return;
                }
                var item = (ProjectItem) items.Current;
                ProcessItem(item, worker, list);
            }
        }

        /// <summary>
        /// Process a project item.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="worker"></param>
        private void ProcessProject(Project project, BackgroundWorker worker, List<string> list) {
            if(project != null) {
                if(project.ProjectItems != null) {
                    try {
                        ProcessItems(project.ProjectItems.GetEnumerator(), worker, list);
                    } catch(Exception e) {
                        Console.WriteLine("Problem parsing files:" + e.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Process a single source file. (Not include file deletion.)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessSingleFile(ProjectItem item, BackgroundWorker worker, List<string> list) {
            string path = "";
            try {
                if(item != null && item.Name != null) {
                    try {
                        path = item.FileNames[0];
                    } catch(Exception e) {
                        Console.WriteLine("Exception when getting file names: " + path + "; " + e.Message);
                        path = item.FileNames[1];
                    }

                    if(File.Exists(path)) {
                        ////AllMonitoredFiles.Add(path);
                        list.Add(path);
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("Problem parsing file: " + path + "; " + e.Message);
            }
        }

        /// <summary>
        /// Recursively walk through the solution/projects to check if srcML files need to be ADDED
        /// or CHANGED.
        /// TODO: may process files in parallel
        /// </summary>
        /// <param name="worker"></param>
        private void WalkSolutionTree(BackgroundWorker worker) {
            try {
                var allProjects = OpenSolution.getProjects();
                var enumerator = allProjects.GetEnumerator();
                while(enumerator.MoveNext()) {
                    var project = enumerator.Current as Project;
                    ProcessProject(project, worker, AllMonitoredFiles);
                    if(worker != null && worker.CancellationPending) {
                        return;
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("Problem walk through the solution/projects. " + e.Message);
            }
        }

        #region IVsTrackProjectDocumentsEvents2

        /// <summary>
        /// This method notifies the client after directories are added to the project.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cDirectories"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client after a project has added files.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx(int cProjects,
                                                      int cFiles,
                                                      IVsProject[] rgpProjects,
                                                      int[] rgFirstIndices,
                                                      string[] rgpszMkDocuments,
                                                      VSADDFILEFLAGS[] rgFlags) {
            //////SrcMLFileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx()");
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, FileEventType.FileAdded);
        }

        /// <summary>
        /// This method notifies the client when directories have been removed from the project.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cDirectories"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client after files are removed from the project.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles(int cProjects,
                                                               int cFiles,
                                                               IVsProject[] rgpProjects,
                                                               int[] rgFirstIndices,
                                                               string[] rgpszMkDocuments,
                                                               VSREMOVEFILEFLAGS[] rgFlags) {
            //////SrcMLFileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles()");
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, FileEventType.FileDeleted);
        }

        /// <summary>
        /// This method notifies the client when directories have been renamed in the project.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cDirs"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgszMkOldNames"></param>
        /// <param name="rgszMkNewNames"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when files have been renamed in the project.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgszMkOldNames"></param>
        /// <param name="rgszMkNewNames"></param>
        /// <param name="rgFlags"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles(int cProjects,
                                                               int cFiles,
                                                               IVsProject[] rgpProjects,
                                                               int[] rgFirstIndices,
                                                               string[] rgszMkOldNames,
                                                               string[] rgszMkNewNames,
                                                               VSRENAMEFILEFLAGS[] rgFlags) {
            //////SrcMLFileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles()");
            OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkOldNames, FileEventType.FileDeleted);
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkNewNames, FileEventType.FileAdded);
        }

        /// <summary>
        /// This method notifies the client when source control status has changed.
        /// </summary>
        /// <param name="cProjects"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpProjects"></param>
        /// <param name="rgFirstIndices"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgdwSccStatus"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to add directories.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cDirectories"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to add files.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to remove directories.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cDirectories"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to remove files.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgpszMkDocuments"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to rename directories.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cDirs"></param>
        /// <param name="rgszMkOldNames"></param>
        /// <param name="rgszMkNewNames"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject, int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This method notifies the client when a project has requested to rename files.
        /// </summary>
        /// <param name="pProject"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgszMkOldNames"></param>
        /// <param name="rgszMkNewNames"></param>
        /// <param name="rgFlags"></param>
        /// <param name="pSummaryResult"></param>
        /// <param name="rgResults"></param>
        /// <returns></returns>
        int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults) {
            return VSConstants.S_OK;
        }

        #endregion IVsTrackProjectDocumentsEvents2

        #region IVsRunningDocTableEvents

        /// <summary>
        /// Called after a change in an attribute of a document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="grfAttribs"></param>
        /// <returns></returns>
        public int OnAfterAttributeChange(uint cookie, uint grfAttribs) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after a document window is placed in the Hide state.
        /// </summary>
        /// <param name="docCookie"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after application of the first lock of the specified type to the specified
        /// document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="lockType"></param>
        /// <param name="readLocksLeft"></param>
        /// <param name="editLocksLeft"></param>
        /// <returns></returns>
        public int OnAfterFirstDocumentLock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after saving a document in the Running Document Table (RDT).
        /// TODO: run in background or not?
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public int OnAfterSave(uint cookie) {
            //////SrcMLFileLogger.DefaultLogger.Info("==> Triggered IVsRunningDocTableEvents.OnAfterSave()");
            uint flags;
            uint readingLocks;
            uint edittingLocks;
            string name;
            IVsHierarchy hierarchy;
            uint documentId;
            IntPtr documentData;

            DocumentTable.GetDocumentInfo(cookie, out flags, out readingLocks, out edittingLocks, out name, out hierarchy, out documentId, out documentData);
            RespondToVSFileChangedEvent(name, null, FileEventType.FileChanged);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called before displaying a document window.
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="fFirstShow"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnBeforeDocumentWindowShow(uint cookie, int fFirstShow, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called before releasing the last lock of the specified type on the specified document in
        /// the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="lockType"></param>
        /// <param name="readLocksLeft"></param>
        /// <param name="editLocksLeft"></param>
        /// <returns></returns>
        public int OnBeforeLastDocumentUnlock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft) {
            return VSConstants.S_OK;
        }

        #endregion IVsRunningDocTableEvents

        #region IVsSolutionEvents

        /// <summary>
        /// Handle project addition/deletion cases. The way these parameters work is:
        /// pHierarchy: Pointer to the IVsHierarchy interface of the project being loaded or closed.
        /// fAddedRemoved: For addition, true if the project is added to the solution after the
        ///                solution is opened, false if the project is added to the solution while
        ///                the solution is being opened. For deletion, true if the project was
        ///                removed from the solution before the solution was closed, false if the
        ///                project was removed from the solution while the solution was being
        ///                closed.
        /// type: FileEventType.FileAdded - project addition, FileEventType.FileDeleted - project
        ///       deletion.
        /// TODO: may process files in parallel
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fAddedRemoved"></param>
        /// <param name="type"></param>
        public void NotifyProjectAddRemove(IVsHierarchy pHierarchy, int fAddedRemoved, FileEventType type) {
            List<string> fileList = new List<string>();
            string projectName;
            pHierarchy.GetCanonicalName(VSConstants.VSITEMID_ROOT, out projectName);
            //SrcMLFileLogger.DefaultLogger.Info("Project Name: [" + projectName + "]");

            // Find out this project in the solution tree
            var allProjects = OpenSolution.getProjects();
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                Project project = enumerator.Current as Project;
                string fullName = null;
                try {
                    //SrcMLFileLogger.DefaultLogger.Info("FileName: [" + project.FileName + "]");
                    fullName = project.FileName;
                } catch(Exception e) {
                    // Ignore unloaded project. It would cause a Not Implemented Exception for an
                    // unloaded project.
                    //SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SolutionMonitor.NotifyProjectAddRemove() - "));
                    continue;
                }
                if(fullName != null && (fullName.Equals(projectName) || fullName.ToLower().Contains(projectName.ToLower()))) {
                    SrcMLFileLogger.DefaultLogger.Info("Project: [" + projectName + "]");
                    ProcessProject(project, null, fileList);
                    break;
                }
            }

            // Generate or delete srcML files for the source files in this project
            try {
                foreach(var filePath in fileList) {
                    if(FileEventType.FileAdded.Equals(type)) {
                        //SrcMLFileLogger.DefaultLogger.Info(">>> AddFile(" + filePath + ")");
                        AddFile(filePath);
                    } else if(FileEventType.FileDeleted.Equals(type)) {
                        //SrcMLFileLogger.DefaultLogger.Info(">>> DeleteFile(" + filePath + ")");
                        DeleteFile(filePath);
                    }
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception when batch adding or deleting srcML files for a specified project."));
            }
        }

        /// <summary>
        /// Notifies listening clients that a solution has been closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <returns></returns>
        public int OnAfterCloseSolution(object pUnkReserved) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnAfterCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project has been loaded. Only being triggered when
        /// reloading a project.
        /// </summary>
        /// <param name="pStubHierarchy"></param>
        /// <param name="pRealHierarchy"></param>
        /// <returns></returns>
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnAfterLoadProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project has been opened.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fAdded"></param>
        /// <returns></returns>
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            SrcMLFileLogger.DefaultLogger.Info("====> SolutionMonitor: Triggered IVsSolutionEvents.OnAfterOpenProject() [" + fAdded + "]");
            NotifyProjectAddRemove(pHierarchy, fAdded, FileEventType.FileAdded);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the solution has been opened.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <param name="fNewSolution"></param>
        /// <returns></returns>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnAfterOpenSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project is about to be closed.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fRemoved"></param>
        /// <returns></returns>
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
            SrcMLFileLogger.DefaultLogger.Info("====> SolutionMonitor: Triggered IVsSolutionEvents.OnBeforeCloseProject() [" + fRemoved + "]");
            // If it is about to stop monitoring, skip this call and do not delete any srcML files
            if(!isAboutToStopMonitoring) {
                NotifyProjectAddRemove(pHierarchy, fRemoved, FileEventType.FileDeleted);
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the solution is about to be closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <returns></returns>
        public int OnBeforeCloseSolution(object pUnkReserved) {
            //SrcMLFileLogger.DefaultLogger.Info("=> SolutionMonitor: Triggered IVsSolutionEvents.OnBeforeCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project is about to be unloaded.
        /// </summary>
        /// <param name="pRealHierarchy"></param>
        /// <param name="pStubHierarchy"></param>
        /// <returns></returns>
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnBeforeUnloadProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the project can be closed.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fRemoving"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnQueryCloseProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the solution can be closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnQueryCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the project can be unloaded.
        /// </summary>
        /// <param name="pRealHierarchy"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            //SrcMLFileLogger.DefaultLogger.Info("==> SolutionMonitor: Triggered IVsSolutionEvents.OnQueryUnloadProject()");
            return VSConstants.S_OK;
        }

        #endregion IVsSolutionEvents
    }
}