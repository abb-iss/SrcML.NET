/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Thread = System.Threading.Thread;

namespace ABB.SrcML.VisualStudio.SolutionMonitor {
    /// <summary>
    /// This class implements two Visual Studio basic IDE interfaces:
    /// (1) IVsTrackProjectDocumentsEvents2: Notifies clients of changes made to project files or directories.
    /// Methods of this interface were implemented to handle file creation and deletion events in Visual Studio envinronment.
    /// (2) IVsRunningDocTableEvents: Implements methods that fire in response to changes to documents in the Running Document Table (RDT).
    /// Methods of this interface were implemented to handle file change events in Visual Studio envinronment.
    /// This class also implements IFileMonitor so that client applications and SrcMLArchive can subscribe events that are raised from solution monitor.
    /// </summary>
    //public class SolutionMonitor : IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents, IFileMonitor {
    public class SolutionMonitor : AbstractFileMonitor, IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents {

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

        /// <summary>
        /// IVsRunningDocumentTable: Manages the set of currently open documents in the environment.
        /// DocumentTableItemId is used in registering/unregistering events.
        /// </summary>
        private IVsRunningDocumentTable DocumentTable;
        private uint DocumentTableItemId;

        /// <summary>
        /// IVsTrackProjectDocumentsEvents2: Used by projects to query the environment for 
        /// permission to add, remove, or rename a file or directory in a solution.
        /// pdwCookie is used in registering/unregistering events.
        /// </summary>
        private IVsTrackProjectDocuments2 ProjectDocumentsTracker;
        private uint PdwCookie = VSConstants.VSCOOKIE_NIL;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="openSolution"></param>
        public SolutionMonitor(List<AbstractArchive> listOfArchives, SolutionWrapper openSolution) {
            OpenSolution = openSolution;
        }


        /*
        #region IFileMonitor
        public event EventHandler<FileEventRaisedArgs> FileEventRaised;

        /// <summary>
        /// Start monitoring the solution.
        /// </summary>
        public void StartMonitoring() {
            //FileLogger.DefaultLogger.Info("======= SolutionMonitor: START MONITORING ======="");
            RegisterTrackProjectDocumentsEvents2();
            RegisterRunningDocumentTableEvents();
        }

        /// <summary>
        /// Stop monitoring the solution.
        /// </summary>
        public void StopMonitoring() {
            //FileLogger.DefaultLogger.Info("======= SolutionMonitor: STOP MONITORING ======="");
            Dispose();
        }

        /// <summary>
        /// Handle SolutionMonitorEvents.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnFileEventRaised(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileEventRaised;
            if(handler != null) {
                handler(this, e);
            }
        }
        #endregion
        */


        ///////  should add the event and method below into AbstractFileMonitor
        public event EventHandler<FileEventRaisedArgs> FileEventRaised;
        /// <summary>
        /// Handle SolutionMonitorEvents.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnFileEventRaised(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileEventRaised;
            if(handler != null) {
                handler(this, e);
            }
        }

        #region AbstractFileMonitor Members
        /// <summary>
        /// Start monitoring the solution.
        /// </summary>
        public override void StartMonitoring() {
            //FileLogger.DefaultLogger.Info("======= SolutionMonitor: START MONITORING ======="");
            RegisterTrackProjectDocumentsEvents2();
            RegisterRunningDocumentTableEvents();

            // Call AbstractFileMonitor's Startup()
            Startup();
        }

