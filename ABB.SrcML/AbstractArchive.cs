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
    public abstract class AbstractArchive : IDisposable
    {
        private string _archivePath;

        protected AbstractArchive(string baseDirectory, string archiveSubDirectory)
            : this(baseDirectory, archiveSubDirectory, TaskScheduler.Default){ }

        /// <summary>
        /// Sets the archive path for AbstractArchive objects
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="archiveSubDirectory">the relative path within <paramref name="baseDirectory"/></param>
        /// <param name="scheduler">the scheduler to use for asynchronous methods</param>
        protected AbstractArchive(string baseDirectory, string archiveSubDirectory, TaskScheduler scheduler) {
            this.ArchivePath = Path.Combine(baseDirectory, archiveSubDirectory);
            this.Scheduler = scheduler;
            this.Factory = new TaskFactory(Scheduler);
        }

        private AbstractArchive()
        {
            
        }

        /// <summary>
        /// Returns true if this archive is empty
        /// </summary>
        public abstract bool IsEmpty { get; }

        protected TaskFactory Factory { get; private set; }

        /// <summary>
        /// Task factory for the asynchronous methods
        /// </summary>
        public TaskScheduler Scheduler { get; protected set; }

        /// <summary>
        /// the extensions supported by this collection. The strings returned by this property should match the ones returned by <see cref="System.IO.Path.GetExtension(string)"/>
        /// </summary>
        public abstract ICollection<string> SupportedExtensions { get; }

        /// <summary>
        /// This event should be raised whenever the archive updates its internal representation for a file
        /// </summary>
        public virtual event EventHandler<FileEventRaisedArgs> FileChanged;

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "add or update file" functionality here in order to enable <see cref="AddOrUpdateFile"/> and <see cref="AddOrUpdateFileAsync"/>
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        protected abstract FileEventType? AddOrUpdateFileImpl(string fileName);

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "delete file" functionality here in order to enable <see cref="DeleteFile"/> and <see cref="DeleteFileAsync"/>
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        protected abstract bool DeleteFileImpl(string fileName);

        /// <summary>
        /// Sub-classes of AbstractArchive should implement the "rename file" functionality here in order to enable <see cref="RenameFile"/> and <see cref="RenameFileAsync"/>
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        protected abstract bool RenameFileImpl(string oldFileName, string newFileName);

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public virtual void AddOrUpdateFile(string fileName) {
            // LogExceptions(task);
            var eventType = AddOrUpdateFileImpl(fileName);
            if(eventType.HasValue) {
                OnFileChanged(new FileEventRaisedArgs(eventType.Value, fileName));
            }
        }

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public virtual Task AddOrUpdateFileAsync(string fileName) {
            //LogExceptions(task);
            var task = this.Factory.StartNew(() => AddOrUpdateFileImpl(fileName));
            task.ContinueWith((t) => {
                if(t.Result.HasValue) {
                    OnFileChanged(new FileEventRaisedArgs(t.Result.Value, fileName));
                }
            });
            return task;
        }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual void DeleteFile(string fileName) {
            // LogExceptions(task);
            if(DeleteFileImpl(fileName)) {
                OnFileChanged(new FileEventRaisedArgs(FileEventType.FileDeleted, fileName));
            }
        }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual Task DeleteFileAsync(string fileName) {
            //LogExceptions(task);
            var task = Factory.StartNew(() => DeleteFileImpl(fileName));
            task.ContinueWith((t) => {
                if(t.Result) {
                    OnFileChanged(new FileEventRaisedArgs(FileEventType.FileDeleted, fileName));
                }
            });
            return task;
        }

        /// <summary>
        /// Renames the file to the new file name
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual void RenameFile(string oldFileName, string newFileName) {
            //LogExceptions(task);
            if(RenameFileImpl(oldFileName, newFileName)) {
                OnFileChanged(new FileEventRaisedArgs(FileEventType.FileRenamed, newFileName, oldFileName));
            }
        }

        /// <summary>
        /// Renames the file to the new file name asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual Task RenameFileAsync(string oldFileName, string newFileName) {
            //LogExceptions(task);
            var task = Factory.StartNew(() => RenameFileImpl(oldFileName, newFileName));
            task.ContinueWith((t) => {
                if(t.Result) {
                    OnFileChanged(new FileEventRaisedArgs(FileEventType.FileRenamed, newFileName, oldFileName));
                }
            });
            return task;
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
            FileChanged = null;
        }
    }
}
