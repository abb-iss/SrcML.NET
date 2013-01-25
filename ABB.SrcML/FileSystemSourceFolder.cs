using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;

namespace ABB.SrcML {
    public class FileSystemSourceFolder : IFileMonitor {
        private DirectoryInfo _folderInfo;
        private FileSystemWatcher _directoryWatcher;

        protected virtual void OnFileEventRaised(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileEventRaised;

            if(null != handler) {
                handler(this, e);
            }
        }

        public FileSystemSourceFolder(string pathToSourceFolder) {
            this.FullFolderPath = pathToSourceFolder;
            SetupFileSystemWatcher();
            StartMonitoring();
        }
        #region ISourceFolder Members


        public string FullFolderPath {
            get {
                return this._folderInfo.FullName;
            }
            set {
                this._folderInfo = new DirectoryInfo(value);
            }
        }

        public void StartMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = true;
        }

        public void StopMonitoring() {
            this._directoryWatcher.EnableRaisingEvents = false;
        }

        public List<string> GetMonitoredFiles(BackgroundWorker worker) {
            return null;
        }

        //temp approach
        public void RaiseSolutionMonitorEvent(string filePath, string oldFilePath, FileEventType type) {
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
            handleFileEvent(e.FullPath, e.FullPath, FileEventType.FileChanged);
        }

        void HandleFileCreated(object sender, FileSystemEventArgs e) {
            handleFileEvent(e.FullPath, e.FullPath, FileEventType.FileAdded);
        }

        void HandleFileDeleted(object sender, FileSystemEventArgs e) {
            handleFileEvent(e.FullPath, e.FullPath, FileEventType.FileDeleted);
        }

        void HandleFileWatcherError(object sender, ErrorEventArgs e) {
            throw new NotImplementedException();
        }

        void HandleFileRenamed(object sender, RenamedEventArgs e) {
            handleFileEvent(e.FullPath, e.OldFullPath, FileEventType.FileRenamed);
        }

        private void handleFileEvent(string pathToFile, string oldPathToFile, FileEventType eventType) {
            if(isFile(pathToFile)) {
                OnFileEventRaised(new FileEventRaisedArgs(pathToFile, oldPathToFile, eventType));
            }
        }

        private static bool isFile(string fullPath) {
            if(!File.Exists(fullPath))
                return false;

            var pathAttributes = File.GetAttributes(fullPath);
            return !pathAttributes.HasFlag(FileAttributes.Directory);
        }

        public event EventHandler<FileEventRaisedArgs> FileEventRaised;
    }
}
