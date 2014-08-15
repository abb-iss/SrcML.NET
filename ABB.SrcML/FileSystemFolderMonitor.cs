using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML {

    /// <summary>
    /// An implementation of <see cref="AbstractFileMonitor"/> that responds to file system events.
    /// </summary>
    public class FileSystemFolderMonitor : AbstractFileMonitor {
        private FileSystemWatcher _directoryWatcher;
        private DirectoryInfo _folderInfo;
        private DirectoryInfo _monitorStorageInfo;

        /// <summary>
        /// Creates a new file system monitor
        /// </summary>
        /// <param name="pathToSourceFolder">The folder to watch</param>
        /// <param name="monitoringStorage">The base directory for the archive data</param>
        public FileSystemFolderMonitor(string pathToSourceFolder, string monitoringStorage) : this(pathToSourceFolder, monitoringStorage, null) { }

        /// <summary>
        /// Creates a new file system monitor
        /// </summary>
        /// <param name="pathToSourceFolder">The folder to watch</param>
        /// <param name="monitoringStorage">The base directory for the archive data</param>
        /// <param name="defaultArchive">The default archive</param>
        /// <param name="otherArchives">Other archives to register</param>
        public FileSystemFolderMonitor(string pathToSourceFolder, string monitoringStorage, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : base(monitoringStorage, defaultArchive, otherArchives) {
            this.FullFolderPath = pathToSourceFolder;
            this._monitorStorageInfo = new DirectoryInfo(monitoringStorage);
            SetupFileSystemWatcher();
        }

        /// <summary>
        /// The full path to the folder being monitored
        /// </summary>
        public string FullFolderPath {
            get {
                return this._folderInfo.FullName;
            }
            set {
                this._folderInfo = new DirectoryInfo(value);
            }
        }

        #region AbstractArchive Members

        public override IEnumerable<string> EnumerateMonitoredFiles() {
            var filePaths = from file in this._folderInfo.GetFiles("*", SearchOption.AllDirectories)
                            select file.FullName;
            return filePaths;
        }

        /// <summary>
        /// Start monitoring
        /// </summary>
        public override void StartMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring
        /// </summary>
        public override void StopMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = false;
        }

        #endregion AbstractArchive Members

        /// <summary>
        /// Checks if the path points to a file
        /// </summary>
        /// <param name="fullPath">the path to check</param>
        /// <returns>true if the path represents a file; false otherwise</returns>
        private static bool isFile(string fullPath) {
            if(!File.Exists(fullPath))
                return false;

            var pathAttributes = File.GetAttributes(fullPath);
            return !pathAttributes.HasFlag(FileAttributes.Directory);
        }

        /// <summary>
        /// Respond to a file-changed event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        private void HandleFileChanged(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                UpdateFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to a file-created event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        private void HandleFileCreated(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                AddFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to a file-changed deleted
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        private void HandleFileDeleted(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                DeleteFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to a file-rename event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        private void HandleFileRenamed(object sender, RenamedEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                RenameFile(e.OldFullPath, e.FullPath);
            }
        }

        /// <summary>
        /// Respond to an error for the file system watcher. Not implemented.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        private void HandleFileWatcherError(object sender, ErrorEventArgs e) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if the path is located within the archive directory
        /// </summary>
        /// <param name="filePath">the path to check</param>
        /// <returns>True if the path is in the see
        /// cref="AbstractFileMonitor.MonitorStoragePath"/></returns>
        private bool IsNotInMonitoringStorage(string filePath) {
            return !Path.GetFullPath(filePath).StartsWith(_monitorStorageInfo.FullName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sets up the internal file system monitor
        /// </summary>
        private void SetupFileSystemWatcher() {
            this._directoryWatcher = new FileSystemWatcher(this.FullFolderPath);

            this._directoryWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Attributes;
            this._directoryWatcher.IncludeSubdirectories = true;

            this._directoryWatcher.Changed += HandleFileChanged;
            this._directoryWatcher.Created += HandleFileCreated;
            this._directoryWatcher.Deleted += HandleFileDeleted;
            this._directoryWatcher.Error += HandleFileWatcherError;
            this._directoryWatcher.Renamed += HandleFileRenamed;
        }
    }
}