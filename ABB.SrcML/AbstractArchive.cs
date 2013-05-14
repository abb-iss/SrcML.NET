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

namespace ABB.SrcML
{
    /// <summary>
    /// The abstract archive class is the base class for archives. Archives are responsible for recording changes to files and then raising an <see cref="FileChanged">event</see> when they are done.
    /// </summary>
    public abstract class AbstractArchive : IDisposable
    {
        private string _archivePath;
        private bool archiveIsReady;
        private int _runningTasks;

        /// <summary>
        /// Sets the archive path for AbstractArchive objects
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="archiveSubDirectory">the relative path within <paramref name="baseDirectory"/></param>
        protected AbstractArchive(string baseDirectory, string archiveSubDirectory, TaskFactory factory) {
            this.archiveIsReady = true;
            this.ArchivePath = Path.Combine(baseDirectory, archiveSubDirectory);
            this.TaskFactory = factory;
        }

        private AbstractArchive()
        {
            
        }

        public bool IsReady {
            get { return this.archiveIsReady; }
            protected set {
                if(value != this.archiveIsReady) {
                    archiveIsReady = value;
                    OnIsReadyChanged(new IsReadyChangedEventArgs(archiveIsReady));
                }
            }
        }

        public TaskFactory TaskFactory { get; set; }

        protected int CountOfRunningTasks {
            get { return _runningTasks; }
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
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        protected abstract void AddOrUpdateFileImpl(string fileName);

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        protected abstract void DeleteFileImpl(string fileName);

        /// <summary>
        /// Renames the file to the new file name
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        protected abstract void RenameFileImpl(string oldFileName, string newFileName);

        public virtual void AddOrUpdateFile(string fileName) { Run(() => AddOrUpdateFileImpl(fileName), true); }

        public virtual void AddOrUpdateFileAsync(string fileName) { Run(() => AddOrUpdateFileImpl(fileName), false); }
        
        public virtual void DeleteFile(string fileName) { Run(() => DeleteFileImpl(fileName), true); }

        public virtual void DeleteFileAsync(string fileName) { Run(() => DeleteFileImpl(fileName), false); }

        public virtual void RenameFile(string oldFileName, string newFileName) { Run(() => RenameFileImpl(oldFileName, newFileName), true); }

        public virtual void RenameFileAsync(string oldFileName, string newFileName) { Run(() => RenameFileImpl(oldFileName, newFileName), false); }

        

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
        /// event handler for <see cref="IsReadyChanged"/>
        /// </summary>
        /// <param name="e">event arguments</param>
        protected virtual void OnIsReadyChanged(IsReadyChangedEventArgs e) {
            EventHandler<IsReadyChangedEventArgs> handler = IsReadyChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Disposes of this object
        /// </summary>
        public virtual void Dispose() {
            IsReadyChanged = null;
            FileChanged = null;
        }

        protected void Run(Action action, bool runSynchronously) {
            IncrementTask();
            Task task;
            if(runSynchronously) {
                task = new Task(action);
            } else {
                task = this.TaskFactory.StartNew(action);   
            }
            DecrementOnCompletion(task);
            LogExceptions(task);

            if(runSynchronously) {
                task.RunSynchronously();
            }
        }

        protected void IncrementTask() {
            Interlocked.Increment(ref _runningTasks);
            if(IsReady) {
                IsReady = false;
            }
        }

        protected void DecrementTask() {
            Interlocked.Decrement(ref _runningTasks);
            if(_runningTasks <= 0) {
                IsReady = true;
            }
        }

        protected void DecrementOnCompletion(Task task) {
            task.ContinueWith(t => DecrementTask());
        }

        protected void LogExceptions(Task task) {
            task.ContinueWith(t => {
                foreach(var exception in t.Exception.InnerExceptions) {
                    // logger.Error(exception);
                    Console.Error.WriteLine(exception);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
