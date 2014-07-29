/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML {
    /// <summary>
    /// The SrcML Project object creates an <see cref="AbstractFileMonitor"/>, <see cref="NonSourceArchive"/>,  <see cref="SourceArchive"/> and wires them all up".
    /// </summary>
    public class SrcMLProject : IDisposable {
        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="baseDirectory">The directory to store the data in</param>
        /// <param name="monitoredDirectory">The directory to monitor</param>
        /// <param name="srcMLBinaryDirectory">The directory that contains <c>src2srcml.exe</c></param>
        public SrcMLProject(string baseDirectory, string monitoredDirectory, string srcMLBinaryDirectory)
            : this(TaskScheduler.Default, new FileSystemFolderMonitor(monitoredDirectory, baseDirectory), new SrcMLGenerator(srcMLBinaryDirectory)) { }

        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="scheduler">The task scheduler</param>
        /// <param name="monitor">The file monitor</param>
        ///<param name="generator">The SrcML generator to use</param>
        public SrcMLProject(TaskScheduler scheduler, AbstractFileMonitor monitor, SrcMLGenerator generator) {
            Scheduler = scheduler;
            Monitor = monitor;
            SetupMonitor(generator);
            SourceArchive.Generator.IsLoggingErrors = true;
            SourceArchive.Generator.ErrorLog = new StreamWriter(Path.Combine(StoragePath, "error.log"), false);
        }

        /// <summary>
        /// The stream to write errors to. If null, no errors are written
        /// </summary>
        public virtual TextWriter ErrorLog {
            get { return SourceArchive.Generator.ErrorLog; }
            set { SourceArchive.Generator.ErrorLog = value; }
        }

        /// <summary>
        /// Whether or not to log exceptions. If this is false, then exceptions will be thrown. If true, they will be caught and logged to <see cref="ErrorLog"/>.
        /// </summary>
        public virtual bool IsLoggingErrors {
            get { return SourceArchive.Generator.IsLoggingErrors; }
            set { SourceArchive.Generator.IsLoggingErrors = value; }
        }

        /// <summary>
        /// The link between the project and the project files
        /// </summary>
        public AbstractFileMonitor Monitor { get; private set; }

        /// <summary>
        /// The archive that stores non-source files
        /// </summary>
        public LastModifiedArchive NonSourceArchive { get; private set; }

        /// <summary>
        /// The archive for srcML
        /// </summary>
        public SrcMLArchive SourceArchive { get; private set; }

        /// <summary>
        /// The scheduler to use for this monitor
        /// </summary>
        protected TaskScheduler Scheduler { get; private set; }

        /// <summary>
        /// The path to store all of the data for this project
        /// </summary>
        public string StoragePath { get { return Monitor.MonitorStoragePath; } }

        /// <summary>
        /// Wires all of the properties together
        /// </summary>
        protected void SetupMonitor(SrcMLGenerator generator) {
            // setup the file monitor
            NonSourceArchive = new LastModifiedArchive(StoragePath, LastModifiedArchive.DEFAULT_FILENAME, Scheduler);
            var archiveDirectory = Path.Combine(StoragePath, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY);
            SourceArchive = new SrcMLArchive(StoragePath, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY, true, generator, new SrcMLFileNameMapping(archiveDirectory), Scheduler);
            Monitor.RegisterArchive(NonSourceArchive, true);
            Monitor.RegisterArchive(SourceArchive, false);
        }

        /// <summary>
        /// Updates the archives based on <see cref="Monitor"/>
        /// </summary>
        public virtual void Update() {
            Monitor.UpdateArchives();
        }

        /// <summary>
        /// Updates the archives asynchronously
        /// </summary>
        /// <returns>The update task</returns>
        public virtual Task UpdateAsync() {
            return Monitor.UpdateArchivesAsync();
        }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public virtual void StartMonitoring() {
            Monitor.StartMonitoring();
        }

        /// <summary>
        /// Stops monitoring
        /// </summary>
        public virtual void StopMonitoring() {
            Monitor.StopMonitoring();
        }
        
        /// <summary>
        /// Dispose of this SrcML Project object
        /// </summary>
        public void Dispose() {
            StopMonitoring();
            SourceArchive.Save();
            NonSourceArchive.Save();
            if(null != SourceArchive.Generator.ErrorLog) {
                SourceArchive.Generator.ErrorLog.Dispose();
            }
            Monitor.Dispose();
        }
    }
}
