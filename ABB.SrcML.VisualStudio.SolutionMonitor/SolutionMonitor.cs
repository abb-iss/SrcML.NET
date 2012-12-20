using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
////using Sando.Core;   "////" means removing the code about index
////using Sando.Core.Extensions;
////using Sando.Core.Extensions.Logging;
////using Sando.Indexer;
using Thread = System.Threading.Thread;

namespace ABB.SrcML.VisualStudio.SolutionMonitor
{
    /// <summary>
    /// This solution monitor implements two Visual Studio basic IDE interfaces:
    /// (1) IVsTrackProjectDocumentsEvents2: Notifies clients of changes made to project files or directories.
    /// Methods of this interface were implemented to handle file creation and deletion events in Visual Studio envinronment.
    /// (2) IVsRunningDocTableEvents: Implements methods that fire in response to changes to documents in the Running Document Table (RDT).
    /// Methods of this interface were implemented to handle file change events in Visual Studio envinronment.
    /// Also tried IVsFileChangeEvents (Notifies clients when selected files have been changed on disk), but it does not meet requirements.
    /// </summary>
    public class SolutionMonitor : IVsTrackProjectDocumentsEvents2, IVsRunningDocTableEvents
    {
        ////private const string StartupThreadName = "Sando: Initial Index of Project";
        private readonly SolutionWrapper _openSolution;
        ////private DocumentIndexer _currentIndexer;

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

        ////private readonly string _currentPath;
        private readonly System.ComponentModel.BackgroundWorker _processFileInBackground;
        private BackgroundWorker startupWorker;

        private readonly SolutionKey _solutionKey;  // Seems to be useless
        public volatile bool ShouldStop = false;    // Only one reference, seems to be useless

        /* //// Remove index part
        public bool PerformingInitialIndexing()
        {
            return !_initialIndexDone;
        }
        */

        ////private readonly IndexUpdateManager _indexUpdateManager;
        ////private bool _initialIndexDone = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="openSolution"></param>
        /// <param name="solutionKey"></param>
        ////public SolutionMonitor(SolutionWrapper openSolution, SolutionKey solutionKey, DocumentIndexer currentIndexer, bool isIndexRecreationRequired)
        public SolutionMonitor(SolutionWrapper openSolution, SolutionKey solutionKey)
        {
            _openSolution = openSolution;
            ////_currentIndexer = currentIndexer;
            ////_currentPath = solutionKey.GetIndexPath();
            _solutionKey = solutionKey;
            ////_indexUpdateManager = new IndexUpdateManager(solutionKey, _currentIndexer, isIndexRecreationRequired);

            _processFileInBackground = new System.ComponentModel.BackgroundWorker();
            _processFileInBackground.DoWork += new DoWorkEventHandler(_processFileInBackground_DoWork);
        }

        /// <summary>
        /// Start monitoring the solution.
        /// </summary>
        public void StartMonitoring()
        {
            startupWorker = new BackgroundWorker();
            startupWorker.WorkerSupportsCancellation = true;
            startupWorker.DoWork += new DoWorkEventHandler(_runStartupInBackground_DoWork);
            startupWorker.RunWorkerAsync();

            // Register TrackProjectDocuments2 events
            RegisterTrackProjectDocumentsEvents2();

            // Register RunningDocumentTable events
            RegisterRunningDocumentTableEvents();
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
                ErrorHandler.ThrowOnFailure(hr); // do nothing if this fails
                writeLog("D:\\Data\\log.txt", "IVsTrackProjectDocumentsEvents2.AdviseTrackProjectDocumentsEvents() DONE. [" + hr.ToString() + "]");
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
                writeLog("D:\\Data\\log.txt", "IVsRunningDocTableEvents.AdviseRunningDocTableEvents() DONE. [" + hr.ToString() + "]");
            }
        }

