using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

namespace ABB.SrcML {
    /// <summary>
    /// An implementation of <see cref="AbstractFileMontor"/> that responds to file system events.
    /// </summary>
    public class FileSystemFolderMonitor : AbstractFileMonitor {
        private DirectoryInfo _folderInfo;
        private DirectoryInfo _monitorStorageInfo;
        private FileSystemWatcher _directoryWatcher;

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

        public override void StartMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = true;
        }

        public override void StopMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = false;
        }

        public override IEnumerable<string> GetFilesFromSource() {
            return Directory.GetFiles(this.FullFolderPath, "*", SearchOption.AllDirectories);
        }

        #endregion

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

        /// <summary>
        /// Respond to a file-changed event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        void HandleFileChanged(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                UpdateFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to a file-created event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        void HandleFileCreated(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                AddFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to a file-changed deleted
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        void HandleFileDeleted(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                DeleteFile(e.FullPath);
            }
        }

        /// <summary>
        /// Respond to an error for the file system watcher. Not implemented.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        void HandleFileWatcherError(object sender, ErrorEventArgs e) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Respond to a file-rename event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">event arguments</param>
        void HandleFileRenamed(object sender, RenamedEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                RenameFile(e.OldFullPath, e.FullPath);
            }
        }

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
        /// Checks if the path is located within the archive directory
        /// </summary>
        /// <param name="filePath">the path to check</param>
        /// <returns>True if the path is in the <see cref="AbstractFileMonitor.MonitorStoragePath"/></returns>
        private bool IsNotInMonitoringStorage(string filePath) {
            return !Path.GetFullPath(filePath).StartsWith(_monitorStorageInfo.FullName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
