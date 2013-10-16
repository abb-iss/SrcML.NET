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
        public const int DEFAULT_SCAN_INTERVAL = 60;
        private const int IDLE = 0;
        private const string MONITOR_LIST_FILENAME = "monitored_directories.txt";
        private const int RUNNING = 1;
        private const int STOPPED = -1;

        private static HashSet<string> Exclusions = new HashSet<string>(new List<string>() {
            "bin", "obj", "TestResults"
        }, StringComparer.InvariantCultureIgnoreCase);

        private List<string> folders;
        private Timer ScanTimer;
        private int syncPoint;

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="monitorFileName">The file name to save the list of monitored directories
        /// to</param>
        /// <param name="scanInterval">The <see cref="ScanInterval"/> in seconds</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(string monitorFileName, double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : base(baseDirectory, defaultArchive, otherArchives) {
            MonitoredDirectoriesFilePath = Path.Combine(baseDirectory, monitorFileName);
            folders = new List<string>();
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
        /// <param name="foldersToMonitor">An initial list of
        /// <see cref="MonitoredDirectories">folders to /see></param>
        /// <param name="scanInterval">The <see cref="ScanInterval"/> in seconds</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(MONITOR_LIST_FILENAME, scanInterval, baseDirectory, defaultArchive, otherArchives) { }

        /// <summary>
        /// Create a new directory scanning monitor
        /// </summary>
        /// <param name="foldersToMonitor">An initial list of
        /// <see cref="MonitoredDirectories">folders to </see> monitor</param>
        /// <param name="baseDirectory">The base directory to use for the archives of this
        /// monitor</param>
        /// <param name="defaultArchive">The default archive to use</param>
        /// <param name="otherArchives">Other archives for specific extensions</param>
        public DirectoryScanningMonitor(string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : this(MONITOR_LIST_FILENAME, DEFAULT_SCAN_INTERVAL, baseDirectory, defaultArchive, otherArchives) { }

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
        /// the file path to save the list of directories to when <see cref="WriteMonitoringList"/>
        /// is called.
        /// </summary>
        protected string MonitoredDirectoriesFilePath { get; private set; }

        /// <summary>
        /// Loads the list of monitored directories from <see cref="MonitoredDirectoriesFilePath"/>.
        /// </summary>
        public void AddDirectoriesFromSaveFile() {
            if(File.Exists(MonitoredDirectoriesFilePath)) {
                foreach(var folderPath in File.ReadAllLines(MonitoredDirectoriesFilePath)) {
                    AddDirectory(folderPath);
                }
            }
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
            // get the full path to the directory with any trailing path separators trimmed off
            var fullPath = GetFullPath(directoryPath);
            bool alreadyMonitoringDirectory = false;

            foreach(var directory in MonitoredDirectories) {
                if(fullPath.StartsWith(directory, StringComparison.InvariantCultureIgnoreCase)) {
                    // if full path starts with directory, then check to see if they
                    alreadyMonitoringDirectory = (fullPath.Length == directory.Length);
                    if(alreadyMonitoringDirectory) {
                        break;
                    }
                    throw new DirectoryScanningMonitorSubDirectoryException(directoryPath, directory, this);
                }
            }

            if(!alreadyMonitoringDirectory) {
                folders.Add(fullPath);
                if(ScanTimer.Enabled) {
                    foreach(var fileName in EnumerateDirectory(directoryPath)) {
                        UpdateFile(fileName);
                    }
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
            var dirName = Path.GetFileName(directory);
            bool startsWithDot = (dirName[0] == '.');
            bool isExcluded = Exclusions.Contains(dirName);

            if(!(startsWithDot || isExcluded)) {
                foreach(var dir in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly)) {
                    foreach(var filePath in EnumerateDirectory(dir)) {
                        yield return filePath;
                    }
                }

                var files = from filePath in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                            let fileName = Path.GetFileName(filePath)
                            where fileName[0] != '.'
                            select filePath;

                foreach(var filePath in files) {
                    yield return filePath;
                }
            }
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
            var fullPath = GetFullPath(fileName);
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
            var directoryFullPath = GetFullPath(directoryPath);

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
        public void WriteMonitoringList() {
            File.WriteAllLines(MonitoredDirectoriesFilePath, MonitoredDirectories);
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                WriteMonitoringList();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the full path for a given path. This calls
        /// <see cref="System.IO.Path.GetFullPath(string)"/> and then trims any
        /// <see cref="System.IO.Path.PathSeparator">path separators</see> off of the end.
        /// </summary>
        /// <param name="path">The path to get a full path for</param>
        /// <returns>The full path for
        /// <paramref name="path"/>with no trailing path separators</returns>
        private string GetFullPath(string path) {
            if(null == path)
                throw new ArgumentNullException("path");

            return Path.GetFullPath(path).TrimEnd(Path.PathSeparator);
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