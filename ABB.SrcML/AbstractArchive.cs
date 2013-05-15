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
        protected TaskManager _taskManager;

        /// <summary>
        /// Sets the archive path for AbstractArchive objects
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="archiveSubDirectory">the relative path within <paramref name="baseDirectory"/></param>
        protected AbstractArchive(string baseDirectory, string archiveSubDirectory, TaskFactory factory) {
            this.ArchivePath = Path.Combine(baseDirectory, archiveSubDirectory);
            this._taskManager = new TaskManager(this, factory);
        }

        private AbstractArchive()
        {
            
        }

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
        public TaskFactory TaskFactory {
            get { return this._taskManager.TaskFactory; }
            set { this._taskManager.TaskFactory = value; }
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
        public virtual void AddOrUpdateFile(string fileName) { _taskManager.Run(() => AddOrUpdateFileImpl(fileName)); }

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public virtual void AddOrUpdateFileAsync(string fileName) { _taskManager.RunAsync(() => AddOrUpdateFileImpl(fileName)); }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual void DeleteFile(string fileName) { _taskManager.Run(() => DeleteFileImpl(fileName)); }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public virtual void DeleteFileAsync(string fileName) { _taskManager.RunAsync(() => DeleteFileImpl(fileName)); }

        /// <summary>
        /// Renames the file to the new file name
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual void RenameFile(string oldFileName, string newFileName) { _taskManager.Run(() => RenameFileImpl(oldFileName, newFileName)); }

        /// <summary>
        /// Renames the file to the new file name asynchronously. A new <see cref="System.Threading.Tasks.Task"/> is run via <see cref="TaskFactory"/>.
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public virtual void RenameFileAsync(string oldFileName, string newFileName) { _taskManager.RunAsync(() => RenameFileImpl(oldFileName, newFileName)); }

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
        /// Disposes of this object
        /// </summary>
        public virtual void Dispose() {
            _taskManager.Dispose();
            FileChanged = null;
        }
    }
}
