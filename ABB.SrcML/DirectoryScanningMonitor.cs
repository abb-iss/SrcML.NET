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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using Timer = System.Timers.Timer;

namespace ABB.SrcML {

    /// <summary>
    /// The directory scanning monitor scans a collection of directories every
    /// <see cref="ScanInterval" /> seconds for source changes and updates the appropriate
    /// <see cref="IArchive">archives</see>. <para>The directory scanning monitor lets you
    /// periodically scan a set of folders and </para>
    /// </summary>
    /// <remarks>
    /// The directory scanning monitor uses a <see cref="System.Timers.Timer"/> to periodically scan
    /// each directory in <see cref="MonitoredDirectories"/>. It first examines all of the archived
    /// files to identify files that have been deleted. Next, it checks the files
    /// </remarks>
    public class DirectoryScanningMonitor : AbstractFileMonitor {
        private const int IDLE = 0;
        private const int RUNNING = 1;
        private const int STOPPED = -1;
        private List<string> folders;
        private Timer ScanTimer;
        private int syncPoint;

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="foldersToMonitor">An initial list of
        /// <see cref="MonitoredDirectories">folders to /see></param>
        /// <param name="scanInterval">The <see cref="ScanInterval"/> in seconds</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(ICollection<string> foldersToMonitor, double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : base(baseDirectory, defaultArchive, otherArchives) {
            folders = new List<string>(foldersToMonitor.Count);
            folders.AddRange(foldersToMonitor);
            MonitoredDirectories = new ReadOnlyCollection<string>(folders);
            ScanTimer = new Timer();
            ScanInterval = scanInterval;
            ScanTimer.AutoReset = true;
            ScanTimer.Elapsed += ScanTimer_Elapsed;
            syncPoint = STOPPED;
        }

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="folderListFileName">An initial list of
        /// <see cref="MonitoredDirectories">folders to </see> monitor</param>
        /// <param name="scanInterval">The <see cref="ScanInterval"/> in seconds</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(string folderListFileName, double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(File.ReadAllLines(folderListFileName).ToList(), scanInterval, baseDirectory, defaultArchive, otherArchives) { }

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(new List<string>(), baseDirectory, defaultArchive, otherArchives) { }

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="foldersToMonitor">An initial list of
        /// <see cref="MonitoredDirectories">folders to </see> monitor</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(ICollection<string> foldersToMonitor, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(foldersToMonitor, 60, baseDirectory, defaultArchive, otherArchives) { }

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="folderListFileName">A file name with the initial list of
        /// <see cref="MonitoredDirectories">folders to /see> monitor</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(string folderListFileName, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(folderListFileName, 60, baseDirectory, defaultArchive, otherArchives) { }

        /// <summary>
        /// A read only collection of the directories being monitored. <para>Use
        /// <see cref="AddDirectory(string)"/> to add a directory and
        /// <see cref="RemoveDirectory(string)"/> to remove one.</para>
        /// </summary>
        public ReadOnlyCollection<string> MonitoredDirectories { get; private set; }

        /// <summary>
        /// The scan interval is the number of seconds between each scan. By default it is set to
        /// 60.
        /// </summary>
        public double ScanInterval {
            get { return ScanTimer.Interval / 1000; }
            set { ScanTimer.Interval = value * 1000; }
        }

        /// <summary>
        /// Add a folder to <see cref="MonitoredDirectories"/>
        /// </summary>
        /// <param name="directoryPath">The directory to start monitoring</param>
        /// <remarks>
        /// Throws a <see cref="DirectoryScanningMonitorSubDirectoryException"/> if
        /// <paramref name="directoryPath"/>is a subdirectory of an existing directory.
        /// </remarks>
        public void AddDirectory(string directoryPath) {
            var fullPath = Path.GetFullPath(directoryPath);
            foreach(var directory in MonitoredDirectories) {
                if(fullPath.StartsWith(directory, StringComparison.InvariantCultureIgnoreCase)) {
                    throw new DirectoryScanningMonitorSubDirectoryException(directoryPath, directory, this);
                }
            }

            folders.Add(Path.GetFullPath(directoryPath));
            if(ScanTimer.Enabled) {
                foreach(var fileName in EnumerateDirectory(directoryPath)) {
                    UpdateFile(fileName);
                }
            }
        }

