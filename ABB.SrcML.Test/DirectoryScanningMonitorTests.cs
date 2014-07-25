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
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ABB.SrcML.Test {

    [TestFixture, Category("Build")]
    public class DirectoryScanningMonitorTests {
        private const string monitorFolder = "monitor";
        private const int numStartingFiles = 100;
        private const string testFolder = "test";
        private const int WaitInterval = 5000;
        #region test setup

        [TearDown]
        public void TestCleanup() {
            Directory.Delete(monitorFolder, true);
            Directory.Delete(testFolder, true);
        }

        [SetUp]
        public void TestSetup() {
            Directory.CreateDirectory(monitorFolder);
            Directory.CreateDirectory(testFolder);
            for(int i = 0; i < numStartingFiles; i++) {
                File.Create(Path.Combine(testFolder, String.Format("{0}.txt", i))).Close();
            }
        }

        #endregion test setup

        [Test]
        public void TestAddDuplicateDirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            AutoResetEvent are = new AutoResetEvent(false);
            monitor.DirectoryAdded += (o, e) => { are.Set(); };
            
            monitor.AddDirectory(testFolder);
            Assert.IsTrue(are.WaitOne(WaitInterval));

            monitor.AddDirectory(testFolder);
            Assert.IsFalse(are.WaitOne(WaitInterval));

            Assert.AreEqual(1, monitor.MonitoredDirectories.Count);
        }

        [Test, ExpectedException(ExpectedException = typeof(DirectoryScanningMonitorSubDirectoryException))]
        public void TestAddSubdirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            AutoResetEvent are = new AutoResetEvent(false);
            monitor.DirectoryAdded += (o, e) => are.Set();
            
            monitor.AddDirectory(testFolder);
            Assert.IsTrue(are.WaitOne(WaitInterval));
            monitor.AddDirectory(Path.Combine(testFolder, "test"));
            //Assert.IsFalse(are.WaitOne(WaitInterval));
        }

        [Test]
        public void TestAddSimilarDirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            AutoResetEvent are = new AutoResetEvent(false);
            monitor.DirectoryAdded += (o, e) => are.Set();

            monitor.AddDirectory(testFolder);
            Assert.IsTrue(are.WaitOne(WaitInterval));
            monitor.AddDirectory(testFolder + "NotSubDirectory");
            Assert.IsTrue(are.WaitOne(WaitInterval));
        }

        [Test]
        public void TestEmptyMonitor() {
            using(var monitor = new DirectoryScanningMonitor(monitorFolder, DirectoryScanningMonitor.DEFAULT_SCAN_INTERVAL)) {
                monitor.AddDirectory(testFolder);
                monitor.FileChanged += (o, e) => Assert.Fail(e.FilePath);
                monitor.UpdateArchives();
            }
        }

        [Test]
        public void TestExcludedDirectory() {
            var testExcludedDirectoryPath = Path.Combine(testFolder, "TestExcludedDirectory");
            var excludedFolders = new string[] {
                Path.Combine(testExcludedDirectoryPath, "TestResults"),
                Path.Combine(testExcludedDirectoryPath, "Backup"),
                Path.Combine(testExcludedDirectoryPath, "Backup1"),
                Path.Combine(testExcludedDirectoryPath, "backup111"),
                Path.Combine(testExcludedDirectoryPath, ".test")
            };

            foreach(var folder in excludedFolders) {
                Directory.CreateDirectory(folder);
                File.Create(Path.Combine(folder, "test.txt")).Close();
            }

            var archive = new LastModifiedArchive(monitorFolder);
            using(var monitor = new DirectoryScanningMonitor(monitorFolder, archive)) {
                monitor.AddDirectory(testExcludedDirectoryPath);
                Assert.AreEqual(0, monitor.GetFilesFromSource().Count);
            }
        }

        [Test]
        public void TestExcludedFiles() {
            var testDirectoryPath = Path.Combine(testFolder, "TestExcludedFiles");
            var exludedFiles = new string[] {
                Path.Combine(testDirectoryPath, ".test.txt"),
                Path.Combine(testDirectoryPath, "#test.txt"),
                Path.Combine(testDirectoryPath, "~autorecover.test.txt"),
                Path.Combine(testDirectoryPath, "~test.txt"),
            };
            
            Directory.CreateDirectory(testDirectoryPath);
            foreach(var filePath in exludedFiles) {
                File.Create(filePath).Close();
            }

            var archive = new LastModifiedArchive(monitorFolder);
            using(var monitor = new DirectoryScanningMonitor(monitorFolder, archive)) {
                monitor.AddDirectory(testDirectoryPath);
                Assert.AreEqual(0, monitor.GetFilesFromSource().Count);
            }
        }
        [Test, ExpectedException(ExpectedException=typeof(ForbiddenDirectoryException))]
        public void TestForbiddenDirectory() {
            var forbiddenDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
            var archive = new LastModifiedArchive(monitorFolder);
            var monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.AddDirectory(forbiddenDirectory);
        }
        [Test]
        public void TestFileChanges() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.ScanInterval = 1;
            monitor.AddDirectory(testFolder);
            monitor.UpdateArchives();

            AutoResetEvent are = new AutoResetEvent(false);
            var expectedEventType = FileEventType.FileAdded;
            var expectedFileName = Path.GetFullPath(Path.Combine(testFolder, "new.txt"));
            monitor.FileChanged += (o, e) => {
                if(e.EventType == expectedEventType && e.FilePath == expectedFileName) {
                    are.Set();
                }
            };
            monitor.StartMonitoring();

            File.Create(expectedFileName).Close();
            Assert.IsTrue(are.WaitOne(WaitInterval));

            expectedEventType = FileEventType.FileChanged;
            var expectedLastWriteTime = DateTime.Now;
            File.SetLastWriteTime(expectedFileName, expectedLastWriteTime);
            Assert.IsTrue(are.WaitOne(WaitInterval));
            Assert.AreEqual(expectedLastWriteTime, archive.GetLastModifiedTime(expectedFileName));

            expectedEventType = FileEventType.FileDeleted;
            File.Delete(expectedFileName);
            Assert.IsTrue(are.WaitOne(WaitInterval));
        }

        [Test]
        public void TestFileSaveAndRestore() {
            using(var monitor = new DirectoryScanningMonitor(monitorFolder, new LastModifiedArchive(monitorFolder))) {
                monitor.AddDirectory(testFolder);
            }

            using(var monitor = new DirectoryScanningMonitor(monitorFolder, new LastModifiedArchive(monitorFolder))) {
                monitor.AddDirectoriesFromSaveFile();
                Assert.AreEqual(1, monitor.MonitoredDirectories.Count);
                Assert.AreEqual(Path.GetFullPath(testFolder), monitor.MonitoredDirectories[0].TrimEnd(Path.DirectorySeparatorChar));
            }

            using(var monitor = new DirectoryScanningMonitor(monitorFolder, new LastModifiedArchive(monitorFolder))) {
                monitor.AddDirectoriesFromSaveFile();
                Assert.AreEqual(1, monitor.MonitoredDirectories.Count);
                Assert.AreEqual(Path.GetFullPath(testFolder), monitor.MonitoredDirectories[0].TrimEnd(Path.DirectorySeparatorChar));
            }
        }

        [Test]
        public void TestIsMonitoringFile() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            monitor.AddDirectory(testFolder);

            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(monitor.IsMonitoringFile(fileName), "should be able to use the file name with the relative path");
                Assert.IsTrue(monitor.IsMonitoringFile(Path.GetFullPath(fileName)), "should be able to find the file name with the absolute path");
            }
        }

        [Test]
        public void TestRemoveDirectory() {
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            AutoResetEvent directoryResetEvent = new AutoResetEvent(false);
            
            monitor.DirectoryAdded += (o, e) => directoryResetEvent.Set();
            monitor.DirectoryRemoved += (o, e) => directoryResetEvent.Set();

            monitor.AddDirectory(testFolder);
            Assert.IsTrue(directoryResetEvent.WaitOne(WaitInterval));
            monitor.UpdateArchives();

            Assert.AreEqual(numStartingFiles, monitor.GetArchivedFiles().Count());
            monitor.RemoveDirectory("test1");
            Assert.IsFalse(directoryResetEvent.WaitOne(WaitInterval));
            Assert.AreEqual(numStartingFiles, monitor.GetArchivedFiles().Count());

            AutoResetEvent fileDeletionResetEvent = new AutoResetEvent(false);
            int count = numStartingFiles;
            monitor.FileChanged += (o, e) => {
                if(e.EventType == FileEventType.FileDeleted) {
                    if(--count == 0)
                        fileDeletionResetEvent.Set();
                }
            };

            monitor.RemoveDirectory(testFolder);
            Assert.IsTrue(directoryResetEvent.WaitOne(WaitInterval));
            Assert.IsTrue(fileDeletionResetEvent.WaitOne(WaitInterval));

            Assert.AreEqual(0, monitor.GetArchivedFiles().Count());
            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(File.Exists(fileName));
            }
        }

        [Test]
        public void TestStartup() {
            AutoResetEvent are = new AutoResetEvent(false);
            var archive = new LastModifiedArchive(monitorFolder);
            DirectoryScanningMonitor monitor = new DirectoryScanningMonitor(monitorFolder, archive);
            
            monitor.DirectoryAdded += (o, e) => { are.Set(); };
            monitor.AddDirectory(testFolder);
            Assert.IsTrue(are.WaitOne(WaitInterval));
            
            int count = 0;
            monitor.FileChanged += (o, e) => {
                if(e.EventType == FileEventType.FileAdded) {
                    count++;
                    if(count == numStartingFiles) {
                        are.Set();
                    }
                }
            };
            monitor.UpdateArchives();

            Assert.IsTrue(are.WaitOne(WaitInterval));
            Assert.AreEqual(numStartingFiles, archive.GetFiles().Count(), String.Format("only found {0} files in the archive", archive.GetFiles().Count()));

            foreach(var fileName in Directory.EnumerateFiles(testFolder)) {
                Assert.IsTrue(archive.ContainsFile(fileName));
                Assert.IsFalse(archive.IsOutdated(fileName));
                Assert.AreEqual(File.GetLastWriteTime(fileName), archive.GetLastModifiedTime(fileName));
            }
        }
    }
}