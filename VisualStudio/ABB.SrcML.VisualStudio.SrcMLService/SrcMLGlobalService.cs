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
using ABB.VisualStudio;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Timers;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio {

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

        public SrcMLProject CurrentProject { get; private set; }

        /// <summary>
        /// SrcML.NET's Solution Monitor.
        /// </summary>
        public SourceMonitor CurrentMonitor { get; private set; }

        /// <summary>
        /// SrcML.NET's SrcMLArchive.
        /// </summary>
        public SrcMLArchive CurrentSrcMLArchive {
            get {
                return (null != CurrentProject ? CurrentProject.SourceArchive : null);
            }
        }

        private int frozen;
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

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="extensionDirectory"></param>
        public SrcMLGlobalService(IServiceProvider sp, string extensionDirectory) {
            SrcMLFileLogger.DefaultLogger.InfoFormat("Constructing a new instance of SrcMLGlobalService in {0}", extensionDirectory);
            serviceProvider = sp;
            SrcMLServiceDirectory = extensionDirectory;
            statusBar = (IVsStatusbar) Package.GetGlobalService(typeof(SVsStatusbar));
            _taskManager = (ITaskManagerService) Package.GetGlobalService(typeof(STaskManagerService));
            SaveTimer = new ReentrantTimer(() => CurrentMonitor.Save(), _taskManager.GlobalScheduler);
            SaveInterval = DEFAULT_SAVE_INTERVAL;
        }

        #region ISrcMLGlobalService Members

        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryAdded;
        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryRemoved;

        public event EventHandler MonitoringStarted;
        public event EventHandler MonitoringStopped;

        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;

        public event EventHandler UpdateArchivesStarted;
        public event EventHandler UpdateArchivesCompleted;

        public bool IsMonitoring { get; private set; }

        public bool IsUpdating { get; private set; }

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

                CurrentMonitor = new SourceMonitor(openSolution, DirectoryScanningMonitor.DEFAULT_SCAN_INTERVAL, _taskManager.GlobalScheduler, baseDirectory);
                CurrentProject = new SrcMLProject(_taskManager.GlobalScheduler, CurrentMonitor, new SrcMLGenerator(srcMLBinaryDirectory));

                // Create a new instance of SrcML.NET's solution monitor
                if(openSolution != null) {
                    CurrentMonitor.DirectoryAdded += RespondToDirectoryAddedEvent;
                    CurrentMonitor.DirectoryRemoved += RespondToDirectoryRemovedEvent;
                    CurrentMonitor.UpdateArchivesStarted += CurrentMonitor_UpdateArchivesStarted;
                    CurrentMonitor.UpdateArchivesCompleted += CurrentMonitor_UpdateArchivesCompleted;
                    CurrentMonitor.AddDirectoriesFromSaveFile();
                    if(0 == CurrentMonitor.MonitoredDirectories.Count) {
                        CurrentMonitor.AddSolutionDirectories();
                    }
                }

                // Subscribe events from Solution Monitor
                if(CurrentMonitor != null) {
                    CurrentMonitor.FileChanged += RespondToFileChangedEvent;

                    // Initialize the progress bar.
                    if(statusBar != null) {
                        statusBar.Progress(ref cookie, 1, "", 0, 0);
                    }
                    // Start monitoring
                    var updateTask = CurrentMonitor.UpdateArchivesAsync();

                    CurrentMonitor.StartMonitoring();
                    OnMonitoringStarted(new EventArgs());
                    SaveTimer.Start();
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitoring()"));
            }
        }

        void CurrentMonitor_UpdateArchivesCompleted(object sender, EventArgs e) {
            OnUpdateArchivesCompleted(e);
        }

        void CurrentMonitor_UpdateArchivesStarted(object sender, EventArgs e) {
            OnUpdateArchivesStarted(e);
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
                    OnMonitoringStopped(new EventArgs());
                    SaveTimer.Stop();
                    CurrentProject.StopMonitoring();
                    CurrentMonitor.FileChanged -= RespondToFileChangedEvent;
                    CurrentMonitor.DirectoryAdded -= RespondToDirectoryAddedEvent;
                    CurrentMonitor.DirectoryRemoved -= RespondToDirectoryRemovedEvent;
                    CurrentProject.Dispose();
                    CurrentProject = null;
                    CurrentMonitor = null;
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StopMonitoring()"));
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

        protected virtual void OnMonitoringStarted(EventArgs e) {
            IsMonitoring = true;
            EventHandler handler = MonitoringStarted;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMonitoringStopped(EventArgs e) {
            IsMonitoring = false;
            EventHandler handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateArchivesStarted(EventArgs e) {
            IsUpdating = true;
            EventHandler handler = UpdateArchivesStarted;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateArchivesCompleted(EventArgs e) {
            IsUpdating = false;
            EventHandler handler = UpdateArchivesCompleted;
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
            OnFileChanged(eventArgs);
        }

        private void RespondToDirectoryAddedEvent(object sender, DirectoryScanningMonitorEventArgs eventArgs) {
            OnDirectoryAdded(eventArgs);
        }

        private void RespondToDirectoryRemovedEvent(object sender, DirectoryScanningMonitorEventArgs eventArgs) {
            OnDirectoryRemoved(eventArgs);
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

    }
}