        /// <summary>
        /// Returns an enumerable of all the files in
        /// <paramref name="directory"/>.
        /// </summary>
        /// <param name="directory">The directory to enumerate</param>
        /// <returns>An enumerable of the full file names in
        /// <paramref name="directory"/></returns>
        public IEnumerable<string> EnumerateDirectory(string directory) {
            var files = from filePath in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                        select Path.GetFullPath(filePath);
            return files;
        }

        /// <summary>
        /// Returns an enumerable of all the monitored files
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> EnumerateMonitoredFiles() {
            var monitoredFiles = from directory in MonitoredDirectories
                                 from fileName in EnumerateDirectory(directory)
                                 select fileName;
            return monitoredFiles;
        }

        /// <summary>
        /// Checks to see if
        /// <paramref name="fileName"/>is in any of the <see cref="MonitoredDirectories"/>.
        /// </summary>
        /// <param name="fileName">The file name to check</param>
        /// <returns>True if the file is in a <see cref="MonitoredDirectories">monitored
        /// directory</see>, false otherwise</returns>
        public bool IsMonitoringFile(string fileName) {
            var fullPath = Path.GetFullPath(fileName);
            return MonitoredDirectories.Any(d => fullPath.StartsWith(d, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Remove a directory from <see cref="MonitoredDirectories"/>. Files in this directory will
        /// be removed from all archives.
        /// </summary>
        /// <param name="directoryPath">The directory to remove</param>
        /// <remarks>
        /// If
        /// <paramref name="directoryPath"/>is not in <see cref="MonitoredDirectories"/> this method
        /// has no effect.
        /// </remarks>
        public void RemoveDirectory(string directoryPath) {
            bool isRunning = ScanTimer.Enabled;
            if(isRunning) {
                while(RUNNING == Interlocked.CompareExchange(ref syncPoint, RUNNING, IDLE)) {
                    Thread.Sleep(1);
                }
            }
            var directoryFullPath = Path.GetFullPath(directoryPath);

            if(folders.Contains(directoryFullPath, StringComparer.InvariantCultureIgnoreCase)) {
                folders.Remove(directoryFullPath);
                foreach(var fileName in GetArchivedFiles()) {
                    if(fileName.StartsWith(directoryFullPath, StringComparison.InvariantCultureIgnoreCase)) {
                        DeleteFile(fileName);
                    }
                }
            }

            if(isRunning) {
                syncPoint = IDLE;
            }
        }

        /// <summary>
        /// Start scanning <see cref="MonitoredDirectories">monitored directories</see> every
        /// <see cref="ScanInterval"/> seconds.
        /// </summary>
        /// <remarks>
        /// Has no effect if the monitor is already running.
        /// </remarks>
        public override void StartMonitoring() {
            if(STOPPED == Interlocked.CompareExchange(ref syncPoint, IDLE, STOPPED)) {
                ScanTimer.Start();
            }
        }

        /// <summary>
        /// Stop monitoring <see cref="MonitoredDirectories">monitored directories</see>.
        /// </summary>
        /// <remarks>
        /// Stops monitoring
        /// </remarks>
        public override void StopMonitoring() {
            if(ScanTimer.Enabled) {
                ScanTimer.Stop();

                while(RUNNING == Interlocked.CompareExchange(ref syncPoint, STOPPED, IDLE)) {
                    Thread.Sleep(1);
                }
                base.StopMonitoring();
            }
        }

        /// <summary>
        /// Writes the current list of <see cref="MonitoredDirectories"/> to
        /// <paramref name="fileName"/></summary>
        /// <param name="fileName">The file name to write the list of directories to</param>
        public void WriteMonitoringList(string fileName) {
            File.WriteAllLines(fileName, MonitoredDirectories);
        }

        /// <summary>
        /// Runs whenever the built-in timer expires.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// This function executes if it is not already running (from a previous event) and
        /// <see cref="StopMonitoring()"/> hasn't been called.
        /// </remarks>
        private void ScanTimer_Elapsed(object sender, ElapsedEventArgs e) {
            int sync = Interlocked.CompareExchange(ref syncPoint, RUNNING, IDLE);
            if(IDLE == sync) {
                UpdateArchives();
                syncPoint = IDLE;
            }
        }
    }
}