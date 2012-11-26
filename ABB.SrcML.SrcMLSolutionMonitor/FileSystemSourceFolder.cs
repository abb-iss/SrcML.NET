using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ABB.SrcML.SrcMLSolutionMonitor
{
    public class FileSystemSourceFolder : ISourceFolder
    {
        private DirectoryInfo _folderInfo;
        private FileSystemWatcher _directoryWatcher;

        protected virtual void OnSourceFileChanged(SourceEventArgs e)
        {
            EventHandler<SourceEventArgs> handler = SourceFileChanged;

            if (null != handler)
            {
                handler(this, e);
            }
        }

        public FileSystemSourceFolder(string pathToSourceFolder)
        {
            this.FullFolderPath = pathToSourceFolder;
            SetupFileSystemWatcher();
            StartWatching();
        }
        #region ISourceFolder Members

        public event EventHandler<SourceEventArgs> SourceFileChanged;

        public string FullFolderPath
        {
            get
            {
                return this._folderInfo.FullName;
            }
            set
            {
                this._folderInfo = new DirectoryInfo(value);
            }
        }

        public void StartWatching()
        {
            this._directoryWatcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            this._directoryWatcher.EnableRaisingEvents = false;
        }

        #endregion

        private void SetupFileSystemWatcher()
        {
            this._directoryWatcher = new FileSystemWatcher(this.FullFolderPath);

            this._directoryWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Attributes;
            this._directoryWatcher.IncludeSubdirectories = true;

            this._directoryWatcher.Changed += HandleFileChanged;
            this._directoryWatcher.Created += HandleFileCreated;
            this._directoryWatcher.Deleted += HandleFileDeleted;
            this._directoryWatcher.Error += HandleFileWatcherError;
            this._directoryWatcher.Renamed += HandleFileRenamed;
        }

        void HandleFileChanged(object sender, FileSystemEventArgs e)
        {
            handleFileEvent(e.FullPath, e.FullPath, SourceEventType.Changed);
        }

        void HandleFileCreated(object sender, FileSystemEventArgs e)
        {
            handleFileEvent(e.FullPath, e.FullPath, SourceEventType.Added);
        }

        void HandleFileDeleted(object sender, FileSystemEventArgs e)
        {
            handleFileEvent(e.FullPath, e.FullPath, SourceEventType.Deleted);
        }

        void HandleFileWatcherError(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        void HandleFileRenamed(object sender, RenamedEventArgs e)
        {
            handleFileEvent(e.FullPath, e.OldFullPath, SourceEventType.Renamed);
        }

        private void handleFileEvent(string pathToFile, string oldPathToFile, SourceEventType eventType)
        {
            if (isFile(pathToFile))
            {
                OnSourceFileChanged(new SourceEventArgs(pathToFile, oldPathToFile, eventType));
            }
        }

        private static bool isFile(string fullPath)
        {
            if (!File.Exists(fullPath))
                return false;

            var pathAttributes = File.GetAttributes(fullPath);
            return !pathAttributes.HasFlag(FileAttributes.Directory);
        }
    }
}
