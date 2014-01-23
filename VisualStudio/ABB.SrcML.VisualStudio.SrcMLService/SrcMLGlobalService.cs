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

using ABB.SrcML.Data;
using ABB.SrcML.Utilities;
using ABB.VisualStudio.Interfaces;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Timers;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio.SrcMLService {

    /// <summary>
    /// Step 4: Implement the global service class. This is the class that implements the global
    /// service. All it needs to do is to implement the interfaces exposed by this service (in this
    /// case ISrcMLGlobalService). This class also needs to implement the SSrcMLGlobalService
    /// interface in order to notify the package that it is actually implementing this service.
    /// </summary>
    public class SrcMLGlobalService : ISrcMLGlobalService, SSrcMLGlobalService {

        /// <summary>
        /// The folder name of storing srcML archives.
        /// </summary>
        private const string srcMLArchivesFolderString = "\\SrcMLArchives";

        private const string ResetFileName = "RESETSOLUTION";

        private uint amountCompleted = 0;

        private uint cookie = 0;

        private IDataRepository CurrentDataRepository;

        /// <summary>
        /// SrcML.NET's Solution Monitor.
        /// </summary>
        private SourceMonitor CurrentMonitor;

        /// <summary>
        /// SrcML.NET's SrcMLArchive.
        /// </summary>
        private ISrcMLArchive CurrentSrcMLArchive;

        private bool duringStartup;

        private int frozen;
        private ReadyNotifier ReadyState;
        private ReentrantTimer SaveTimer;
        

        /// <summary>
        /// Store in this variable the service provider that will be used to query for other
        /// services.
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// The path of SrcML.NET Service VS extension.
        /// </summary>
        private string SrcMLServiceDirectory;

        private ITaskManagerService _taskManager;

        /// <summary>
        /// Status bar service.
        /// </summary>
        private IVsStatusbar statusBar;

        public const int DEFAULT_SAVE_INTERVAL = 300;
        public const string GENERATE_DATA_INDICATOR_FILENAME = "GENDATA";

        public bool DataEnabled { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="extensionDirectory"></param>
        public SrcMLGlobalService(IServiceProvider sp, string extensionDirectory) {
            SrcMLFileLogger.DefaultLogger.InfoFormat("Constructing a new instance of SrcMLGlobalService in {0}", extensionDirectory);
            numRunningSources = 0;
            ReadyState = new ReadyNotifier(this);
            serviceProvider = sp;
            SrcMLServiceDirectory = extensionDirectory;
            DataEnabled = ShouldGenerateData(extensionDirectory);
            statusBar = (IVsStatusbar) Package.GetGlobalService(typeof(SVsStatusbar));
            _taskManager = (ITaskManagerService) Package.GetGlobalService(typeof(STaskManagerService));
            _taskManager.SchedulerIdled += _taskManager_SchedulerIdled;
            SaveTimer = new ReentrantTimer(() => CurrentMonitor.Save(), new TaskManager(this, _taskManager.GlobalScheduler));
            SaveInterval = DEFAULT_SAVE_INTERVAL;
        }

        private static bool ShouldGenerateData(string extensionDirectory) {
            if(null != extensionDirectory) {
                var fileName = Path.Combine(extensionDirectory, GENERATE_DATA_INDICATOR_FILENAME);
                return File.Exists(fileName);
            }
            return false;
        }

        private int numRunningSources { get; set; }

        private int NumRunningSources {
            get { return this.numRunningSources; }
            set {
                this.numRunningSources = value;
                IsReady = (numRunningSources == 0);
            }
        }

        //private bool duringStartup;
        // Implement the methods of ISrcMLLocalService here.

        #region ISrcMLGlobalService Members

        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryAdded;
        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryRemoved;

        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged {
            add { this.ReadyState.IsReadyChanged += value; }
            remove { this.ReadyState.IsReadyChanged -= value; }
        }

        public event EventHandler<EventArgs> MonitoringStopped;

        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;

        public bool IsReady {
            get { return this.ReadyState.IsReady; }
            private set { this.ReadyState.IsReady = value; }
        }

        public ReadOnlyCollection<string> MonitoredDirectories { get { return (CurrentMonitor == null ? null : this.CurrentMonitor.MonitoredDirectories); } }

        public double SaveInterval {
            get { return SaveTimer.Interval / 1000; }
            set { SaveTimer.Interval = value * 1000; }
        }

        public double ScanInterval {
            get { return (CurrentMonitor == null ? Double.NaN : CurrentMonitor.ScanInterval); }
            set {
                if(CurrentMonitor == null)
                    throw new InvalidOperationException("There is no monitor to update");
                if(value < 0.0)
                    throw new ArgumentOutOfRangeException("value", value, "ScanInterval must be greater than 0");
                CurrentMonitor.ScanInterval = value;
            }
        }

        public void AddDirectoryToMonitor(string pathToDirectory) {
            if(null == CurrentMonitor) {
                throw new InvalidOperationException("Only valid once a solution has been opened.");
            }
            if(null == pathToDirectory) {
                throw new ArgumentNullException("pathToDirectory");
            }
            CurrentMonitor.AddDirectory(pathToDirectory);
        }

        public IDataRepository GetDataRepository() {
            return CurrentDataRepository;
        }

        /// <summary>
        /// Get current SrcMLArchive instance.
        /// </summary>
        /// <returns></returns>
        public ISrcMLArchive GetSrcMLArchive() {
            return CurrentSrcMLArchive;
        }

        /// <summary>
        /// Gets the XElement for the specified source file.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public XElement GetXElementForSourceFile(string sourceFilePath) {
            return(CurrentSrcMLArchive == null ? null : CurrentSrcMLArchive.GetXElementForSourceFile(sourceFilePath));
        }

        public void RemoveDirectoryFromMonitor(string pathToDirectory) {
            if(null == CurrentMonitor) {
                throw new InvalidOperationException("Only valid once a solution has been opened.");
            }
            if(null == pathToDirectory) {
                throw new ArgumentNullException("pathToDirectory");
            }

            CurrentMonitor.RemoveDirectory(pathToDirectory);
        }

        public void Reset() {
            var openSolution = GetOpenSolution();
            if(null != openSolution) {
                var baseDirectory = GetSrcMLArchiveFolder(openSolution);
                File.Create(Path.Combine(baseDirectory, ResetFileName)).Close();
            }
        }

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        /// <param name="srcMLArchiveDirectory"></param>
        /// <param name="shouldReset"></param>
        public void StartMonitoring(bool shouldReset, string srcMLBinaryDirectory) {
            // Get the path of the folder that storing the srcML archives
            var openSolution = GetOpenSolution();
            string baseDirectory = GetSrcMLArchiveFolder(openSolution);

            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitoring( " + baseDirectory + " )");
            try {

                if(shouldReset) {
                    SrcMLFileLogger.DefaultLogger.Info("Reset flag is set - Removing " + baseDirectory);
                    Directory.Delete(baseDirectory, true);
                }

                // Create a new instance of SrcML.NET's LastModifiedArchive
                LastModifiedArchive lastModifiedArchive = new LastModifiedArchive(baseDirectory, LastModifiedArchive.DEFAULT_FILENAME,
                                                                                  _taskManager.GlobalScheduler);

                // Create a new instance of SrcML.NET's SrcMLArchive
                SrcMLArchive sourceArchive = new SrcMLArchive(baseDirectory, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY, true,
                                                              new SrcMLGenerator(srcMLBinaryDirectory),
                                                              new ShortXmlFileNameMapping(Path.Combine(baseDirectory, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY)),
                                                              _taskManager.GlobalScheduler);
                CurrentSrcMLArchive = sourceArchive;
                if(DataEnabled) {
                    CurrentDataRepository = new DataRepository(CurrentSrcMLArchive);

                    if(CurrentSrcMLArchive.IsEmpty) {
                        CurrentSrcMLArchive.IsReadyChanged += InitDataWhenReady;
                    } else {
                        InitDataWhenReady(this, new IsReadyChangedEventArgs(true));
                    }
                }

                // Create a new instance of SrcML.NET's solution monitor
                if(openSolution != null) {
                    CurrentMonitor = new SourceMonitor(openSolution, DirectoryScanningMonitor.DEFAULT_SCAN_INTERVAL,
                                                       _taskManager.GlobalScheduler, baseDirectory, lastModifiedArchive, sourceArchive);
                    CurrentMonitor.DirectoryAdded += RespondToDirectoryAddedEvent;
                    CurrentMonitor.DirectoryRemoved += RespondToDirectoryRemovedEvent;
                    CurrentMonitor.AddDirectoriesFromSaveFile();
                    CurrentMonitor.AddSolutionDirectory();
                }

                // Subscribe events from Solution Monitor
                if(CurrentMonitor != null) {
                    CurrentMonitor.FileChanged += RespondToFileChangedEvent;
                    CurrentMonitor.MonitoringStopped += RespondToMonitoringStoppedEvent;

                    if(DataEnabled) {
                        CurrentDataRepository.FileProcessed += RespondToFileChangedEvent;
                        CurrentDataRepository.IsReadyChanged += RespondToIsReadyChangedEvent;
                    }
                    // Initialize the progress bar.
                    if(statusBar != null) {
                        statusBar.Progress(ref cookie, 1, "", 0, 0);
                    }

                    // Start monitoring
                    duringStartup = true;
                    CurrentMonitor.UpdateArchives();
                    CurrentMonitor.StartMonitoring();
                    SaveTimer.Start();
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitoring()"));
            }
        }

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        public void StartMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitoring() - default");
            StartMonitoring(ShouldReset(), SrcMLHelper.GetSrcMLDefaultDirectory(SrcMLServiceDirectory));
        }

        /// <summary>
        /// SrcML service stops monitoring the opened solution.
        /// </summary>
        public void StopMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StopMonitoring()");
            try {
                if(CurrentMonitor != null && CurrentSrcMLArchive != null) {
                    SaveTimer.Stop();
                    CurrentMonitor.StopMonitoring();
                    CurrentMonitor.FileChanged -= RespondToFileChangedEvent;
                    CurrentMonitor.DirectoryAdded -= RespondToDirectoryAddedEvent;
                    CurrentMonitor.DirectoryRemoved -= RespondToDirectoryRemovedEvent;
                    CurrentMonitor.MonitoringStopped -= RespondToMonitoringStoppedEvent;
                    CurrentMonitor.Dispose();
                    
                    if(DataEnabled) {
                        CurrentDataRepository.FileProcessed -= RespondToFileChangedEvent;
                        CurrentDataRepository.IsReadyChanged -= RespondToIsReadyChangedEvent;
                        CurrentDataRepository.Dispose();
                    }

                    CurrentSrcMLArchive = null;
                    CurrentDataRepository = null;
                    CurrentMonitor = null;
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StopMonitoring()"));
            }
        }

        private void InitDataWhenReady(object sender, IsReadyChangedEventArgs e) {
            if(e.ReadyState) {
                CurrentSrcMLArchive.IsReadyChanged -= InitDataWhenReady;
                CurrentDataRepository.InitializeDataConcurrent(_taskManager.GlobalScheduler);
            }
        }

        #endregion ISrcMLGlobalService Members

        /// <summary>
        /// Generate the folder path for storing srcML files. (For all the following four methods.)
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        public string GetSrcMLArchiveFolder(Solution openSolution) {
            return CreateNamedFolder(openSolution, srcMLArchivesFolderString);
        }

        protected virtual void OnDirectoryAdded(DirectoryScanningMonitorEventArgs e) {
            EventHandler<DirectoryScanningMonitorEventArgs> handler = DirectoryAdded;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnDirectoryRemoved(DirectoryScanningMonitorEventArgs e) {
            EventHandler<DirectoryScanningMonitorEventArgs> handler = DirectoryRemoved;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = SourceFileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler<EventArgs> handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Get the open solution.
        /// </summary>
        /// <returns></returns>
        private static Solution GetOpenSolution() {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if(dte != null) {
                var openSolution = dte.Solution;
                return openSolution;
            } else {
                return null;
            }
        }

        private string CreateFolder(string folderName, string parentDirectory) {
            if(!File.Exists(parentDirectory + folderName)) {
                var directoryInfo = Directory.CreateDirectory(parentDirectory + folderName);
                return directoryInfo.FullName;
            } else {
                return parentDirectory + folderName;
            }
        }

        private string CreateNamedFolder(Solution openSolution, string str) {
            var srcMLFolder = CreateFolder(str, SrcMLServiceDirectory);
            CreateFolder(GetName(openSolution), srcMLFolder + "\\");
            return srcMLFolder + "\\" + GetName(openSolution);
        }

        /// <summary>
        /// Display text on the Visual Studio status bar.
        /// </summary>
        /// <param name="text"></param>
        private void DisplayTextOnStatusBar(string text) {
            if(statusBar != null) {
                statusBar.IsFrozen(out frozen);
                if(frozen == 0) {
                    // Set the status bar text and make its display static.
                    statusBar.SetText(text);
                    statusBar.FreezeOutput(1);
                    // Clear the status bar text.
                    statusBar.FreezeOutput(0);
                    statusBar.Clear();
                }
            }
        }

        private string GetName(Solution openSolution) {
            var fullName = openSolution.FullName;
            var split = fullName.Split('\\');
            return split[split.Length - 1].Substring(0, split[split.Length - 1].Length - 4) + fullName.GetHashCode();
        }

        /// <summary>
        /// Respond to the FileChanged event from Solution Monitor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToFileChangedEvent(object sender, FileEventRaisedArgs eventArgs) {
            //SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToFileChangedEvent(), File = " + eventArgs.FilePath + ", EventType = " + eventArgs.EventType + ", HasSrcML = " + eventArgs.HasSrcML);
            // Show progress on the status bar.
            //if(duringStartup) {
            //amountCompleted++;
            //ShowProgressOnStatusBar("SrcML Service is processing " + eventArgs.FilePath);
            //}
            OnFileChanged(eventArgs);
            var senderIsDataRepo = (sender == CurrentDataRepository);
            DisplayTextOnStatusBar(String.Format("SrcML Service is {0} {1}", (senderIsDataRepo ? "generating data for" : "processing"), eventArgs.FilePath));
        }

        private void RespondToDirectoryAddedEvent(object sender, DirectoryScanningMonitorEventArgs eventArgs) {
            OnDirectoryAdded(eventArgs);
            DisplayTextOnStatusBar(String.Format("Now monitoring {0}", eventArgs.Directory));
        }

        private void RespondToDirectoryRemovedEvent(object sender, DirectoryScanningMonitorEventArgs eventArgs) {
            OnDirectoryRemoved(eventArgs);
            DisplayTextOnStatusBar(String.Format("No longer monitoring {0}", eventArgs.Directory));
        }

        void _taskManager_SchedulerIdled(object sender, EventArgs e) {
            DisplayTextOnStatusBar("Finished indexing");
        }

        /// <summary>
        /// Respond to the IsReady
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToIsReadyChangedEvent(object sender, IsReadyChangedEventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToStartupCompletedEvent()");
            if(eventArgs.ReadyState) {
                NumRunningSources--;

                // Clear the progress bar.

                amountCompleted = 0;
                if(statusBar != null) {
                    statusBar.Progress(ref cookie, 0, "", 0, 0);
                }
                string message = "SrcML Service has no idea where this message came from";
                if(sender == CurrentMonitor) {
                    message = "SrcML Service has finished parsing files";
                } else if(sender == CurrentDataRepository) {
                    message = "SrcML Service has finished generating analysis data";
                }

                DisplayTextOnStatusBar(message);
                // duringStartup = false;
            } else {
                NumRunningSources++;
            }
        }
        
        private bool ShouldReset() {
            var openSolution = GetOpenSolution();
            if(null != openSolution) {
                var baseDirectory = GetSrcMLArchiveFolder(openSolution);
                return File.Exists(Path.Combine(baseDirectory, ResetFileName));
            }
            return false;
        }
        void SaveTimer_Elapsed(object sender, ElapsedEventArgs e) {
            CurrentMonitor.Save();
        }

        /// <summary>
        /// Respond to the MonitorStopped event from SrcMLArchive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToMonitoringStoppedEvent(object sender, EventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToMonitoringStoppedEvent()");
            OnMonitoringStopped(eventArgs);
        }

        /// <summary>
        /// Display incremental progress on the Visual Studio status bar.
        /// </summary>
        /// <param name="label"></param>
        private void ShowProgressOnStatusBar(string label) {
            if(statusBar != null) {
                statusBar.Progress(ref cookie, 1, label, amountCompleted, (uint) CurrentMonitor.NumberOfAllMonitoredFiles);
            }
        }
    }
}