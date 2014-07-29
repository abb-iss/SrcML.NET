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
    public class SrcMLProject : IDisposable {
        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="baseDirectory">The directory to store the data in</param>
        /// <param name="monitoredDirectory">The directory to monitor</param>
        /// <param name="srcMLBinaryDirectory">The directory that contains src2srcml.exe</param>
        public SrcMLProject(string baseDirectory, string monitoredDirectory, string srcMLBinaryDirectory)
            : this(TaskScheduler.Default, new FileSystemFolderMonitor(monitoredDirectory, baseDirectory), new SrcMLGenerator(srcMLBinaryDirectory)) { }

        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="scheduler">The task scheduler</param>
        /// <param name="monitor">The file monitor</param>
        /// <param name="workingSet"></param>
        public SrcMLProject(TaskScheduler scheduler, AbstractFileMonitor monitor, SrcMLGenerator generator) {
            Scheduler = scheduler;
            Monitor = monitor;
            SetupMonitor(generator);
            SourceArchive.Generator.IsLoggingErrors = true;
            SourceArchive.Generator.ErrorLog = new StreamWriter(Path.Combine(Monitor.MonitorStoragePath, "error.log"), false);
        }

        public virtual TextWriter ErrorLog {
            get { return SourceArchive.Generator.ErrorLog; }
            set { SourceArchive.Generator.ErrorLog = value; }
        }

        public virtual bool IsLoggingErrors {
            get { return SourceArchive.Generator.IsLoggingErrors; }
            set { SourceArchive.Generator.IsLoggingErrors = value; }
        }
        /// <summary>
        /// The scheduler to use for this monitor
        /// </summary>
        protected TaskScheduler Scheduler { get; private set; }

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
        /// Wires all of the properties together
        /// </summary>
        protected void SetupMonitor(SrcMLGenerator generator) {
            // setup the file monitor
            NonSourceArchive = new LastModifiedArchive(Monitor.MonitorStoragePath, LastModifiedArchive.DEFAULT_FILENAME, Scheduler);
            var archiveDirectory = Path.Combine(Monitor.MonitorStoragePath, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY);
            SourceArchive = new SrcMLArchive(Monitor.MonitorStoragePath, SrcMLArchive.DEFAULT_ARCHIVE_DIRECTORY, true, generator, new SrcMLFileNameMapping(archiveDirectory), Scheduler);
            Monitor.RegisterArchive(NonSourceArchive, true);
            Monitor.RegisterArchive(SourceArchive, false);
        }

        public virtual void Update() {
            Monitor.UpdateArchives();
        }

        public virtual Task UpdateAsync() {
            return Monitor.UpdateArchivesAsync();
        }

        public virtual void StartMonitoring() {
            Monitor.StartMonitoring();
        }

        public virtual void StopMonitoring() {
            Monitor.StopMonitoring();
        }

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
