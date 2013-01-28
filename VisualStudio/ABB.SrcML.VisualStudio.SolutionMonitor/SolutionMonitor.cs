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

namespace ABB.SrcML.VisualStudio.SolutionMonitor
{
    /// <summary>
    /// This class implements two Visual Studio basic IDE interfaces:
    /// (1) IVsTrackProjectDocumentsEvents2: Notifies clients of changes made to project files or directories.
    /// Methods of this interface were implemented to handle file creation and deletion events in Visual Studio envinronment.
    /// (2) IVsRunningDocTableEvents: Implements methods that fire in response to changes to documents in the Running Document Table (RDT).
    /// Methods of this interface were implemented to handle file change events in Visual Studio envinronment.
    /// Also tried IVsFileChangeEvents (Notifies clients when selected files have been changed on disk), but it does not meet requirements.
    /// 
    /// This class also implements ISolutionMonitorEvents so that client applications and SrcMLArchive can subscribe events that are raised from solution monitor.
    /// </summary>
    public class SolutionMonitor : IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents, IFileMonitor
    {
        public string FullFolderPath {
            get;
            set;
        }

        /// <summary>
        /// _openSolution: The solution to be monitored.
        /// </summary>
        private readonly SolutionWrapper _openSolution;

        /// <summary>
        /// List of all "monitored" files.
        /// </summary>
        private List<string> allMonitoredFiles;

        /// <summary>
        /// IVsRunningDocumentTable: Manages the set of currently open documents in the environment.
        /// _documentTableItemId is used in registering/unregistering events.
        /// </summary>
        private IVsRunningDocumentTable _documentTable;
        private uint _documentTableItemId;

        /// <summary>
        /// IVsTrackProjectDocumentsEvents2: Used by projects to query the environment for 
        /// permission to add, remove, or rename a file or directory in a solution.
        /// pdwCookie is used in registering/unregistering events.
        /// </summary>
        private IVsTrackProjectDocuments2 _projectDocumentsTracker;
        private uint _pdwCookie = VSConstants.VSCOOKIE_NIL;
        
        /// <summary>
        /// Background workers.
        /// </summary>
        //private readonly BackgroundWorker _processFileInBackground;   // no use so far, will consider this later
        //private BackgroundWorker startupWorker;   // moved to SrcMLArchive

        /// <summary>
        /// The full path for storing srcML files.
        /// TODO: move the string to a Constant file.
        ///       maybe: private readonly string _archivePath;
        ///       Obsolete
        /// </summary>
        //public string ArchivePath = "D:\\Data\\SrcML.NETDemo\\MySrcMLArchive";

        //// Note: "////" means code about index, which can be removed when cleaning up this class.
        ////private const string StartupThreadName = "Sando: Initial Index of Project";
        ////private DocumentIndexer _currentIndexer;
        ////private readonly string _currentPath;
        ////private readonly SolutionKey _solutionKey;
        ////public volatile bool ShouldStop = false;
        ////private readonly IndexUpdateManager _indexUpdateManager;
        ////private bool _initialIndexDone = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="openSolution"></param>
        ////public SolutionMonitor(SolutionWrapper openSolution, SolutionKey solutionKey, DocumentIndexer currentIndexer, bool isIndexRecreationRequired)
        public SolutionMonitor(SolutionWrapper openSolution)
        {
            _openSolution = openSolution;
            ////_currentIndexer = currentIndexer;
            ////_currentPath = solutionKey.GetIndexPath();
            ////_solutionKey = solutionKey;
            ////_indexUpdateManager = new IndexUpdateManager(solutionKey, _currentIndexer, isIndexRecreationRequired);

            // no use so far, will consider this later
            //_processFileInBackground = new BackgroundWorker();
            //_processFileInBackground.DoWork += new DoWorkEventHandler(_processFileInBackground_DoWork);
        }

