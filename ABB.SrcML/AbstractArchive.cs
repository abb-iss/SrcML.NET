using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Collections.ObjectModel;

namespace ABB.SrcML
{
    public abstract class AbstractArchive : IDisposable
    {
        private string _archivePath;
        
        protected AbstractArchive(string archivePath) {
            this.ArchivePath = archivePath;
        }

        private AbstractArchive()
        {
            
        }

        public abstract ICollection<string> SupportedExtensions { get; }

        /// <summary>
        /// This event should be raised whenever the archive updates its internal representation for a file
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileChanged;

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> within the archive
        /// </summary>
        /// <param name="fileName">The file name to add or update. If the file exists, it is deleted and then added regardless of whether or not the file is outdated</param>
        public abstract void AddOrUpdateFile(string fileName);

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive
        /// </summary>
        /// <param name="fileName">The file name to delete. If it does not exist, nothing happens</param>
        public abstract void DeleteFile(string fileName);

        /// <summary>
        /// Renames the file to the new file name
        /// </summary>
        /// <param name="oldFileName">the existing path</param>
        /// <param name="newFileName">the new path</param>
        public abstract void RenameFile(string oldFileName, string newFileName);

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
        public abstract IEnumerable<string> GetFiles();

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

        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        public virtual void Dispose() {
            FileChanged = null;
        }
    }
}
