using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ABB.SrcML.Utilities;

namespace ABB.SrcML
{
    /// <summary>
    /// The abstract archive class is the base class for archives. Archives are responsible for recording changes to files and then raising an <see cref="FileChanged">event</see> when they are done.
    /// </summary>
    public abstract class AbstractArchive : IArchive
    {
        private string _archivePath;
        protected TaskManager _taskManager;

        protected AbstractArchive(string baseDirectory, string archiveSubDirectory) {
            this.ArchivePath = Path.Combine(baseDirectory, archiveSubDirectory);
            this._taskManager = new TaskManager(this);
        }

        /// <summary>
        /// Sets the archive path for AbstractArchive objects
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="archiveSubDirectory">the relative path within <paramref name="baseDirectory"/></param>
        /// <param name="scheduler">the scheduler to use for asynchronous methods</param>
        protected AbstractArchive(string baseDirectory, string archiveSubDirectory, TaskScheduler scheduler) {
            this.ArchivePath = Path.Combine(baseDirectory, archiveSubDirectory);
            this._taskManager = new TaskManager(this, scheduler);
        }

        private AbstractArchive()
        {
            
        }

        /// <summary>
        /// Returns true if this archive is empty
        /// </summary>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// Archives are "ready" when they have no running tasks. This property automatically changes to false
        /// when the number of running tasks is zero. Whenever the value changes, the <see cref="IsReadyChanged"/> event fires.
        /// </summary>
        public bool IsReady {
            get { return this._taskManager.IsReady; }
        }

        /// <summary>
        /// Task factory for the asynchronous methods
        /// </summary>
        public TaskScheduler Scheduler {
            get { return this._taskManager.Scheduler; }
            set { this._taskManager.Scheduler = value; }
        }

        /// <summary>
        /// the extensions supported by this collection. The strings returned by this property should match the ones returned by <see cref="System.IO.Path.GetExtension(string)"/>
        /// </summary>
        public abstract ICollection<string> SupportedExtensions { get; }

        /// <summary>
        /// This event should be raised whenever the archive updates its internal representation for a file
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileChanged;

        /// <summary>
        /// Event fires when the <see cref="IsReady"/> property changes
        /// </summary>
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged {
            add { this._taskManager.IsReadyChanged += value; }
            remove { this._taskManager.IsReadyChanged -= value; }
        }

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "add or update file" functionality here in order to enable <see cref="AddOrUpdateFile"/> and <see cref="AddOrUpdateFileAsync"/>
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        protected abstract void AddOrUpdateFileImpl(string fileName);

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "delete file" functionality here in order to enable <see cref="DeleteFile"/> and <see cref="DeleteFileAsync"/>
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        protected abstract void DeleteFileImpl(string fileName);

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "rename file" functionality here in order to enable <see cref="RenameFile"/> and <see cref="RenameFileAsync"/>
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        protected abstract void RenameFileImpl(string oldFileName, string newFileName);

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public virtual void AddOrUpdateFile(string fileName) {
            var task = new Task(() => AddOrUpdateFileImpl(fileName));
            LogExceptions(task);
            _taskManager.Run(task);
        }

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public virtual void AddOrUpdateFileAsync(string fileName) {
            var task = new Task(() => AddOrUpdateFileImpl(fileName));
            LogExceptions(task);
            _taskManager.RunAsync(task);
        }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual void DeleteFile(string fileName) {
            var task = new Task(() => DeleteFileImpl(fileName));
            LogExceptions(task);
            _taskManager.Run(task);
        }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual void DeleteFileAsync(string fileName) {
            var task = new Task(() => DeleteFileImpl(fileName));
            LogExceptions(task);
            _taskManager.RunAsync(task);
        }

        /// <summary>
        /// Renames the file to the new file name
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual void RenameFile(string oldFileName, string newFileName) {
            var task = new Task(() => RenameFileImpl(oldFileName, newFileName));
            LogExceptions(task);
            _taskManager.Run(task);
        }

        /// <summary>
        /// Renames the file to the new file name asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual void RenameFileAsync(string oldFileName, string newFileName) {
            var task = new Task(() => RenameFileImpl(oldFileName, newFileName));
            LogExceptions(task);
            _taskManager.RunAsync(task);
        }

        /// <summary>
        /// Tests to see if the archive contains <paramref name="fileName"/>
        /// </summary>
        /// <param name="fileName">the file name to check for</param>
        /// <returns>True if the file is in the archive; false otherwise</returns>
        public abstract bool ContainsFile(string fileName);

        /// <summary>
        /// Compares file name with the archive representation
        /// </summary>
        /// <param name="fileName">the file name to check for</param>
        /// <returns>True if the archive version of the file is older than <paramref name="fileName"/></returns>
        public abstract bool IsOutdated(string fileName);

        /// <summary>
        /// Gets all of the file names stored in this archive
        /// </summary>
        /// <returns>An enumerable of filenames stored in this archive.</returns>
        public abstract Collection<string> GetFiles();

        /// <summary>
        /// The path where this archive is stored.
        /// </summary>
        public string ArchivePath
        {
            get
            {
                return this._archivePath;
            }
            protected set
            {
                this._archivePath = value;
            }
        }

        /// <summary>
        /// Raise the FileChanged event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Convenience function for logging exceptions upon task failure.
        /// </summary>
        /// <param name="task"></param>
        protected void LogExceptions(Task task) {
            task.ContinueWith(t => {
                foreach(var exception in t.Exception.InnerExceptions) {
                    // logger.Error(exception);
                    Console.Error.WriteLine(exception);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public abstract void Save();

        /// <summary>
        /// Disposes of this object
        /// </summary>
        public virtual void Dispose() {
            Save();
            _taskManager.Dispose();
            FileChanged = null;
        }
    }
}
