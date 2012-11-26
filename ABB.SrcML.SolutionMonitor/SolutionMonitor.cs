using System;
using System.Collections;
// Code changed by JZ on 10/29: Added the Delete case
using System.Collections.Generic;
// End of code changes
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
////using Sando.Core;
////using Sando.Core.Extensions;
////using Sando.Core.Extensions.Logging;
////using Sando.Indexer;
using Thread = System.Threading.Thread;

namespace ABB.SrcML.SolutionMonitor
{
    public class SolutionMonitor : IVsRunningDocTableEvents
    {
        ////private const string StartupThreadName = "Sando: Initial Index of Project";
        private readonly SolutionWrapper _openSolution;
        ////private DocumentIndexer _currentIndexer;
        private IVsRunningDocumentTable _documentTable;
        private uint _documentTableItemId;

        ////private readonly string _currentPath;
        private readonly System.ComponentModel.BackgroundWorker _processFileInBackground;
        private readonly SolutionKey _solutionKey;
        public volatile bool ShouldStop = false;

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

        private void _processFileInBackground_DoWork(object sender, DoWorkEventArgs e)
        {
            ProjectItem projectItem = e.Argument as ProjectItem;
            ProcessItem(projectItem, null);
            ////UpdateAfterAdditions();
        }

        private void _runStartupInBackground_DoWork(object sender, DoWorkEventArgs anEvent)
        {
            var worker = sender as BackgroundWorker;
            try
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

                // Code changed by JZ on 10/30: To complete the Delete case: walk through all index files
                /* //// Remove index part
                List<string> allIndexedFileNames = _indexUpdateManager.GetAllIndexedFileNames();
                foreach (string filename in allIndexedFileNames)
                {
                    if (!File.Exists(filename))
                    {
                        Console.WriteLine("Delete index for: " + filename);
                        _indexUpdateManager.UpdateFile(filename);
                    }
                }
                */
                // End of code changes
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

        private BackgroundWorker startupWorker;

        public void StartMonitoring()
        {
            startupWorker = new BackgroundWorker();
            startupWorker.WorkerSupportsCancellation = true;
            startupWorker.DoWork +=
                    new DoWorkEventHandler(_runStartupInBackground_DoWork);
            startupWorker.RunWorkerAsync();

            // Register events for doc table
            _documentTable = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            _documentTable.AdviseRunningDocTableEvents(this, out _documentTableItemId);
        }

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
        
        private void ProcessItem(ProjectItem item, BackgroundWorker worker)
        {
            ProcessSingleFile(item, worker);
            ProcessChildren(item, worker);
        }

        private void ProcessChildren(ProjectItem item, BackgroundWorker worker)
        {
            try
            {
                if (item != null && item.ProjectItems != null)
                    ProcessItems(item.ProjectItems.GetEnumerator(), worker);
            }
            catch (COMException dll)
            {
                //ignore, can't parse these types of files
            }
        }

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
                        //////if (ExtensionPointsRepository.Instance.GetParserImplementation(fileExtension) != null)
                        {
                            Debug.WriteLine("Start: " + path);

                            ////ProcessFileForTesting(path);

                            Debug.WriteLine("End: " + path);
                        }
                    }
                }
            }
            //TODO - don't catch a generic exception
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

        public void StopMonitoring(bool killReaders = false)
        {
            Dispose(killReaders);
        }

        public void Dispose(bool killReaders = false)
        {
            try
            {
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

        //// ??
        public int OnAfterFirstDocumentLock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
            //throw new NotImplementedException();
            return VSConstants.S_OK;
        }

        //// ??
        public int OnBeforeLastDocumentUnlock(uint cookie, uint lockType, uint readLocksLeft, uint editLocksLeft)
        {
            //throw new NotImplementedException();
            return VSConstants.S_OK;
        }

        //// ??
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

        //// ??
        public int OnAfterAttributeChange(uint cookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        //// ??
        public int OnBeforeDocumentWindowShow(uint cookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        //// ??
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
