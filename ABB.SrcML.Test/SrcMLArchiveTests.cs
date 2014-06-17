/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ABB.SrcML;
using NUnit.Framework;

namespace ABB.SrcML.Test
{
    [TestFixture]
    [Category("Build")]
    public class SrcMLArchiveTests
    {
        public const string SourceDirectory = "testSourceDir";
        public const string ArchiveDirectory = "SrcMLArchive";

        private DirectoryInfo srcDirectoryInfo;
        private DirectoryInfo archiveDirectoryInfo;
        private bool startupCompleted = false;

        [TestFixtureSetUp]
        public void Setup()
        {
            if(!Directory.Exists(SourceDirectory)) {
                srcDirectoryInfo = Directory.CreateDirectory(SourceDirectory);
            } else {
                srcDirectoryInfo = new DirectoryInfo(SourceDirectory);
            }

            if(!Directory.Exists(ArchiveDirectory)) {
                archiveDirectoryInfo = Directory.CreateDirectory(ArchiveDirectory);
            } else {
                archiveDirectoryInfo = new DirectoryInfo(ArchiveDirectory);
            }
        }

        [SetUp]
        public void TestSetUp() {
            if(srcDirectoryInfo.Exists) {
                foreach(var file in srcDirectoryInfo.GetFiles("*.*")) {
                    File.Delete(file.Name);
                }
            }
            if(archiveDirectoryInfo.Exists) {
                foreach(var file in archiveDirectoryInfo.GetFiles("*.*")) {
                    File.Delete(file.Name);
                }
            }
        }

        [TestFixtureTearDown]
        public void TearDown() {
            if(Directory.Exists(SourceDirectory)) {
                Directory.Delete(SourceDirectory, true);
            }
            if(Directory.Exists(ArchiveDirectory)) {
                Directory.Delete(ArchiveDirectory, true);
            }
        }

