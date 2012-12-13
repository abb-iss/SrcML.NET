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
    /// IVsRunningDocTableEvents: Implements methods that fire in response to changes to documents in the Running Document Table (RDT).
    /// TODO: IVsRunningDocTableEvents does not have OnAfterDelete(), only has OnAfterSave() that can handle ADDED/CHANGED cases
    /// </summary>
    public class SolutionMonitor : IVsRunningDocTableEvents, IVsFileChangeEvents
    {
        ////private const string StartupThreadName = "Sando: Initial Index of Project";
        private readonly SolutionWrapper _openSolution;
        ////private DocumentIndexer _currentIndexer;
        private IVsRunningDocumentTable _documentTable; // IVsRunningDocumentTable: Manages the set of currently open documents in the environment
        private uint _documentTableItemId;              // Used in registering/unregistering events for the Running Document Table

        // Try IVsFileChangeEvents
        private IVsFileChangeEx fileChangeEx;
        private readonly List<KeyValuePair<string, uint>> listenInfos = new List<KeyValuePair<string, uint>>();

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
        /// Run in background for a specific file
        /// So far only called in OnAfterSave()
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
        /// Run in background when starting up the solution
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
        /// Start monitoring the solution.
        /// </summary>
        public void StartMonitoring()
        {
            // Try FileChangeEvents
            RegisterFileChangeEvents();

            startupWorker = new BackgroundWorker();
            startupWorker.WorkerSupportsCancellation = true;
            startupWorker.DoWork += new DoWorkEventHandler(_runStartupInBackground_DoWork);
            startupWorker.RunWorkerAsync();

            // Register events for the Running Document Table
            _documentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            _documentTable.AdviseRunningDocTableEvents(this, out _documentTableItemId);
        }

        /// <summary>
        /// Register file change events
        /// </summary>
        public void RegisterFileChangeEvents()
        {
            //if (listenInfos.Any(i => file.Equals(i.Key, StringComparison.InvariantCultureIgnoreCase)))
            //    return;

            fileChangeEx = Package.GetGlobalService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
            uint dircookie;
            string solutionFolder = Path.GetDirectoryName(_solutionKey.GetSolutionPath());
            fileChangeEx.AdviseDirChange(solutionFolder, 1, this, out dircookie);

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
                            ProcessItems(project.ProjectItems.GetEnumerator(), null);
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("Problem parsing files:" + e.Message);
                        }
                        finally
                        {
                            ////UpdateAfterAdditions();
                        }
                    }
                }
            }

            // Print out all monitored files
            PrintAllMonitoredFiles("D:\\Data\\MonitoredFiles1.txt");
        }



        /// <summary>
        /// Recursively process project items
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
        /// Process a single project item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessItem(ProjectItem item, BackgroundWorker worker)
        {
            ProcessSingleFile(item, worker);
            ProcessChildren(item, worker);
        }

        /// <summary>
        /// Process the children items of a project item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="worker"></param>
        private void ProcessChildren(ProjectItem item, BackgroundWorker worker)
        {
            try
            {
                if (item != null && item.ProjectItems != null)
                    ProcessItems(item.ProjectItems.GetEnumerator(), worker);
            }
            catch (COMException dll)
            {
                ////ignore, can't parse these types of files
            }
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
                            //       The MonitoredSourceFileList: via Running Document Table or srcML?

                            //path = Path.GetFullPath(path);

                            StartMonitoringAFile(path);

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
        /// Start to monitor a file and add the file into the list of monitored files
        /// </summary>
        /// <param name="path"></param>
        public void StartMonitoringAFile(string path)
        {
            uint cookie;
            fileChangeEx.AdviseFileChange(
                    path,
                    (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Add),
                    this,
                    out cookie);
            listenInfos.Add(new KeyValuePair<string, uint>(path, cookie));
        }

        /// <summary>
        /// For debugging. May be adapted to GetListOfAllMonitoredFiles()
        /// </summary>
        /// <param name="outputFile"></param>
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


        /// <summary>
        /// Get a list of all files being monitored by SrcML.NET (RDT version)
        /// </summary>
        /// <returns></returns>
        public List<string> GetListOfAllMonitoredFiles()
        {
            StreamWriter sw = new StreamWriter("D:\\Data\\SolutionFiles.txt", false, System.Text.Encoding.ASCII);
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

                    Console.Out.WriteLine("[ " + moniker + "]");
                    list.Add(moniker);
                    sw.WriteLine(moniker);
                }
            }

            //for (int i = 0; i < list.Count; i++)
            //{
            //}
            sw.Close();

            return list;
        }

        /// <summary>
        /// Stop monitoring the solution
        /// TODO: killReaders?
        /// </summary>
        /// <param name="killReaders"></param>
        public void StopMonitoring(bool killReaders = false)
        {
            Dispose(killReaders);
        }

        /// <summary>
        /// Dispose this solution monitor
        /// </summary>
        /// <param name="killReaders"></param>
        public void Dispose(bool killReaders = false)
        {
            try
            {
                // Try FileChangeEvents
                if (fileChangeEx != null && listenInfos.Any())
                {
                    var cookies = listenInfos.Select(i => i.Value).ToArray();
                    listenInfos.Clear();
                    foreach (var cookie in cookies)
                    {
                        fileChangeEx.UnadviseFileChange(cookie);
                    }
                }
                fileChangeEx = null;


                if (_documentTable != null)
                {
                    _documentTable.UnadviseRunningDocTableEvents(_documentTableItemId);
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

        #region IVsFileChangeEvents
        /// <summary>
        /// From IVsFileChangeEvents.
        /// Notifies clients of changes made to one or more files.
        /// </summary>
        /// <param name="numberOfFilesChanged"></param>
        /// <param name="filesChanged"></param>
        /// <param name="rggrfChange"></param>
        /// <returns></returns>
        public int FilesChanged(uint numberOfFilesChanged, string[] filesChanged, uint[] rggrfChange)
        {
            StreamWriter sw = new StreamWriter("D:\\Data\\ChangedFiles.txt", true, System.Text.Encoding.ASCII);
            for (int i = 0; i < numberOfFilesChanged; i++)
            {
                //FileChanged(filesChanged[i]);
                sw.WriteLine("File: [" + filesChanged[i] + "]; Flag: [" + rggrfChange[i] + "]");
            }
            sw.Close();

            return VSConstants.S_OK;
        }

        /// <summary>
        /// From IVsFileChangeEvents.
        /// Notifies clients of changes made to a directory.
        /// </summary>
        /// <param name="pszDirectory"></param>
        /// <returns></returns>
        public int DirectoryChanged(string pszDirectory)
        {
            StreamWriter sw = new StreamWriter("D:\\Data\\ChangedFiles.txt", true, System.Text.Encoding.ASCII);
            sw.WriteLine("Folder: [" + pszDirectory + "]");
            sw.Close();

            return VSConstants.S_OK;
        }
        #endregion

        /// <summary>
        /// From IVsRunningDocTableEvents. 
        /// Called after saving a document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public int OnAfterSave(uint cookie)
        {
            uint readingLocks, edittingLocks, flags; IVsHierarchy hierarchy; IntPtr documentData; string name; uint documentId;
            _documentTable.GetDocumentInfo(cookie, out flags, out readingLocks, out edittingLocks, out name, out hierarchy, out documentId, out documentData);
            var projectItem = _openSolution.FindProjectItem(name);
            if (projectItem != null)
            {
                _processFileInBackground.RunWorkerAsync(projectItem);
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// From IVsRunningDocTableEvents. 
        /// Called after application of the first lock of the specified type to the specified document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="lockType"></param>
        /// <param name="readLocksLeft"></param>
        /// <param name="editLocksLeft"></param>
        /// <returns></returns>
        public int OnAfterFirstDocumentLock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
            //throw new NotImplementedException();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// From IVsRunningDocTableEvents. 
        /// Called before releasing the last lock of the specified type on the specified document in the Running Document Table (RDT).
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="lockType"></param>
        /// <param name="readLocksLeft"></param>
        /// <param name="editLocksLeft"></param>
        /// <returns></returns>
        public int OnBeforeLastDocumentUnlock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
            //throw new NotImplementedException();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// From IVsRunningDocTableEvents. 
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
        /// From IVsRunningDocTableEvents. 
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
        /// From IVsRunningDocTableEvents. 
        /// Called after a document window is placed in the Hide state.
        /// </summary>
        /// <param name="docCookie"></param>
        /// <param name="pFrame"></param>
        /// <returns></returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        /* //// Remove index part
        public string GetCurrentDirectory()
        {
            return _currentPath;
        }
        */

        /// <summary>
        /// Get the solution key
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

    }
}
