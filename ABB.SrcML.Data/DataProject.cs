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

namespace ABB.SrcML.Data {
    /// <summary>
    /// The project class is a wrapper that automates the connections between <see cref="AbstractWorkingSet"/> objects, <see cref="DataArchive"/> objects, and <see cref="SrcMLArchive"/> objects.
    /// </summary>
    public class DataProject<TWorkingSet> : SrcMLProject where TWorkingSet : AbstractWorkingSet, new() {
        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="baseDirectory">The directory to store the data in</param>
        /// <param name="monitoredDirectory">The directory to monitor</param>
        /// <param name="srcMLBinaryDirectory">The directory with <c>src2srcml.exe</c></param>
        public DataProject(string baseDirectory, string monitoredDirectory, string srcMLBinaryDirectory)
            : base(baseDirectory, monitoredDirectory, srcMLBinaryDirectory) {
            Data = new DataArchive(baseDirectory, SourceArchive);
            SetupWorkingSet();
            Data.Generator.IsLoggingErrors = true;
            Data.Generator.ErrorLog = SourceArchive.Generator.ErrorLog;
        }

        /// <summary>
        /// Creates a new project object
        /// </summary>
        /// <param name="scheduler">The task scheduler</param>
        /// <param name="monitor">The file monitor</param>
        public DataProject(TaskScheduler scheduler, AbstractFileMonitor monitor) 
        : base(scheduler, monitor, new SrcMLGenerator()) {
            var fileMapPath = Path.Combine(Monitor.MonitorStoragePath, DataArchive.DEFAULT_ARCHIVE_DIRECTORY);
            Data = new DataArchive(Monitor.MonitorStoragePath, DataArchive.DEFAULT_ARCHIVE_DIRECTORY, true, SourceArchive, new DataGenerator(), new DataFileNameMapping(fileMapPath), scheduler);

            SetupWorkingSet();

            Data.Generator.IsLoggingErrors = true;
            Data.Generator.ErrorLog = SourceArchive.Generator.ErrorLog;
        }

        /// <summary>
        /// The archive for data
        /// </summary>
        public DataArchive Data { get; private set; }

        public override TextWriter ErrorLog {
            get { return base.ErrorLog; }
            set {
                base.ErrorLog = value;
                Data.Generator.ErrorLog = value;
            }
        }

        public override bool IsLoggingErrors {
            get { return base.IsLoggingErrors; }
            set {
                base.IsLoggingErrors = value;
                Data.Generator.IsLoggingErrors = value;
            }
        }

        /// <summary>
        /// Monitor to connect <see cref="SrcMLProject.SourceArchive"/> and <see cref="Data"/>
        /// </summary>
        protected ArchiveMonitor<SrcMLArchive> SourceArchiveMonitor { get; private set; }

        public TextWriter UnknownLog {
            get { return Data.Generator.UnknownLog; }
            set { Data.Generator.UnknownLog = value; }
        }

        /// <summary>
        /// The working set
        /// </summary>
        public TWorkingSet WorkingSet { get; private set; }

        protected void SetupWorkingSet() {
            WorkingSet = new TWorkingSet();
            WorkingSet.Factory = new TaskFactory(Scheduler);
            WorkingSet.Archive = Data;
            SourceArchiveMonitor = new ArchiveMonitor<SrcMLArchive>(Scheduler, Monitor.MonitorStoragePath, SourceArchive, Data);
        }
        /// <summary>
        /// Updates all of the components of this working set:
        /// <list type="number">
        /// <item><description>Updates the <see cref="SrcMLProject.SourceArchive"/> and <see cref="SrcMLProject.NonSourceArchive"/> objects based on <see cref="SrcMLProject.Monitor"/></description></item>
        /// <item><description>Updates the <see cref="Data"/> based on the <see cref="SrcMLProject.SourceArchive"/></description></item>
        /// <item><description>Updates the <see cref="Data"/> based on the <see cref="SrcMLProject.SourceArchive"/></description></item>
        /// </list>
        /// </summary>
        public override void Update() {
            base.Update();
            SourceArchiveMonitor.UpdateArchives();
            WorkingSet.Initialize();
        }

        /// <summary>
        /// Updates all of the components of the working set asynchronously. This launches three tasks. Each task depends on its predecessor:
        /// <list type="number">
        /// <item><description>Update the <see cref="SrcMLProject.SourceArchive"/> and the <see cref="SrcMLProject.NonSourceArchive"/> objects based on <see cref="SrcMLProject.Monitor"/></description></item>
        /// <item><description>Updates the <see cref="Data"/> based on the <see cref="SrcMLProject.SourceArchive"/></description></item>
        /// <item><description>Updates the <see cref="Data"/> based on the <see cref="SrcMLProject.SourceArchive"/></description></item>
        /// </list>
        /// </summary>
        /// <returns>The 3rd task</returns>
        public override Task UpdateAsync() {
            return Task.Factory.StartNew(() => base.UpdateAsync().Wait())
                .ContinueWith((t) => SourceArchiveMonitor.UpdateArchivesAsync().Wait(), TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWith((t) => WorkingSet.InitializeAsync().Wait(), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public void StartMonitoring() {
            WorkingSet.StartMonitoring();
            SourceArchiveMonitor.StartMonitoring();
            base.StartMonitoring();
        }

        /// <summary>
        /// Stops monitoring
        /// </summary>
        public void StopMonitoring() {
            base.StopMonitoring();
            SourceArchiveMonitor.StopMonitoring();
            WorkingSet.StopMonitoring();
        }
    }
}