        /// <summary>
        /// Run in background for a specific file.
        /// So far only called in IVsRunningDocTableEvents.OnAfterSave()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _processFileInBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            // Check if the corresponding srcML file need to be ADDED or CHANGED
            // TODO: Should be ProcessSingleFile(projectItem, null), instead of ProcessItem(projectItem, null)?
            ProjectItem projectItem = e.Argument as ProjectItem;    // ProjectItem: Represents an item in a project
            ProcessItem(projectItem, null);
            ////UpdateAfterAdditions();

            // TODO: In some way (e.g., checking Running Document Table as the list of monitored source files)
            //       to see if srcML files need to be DELETED
        }

        /// <summary>
        /// Run in background when starting up the solution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="anEvent"></param>
        private void _runStartupInBackground_DoWork(object sender, DoWorkEventArgs anEvent)
        {
            var worker = sender as BackgroundWorker;
            try
            {
                // Recursively walk through the solution/projects to check if srcML files need to be ADDED or CHANGED
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

                // TODO: In some way (e.g., checking Running Document Table as the list of monitored source files)
                //       to see if srcML files need to be DELETED

                /* //// Remove index part
                // Code changed by JZ on 10/30: To complete the Delete case: walk through all index files
                List<string> allIndexedFileNames = _indexUpdateManager.GetAllIndexedFileNames();
                foreach (string filename in allIndexedFileNames)
                {
                    if (!File.Exists(filename))
                    {
                        Console.WriteLine("Delete index for: " + filename);
                        _indexUpdateManager.UpdateFile(filename);
                    }
                }
                // End of code changes
                */
            }
            catch (Exception e)
            {
                ////FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Problem getting projects to process."));
                Console.WriteLine("Problem getting projects to process." + e.Message);
            }
            finally
            {
                ////_initialIndexDone = true;
                ShouldStop = false;

            }
        }

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
        /// Process a single source file
        /// (ADDED / CHANGED)
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
                    string fileExtension = Path.GetExtension(path);
                    if (fileExtension != null && !fileExtension.Equals(String.Empty))
                    {
                        // TODO: about the file extension issue
                        ////if (ExtensionPointsRepository.Instance.GetParserImplementation(fileExtension) != null)
                        {
                            Debug.WriteLine("Start: " + path);

                            ////ProcessFileForTesting(path);

                            // TODO: insert code to link to srcML generation for a single source file (ADDED / CHANGED)
                            //       similar to RespondToFileChangedEvent but no event needed
                            //       return XElement of the latest srcML file? or just void (with file generated on disk)?
                            //       Sando keeps its own index states? Sando/SrcML.NET's ADDED / CHANGED processing are both exactly the same
                            //       Also consider "DONOTHING" case, to save time
                            //       The MonitoredSourceFileList: via srcML?

                            //path = Path.GetFullPath(path);

                            Debug.WriteLine("End: " + path);
                        }
                    }
                }
            }
            ////TODO - don't catch a generic exception
            catch (Exception e)
            {
                ////FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Problem parsing file: " + path));
                Console.WriteLine("Problem parsing file: " + path + "; " + e.Message);
            }
        }

        /* //// Remove index part
        public void ProcessFileForTesting(string path)
        {
            _indexUpdateManager.UpdateFile(path);
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
                        writeLog("D:\\Data\\log.txt", "IVsRunningDocumentTable.SaveDocuments() DONE. [" + moniker + "]");
                    }
                }
            }
        }

        /// <summary>
        /// Stop monitoring the solution.
        /// TODO: what is killReaders?
        /// </summary>
        /// <param name="killReaders"></param>
        public void StopMonitoring(bool killReaders = false)
        {
            Dispose(killReaders);
        }

        /// <summary>
        /// Dispose this solution monitor.
        /// </summary>
        /// <param name="killReaders"></param>
        public void Dispose(bool killReaders = false)
        {
            try
            {
                // Unregister IVsTrackProjectDocumentsEvents2 events
                if (_pdwCookie != VSConstants.VSCOOKIE_NIL && _projectDocumentsTracker != null)
                {
                    int hr = _projectDocumentsTracker.UnadviseTrackProjectDocumentsEvents(_pdwCookie);
                    ErrorHandler.Succeeded(hr); // do nothing if this fails
                    _pdwCookie = VSConstants.VSCOOKIE_NIL;
                    writeLog("D:\\Data\\log.txt", "IVsTrackProjectDocumentsEvents2.UnadviseTrackProjectDocumentsEvents() DONE. [" + hr.ToString() + "]");
                }

                // Unregister IVsRunningDocTableEvents events
                if (_documentTable != null)
                {
                    int hr = _documentTable.UnadviseRunningDocTableEvents(_documentTableItemId);
                    ErrorHandler.Succeeded(hr);
                    writeLog("D:\\Data\\log.txt", "IVsRunningDocTableEvents.UnadviseRunningDocTableEvents() DONE. [" + hr.ToString() + "]");
                }
                if (startupWorker != null)
                {
                    startupWorker.CancelAsync();
                }
            }
            finally
            {
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
            writeLog("D:\\Data\\log.txt", "==> IVsTrackProjectDocumentsEvents2.OnAfterAddFilesEx()");
            string logStr = "Added Files: \n";
            for (int i = 0; i < cFiles; i++)
            {
                //FileChanged(filesChanged[i]);
                logStr += "File: [" + rgpszMkDocuments[i] + "]; Flag: [" + rgFlags[i].ToString() + "]\n";
            }
            writeLog("D:\\Data\\log.txt", logStr);
            return VSConstants.S_OK;
            //return OnNotifyTestFileAddRemove(cProjects, rgpProjects, rgpszMkDocuments, rgFirstIndices, TestFileChangedReason.Added);
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
            writeLog("D:\\Data\\log.txt", "==> IVsTrackProjectDocumentsEvents2.OnAfterRemoveFiles()");
            string logStr = "Removed Files: \n";
            for (int i = 0; i < cFiles; i++)
            {
                //FileChanged(filesChanged[i]);
                logStr += "File: [" + rgpszMkDocuments[i] + "]; Flag: [" + rgFlags[i].ToString() + "]";
            }
            writeLog("D:\\Data\\log.txt", logStr);
            return VSConstants.S_OK;
            //return OnNotifyTestFileAddRemove(cProjects, rgpProjects, rgpszMkDocuments, rgFirstIndices, TestFileChangedReason.Removed);
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
            writeLog("D:\\Data\\log.txt", "==> IVsTrackProjectDocumentsEvents2.OnAfterRenameFiles()");
            string logStr = "Renamed Files: \n";
            for (int i = 0; i < cFiles; i++)
            {
                //FileChanged(filesChanged[i]);
                logStr += "File: [" + rgszMkOldNames[i] + "-> " + rgszMkNewNames[i] + "]; Flag: [" + rgFlags[i].ToString() + "]";
            }
            writeLog("D:\\Data\\log.txt", logStr);
            return VSConstants.S_OK;
            //OnNotifyTestFileAddRemove(cProjects, rgpProjects, rgszMkOldNames, rgFirstIndices, TestFileChangedReason.Removed);
            //return OnNotifyTestFileAddRemove(cProjects, rgpProjects, rgszMkNewNames, rgFirstIndices, TestFileChangedReason.Added);
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
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public int OnAfterSave(uint cookie)
        {
            writeLog("D:\\Data\\log.txt", "==> IVsRunningDocTableEvents.OnAfterSave()");
            uint flags;
            uint readingLocks;
            uint edittingLocks;
            string name;
            IVsHierarchy hierarchy;
            uint documentId;
            IntPtr documentData;

            _documentTable.GetDocumentInfo(cookie, out flags, out readingLocks, out edittingLocks, out name, out hierarchy, out documentId, out documentData);

            var projectItem = _openSolution.FindProjectItem(name);
            if (projectItem != null)
            {
                _processFileInBackground.RunWorkerAsync(projectItem);
            }
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

        /* //// Remove index part
        public string GetCurrentDirectory()
        {
            return _currentPath;
        }
        */

        /// <summary>
        /// Get the solution key.
        /// </summary>
        /// <returns></returns>
        public SolutionKey GetSolutionKey()
        {
            return _solutionKey;
        }

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
    }
}