        /// <summary>
        /// Register TrackProjectDocuments2 events.
        /// </summary>
        public void RegisterTrackProjectDocumentsEvents2()
        {
            _projectDocumentsTracker = Package.GetGlobalService(typeof(SVsTrackProjectDocuments)) as IVsTrackProjectDocuments2;
            if (_projectDocumentsTracker != null)
            {
                int hr = _projectDocumentsTracker.AdviseTrackProjectDocumentsEvents(this, out _pdwCookie);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Register RunningDocumentTable events.
        /// </summary>
        public void RegisterRunningDocumentTableEvents()
        {
            _documentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            if (_documentTable != null)
            {
                int hr = _documentTable.AdviseRunningDocTableEvents(this, out _documentTableItemId);
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// no use so far, will consider this later
        /// Run in background for processing a specific file.
        /// TODO: Should be ProcessSingleFile(projectItem, null) or ProcessItem(projectItem, null)?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _processFileInBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            ProjectItem projectItem = e.Argument as ProjectItem;
            ProcessItem(projectItem, null);
            ////UpdateAfterAdditions();
        }

        /* // moved to SrcMLArchive
        /// <summary>
        /// Run in background when starting up the solution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="anEvent"></param>
        private void _runStartupInBackground_DoWork(object sender, DoWorkEventArgs anEvent)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var worker = sender as BackgroundWorker;
            try
            {
                WalkSolutionTree(worker);
                WalkSrcMLDirectory();
            }
            catch (Exception e)
            {
                ////FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Problem getting projects to process."));
                Console.WriteLine("Problem getting projects to process." + e.Message);
            }
            finally
            {
                OnSrcMLDOTNETEventsRaised(new SrcMLDOTNETEventArgs(null, SrcMLDOTNETEventType.StartupCompleted));
                ////_initialIndexDone = true;
                ////ShouldStop = false;
            }

            stopwatch.Stop();
            writeLog("D:\\Data\\log.txt", "Total time for startup check: " + stopwatch.Elapsed.ToString());
        }
        */

        /// <summary>
        /// Get all "monitored" files in this solution.
        /// TODO: exclude directories.
        /// </summary>
        /// <returns></returns>
        public List<string> GetMonitoredFiles(BackgroundWorker worker)
        {
            allMonitoredFiles = new List<string>();

            WalkSolutionTree(worker);
            
            return allMonitoredFiles;
        }


        /// <summary>
        /// Recursively walk through the solution/projects to check if srcML files need to be ADDED or CHANGED.
        /// TODO: may process files in parallel
        /// </summary>
        /// <param name="worker"></param>
        private void WalkSolutionTree(BackgroundWorker worker)
        {
            var allProjects = _openSolution.getProjects();
            var enumerator = allProjects.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var project = (Project)enumerator.Current;
                if (project != null)
                {
                    if (project.ProjectItems != null)
                    {
                        try
                        {
                            ProcessItems(project.ProjectItems.GetEnumerator(), worker);
                        }
                        catch (Exception e)
                        {
                            ////FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Problem parsing files:"));
                            Console.WriteLine("Problem parsing files:" + e.Message);
                        }
                        finally
                        {
                            ////UpdateAfterAdditions();
                        }
                    }
                    if (worker != null && worker.CancellationPending)
                    {
                        return;
                    }
                }
            }
        }

        /* // moved to SrcMLArchive
        /// <summary>
        /// Walk srcML directory to see if there are any srcML files to be DELETED.
        /// TODO: may improve performance
        /// </summary>
        private void WalkSrcMLDirectory()
        {
            List<string> allSrcMLedFileNames = GetAllSrcMLedFiles();
            foreach (string sourceFilePath in allSrcMLedFileNames)
            {
                ProcessSingleSourceFile(sourceFilePath);
            }
        }
        */

        /* //// Remove index part
        public void UpdateAfterAdditions()
        {
            _currentIndexer.CommitChanges();
            _indexUpdateManager.SaveFileStates();
        }
        */

        /// <summary>
        /// Recursively process project items.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="worker"></param>
        private void ProcessItems(IEnumerator items, BackgroundWorker worker)
        {
            while (items.MoveNext())
            {
                if (worker != null && worker.CancellationPending)
                {
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
        private void ProcessItem(ProjectItem item, BackgroundWorker worker)
        {
            ProcessSingleFile(item, worker);
            ProcessChildren(item, worker);
        }

        /// <summary>
        /// Process the children items of a project item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessChildren(ProjectItem item, BackgroundWorker worker)
        {
            ////try
            ////{
                if (item != null && item.ProjectItems != null)
                {
                    ProcessItems(item.ProjectItems.GetEnumerator(), worker);
                }
            ////}
            ////catch (COMException dll)
            ////{
                ////ignore, can't parse these types of files
            ////}
        }

        /// <summary>
        /// Process a single source file. (Not include file deletion.)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessSingleFile(ProjectItem item, BackgroundWorker worker)
        {
            string path = "";
            try
            {
                if (item != null && item.Name != null)
                {
                    try
                    {
                        path = item.FileNames[0];
                    }
                    catch (Exception e)
                    {
                        path = item.FileNames[1];
                    }
                    ////string fileExtension = Path.GetExtension(path);
                    ////if (fileExtension != null && !fileExtension.Equals(String.Empty))
                    ////{
                        ////if (ExtensionPointsRepository.Instance.GetParserImplementation(fileExtension) != null)
                        ////Debug.WriteLine("Start: " + path);
                        ////ProcessFileForTesting(path);

                        //ProcessSingleSourceFile(path);    // moved to SrcMLArchive
                        
                        //writeLog("D:\\Data\\log.txt", "allMonitoredFiles.Add(" + path + ")");
                        
                        // TODO: exclude directories
                        allMonitoredFiles.Add(path);

                        ////Debug.WriteLine("End: " + path);
                    ////}
                }
            }
            ////TODO - don't catch a generic exception
            catch (Exception e)
            {
                ////FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Problem parsing file: " + path));
                Console.WriteLine("Problem parsing file: " + path + "; " + e.Message);
            }
        }

        /* // moved to SrcMLArchive
        // Temp method for file extension check
        private bool IsValidFileExtension(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            if (fileExtension != null && !fileExtension.Equals(String.Empty))
            {
                if (".c".Equals(fileExtension) || ".h".Equals(fileExtension) || ".cpp".Equals(fileExtension) || ".hpp".Equals(fileExtension))
                {
                    return true;
                }
            }
            return false;
        }
        */

        /* //// Remove index part
        public void ProcessFileForTesting(string path)
        {
            _indexUpdateManager.UpdateFile(path);
        }
        */

        /* moved to SrcMLArchive
        /// <summary>
        /// Process a single source file to add or change the corresponding srcML file, or do nothing.
        /// TODO: GetXmlPathForSourcePath() twice
        /// </summary>
        /// <param name="sourceFilePath"></param>
        public void ProcessSingleSourceFile(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                // If there is not such a source file, then delete the corresponding srcML file
                writeLog("D:\\Data\\log.txt", "--> To DELETE srcML for: " + sourceFilePath);
                RespondToFileChangedEvent(sourceFilePath, SrcMLDOTNETEventType.SourceFileDeleted);
            }
            else
            {
                if (IsValidFileExtension(sourceFilePath))
                {
                    string srcMLFilePath = GetXmlPathForSourcePath(sourceFilePath);
                    writeLog("D:\\Data\\log.txt", "ProcessSingleSourceFile(): src = [" + sourceFilePath + "], srcML = [" + srcMLFilePath + "]");
                    if (!File.Exists(srcMLFilePath))
                    {
                        // If there is not a corresponding srcML file, then generate the srcML file
                        writeLog("D:\\Data\\log.txt", "--> To ADD: " + srcMLFilePath);
                        RespondToFileChangedEvent(sourceFilePath, SrcMLDOTNETEventType.SourceFileAdded);
                    }
                    else
                    {
                        DateTime sourceFileTimestamp = new FileInfo(sourceFilePath).LastWriteTime;
                        DateTime srcLMFileTimestamp = new FileInfo(srcMLFilePath).LastWriteTime;
                        if (sourceFileTimestamp.CompareTo(srcLMFileTimestamp) > 0)
                        {
                            // If source file's timestamp is later than its srcML file's timestamp, then generate the srcML file, otherwise do nothing
                            writeLog("D:\\Data\\log.txt", "--> To CHANGE: " + srcMLFilePath);
                            RespondToFileChangedEvent(sourceFilePath, SrcMLDOTNETEventType.SourceFileChanged);
                        }
                        else
                        {
                            writeLog("D:\\Data\\log.txt", "--> NO ACTION: " + sourceFilePath);
                        }
                    }
                }
            }
        }
        */

        /// <summary>
        /// For debugging. May be adapted to GetListOfAllMonitoredFiles()
        /// </summary>
        /// <param name="outputFile"></param>
        /*
        public void PrintAllMonitoredFiles(string outputFile)
        {
            StreamWriter sw = new StreamWriter(outputFile, false, System.Text.Encoding.ASCII);
            sw.WriteLine("All Monitored Files");
            if (fileChangeEx != null && listenInfos.Any())
            {
                var filenames = listenInfos.Select(i => i.Key).ToArray();
                foreach (var filename in filenames)
                {
                    sw.WriteLine("File: [" + filename + "]");
                }
            }
            sw.Close();
        }
        */

        /// <summary>
        /// Get a list of all files in the Running Docuement Table.
        /// Seems to be useless now.
        /// </summary>
        /// <returns></returns>
        public List<string> GetRDTFiles()
        {
            List<string> list = new List<string>();
            IEnumRunningDocuments documents;
            if (_documentTable != null)
            {
                _documentTable.GetRunningDocumentsEnum(out documents);
                uint[] docCookie = new uint[1];
                uint fetched;
                while ((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched))
                {
                    uint flags;
                    uint editLocks;
                    uint readLocks;
                    string moniker;
                    IVsHierarchy docHierarchy;
                    uint docId;
                    IntPtr docData = IntPtr.Zero;
                    _documentTable.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData);
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
        public void saveRDTFile(string fileName)
        {
            IEnumRunningDocuments documents;
            if (_documentTable != null)
            {
                _documentTable.GetRunningDocumentsEnum(out documents);
                uint[] docCookie = new uint[1];
                uint fetched;
                while ((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched))
                {
                    uint flags;
                    uint editLocks;
                    uint readLocks;
                    string moniker;
                    IVsHierarchy docHierarchy;
                    uint docId;
                    IntPtr docData = IntPtr.Zero;
                    _documentTable.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData);
                    if (fileName.Equals(moniker))
                    {
                        _documentTable.SaveDocuments((uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_ForceSave, null, 0, docCookie[0]);
                        //writeLog("D:\\Data\\log.txt", "IVsRunningDocumentTable.SaveDocuments() DONE. [" + moniker + "]");
                    }
                }
            }
        }

        /// <summary>
        /// Dispose this solution monitor.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Unregister IVsTrackProjectDocumentsEvents2 events
                if (_pdwCookie != VSConstants.VSCOOKIE_NIL && _projectDocumentsTracker != null)
                {
                    int hr = _projectDocumentsTracker.UnadviseTrackProjectDocumentsEvents(_pdwCookie);
                    ErrorHandler.Succeeded(hr);
                    _pdwCookie = VSConstants.VSCOOKIE_NIL;
                }

                // Unregister IVsRunningDocTableEvents events
                if (_documentTable != null)
                {
                    int hr = _documentTable.UnadviseRunningDocTableEvents(_documentTableItemId);
                    ErrorHandler.Succeeded(hr);
                }

                /* // moved to SrcMLArchive
                // Disable the startup background worker
                if (startupWorker != null)
                {
                    startupWorker.CancelAsync();
                }

                // Disable the file processing background worker
                if (_processFileInBackground != null)
                {
                    //_processFileInBackground.CancelAsync();
                }
                */
            }
            finally
            {
                /* // moved to SrcMLArchive
                writeLog("D:\\Data\\log.txt", "To raise a MonitoringStopped event.");
                OnSrcMLDOTNETEventsRaised(new SrcMLDOTNETEventArgs(null, SrcMLDOTNETEventType.MonitoringStopped));
                */
                /* //// Remove index part
                //shut down the current indexer
                if (_currentIndexer != null)
                {
                    //cleanup 
                    _currentIndexer.CommitChanges();
                    _indexUpdateManager.SaveFileStates();
                    //dispose
                    _currentIndexer.Dispose(killReaders);
                    _currentIndexer = null;
                }
                */
            }
        }

        /* //// Remove index part
        public string GetCurrentDirectory()
        {
            return _currentPath;
        }
        */

        /* //// Remove index part
        public SolutionKey GetSolutionKey()
        {
            return _solutionKey;
        }
        */

        /* //// Remove index part
        public bool PerformingInitialIndexing()
        {
            return !_initialIndexDone;
        }
        */

        /* //// Remove index part
        public void AddUpdateListener(IIndexUpdateListener listener)
        {
            _currentIndexer.AddIndexUpdateListener(listener);
        }
         */

        /* //// Remove index part
        public void RemoveUpdateListener(IIndexUpdateListener listener)
        {
            _currentIndexer.RemoveIndexUpdateListener(listener);
        }
        */


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
                                        FileEventType type)
        {
            int projItemIndex = 0;
            for (int changeProjIndex = 0; changeProjIndex < cProjects; changeProjIndex++)
            {
                int endProjectIndex = ((changeProjIndex + 1) == cProjects) ? rgpszMkDocuments.Length : rgFirstIndices[changeProjIndex + 1];
                for (; projItemIndex < endProjectIndex; projItemIndex++)
                {
                    if (rgpProjects[changeProjIndex] != null)
                    {
                        RaiseSolutionMonitorEvent(rgpszMkDocuments[projItemIndex], null, type);
                    }
                }
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Raise events of file creation/change/deletion in the solution in Visual Studio
        /// TODO: why need the condition check? (From original SrcML.NET)
        /// </summary>
        /// <param name="file"></param>
        /// <param name="type"></param>
        public void RaiseSolutionMonitorEvent(string filePath, string oldFilePath, FileEventType type)
        {
            //var directoryName = Path.GetDirectoryName(Path.GetFullPath(eventArgs.SourceFilePath));
            //var xmlFullPath = Path.GetFullPath(this.ArchivePath);

            //if (!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                FileEventRaisedArgs eventArgs = null;
                switch (type)
                {
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
                //writeLog("D:\\Data\\log.txt", "SolutionMonitor raises a " + type + " event for [" + filePath + "]");
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
                                                      VSADDFILEFLAGS[] rgFlags)
        {
            //writeLog("D:\\Data\\log.txt", "==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx()");
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
                                                               VSREMOVEFILEFLAGS[] rgFlags)
        {
            //writeLog("D:\\Data\\log.txt", "==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles()");
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
                                                               VSRENAMEFILEFLAGS[] rgFlags)
        {
            //writeLog("D:\\Data\\log.txt", "==> Triggered IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles()");
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
        int IVsTrackProjectDocumentsEvents2.OnAfterAddDirectoriesEx(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSADDDIRECTORYFLAGS[] rgFlags)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnAfterRemoveDirectories(int cProjects, int cDirectories, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, VSREMOVEDIRECTORYFLAGS[] rgFlags)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnAfterRenameDirectories(int cProjects, int cDirs, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgszMkOldNames, string[] rgszMkNewNames, VSRENAMEDIRECTORYFLAGS[] rgFlags)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnAfterSccStatusChanged(int cProjects, int cFiles, IVsProject[] rgpProjects, int[] rgFirstIndices, string[] rgpszMkDocuments, uint[] rgdwSccStatus)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryAddDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYADDDIRECTORYFLAGS[] rgFlags, VSQUERYADDDIRECTORYRESULTS[] pSummaryResult, VSQUERYADDDIRECTORYRESULTS[] rgResults)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryAddFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYADDFILEFLAGS[] rgFlags, VSQUERYADDFILERESULTS[] pSummaryResult, VSQUERYADDFILERESULTS[] rgResults)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveDirectories(IVsProject pProject, int cDirectories, string[] rgpszMkDocuments, VSQUERYREMOVEDIRECTORYFLAGS[] rgFlags, VSQUERYREMOVEDIRECTORYRESULTS[] pSummaryResult, VSQUERYREMOVEDIRECTORYRESULTS[] rgResults)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryRemoveFiles(IVsProject pProject, int cFiles, string[] rgpszMkDocuments, VSQUERYREMOVEFILEFLAGS[] rgFlags, VSQUERYREMOVEFILERESULTS[] pSummaryResult, VSQUERYREMOVEFILERESULTS[] rgResults)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryRenameDirectories(IVsProject pProject,  int cDirs, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEDIRECTORYFLAGS[] rgFlags, VSQUERYRENAMEDIRECTORYRESULTS[] pSummaryResult, VSQUERYRENAMEDIRECTORYRESULTS[] rgResults)
        {
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
        int IVsTrackProjectDocumentsEvents2.OnQueryRenameFiles(IVsProject pProject, int cFiles, string[] rgszMkOldNames, string[] rgszMkNewNames, VSQUERYRENAMEFILEFLAGS[] rgFlags, VSQUERYRENAMEFILERESULTS[] pSummaryResult, VSQUERYRENAMEFILERESULTS[] rgResults)
        {
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
        public int OnAfterSave(uint cookie)
        {
            //writeLog("D:\\Data\\log.txt", "==> Triggered IVsRunningDocTableEvents.OnAfterSave()");
            uint flags;
            uint readingLocks;
            uint edittingLocks;
            string name;
            IVsHierarchy hierarchy;
            uint documentId;
            IntPtr documentData;

            _documentTable.GetDocumentInfo(cookie, out flags, out readingLocks, out edittingLocks, out name, out hierarchy, out documentId, out documentData);
            RaiseSolutionMonitorEvent(name, null, FileEventType.FileChanged);
            /* ////
            var projectItem = _openSolution.FindProjectItem(name);
            if (projectItem != null)
            {
                _processFileInBackground.RunWorkerAsync(projectItem);
            }
            */
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
        public int OnAfterFirstDocumentLock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
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
        public int OnBeforeLastDocumentUnlock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after a change in an attribute of a document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="grfAttribs"></param>
        /// <returns></returns>
        public int OnAfterAttributeChange(uint cookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called before displaying a document window.
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="fFirstShow"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnBeforeDocumentWindowShow(uint cookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Called after a document window is placed in the Hide state.
        /// </summary>
        /// <param name="docCookie"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }
        #endregion

        #region IFileMonitor
        public event EventHandler<FileEventRaisedArgs> FileEventRaised;

        /// <summary>
        /// Start monitoring the solution.
        /// </summary>
        public void StartMonitoring() {
            //writeLog("D:\\Data\\log.txt", "======= SolutionMonitor: START MONITORING =======");
            // moved to SrcMLArchive
            //startupWorker = new BackgroundWorker();
            //startupWorker.WorkerSupportsCancellation = true;
            //startupWorker.DoWork += new DoWorkEventHandler(_runStartupInBackground_DoWork);
            //startupWorker.RunWorkerAsync();

            RegisterTrackProjectDocumentsEvents2();
            RegisterRunningDocumentTableEvents();
        }

        /// <summary>
        /// Stop monitoring the solution.
        /// </summary>
        public void StopMonitoring() {
            //writeLog("D:\\Data\\log.txt", "======= SolutionMonitor: STOP MONITORING =======");
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

        /*
        /// <summary>
        /// For debugging.
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="str"></param>
        private void writeLog(string logFile, string str)
        {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }
        */
    }
}
