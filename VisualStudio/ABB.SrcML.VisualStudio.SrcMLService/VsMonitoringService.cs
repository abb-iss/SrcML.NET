/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Utilities;
using ABB.VisualStudio;
using EnvDTE;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using VsPackage = Microsoft.VisualStudio.Shell.Package;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// Service for monitoring changes to files within the currently open solution
    /// </summary>
    public class VsMonitoringService : AbstractMonitoringService, ISrcMLGlobalService, SSrcMLGlobalService {
        /// <summary>
        /// The directory in the extension folder to store data in for each solution
        /// </summary>
        public const string DEFAULT_STORAGE_FOLDER = "Archives";

        /// <summary>
        /// The file name that indicates that the data directory should be deleted
        /// </summary>
        public const string RESET_FILE_NAME = "RESETSOLUTION";

        /// <summary>
        /// Creates a new monitoring service object
        /// </summary>
        /// <param name="serviceProvider">The container where this service is sited</param>
        /// <param name="taskManagerService">The task manager</param>
        public VsMonitoringService(SrcMLServicePackage serviceProvider, ITaskManagerService taskManagerService)
            : base(serviceProvider, taskManagerService) {
        }

        /// <summary>
        /// Raised when a directory is added via <see cref="AddDirectoryToMonitor"/>
        /// </summary>
        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryAdded;

        /// <summary>
        /// Raised when a directory is removed via <see cref="RemoveDirectoryFromMonitor"/>
        /// </summary>
        public event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryRemoved;

        /// <summary>
        /// Raised when the monitor detects a file has changed
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;

        /// <summary>
        /// Raised when an update starts. This is a wrapper around <see cref="AbstractMonitoringService.UpdateStarted"/> for backwards compatibility
        /// </summary>
        public event EventHandler UpdateArchivesStarted {
            add { UpdateStarted += value; }
            remove { UpdateStarted -= value; }
        }

        /// <summary>
        /// Raised when an update completes. This is a wrapper around <see cref="AbstractMonitoringService.UpdateCompleted"/> for backwards compatibility
        /// </summary>
        public event EventHandler UpdateArchivesCompleted {
            add { UpdateCompleted += value; }
            remove { UpdateCompleted -= value; }
        }

        /// <summary>
        /// The current monitor object
        /// </summary>
        public SourceMonitor CurrentMonitor { get; private set; }

        /// <summary>
        /// the srcML project that wires everything better
        /// </summary>
        public SrcMLProject CurrentProject { get; private set; }

        /// <summary>
        /// The current srcML archive
        /// </summary>
        public SrcMLArchive CurrentSrcMLArchive { get { return (null != CurrentProject ? CurrentProject.SourceArchive : null); } }

        /// <summary>
        /// The collection of monitored directories
        /// </summary>
        public ReadOnlyCollection<string> MonitoredDirectories { get { return (CurrentMonitor == null ? null : this.CurrentMonitor.MonitoredDirectories); } }

        /// <summary>
        /// The interval at which to scan <see cref="MonitoredDirectories"/>
        /// </summary>
        public double ScanInterval {
            get { return (null == CurrentMonitor ? Double.NaN : CurrentMonitor.ScanInterval); }
            set {
                if(null == CurrentMonitor) { throw new InvalidOperationException("There is no monitor to update"); }
                if(value < 0.0) { throw new ArgumentOutOfRangeException("value", "ScanInterval must be greater than 0"); }
                CurrentMonitor.ScanInterval = value;
            }
        }

        /// <summary>
        /// Sets up the monitor prior to <see cref="AbstractMonitoringService.StartMonitoring"/>
        /// </summary>
        protected override void Setup() {
            var openSolution = GetOpenSolution();

            if(null != openSolution) {
                var storagePath = GetBaseDirectory(openSolution);
                if(ShouldReset()) {
                    Directory.Delete(storagePath, true);
                }
                CurrentMonitor = new SourceMonitor(openSolution, DirectoryScanningMonitor.DEFAULT_SCAN_INTERVAL, GlobalScheduler, storagePath);
                CurrentProject = new SrcMLProject(GlobalScheduler, CurrentMonitor, new SrcMLGenerator(Path.Combine(ServiceProvider.ExtensionDirectory, "SrcML")));

                CurrentMonitor.DirectoryAdded += CurrentMonitor_DirectoryAdded;
                CurrentMonitor.DirectoryRemoved += CurrentMonitor_DirectoryRemoved;
                CurrentMonitor.UpdateArchivesStarted += CurrentMonitor_UpdateArchivesStarted;
                CurrentMonitor.UpdateArchivesCompleted += CurrentMonitor_UpdateArchivesCompleted;
                CurrentMonitor.FileChanged += CurrentMonitor_FileChanged;
                CurrentMonitor.AddDirectoriesFromSaveFile();
                if(0 == CurrentMonitor.MonitoredDirectories.Count) {
                    CurrentMonitor.AddSolutionDirectories();
                }
                
            }
        }

        /// <summary>
        /// Implementation method for <see cref="AbstractMonitoringService.Update"/>
        /// </summary>
        protected override void UpdateImpl() {
            CurrentMonitor.UpdateArchivesAsync().Wait();
        }

        /// <summary>
        /// Raises the <see cref="SourceFileChanged"/> event
        /// </summary>
        /// <param name="e">The file event arguments</param>
        protected virtual void OnSourceFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = SourceFileChanged;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="DirectoryRemoved"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDirectoryRemoved(DirectoryScanningMonitorEventArgs e) {
            EventHandler<DirectoryScanningMonitorEventArgs> handler = DirectoryRemoved;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="DirectoryAdded"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDirectoryAdded(DirectoryScanningMonitorEventArgs e) {
            EventHandler<DirectoryScanningMonitorEventArgs> handler = DirectoryAdded;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Implementation for <see cref="AbstractMonitoringService.StartMonitoring"/>
        /// </summary>
        protected override void StartMonitoringImpl() {
            if(null != CurrentMonitor) {
                CurrentMonitor.StartMonitoring();
            }
        }

        /// <summary>
        /// Implementation for <see cref="AbstractMonitoringService.StopMonitoring"/>
        /// </summary>
        protected override void StopMonitoringImpl() {
            if(CurrentMonitor != null && CurrentSrcMLArchive != null) {
                SaveTimer.Stop();
                CurrentProject.StopMonitoring();
                
                CurrentMonitor.FileChanged -= CurrentMonitor_FileChanged;
                CurrentMonitor.DirectoryAdded -= CurrentMonitor_DirectoryAdded;
                CurrentMonitor.DirectoryRemoved -= CurrentMonitor_DirectoryRemoved;
                CurrentMonitor.UpdateArchivesStarted -= CurrentMonitor_UpdateArchivesStarted;
                CurrentMonitor.UpdateArchivesCompleted -= CurrentMonitor_UpdateArchivesCompleted;

                CurrentProject.Dispose();
                CurrentProject = null;
                CurrentMonitor = null;
            }
        }

        /// <summary>
        /// Adds <paramref name="pathToDirectory"/> to the <see cref="MonitoredDirectories"/> collection
        /// </summary>
        /// <param name="pathToDirectory">The path to the directory to be added</param>
        public void AddDirectoryToMonitor(string pathToDirectory) {
            if(null == CurrentMonitor) { throw new InvalidOperationException("Only valid once a solution has been opened."); }
            if(String.IsNullOrWhiteSpace(pathToDirectory)) { throw new ArgumentNullException("pathToDirectory"); }
            CurrentMonitor.AddDirectory(pathToDirectory);
        }

        /// <summary>
        /// Gets a srcML element for <paramref name="sourceFilePath"/> from <see cref="CurrentSrcMLArchive"/>
        /// </summary>
        /// <param name="sourceFilePath">The source file path</param>
        /// <returns>The XML element for <paramref name="sourceFilePath"/> (null if it is not in the archive)</returns>
        public XElement GetXElementForSourceFile(string sourceFilePath) {
            return (CurrentSrcMLArchive == null ? null : CurrentSrcMLArchive.GetXElementForSourceFile(sourceFilePath));
        }

        /// <summary>
        /// Removes <paramref name="pathToDirectory"/> from <see cref="MonitoredDirectories"/>
        /// </summary>
        /// <param name="pathToDirectory">The path to the directory to be added</param>
        public void RemoveDirectoryFromMonitor(string pathToDirectory) {
            if(null == CurrentMonitor) { throw new InvalidOperationException("Only valid once a solution has been opened."); }
            if(String.IsNullOrWhiteSpace(pathToDirectory)) { throw new ArgumentNullException("pathToDirectory"); }

            CurrentMonitor.RemoveDirectory(pathToDirectory);
        }

        /// <summary>
        /// Creates <see cref="RESET_FILE_NAME"/> in the data directory for the open solution. This indicates the directory should be deleted
        /// on the next call to <see cref="Setup"/>
        /// </summary>
        public void Reset() {
            var openSolution = GetOpenSolution();
            if(null != openSolution) {
                var baseDirectory = GetBaseDirectory(openSolution);
                File.Create(Path.Combine(baseDirectory, RESET_FILE_NAME)).Close();
            }
        }

        /// <summary>
        /// Generate the folder path for storing srcML files. (For all the following four methods.)
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        public string GetBaseDirectory(Solution openSolution) {
            var fullName = openSolution.FullName;
            var solutionName = Path.GetFileNameWithoutExtension(fullName);
            var directoryName = String.Format("{0}{1}", solutionName, fullName.GetHashCode());

            return Path.Combine(this.ServiceProvider.ExtensionDirectory, DEFAULT_STORAGE_FOLDER, directoryName);
        }

        /// <summary>
        /// Saves the state of <see cref="CurrentMonitor"/>
        /// </summary>
        protected override void Save() {
            if(null != CurrentMonitor) {
                CurrentMonitor.Save();
            }
        }

        private bool ShouldReset() {
            var openSolution = GetOpenSolution();
            if(null != openSolution) {
                var baseDirectory = GetBaseDirectory(openSolution);
                return File.Exists(Path.Combine(baseDirectory, RESET_FILE_NAME));
            }
            return false;
        }

        /// <summary>
        /// Get the open solution.
        /// </summary>
        /// <returns></returns>
        private static Solution GetOpenSolution() {
            var dte = VsPackage.GetGlobalService(typeof(DTE)) as DTE;
            if(dte != null) {
                var openSolution = dte.Solution;
                return openSolution;
            } else {
                return null;
            }
        }

        private void CurrentMonitor_FileChanged(object sender, FileEventRaisedArgs e) {
            OnSourceFileChanged(e);
        }
        private void CurrentMonitor_UpdateArchivesCompleted(object sender, EventArgs e) {
            OnUpdateCompleted(e);
        }

        private void CurrentMonitor_UpdateArchivesStarted(object sender, EventArgs e) {
            OnUpdateStarted(e);
        }

        private void CurrentMonitor_DirectoryRemoved(object sender, DirectoryScanningMonitorEventArgs e) {
            OnDirectoryRemoved(e);
        }

        private void CurrentMonitor_DirectoryAdded(object sender, DirectoryScanningMonitorEventArgs e) {
            OnDirectoryAdded(e);
        }
    }
}