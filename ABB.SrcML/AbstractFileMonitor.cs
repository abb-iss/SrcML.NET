/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Utilities;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML {

    /// <summary>
    /// <para>Represents an abstract file monitor. This class contains archives for storing various
    /// file types To start using it, you first instantiate it with a
    /// <see cref="AbstractArchive">default archive</see>. You then call
    /// <see cref="RegisterArchive"/> for each alternative archive. This class automatically routes
    /// files to the appropriate archive.</para> <para>You begin monitoring by calling
    /// <see cref="StartMonitoring"/>. <see cref="StartMonitoring"/> should subscribe to any events
    /// and then call functions to respond to those events:</para> <list type="bullet">
    /// <item><description><see cref="AddFile(string)"/></description></item>
    /// <item><description><see cref="DeleteFile(string)"/></description></item>
    /// <item><description></description><see cref="UpdateFile(string)"/></item>
    /// <item><description><see cref="RenameFile(string,string)"/></description></item> </list>
    /// <para>When the archive is done processing the file, it raises its own
    /// <see cref="AbstractArchive.FileChanged">event</see></para>
    /// </summary>
    public abstract class AbstractFileMonitor : IDisposable {
        private Dictionary<string, AbstractArchive> archiveMap;
        private AbstractArchive defaultArchive;
        private int numberOfWorkingArchives;
        private ReadyNotifier ReadyState;
        private HashSet<AbstractArchive> registeredArchives;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory"></param>
        protected AbstractFileMonitor(TaskScheduler scheduler, string baseDirectory) :
            this(scheduler, baseDirectory, null) { }

        /// <summary>
        /// Creates a new AbstractFileMonitor with the default archive and a collection of
        /// non-default archives that should be registered
        /// </summary>
        /// <param name="baseDirectory">The folder where this monitor stores it archives</param>
        /// <param name="defaultArchive">The default archive</param>
        /// <param name="otherArchives">A list of other archives that should be registered via see
        /// cref="RegisterArchive(IArchive)"/></param>
        protected AbstractFileMonitor(string baseDirectory, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives) {
            this.MonitorStoragePath = baseDirectory;
            this.registeredArchives = new HashSet<AbstractArchive>();
            this.archiveMap = new Dictionary<string, AbstractArchive>(StringComparer.OrdinalIgnoreCase);
            this.ReadyState = new ReadyNotifier(this);
            this.numberOfWorkingArchives = 0;
            Factory = Task.Factory;

            if(null != defaultArchive) {
                RegisterArchive(defaultArchive, true);
            }
            
            foreach(var archive in otherArchives) {
                this.RegisterArchive(archive, false);
            }
        }

        /// <summary>
        /// Creates a new AbstractFileMonitor with the default archive and a collection of
        /// non-default archives that should be registered
        /// </summary>
        /// <param name="baseDirectory">The folder where this monitor stores it archives</param>
        /// <param name="defaultArchive">The default archive</param>
        /// <param name="otherArchives">A list of other archives that should be registered via see
        /// cref="RegisterArchive(IArchive)"/></param>
        protected AbstractFileMonitor(TaskScheduler scheduler, string baseDirectory, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives) {
            this.MonitorStoragePath = baseDirectory;
            this.registeredArchives = new HashSet<AbstractArchive>();
            this.archiveMap = new Dictionary<string, AbstractArchive>(StringComparer.OrdinalIgnoreCase);
            this.ReadyState = new ReadyNotifier(this);
            this.numberOfWorkingArchives = 0;
            Factory = new TaskFactory(scheduler);

            if(null != defaultArchive) {
                RegisterArchive(defaultArchive, true);
            }
            
            foreach(var archive in otherArchives) {
                this.RegisterArchive(archive, false);
            }
        }

        /// <summary>
        /// Calls <see cref="Dispose(bool)"/> with false as the argument in case disposal hasn't
        /// already been done.
        /// </summary>
        ~AbstractFileMonitor() {
            Dispose(false);
        }

        /// <summary>
        /// Event fires when any of the archives raises their
        /// <see cref="AbstractArchive.FileChanged"/>.
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileChanged;

        /// <summary>
        /// Event fires when <see cref="StopMonitoring()"/> is completed
        /// </summary>
        public event EventHandler MonitoringStopped;

        public event EventHandler UpdateArchivesStarted;
        
        public event EventHandler UpdateArchivesCompleted;

        /// <summary>
        /// The folder where all of the archives can store their data. <see cref="AbstractArchive"/>
        /// objects can use this as their root folder
        /// </summary>
        public string MonitorStoragePath { get; protected set; }

        /// <summary>
        /// Number of the elements in the returned collection from GetFilesFromSource()
        /// </summary>
        public int NumberOfAllMonitoredFiles { get; protected set; }

        public bool UpdateArchivesRunning { get; protected set; }

        protected TaskFactory Factory { get; private set; }

        /// <summary>
        /// Processes a file addition by adding the file to the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to add</param>
        public void AddFile(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                archive.AddOrUpdateFile(filePath);
            }
        }

        /// <summary>
        /// Processes a file addition by adding the file to the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to add</param>
        public Task AddFileAsync(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                return archive.AddOrUpdateFileAsync(filePath);
            }
            return null;
        }

        /// <summary>
        /// Processes a file deletion by deleting the file from the appropriate archive
        /// </summary>
        /// <param name="filePath">The file to delete</param>
        public void DeleteFile(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                archive.DeleteFile(filePath);
            }
        }

        /// <summary>
        /// Processes a file deletion by deleting the file from the appropriate archive
        /// </summary>
        /// <param name="filePath">The file to delete</param>
        public Task DeleteFileAsync(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                return archive.DeleteFileAsync(filePath);
            }
            return null;
        }
        /// <summary>
        /// disposes of all of the archives and stops the events
        /// </summary>
        public void Dispose() {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Dispose()");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns an enumerable of all the files monitored by this monitor
        /// </summary>
        /// <returns>An enumerable of monitored files</returns>
        public abstract IEnumerable<string> EnumerateMonitoredFiles();

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
        /// Gets the list of source files from the object being monitored
        /// </summary>
        /// <returns>An enumerable of files to be monitored</returns>
        public virtual Collection<string> GetFilesFromSource() {
            return new Collection<string>(EnumerateMonitoredFiles().ToList());
        }

        /// <summary>
        /// Registers an archive in the file monitor. All file changes will be automatically routed
        /// to the appropriate archive based on file extension (via
        /// <see cref="AbstractArchive.SupportedExtensions"/>
        /// </summary>
        /// <param name="archive">the archive to add.</param>
        /// <param name="isDefault">whether or not to use this archive as the default
        /// archive</param>
        public void RegisterArchive(AbstractArchive archive, bool isDefault) {
            this.registeredArchives.Add(archive);
            archive.FileChanged += RespondToArchiveFileEvent;
            if(isDefault) {
                this.defaultArchive = archive;
            } else {
                foreach(var extension in archive.SupportedExtensions) {
                    if(archiveMap.ContainsKey(extension)) {
                        SrcMLFileLogger.DefaultLogger.WarnFormat("AbstractFileMonitor.RegisterNonDefaultArchive() - Archive already registered for extension {0}, will be replaced with the new archive.", extension);
                    }
                    archiveMap[extension] = archive;
                }
            }
        }

        /// <summary>
        /// Processes a file rename. If the old and new path are both in the same archive, a
        /// <see cref="AbstractArchive.RenameFile(string,string)"/> is called on the appropriate
        /// archive. If they are in different archives, the
        /// <see cref="AbstractArchive.DeleteFile(string)"/> is called on
        /// <paramref name="oldFilePath"/>and <see cref="AbstractArchive.AddOrUpdateFile(string)"/>
        /// is called on
        /// <paramref name="newFilePath"/></summary>
        /// <param name="oldFilePath">the old file name</param>
        /// <param name="newFilePath">the new file name</param>
        public void RenameFile(string oldFilePath, string newFilePath) {
            var oldArchive = GetArchiveForFile(oldFilePath);
            var newArchive = GetArchiveForFile(newFilePath);

            if(null != oldArchive && oldArchive.Equals(newArchive)) {
                oldArchive.RenameFile(oldFilePath, newFilePath);
            } else if(null != oldArchive){
                oldArchive.DeleteFile(oldFilePath);
                if(null != newArchive) {
                    newArchive.AddOrUpdateFile(newFilePath);
                }
            }
        }

        /// <summary>
        /// Processes a file rename. If the old and new path are both in the same archive, a
        /// <see cref="AbstractArchive.RenameFile(string,string)"/> is called on the appropriate
        /// archive. If they are in different archives, the
        /// <see cref="AbstractArchive.DeleteFile(string)"/> is called on
        /// <paramref name="oldFilePath"/>and <see cref="AbstractArchive.AddOrUpdateFile(string)"/>
        /// is called on
        /// <paramref name="newFilePath"/></summary>
        /// <param name="oldFilePath">the old file name</param>
        /// <param name="newFilePath">the new file name</param>
        public Task RenameFileAsync(string oldFilePath, string newFilePath) {
            var oldArchive = GetArchiveForFile(oldFilePath);
            var newArchive = GetArchiveForFile(newFilePath);

            if(null != oldArchive && oldArchive.Equals(newArchive)) {
                return oldArchive.RenameFileAsync(oldFilePath, newFilePath);
            } else if(null != oldArchive) {
                return oldArchive.DeleteFileAsync(oldFilePath).ContinueWith((t) => {
                    if(null != newArchive) {
                        newArchive.AddOrUpdateFile(newFilePath);
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
            return null;
        }

        public virtual void Save() {
            foreach(var archive in registeredArchives) {
                archive.Save();
            }
        }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public abstract void StartMonitoring();

        /// <summary>
        /// For Sando, add degree of parallelism Synchronizes the archives with the object being
        /// monitored. Startup adds or updates outdated archive files and deletes archive files that
        /// are no longer present on disk.
        /// </summary>
        public virtual void Startup_Concurrent(int degreeOfParallelism) {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Startup()");

            // make a hashset of all the files to monitor
            var monitoredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
            ParallelOptions option = new ParallelOptions();
            //number of threads, for Sando application, 2 is the best trade-off
            option.MaxDegreeOfParallelism = degreeOfParallelism;

            Parallel.ForEach(outdatedFiles, option, currentFile => {
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

            // find all the files to delete (files in the archive that are not in the list of files
            // to monitor
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
        }

        public virtual void Startup_Concurrent() {
            SrcMLFileLogger.DefaultLogger.Info("AbstractFileMonitor.Startup()");

            // make a hashset of all the files to monitor
            var monitoredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            // find all the files to delete (files in the archive that are not in the list of files
            // to monitor
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
        }

        /// <summary>
        /// Stops monitoring. Also calls <see cref="Dispose()"/>
        /// </summary>
        public virtual void StopMonitoring() {
            OnMonitoringStopped(new EventArgs());
        }

        public void UpdateArchives() {
            OnUpdateArchivesStarted(new EventArgs());
            var monitoredFiles = new HashSet<string>(GetFilesFromSource(), StringComparer.OrdinalIgnoreCase);

            var outdatedFiles = from filePath in monitoredFiles
                                let archive = GetArchiveForFile(filePath)
                                where null != archive && archive.IsOutdated(filePath)
                                select filePath;

            var deletedFiles = from filePath in GetArchivedFiles()
                               let archive = GetArchiveForFile(filePath)
                               where null != archive && null != filePath && !monitoredFiles.Contains(filePath)
                               select filePath;

            foreach(var filePath in outdatedFiles) {
                UpdateFile(filePath);
            }

            foreach(var filePath in deletedFiles) {
                DeleteFile(filePath);
            }
            OnUpdateArchivesCompleted(new EventArgs());
        }

        public Task UpdateArchivesAsync() {
            var task = Factory.StartNew(() => {
                OnUpdateArchivesStarted(new EventArgs());
                var monitoredFiles = new HashSet<string>(GetFilesFromSource(), StringComparer.OrdinalIgnoreCase);

                var outdatedFileTasks = from filePath in monitoredFiles
                                        let archive = GetArchiveForFile(filePath)
                                        where null != archive && archive.IsOutdated(filePath)
                                        select UpdateFileAsync(filePath);
                Task.WaitAll(outdatedFileTasks.ToArray());

                var deletedFileTasks = from filePath in GetArchivedFiles()
                                       let archive = GetArchiveForFile(filePath)
                                       where null != archive && null != filePath && !monitoredFiles.Contains(filePath)
                                       select DeleteFileAsync(filePath);

                Task.WaitAll(deletedFileTasks.ToArray());
                OnUpdateArchivesCompleted(new EventArgs());
            });
            return task;
        }

        /// <summary>
        /// Processes a file update by updating the file in the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to update</param>
        public void UpdateFile(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                archive.AddOrUpdateFile(filePath);
            }
        }

        /// <summary>
        /// Processes a file update by updating the file in the appropriate archive
        /// </summary>
        /// <param name="filePath">the file to update</param>
        public Task UpdateFileAsync(string filePath) {
            var archive = this.GetArchiveForFile(filePath);
            if(null != archive) {
                return archive.AddOrUpdateFileAsync(filePath);
            }
            return null;
        }
        /// <summary>
        /// Sets the published events to null and calls Dispose on the registered archives if
        /// <paramref name="disposing"/>is true.
        /// </summary>
        /// <param name="disposing">Causes this method to dispose of the registered archives</param>
        protected virtual void Dispose(bool disposing) {
            if(disposing) {
                foreach(var archive in registeredArchives) {
                    archive.Dispose();
                }
            }
            FileChanged = null;
        }

        /// <summary>
        /// Gets the appropriate archive for string this file name (based on
        /// <see cref="Path.GetExtension(string)"/>
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>The archive that should contain this file name</returns>
        protected AbstractArchive GetArchiveForFile(string fileName) {
            if(null == fileName)
                throw new ArgumentNullException("fileName");

            AbstractArchive selectedArchive;
            var extension = Path.GetExtension(fileName);

            if(!this.archiveMap.TryGetValue(extension, out selectedArchive)) {
                selectedArchive = defaultArchive;
            }
            return selectedArchive;
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
        /// event handler for <see cref="MonitoringStopped"/>
        /// </summary>
        /// <param name="e">null event</param>
        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateArchivesCompleted(EventArgs e) {
            EventHandler handler = UpdateArchivesCompleted;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateArchivesStarted(EventArgs e) {
            EventHandler handler = UpdateArchivesStarted;
            if(handler != null) {
                handler(this, e);
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
    }
}