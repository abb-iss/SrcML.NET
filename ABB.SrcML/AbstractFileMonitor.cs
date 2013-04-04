using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using log4net;
using ABB.SrcML.Utilities;

namespace ABB.SrcML {
    /// <summary>
    /// <para>Represents an abstract file monitor. This class contains archives for storing various file types To start using it, you first instantiate
    /// it with a <see cref="AbstractArchive">default archive</see>. You then call <see cref="RegisterNonDefaultArchive"/> for each alternative 
    /// <see cref="archive"/>. This class automatically routes files to the appropriate archive.</para>
    /// <para>You begin monitoring by calling <see cref="StartMonitoring"/>. <see cref="StartMonitoring"/> should subscribe to any events and then call
    /// functions to respond to those events:</para>
    /// <list type="bullet">
    /// <item><description><see cref="AddFile(string)"/></description></item>
    /// <item><description><see cref="DeleteFile(string)"/></description></item>
    /// <item><description<see cref="UpdateFile(string)"/></item>
    /// <item><description><see cref="RenameFile(string,string)"/></description></item>
    /// </list>
    /// <para>When the archive is done processing the file, it raises its own <see cref="AbstractArchive.FileChanged">event</see></para>
    /// </summary>
    public abstract class AbstractFileMonitor : IDisposable {
        private AbstractArchive defaultArchive;
        private HashSet<AbstractArchive> registeredArchives;
        private Dictionary<string, AbstractArchive> archiveMap;