        [Test]
        public void GenerateXmlForDirectoryTest() {
            ManualResetEvent resetEvent = new ManualResetEvent(false);

            var archive = new SrcMLArchive(ArchiveDirectory, false, new SrcMLGenerator(TestConstants.SrcmlPath));
            FileEventType expectedEventType = FileEventType.FileAdded;
            FileEventType actualEventType = FileEventType.FileChanged;

            archive.FileChanged += (sender, e) => {
                actualEventType = e.EventType;
                bool shouldHaveSrcML = (e.EventType != FileEventType.FileDeleted);
                Assert.AreEqual(shouldHaveSrcML, e.HasSrcML);
                resetEvent.Set();
            };

            Dictionary<string, string> sourceFiles = new Dictionary<string, string>() {
                { Path.Combine(SourceDirectory, "foo.c"), String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine) }, 
                { Path.Combine(SourceDirectory, "bar.c"), String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "foo1.c"), String.Format(@"int foo1() {{{0}printf(""hello world 1!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "bar1.c"), String.Format(@"int bar1() {{{0}    printf(""goodbye, world 1!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "foo2.c"), String.Format(@"int foo2() {{{0}printf(""hello world 2!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "bar2.c"), String.Format(@"int bar2() {{{0}    printf(""goodbye, world 2!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "subdir11", "foo11.c"), String.Format(@"int foo11() {{{0}printf(""hello world 11!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "subdir11", "bar11.c"), String.Format(@"int bar11() {{{0}    printf(""goodbye, world 11!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "subdir12", "foo12.c"), String.Format(@"int foo12() {{{0}printf(""hello world 12!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir1", "subdir12", "bar12.c"), String.Format(@"int bar12() {{{0}    printf(""goodbye, world 12!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "subdir21", "foo21.c"), String.Format(@"int foo21() {{{0}printf(""hello world 21!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "subdir21", "bar21.c"), String.Format(@"int bar21() {{{0}    printf(""goodbye, world 21!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "subdir22", "foo22.c"), String.Format(@"int foo22() {{{0}printf(""hello world 22!"");{0}}}", Environment.NewLine) },
                { Path.Combine(SourceDirectory, "subdir2", "subdir22", "bar22.c"), String.Format(@"int bar22() {{{0}    printf(""goodbye, world 22!"");{0}}}", Environment.NewLine) },
            };

            foreach(var fileDataPair in sourceFiles) {
                var directory = Path.GetDirectoryName(fileDataPair.Key);
                if(!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(fileDataPair.Key, fileDataPair.Value);
                archive.AddOrUpdateFile(fileDataPair.Key);
                Assert.That(resetEvent.WaitOne(300));
                Assert.AreEqual(expectedEventType, actualEventType);
            }

            foreach(var fileName in sourceFiles.Keys) {
                Assert.That(archive.ContainsFile(fileName), String.Format("Archive should contain {0}", fileName));
            }

            var changedFileName = Path.Combine(SourceDirectory, "foo.c");
            var changedFileContents = String.Format(@"int foo() {{{0}printf(""hello world! changed"");{0}}}", Environment.NewLine);

            expectedEventType = FileEventType.FileChanged;
            File.WriteAllText(changedFileName, changedFileContents);
            File.SetLastWriteTime(changedFileName, DateTime.Now);
            
            Assert.That(archive.ContainsFile(changedFileName));
            Assert.That(archive.IsOutdated(changedFileName));

            archive.AddOrUpdateFile(changedFileName);
            Assert.That(resetEvent.WaitOne(300));
            Assert.AreEqual(expectedEventType, actualEventType);

            expectedEventType = FileEventType.FileDeleted;
            var deletedFileName = Path.Combine(SourceDirectory, "subdir1", "subdir12", "bar12.c");
            File.Delete(deletedFileName);
            Assert.That(archive.IsOutdated(deletedFileName));
            archive.DeleteFile(deletedFileName);
            Assert.That(resetEvent.WaitOne(300));
            Assert.AreEqual(expectedEventType, actualEventType);

            expectedEventType = FileEventType.FileRenamed;
            var movedFileName = Path.Combine(SourceDirectory, "subdir1", "subdir11", "foo11.c");
            var newNameForMoved = Path.Combine(SourceDirectory, "subdir1", "subdir11", "foo1111111.c");
            File.Move(movedFileName, newNameForMoved);
            Assert.That(archive.IsOutdated(movedFileName));
            archive.RenameFile(movedFileName, newNameForMoved);
            Assert.That(resetEvent.WaitOne(300));
            Assert.AreEqual(expectedEventType, actualEventType);
            Assert.That(archive.ContainsFile(newNameForMoved));
            Assert.IsFalse(archive.ContainsFile(movedFileName));
        }

        [Test]
        public void TestDontUseExistingSrcML() {
            //convert the test files and place in the xml directory
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            var archive = new SrcMLArchive(ArchiveDirectory, false, new SrcMLGenerator(TestConstants.SrcmlPath));
            archive.FileChanged += (o, e) => { resetEvent.Set(); };

            string[] sourceFiles = new[] { @"..\..\TestInputs\foo.c", @"..\..\TestInputs\baz.cpp", @"..\..\TestInputs\function_def.cpp" };

            foreach(var sourceFile in sourceFiles) {
                archive.AddOrUpdateFile(sourceFile);
                Assert.That(resetEvent.WaitOne(300), "Timed out waiting for " + sourceFile);
            }
            foreach(var sourceFile in sourceFiles) {
                Assert.That(archive.ContainsFile(sourceFile), sourceFile + " should be in the archive!");
            }
            archive.Dispose();

            //make new archive, and ignore existing srcml files in xml directory
            archive = new SrcMLArchive(ArchiveDirectory, false, new SrcMLGenerator(TestConstants.SrcmlPath));
            foreach(var sourceFile in sourceFiles) {
                Assert.IsFalse(archive.ContainsFile(sourceFile));
            }
            archive.Dispose();
        }

        [Test]
        public void TestEmptyArchive() {
            var archive = new SrcMLArchive(ArchiveDirectory, false, new SrcMLGenerator(Path.Combine(".", "SrcML")));
            Assert.That(archive.IsEmpty);
            var foo_c = Path.Combine(SourceDirectory, "foo.c");
            File.WriteAllText(foo_c, String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            archive.AddOrUpdateFile(foo_c);
            Assert.That(archive.IsEmpty, Is.False);
        }

        //[Test]
        //public void GenerateXmlForDirectoryStressTest()
        //{
        //    Console.WriteLine("------- Start -------\n");

        //    Process thisProcess = null;

        //    thisProcess = Process.GetCurrentProcess();
        //    Console.WriteLine("ID: [" + thisProcess.Id + "\n");
        //    Console.WriteLine("NonpagedSystemMemorySize64: [" + thisProcess.NonpagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PagedMemorySize64: [" + thisProcess.PagedMemorySize64 + "\n");
        //    Console.WriteLine("PagedSystemMemorySize64: [" + thisProcess.PagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PeakPagedMemorySize64: [" + thisProcess.PeakPagedMemorySize64 + "\n");
        //    Console.WriteLine("PeakVirtualMemorySize64: [" + thisProcess.PeakVirtualMemorySize64 + "\n");
        //    Console.WriteLine("PeakWorkingSet64: [" + thisProcess.PeakWorkingSet64 + "\n");
        //    Console.WriteLine("PrivateMemorySize64: [" + thisProcess.PrivateMemorySize64 + "\n");
        //    Console.WriteLine("VirtualMemorySize64: [" + thisProcess.VirtualMemorySize64 + "\n");

        //    Stopwatch swInit = new Stopwatch();
        //    swInit.Start();

        //    IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

        //    var archive = new SrcMLArchive(watchedFolder, Path.Combine(srcDirectoryInfo.FullName, ".srcml"), new SrcMLGenerator(TestConstants.SrcmlPath));
        //    var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

        //    for (int i = 0; i < 10; i++)
        //    {
        //        File.WriteAllText(SourceDirectory + "\\foo(" + i + ").c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
        //        File.WriteAllText(SourceDirectory + "\\bar(" + i + ").c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));
        //        Directory.CreateDirectory(Path.Combine(SourceDirectory, "subdir_" + i));
        //        for (int j = 0; j < 10; j++)
        //        {
        //            File.WriteAllText(SourceDirectory + "\\subdir_" + i + "\\foo(" + i + "_" + j + ").c", String.Format(@"int foo1() {{{0}printf(""hello world 1!"");{0}}}", Environment.NewLine));
        //            File.WriteAllText(SourceDirectory + "\\subdir_" + i + "\\bar(" + i + "_" + j + ").c", String.Format(@"int bar1() {{{0}    printf(""goodbye, world 1!"");{0}}}", Environment.NewLine));
        //            Directory.CreateDirectory(Path.Combine(SourceDirectory, "subdir_" + i + "\\subdir_" + i + "_" + j));
        //            for (int k = 0; k < 10; k++)
        //            {
        //                File.WriteAllText(SourceDirectory + "\\subdir_" + i + "\\subdir_" + i + "_" + j + "\\foo(" + i + "_" + j + "_" + k + ").c", String.Format(@"int foo1() {{{0}printf(""hello world 1!"");{0}}}", Environment.NewLine));
        //                File.WriteAllText(SourceDirectory + "\\subdir_" + i + "\\subdir_" + i + "_" + j + "\\bar(" + i + "_" + j + "_" + k + ").c", String.Format(@"int bar1() {{{0}    printf(""goodbye, world 1!"");{0}}}", Environment.NewLine));
        //            }
        //        }
        //    }

        //    swInit.Stop();
        //    Console.WriteLine("\nTotal time elapsed for initialization: {0}", swInit.Elapsed.ToString());
        //    Console.WriteLine("ID: [" + thisProcess.Id + "\n");
        //    Console.WriteLine("NonpagedSystemMemorySize64: [" + thisProcess.NonpagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PagedMemorySize64: [" + thisProcess.PagedMemorySize64 + "\n");
        //    Console.WriteLine("PagedSystemMemorySize64: [" + thisProcess.PagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PeakPagedMemorySize64: [" + thisProcess.PeakPagedMemorySize64 + "\n");
        //    Console.WriteLine("PeakVirtualMemorySize64: [" + thisProcess.PeakVirtualMemorySize64 + "\n");
        //    Console.WriteLine("PeakWorkingSet64: [" + thisProcess.PeakWorkingSet64 + "\n");
        //    Console.WriteLine("PrivateMemorySize64: [" + thisProcess.PrivateMemorySize64 + "\n");
        //    Console.WriteLine("VirtualMemorySize64: [" + thisProcess.VirtualMemorySize64 + "\n");

        //    //System.Threading.Thread.Sleep(1000);
        //    Stopwatch sw = new Stopwatch();
        //    sw.Start();
            
        //    ////archive.GenerateXmlForDirectory(SourceDirectory);
            
        //    sw.Stop();
        //    Console.WriteLine("\nTotal time elapsed for srcML files generation: {0}", sw.Elapsed.ToString());
        //    Console.WriteLine("ID: [" + thisProcess.Id + "\n");
        //    Console.WriteLine("NonpagedSystemMemorySize64: [" + thisProcess.NonpagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PagedMemorySize64: [" + thisProcess.PagedMemorySize64 + "\n");
        //    Console.WriteLine("PagedSystemMemorySize64: [" + thisProcess.PagedSystemMemorySize64 + "\n");
        //    Console.WriteLine("PeakPagedMemorySize64: [" + thisProcess.PeakPagedMemorySize64 + "\n");
        //    Console.WriteLine("PeakVirtualMemorySize64: [" + thisProcess.PeakVirtualMemorySize64 + "\n");
        //    Console.WriteLine("PeakWorkingSet64: [" + thisProcess.PeakWorkingSet64 + "\n");
        //    Console.WriteLine("PrivateMemorySize64: [" + thisProcess.PrivateMemorySize64 + "\n");
        //    Console.WriteLine("VirtualMemorySize64: [" + thisProcess.VirtualMemorySize64 + "\n");
        //    Console.WriteLine("\n------- End -------\n");

        //    /*
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\foo1.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\bar1.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\foo2.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\bar2.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo11.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\bar11.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\foo12.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\bar12.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\foo21.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\bar21.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\foo22.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\bar22.c.xml")));
        //    Assert.That(archive.FileUnits.Count(), Is.EqualTo(14));
        //    */

        //    /*
        //    File.WriteAllText(SourceDirectory + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world! changed"");{0}}}", Environment.NewLine));
        //    File.WriteAllText(SourceDirectory + "\\subdir2\\subdir21\\bar21.c", String.Format(@"int bar21() {{{0}    printf(""goodbye, world 21! changed"");{0}}}", Environment.NewLine));
        //    File.Delete("C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir12\\bar12.c");
        //    File.Move("C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir11\\foo11.c",
        //        "C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir11\\foo1111111.c");

        //    System.Threading.Thread.Sleep(5000);
        //    archive.GenerateXmlForDirectory(SourceDirectory);
        //    */

        //    /*
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\foo1.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\bar1.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\foo2.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\bar2.c.xml")));
        //    Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo11.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo1111111.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\bar11.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\foo12.c.xml")));
        //    Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\bar12.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\foo21.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\bar21.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\foo22.c.xml")));
        //    Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\bar22.c.xml")));
        //    Assert.That(archive.FileUnits.Count(), Is.EqualTo(13));
        //    */

        //}
    }
}