        /// <summary>
        /// Stop monitoring the solution.
        /// </summary>
        public void StopMonitoring() {
            //FileLogger.DefaultLogger.Info("======= SolutionMonitor: STOP MONITORING ======="");

            // unsubscribe from visual studio
            Dispose();
        }
        #endregion AbstractFileMonitor Members

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
        /// Register RunningDocumentTable events.
        /// </summary>
        public void RegisterRunningDocumentTableEvents() {
            DocumentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            if(DocumentTable != null) {
                int hr = DocumentTable.AdviseRunningDocTableEvents(this, out DocumentTableItemId);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Dispose this solution monitor.
        /// </summary>
        public override void Dispose() {
            // Unregister IVsTrackProjectDocumentsEvents2 events
            if(PdwCookie != VSConstants.VSCOOKIE_NIL && ProjectDocumentsTracker != null) {
                int hr = ProjectDocumentsTracker.UnadviseTrackProjectDocumentsEvents(PdwCookie);
                ErrorHandler.Succeeded(hr);
                PdwCookie = VSConstants.VSCOOKIE_NIL;
            }

            // Unregister IVsRunningDocTableEvents events
            if(DocumentTable != null) {
                int hr = DocumentTable.UnadviseRunningDocTableEvents(DocumentTableItemId);
                ErrorHandler.Succeeded(hr);
            }
            base.Dispose();
        }




        ////// obsolete methods from here

        /// <summary>
        /// Get all "monitored" files in this solution.
        /// TODO: exclude directories?
        /// </summary>
        /// <returns></returns>
        public List<string> GetMonitoredFiles(BackgroundWorker worker) {
            AllMonitoredFiles = new List<string>();
            WalkSolutionTree(worker);
            return AllMonitoredFiles;
        }

        /// <summary>
        /// Recursively walk through the solution/projects to check if srcML files need to be ADDED or CHANGED.
        /// TODO: may process files in parallel
        /// </summary>
        /// <param name="worker"></param>
        private void WalkSolutionTree(BackgroundWorker worker) {
            var allProjects = OpenSolution.getProjects();
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                var project = (Project)enumerator.Current;
                if(project != null) {
                    if(project.ProjectItems != null) {
                        try {
                            ProcessItems(project.ProjectItems.GetEnumerator(), worker);
                        } catch(Exception e) {
                            Console.WriteLine("Problem parsing files:" + e.Message);
                        }
                    }
                    if(worker != null && worker.CancellationPending) {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Recursively process project items.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="worker"></param>
        private void ProcessItems(IEnumerator items, BackgroundWorker worker) {
            while(items.MoveNext()) {
                if(worker != null && worker.CancellationPending) {
                    return;
                }
                var item = (ProjectItem)items.Current;
                ProcessItem(item, worker);
            }
        }

        /// <summary>
        /// Process a single project item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessItem(ProjectItem item, BackgroundWorker worker) {
            ProcessSingleFile(item, worker);
            ProcessChildren(item, worker);
        }

        /// <summary>
        /// Process the children items of a project item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessChildren(ProjectItem item, BackgroundWorker worker) {
            if(item != null && item.ProjectItems != null) {
                ProcessItems(item.ProjectItems.GetEnumerator(), worker);
            }
        }

        /// <summary>
        /// Process a single source file. (Not include file deletion.)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessSingleFile(ProjectItem item, BackgroundWorker worker) {
            string path = "";
            try {
                if(item != null && item.Name != null) {
                    try {
                        path = item.FileNames[0];
                    } catch(Exception e) {
                        Console.WriteLine("Exception when getting file names: " + path + "; " + e.Message);
                        path = item.FileNames[1];
                    }

                    // TODO: exclude directories?
                    AllMonitoredFiles.Add(path);
                }
            } catch(Exception e) {
                Console.WriteLine("Problem parsing file: " + path + "; " + e.Message);
            }
        }

        ////// obsolete methods above




        /// <summary>
        /// Handle file creation/deletion cases.
        /// The way these parameters work is:
        /// rgFirstIndices contains a list of the starting index into the changeProjectItems array for each project listed in the changedProjects list
        /// Example: if you get two projects, then rgFirstIndices should have two elements, the first element is probably zero since rgFirstIndices would start at zero.
        /// Then item two in the rgFirstIndices array is where in the changeProjectItems list that the second project's changed items reside.
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
                        RaiseSolutionMonitorEvent(rgpszMkDocuments[projItemIndex], null, type);
                    }
                }
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Raise events of file creation/change/deletion in the solution in Visual Studio
        /// TODO: the condition check? (From original SrcML.NET)
        /// </summary>
        /// <param name="file"></param>
        /// <param name="type"></param>
        public void RaiseSolutionMonitorEvent(string filePath, string oldFilePath, FileEventType type) {
            //var directoryName = Path.GetDirectoryName(Path.GetFullPath(eventArgs.SourceFilePath));
            //var xmlFullPath = Path.GetFullPath(this.ArchivePath);

            //if (!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                FileEventRaisedArgs eventArgs = null;
                switch(type) {
                    case FileEventType.FileAdded:
                        eventArgs = new FileEventRaisedArgs(filePath, FileEventType.FileAdded);
                        break;
                    case FileEventType.FileChanged:
                        eventArgs = new FileEventRaisedArgs(filePath, FileEventType.FileChanged);
                        break;
                    case FileEventType.FileDeleted:
                        eventArgs = new FileEventRaisedArgs(filePath, FileEventType.FileDeleted);
                        break;
                    case FileEventType.FileRenamed:  // actually not used
                        eventArgs = new FileEventRaisedArgs(filePath, oldFilePath, FileEventType.FileRenamed);
                        break;
                }
                //FileLogger.DefaultLogger.Info("SolutionMonitor raises a " + type + " event for [" + filePath + "]");
                OnFileEventRaised(eventArgs);
            }
        }

        #region IVsTrackProjectDocumentsEvents2
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
            //FileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx()");
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, FileEventType.FileAdded);
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
            //FileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles()");
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgpszMkDocuments, FileEventType.FileDeleted);
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
            //FileLogger.DefaultLogger.Info("==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles()");
            OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkOldNames, FileEventType.FileDeleted);
            return OnNotifyFileAddRemove(cProjects, cFiles, rgpProjects, rgFirstIndices, rgszMkNewNames, FileEventType.FileAdded);
        }

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
        #endregion

        #region IVsRunningDocTableEvents
        /// <summary>
        /// Called after saving a document in the Running Document Table (RDT).
        /// TODO: run in background or not?
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public int OnAfterSave(uint cookie) {
            //FileLogger.DefaultLogger.Info("==> Triggered IVsRunningDocTableEvents.OnAfterSave()");
            uint flags;
            uint readingLocks;
            uint edittingLocks;
            string name;
            IVsHierarchy hierarchy;
            uint documentId;
            IntPtr documentData;

            DocumentTable.GetDocumentInfo(cookie, out flags, out readingLocks, out edittingLocks, out name, out hierarchy, out documentId, out documentData);
            RaiseSolutionMonitorEvent(name, null, FileEventType.FileChanged);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after application of the first lock of the specified type to the specified document in the Running Document Table (RDT).
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
        /// Called before releasing the last lock of the specified type on the specified document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="lockType"></param>
        /// <param name="readLocksLeft"></param>
        /// <param name="editLocksLeft"></param>
        /// <returns></returns>
        public int OnBeforeLastDocumentUnlock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft) {
            return VSConstants.S_OK;
        }

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
        /// Called after a document window is placed in the Hide state.
        /// </summary>
        /// <param name="docCookie"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }
        #endregion








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
        /// Save a specific file in the Running Docuement Table.
        /// Being called in Sando's SolutionMonitor_SaveProjectItemsTest()
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
                        DocumentTable.SaveDocuments((uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_ForceSave, null, 0, docCookie[0]);
                        //FileLogger.DefaultLogger.Info("IVsRunningDocumentTable.SaveDocuments() DONE. [" + moniker + "]");
                    }
                }
            }
        }

        /// <summary>
        /// For debugging.
        /// writeLog("C:\\Data\\srcMLNETlog.txt", "======= SolutionMonitor: START MONITORING =======");
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="str"></param>
        private void writeLog(string logFile, string str) {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }

    
    }
}