        /// <summary>
        /// Creates a new AbstractFileMonitor with the default archive and a collection of non-default archives that should be registered
        /// </summary>
        /// <param name="baseDirectory">The folder where this monitor stores it archives</param>
        /// <param name="defaultArchive">The default archive</param>
        /// <param name="otherArchives">A list of other archives that should be registered via <see cref="RegisterNonDefaultArchive(AbstractArchive)"/></param>
        public AbstractFileMonitor(string baseDirectory, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : this(baseDirectory, defaultArchive) {
                foreach(var archive in otherArchives) {
                    this.RegisterNonDefaultArchive(archive);
                }
        }
        /// <summary>
        /// Creates a new AbstractFileMonitor with the default archive
        /// </summary>
        /// <param name="baseDirectory">The folder where this monitor stores its archives</param>
        /// <param name="defaultArchive">The default archive</param>
        public AbstractFileMonitor(string baseDirectory, AbstractArchive defaultArchive) {
            this.MonitorStoragePath = baseDirectory;
            this.registeredArchives = new HashSet<AbstractArchive>();
            this.archiveMap = new Dictionary<string, AbstractArchive>(StringComparer.InvariantCultureIgnoreCase);
            this.registeredArchives.Add(defaultArchive);
            this.defaultArchive = defaultArchive;
            this.defaultArchive.FileChanged += RespondToArchiveFileEvent;
        }

        /// <summary>
        /// The folder where all of the archives can store their data. <see cref="AbstractArchive"/> objects can use this as their root folder
        /// </summary>
        public string MonitorStoragePath { get; protected set; }

        /// <summary>
        /// Event fires when any of the archives raises their <see cref="AbstractArchive.FileChanged"/>.
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileChanged;

        /// <summary>
        /// Event fires when <see cref="Startup()"/> is completed
        /// </summary>
        public event EventHandler StartupCompleted;

        /// <summary>
        /// Event fires when <see cref="StopMonitoring()"/> is completed
        /// </summary>
        public event EventHandler MonitoringStopped;

        /// <summary>
        /// Gets the list of source files from the object being monitored
        /// </summary>
        /// <returns>An enumerable of files to be monitored</returns>
        public abstract Collection<string> GetFilesFromSource();

        /// <summary>
        /// Gets the list of files already present in this archive
        /// </summary>
        /// <returns>An enumerable of files present in all of the archives</returns>
        public virtual IEnumerable<string> GetArchivedFiles() {
            var archivedFiles = from archive in registeredArchives
                                from filePath in archive.GetFiles()
                                select filePath;
            return archivedFiles;
        }
        
        /// <summary>
        /// Registers an archive in the file monitor. All file changes will be automatically routed to the appropriate archive
        /// based on file extension (via <see cref="AbstractArchive.SupportedExtensions"/>
        /// </summary>
        /// <param name="archive">the archive to add.</param>
        public void RegisterNonDefaultArchive(AbstractArchive archive) {
            registeredArchives.Add(archive);
            archive.FileChanged += RespondToArchiveFileEvent;
            foreach(var extension in archive.SupportedExtensions) {
                if(archiveMap.ContainsKey(extension)) {
                    SrcMLFileLogger.DefaultLogger.WarnFormat("AbstractFileMonitor.RegisterNonDefaultArchive() - Archive already registered for extension {0}, will be replaced with the new archive.", extension);
                }
                archiveMap[extension] = archive;
            }
        }

        /// <summary>
        /// Raises the <see cref="FileChanged"/> event.
        /// </summary>
        /// <param name="sender">The caller</param>
        /// <param name="e">The event arguments</param>
        protected virtual void RespondToArchiveFileEvent(object sender, FileEventRaisedArgs e) {
            //SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.RespondToArchiveFileEvent() type = " + e.EventType + ", file = " + e.FilePath + ", oldfile = " + e.OldFilePath + ", HasSrcML = " + e.HasSrcML);
            FileInfo fi = new FileInfo(e.FilePath);
            OnFileChanged(e);
        }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public abstract void StartMonitoring();

        /// <summary>
        /// Stops monitoring. Also calls <see cref="Dispose()"/>
        /// </summary>
        public virtual void StopMonitoring() {
            Dispose();

            OnMonitoringStopped(new EventArgs());
        }

        /// <summary>
        /// Processes a file addition by adding the file to the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to add</param>
        public void AddFile(string filePath) {
            this.GetArchiveForFile(filePath).AddOrUpdateFile(filePath);
        }

        /// <summary>
        /// Processes a file deletion by deleting the file from the appropriate archive
        /// </summary>
        /// <param name="filePath">The file to delete</param>
        public void DeleteFile(string filePath) {
            this.GetArchiveForFile(filePath).DeleteFile(filePath);
        }

        /// <summary>
        /// Processes a file update by updating the file in the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to update</param>
        public void UpdateFile(string filePath) {
            this.GetArchiveForFile(filePath).AddOrUpdateFile(filePath);
        }

        /// <summary>
        /// Processes a file rename. If the old and new path are both in the same archive,
        /// a <see cref="AbstractArchive.FileRename(string,string)"/> is called on the appropriate archive.
        /// If they are in different archives, the <see cref="AbstractArchive.DeleteFile(string)"/> is called on <paramref name="oldFilePath"/>
        /// and <see cref="AbstractArchive.AddOrUpdateFile(string)"/> is called on <paramref name="newFilePath"/>
        /// </summary>
        /// <param name="oldFilePath">the old file name</param>
        /// <param name="newFilePath">the new file name</param>
        public void RenameFile(string oldFilePath, string newFilePath) {
            var oldArchive = GetArchiveForFile(oldFilePath);
            var newArchive = GetArchiveForFile(newFilePath);

            if(!oldArchive.Equals(newArchive)) {
                oldArchive.DeleteFile(oldFilePath);
                newArchive.AddOrUpdateFile(newFilePath);
            } else {
                oldArchive.RenameFile(oldFilePath, newFilePath);
            }
        }

        /// <summary>
        /// Synchronizes the archives with the object being monitored. Startup adds or updates outdated archive files and deletes archive files that are
        /// no longer present on disk.
        /// </summary>
        public virtual void Startup() {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Startup()");

            // make a hashset of all the files to monitor
            var monitoredFiles = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach(var filePath in GetFilesFromSource()) {
                monitoredFiles.Add(filePath);
            }

            // find all the files in the hashset that require updating
            var outdatedFiles = from filePath in monitoredFiles
                                where GetArchiveForFile(filePath).IsOutdated(filePath)
                                select filePath;

            // update the outdated files
            foreach(var filePath in outdatedFiles) {
                try {
                    AddFile(filePath);
                } catch(Exception) {
                    // TODO log exception
                }
            }

            // find all the files to delete (files in the archive that are not in
            // the list of files to monitor
            var filesToDelete = from archive in registeredArchives
                                from filePath in archive.GetFiles()
                                where !monitoredFiles.Contains(filePath)
                                select new { 
                                    Archive = archive,
                                    FilePath = filePath,
                                };

            // delete the extra files from the archive
            foreach(var data in filesToDelete) {
                try {
                    data.Archive.DeleteFile(data.FilePath);
                } catch(Exception) {
                    // TODO log exception
                }
            }
            OnStartupCompleted(new EventArgs());
        }

        /// <summary>
        /// Synchronizes the archives with the object being monitored. Startup adds or updates outdated archive files and deletes archive files that are
        /// no longer present on disk.
        /// </summary>
        public virtual void Startup_Concurrent() {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Startup()");

            // make a hashset of all the files to monitor
            var monitoredFiles = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach(var filePath in GetFilesFromSource()) {
                monitoredFiles.Add(filePath);
            }

            // find all the files in the hashset that require updating
            var outdatedFiles = from filePath in monitoredFiles
                                where GetArchiveForFile(filePath).IsOutdated(filePath)
                                select filePath;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            ConcurrentQueue<string> missedFiles = new ConcurrentQueue<string>();

            Parallel.ForEach(outdatedFiles, currentFile => {
                string filePath = currentFile;
                try {
                    AddFile(filePath);
                } catch(Exception e) {
                    //Trace.WriteLine(fileName + " " + e.Message);
                    missedFiles.Enqueue(filePath);
                }
            });

            Task.WaitAll();

            //As a remedial action, regenerate the file missed in the last step
            if(missedFiles.Count > 0) {
                foreach(string fileName in missedFiles)
                    try {
                        AddFile(fileName);
                    } catch(Exception e) {
                        //Log exception
                    }
            }

            sw.Stop();
            Console.WriteLine("Concurrently generating SrcML files: " + sw.Elapsed);

            // find all the files to delete (files in the archive that are not in
            // the list of files to monitor
            var filesToDelete = from archive in registeredArchives
                                from filePath in archive.GetFiles()
                                where !monitoredFiles.Contains(filePath)
                                select new {
                                    Archive = archive,
                                    FilePath = filePath,
                                };

            // delete the extra files from the archive
            foreach(var data in filesToDelete) {
                try {
                    data.Archive.DeleteFile(data.FilePath);
                } catch(Exception) {
                    // TODO log exception
                }
            }
            OnStartupCompleted(new EventArgs());
        }

        /// <summary>
        /// disposes of all of the archives and stops the events
        /// </summary>
        public void Dispose() {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Dispose()");
            StartupCompleted = null;
            FileChanged = null;
            foreach(var archive in registeredArchives) {
                archive.Dispose();
            }
        }

        /// <summary>
        /// event handler for <see cref="FileChanged"/>
        /// </summary>
        /// <param name="e">event arguments</param>
        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// event handler for <see cref="StartupCompleted"/>
        /// </summary>
        /// <param name="e">null event</param>
        protected virtual void OnStartupCompleted(EventArgs e) {
            EventHandler handler = StartupCompleted;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// event handler for <see cref="MonitoringStopped"/>
        /// </summary>
        /// <param name="e">null event</param>
        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Gets the appropriate archive for string this file name (based on <see cref="Path.GetExtension(string)"/>
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>The archive that should contain this file name</returns>
        private AbstractArchive GetArchiveForFile(string fileName) {
            if(null == fileName) throw new ArgumentNullException("fileName");

            AbstractArchive selectedArchive = null;
            var extension = Path.GetExtension(fileName);

            if(!this.archiveMap.TryGetValue(extension, out selectedArchive)) {
                selectedArchive = defaultArchive;
            }
            return selectedArchive;
        }
    }
}
