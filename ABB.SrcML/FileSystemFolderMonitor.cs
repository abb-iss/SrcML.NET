using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

namespace ABB.SrcML {
    public class FileSystemFolderMonitor : AbstractFileMonitor {
        private DirectoryInfo _folderInfo;
        private DirectoryInfo _monitorStorageInfo;
        private FileSystemWatcher _directoryWatcher;

        public FileSystemFolderMonitor(string pathToSourceFolder, string monitoringStorage, AbstractArchive defaultArchive) 
        : base(monitoringStorage, defaultArchive) {
            this.FullFolderPath = pathToSourceFolder;
            this._monitorStorageInfo = new DirectoryInfo(monitoringStorage);
            SetupFileSystemWatcher();
        }

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

        void HandleFileChanged(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                UpdateFile(e.FullPath);
            }
        }

        void HandleFileCreated(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                AddFile(e.FullPath);
            }
        }

        void HandleFileDeleted(object sender, FileSystemEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                DeleteFile(e.FullPath);
            }
        }

        void HandleFileWatcherError(object sender, ErrorEventArgs e) {
            throw new NotImplementedException();
        }

        void HandleFileRenamed(object sender, RenamedEventArgs e) {
            if(IsNotInMonitoringStorage(e.FullPath)) {
                RenameFile(e.OldFullPath, e.FullPath);
            }
        }

        private static bool isFile(string fullPath) {
            if(!File.Exists(fullPath))
                return false;

            var pathAttributes = File.GetAttributes(fullPath);
            return !pathAttributes.HasFlag(FileAttributes.Directory);
        }

        private bool IsNotInMonitoringStorage(string filePath) {
            return !Path.GetFullPath(filePath).StartsWith(_monitorStorageInfo.FullName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
