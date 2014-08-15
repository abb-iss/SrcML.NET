/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML {
    /// <summary>
    /// The archive monitor lets you use an archive as a monitoring source.
    /// </summary>
    /// <typeparam name="TArchive">The type of the archive to monitor</typeparam>
    public class ArchiveMonitor<TArchive> : AbstractFileMonitor where TArchive : AbstractArchive {
        /// <summary>
        /// The archive being monitored
        /// </summary>
        public TArchive MonitoredArchive { get; private set; }

        /// <summary>
        /// Create a new archive monitor
        /// </summary>
        /// <param name="baseDirectory">The base directory for this monitor</param>
        /// <param name="monitoredArchive">The archive to monitor (found in <see cref="MonitoredArchive"/>)</param>
        /// <param name="defaultArchive">The default archive to store data in</param>
        /// <param name="otherArchives">Other archives to store data in</param>
        public ArchiveMonitor(string baseDirectory, TArchive monitoredArchive, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : this(TaskScheduler.Default, baseDirectory, monitoredArchive, defaultArchive, otherArchives) { }

        /// <summary>
        /// Create a new archive monitor
        /// </summary>
        /// <param name="scheduler">The scheduler to use</param>
        /// <param name="baseDirectory">The base directory for this monitor</param>
        /// <param name="monitoredArchive">The archive to monitor (found in <see cref="MonitoredArchive"/>)</param>
        /// <param name="defaultArchive">The default archive to store data in</param>
        /// <param name="otherArchives">Other archives to store data in</param>
        public ArchiveMonitor(TaskScheduler scheduler, string baseDirectory, TArchive monitoredArchive, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : base(scheduler, baseDirectory, defaultArchive, otherArchives) {
            MonitoredArchive = monitoredArchive;
        }

        /// <summary>
        /// Start monitoring <see cref="MonitoredArchive"/> by subscribing to <see cref="AbstractArchive.FileChanged"/>
        /// </summary>
        public override void StartMonitoring() {
            MonitoredArchive.FileChanged += MonitoredArchive_FileChanged;
        }

        /// <summary>
        /// Stop monitoring <see cref="MonitoredArchive"/> by subscribing to <see cref="AbstractArchive.FileChanged"/>
        /// </summary>
        public override void StopMonitoring() {
            MonitoredArchive.FileChanged -= MonitoredArchive_FileChanged;
            base.StopMonitoring();
        }

        /// <summary>
        /// This is the method that is called whenever the <see cref="AbstractArchive.FileChanged"/> event from the <see cref="MonitoredArchive"/> is fired.
        /// </summary>
        /// <param name="sender">The sender (should be <see cref="MonitoredArchive"/>)</param>
        /// <param name="e">The file that has been changed</param>
        protected void MonitoredArchive_FileChanged(object sender, FileEventRaisedArgs e) {
            Task task = null;
            switch(e.EventType) {
                case FileEventType.FileAdded:
                    task = AddFileAsync(e.FilePath);
                    break;
                case FileEventType.FileChanged:
                    task = UpdateFileAsync(e.FilePath);
                    break;
                case FileEventType.FileDeleted:
                    task = DeleteFileAsync(e.FilePath);
                    break;
                case FileEventType.FileRenamed:
                    task = RenameFileAsync(e.OldFilePath, e.FilePath);
                    break;
            }
            task.ContinueWith((t) => OnFileChanged(e));
        }

        /// <summary>
        /// Gets the files kept in <paramref name="MonitoredArchive"/>
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> EnumerateMonitoredFiles() {
            return MonitoredArchive.GetFiles();
        }
    }
}
