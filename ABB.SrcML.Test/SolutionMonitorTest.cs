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
using System.Xml.Linq;
using ABB.SrcML;
using NUnit.Framework;
using NSubstitute;

namespace ABB.SrcML.Test
{
    [TestFixture]
    class SolutionMonitorTest
    {
        public const string SOURCEDIRECTORY = "testSourceDir";
        private DirectoryInfo srcDirectoryInfo;

        [TestFixtureSetUp]
        public void Setup()
        {
            if (Directory.Exists(SOURCEDIRECTORY))
            {
                Directory.Delete(SOURCEDIRECTORY, true);
            }
            srcDirectoryInfo = Directory.CreateDirectory(SOURCEDIRECTORY);
        }

        // Added on 2012.10.10
        [Test]
        public void GenerateXmlForDirectoryTest()
        {
            IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

            var archive = new SrcMLArchive(watchedFolder, ".srcml");
            archive.XmlGenerator.ApplicationDirectory = TestConstants.SrcmlPath;
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            File.WriteAllText(SOURCEDIRECTORY + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\bar.c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir1"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\foo1.c", String.Format(@"int foo1() {{{0}printf(""hello world 1!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\bar1.c", String.Format(@"int bar1() {{{0}    printf(""goodbye, world 1!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir2"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\foo2.c", String.Format(@"int foo2() {{{0}printf(""hello world 2!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\bar2.c", String.Format(@"int bar2() {{{0}    printf(""goodbye, world 2!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir1\\subdir11"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\subdir11\\foo11.c", String.Format(@"int foo11() {{{0}printf(""hello world 11!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\subdir11\\bar11.c", String.Format(@"int bar11() {{{0}    printf(""goodbye, world 11!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir1\\subdir12"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\subdir12\\foo12.c", String.Format(@"int foo12() {{{0}printf(""hello world 12!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir1\\subdir12\\bar12.c", String.Format(@"int bar12() {{{0}    printf(""goodbye, world 12!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir2\\subdir21"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\subdir21\\foo21.c", String.Format(@"int foo21() {{{0}printf(""hello world 21!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\subdir21\\bar21.c", String.Format(@"int bar21() {{{0}    printf(""goodbye, world 21!"");{0}}}", Environment.NewLine));
            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir2\\subdir22"));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\subdir22\\foo22.c", String.Format(@"int foo22() {{{0}printf(""hello world 22!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\subdir22\\bar22.c", String.Format(@"int bar22() {{{0}    printf(""goodbye, world 22!"");{0}}}", Environment.NewLine));

            System.Threading.Thread.Sleep(5000);
            ////archive.GenerateXmlForDirectory(SOURCEDIRECTORY);
            /*
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\foo1.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\bar1.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\foo2.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\bar2.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo11.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\bar11.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\foo12.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\bar12.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\foo21.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\bar21.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\foo22.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\bar22.c.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(14));
            */

            File.WriteAllText(SOURCEDIRECTORY + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world! changed"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\subdir2\\subdir21\\bar21.c", String.Format(@"int bar21() {{{0}    printf(""goodbye, world 21! changed"");{0}}}", Environment.NewLine));
            File.Delete("C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir12\\bar12.c");
            File.Move("C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir11\\foo11.c",
                "C:\\Users\\USJIZHE\\Documents\\GitHub\\SrcML.NET\\Build\\Debug\\testSourceDir\\subdir1\\subdir11\\foo1111111.c");

            System.Threading.Thread.Sleep(5000);
            ////archive.GenerateXmlForDirectory(SOURCEDIRECTORY);
            /*
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\foo1.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\bar1.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\foo2.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\bar2.c.xml")));
            Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo11.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\foo1111111.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir11\\bar11.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\foo12.c.xml")));
            Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "subdir1\\subdir12\\bar12.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\foo21.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir21\\bar21.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\foo22.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir2\\subdir22\\bar22.c.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(13));
            */

        }

        // Added on 2012.10.09
        [Test]
        public void GenerateXmlForSourceTest()
        {
            IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

            var archive = new SrcMLArchive(watchedFolder, ".srcml");
            archive.XmlGenerator.ApplicationDirectory = TestConstants.SrcmlPath;
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            File.WriteAllText(SOURCEDIRECTORY + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\bar.c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));

            archive.GenerateXmlForSource(SOURCEDIRECTORY + "\\foo.c");
            archive.GenerateXmlForSource(SOURCEDIRECTORY + "\\bar.c");

            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));
        }

        /// <summary>
        /// Added by JZ on 12/3/2012
        /// Unit test for SrcMLArchive.public string GenerateXmlAndStringForSource(string sourcePath)
        /// </summary>
        [Test]
        public void GenerateXmlAndStringForSourceTest()
        {
            IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

            var archive = new SrcMLArchive(watchedFolder, ".srcml");
            archive.XmlGenerator.ApplicationDirectory = TestConstants.SrcmlPath;
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            File.WriteAllText(SOURCEDIRECTORY + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\bar.c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));

            string fooXML = archive.GenerateXmlAndStringForSource(SOURCEDIRECTORY + "\\foo.c");
            string barXML = archive.GenerateXmlAndStringForSource(SOURCEDIRECTORY + "\\bar.c");

            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4X88LX7RR.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4Z4UW37RR.xml")));
        }

        /// <summary>
        /// Added by JZ on 12/4/2012
        /// Unit test for SrcMLArchive.public string GenerateXmlAndXElementForSource(string sourcePath)
        /// </summary>
        [Test]
        public void GenerateXmlAndXElementForSourceTest()
        {
            IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

            var archive = new SrcMLArchive(watchedFolder, ".srcml");
            archive.XmlGenerator.ApplicationDirectory = TestConstants.SrcmlPath;
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            File.WriteAllText(SOURCEDIRECTORY + "\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText(SOURCEDIRECTORY + "\\bar.c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));

            XElement fooXElement = archive.GenerateXmlAndXElementForSource(SOURCEDIRECTORY + "\\foo.c");
            XElement barXElement = archive.GenerateXmlAndXElementForSource(SOURCEDIRECTORY + "\\bar.c");

            Assert.IsNotNull(fooXElement);
            Assert.IsNotNull(barXElement);
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4X88LX7RR.xml")));
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4Z4UW37RR.xml")));
        }

        [Test]
        public void FileAddDeleteChangeRenameTest()
        {
            int numberOfEventsRaised = 0;
            IFileMonitor watchedFolder = Substitute.For<IFileMonitor>();

            var archive = new SrcMLArchive(watchedFolder, ".srcml");
            archive.XmlGenerator.ApplicationDirectory = TestConstants.SrcmlPath;
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            ////archive.SourceFileChanged += (o, e) =>
            archive.SourceFileChanged += (o, e) =>
            {
                numberOfEventsRaised++;
                Assert.That(e.SourceFilePath, Is.Not.SamePathOrUnder(xmlDirectory.Name));
                Console.WriteLine("Event Type '{0}': [{1}]", e.EventType, e.SourceFilePath);
            };

            WriteTextAndRaiseEvent(watchedFolder, "foo.c", @"int foo(int i) {
    return i + 1;
}");
            // Base32 encoded filename foo.c.xml
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4X88LX7RR.xml")));

            WriteTextAndRaiseEvent(watchedFolder, "bar.c", @"int bar(int i) {
    return i - 1;
}");
            // Base32 encoded filename bar.c.xml
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4Z4UW37RR.xml")));

            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir"));
            WriteTextAndRaiseEvent(watchedFolder, Path.Combine("subdir", "component.c"), @"int are_equal(int i, int j) {
    return i == j;

}");
            // Base32 encoded filename subdir\component.c.xml
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4649WXBSFB27XN7HFT88KNBJFY64XX.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(3));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(3));

            DeleteSourceAndRaiseEvent(watchedFolder, "bar.c");
            // Base32 encoded filename bar.c.xml
            Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4Z4UW37RR.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(2));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(2));

            WriteTextAndRaiseEvent(watchedFolder, Path.Combine("subdir", "component.c"), @"struct A {
    int a;
    char b;
}");
            // Base32 encoded filename subdir\component.c.xml
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4649WXBSFB27XN7HFT88KNBJFY64XX.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(2));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(1));

            RenameSourceFileAndRaiseEvent(watchedFolder, "foo.c", "foo2.c");
            // Base32 encoded filename foo.c.xml
            Assert.That(!File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4X88LX7RR.xml")));
            // Base32 encoded filename foo2.c.xml
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "2BVUHCFVS6KX3VHC6BBBWFADSZ7EY7FRH48CX7GV627VYYGVEC9WXVFCB8UMWXJSVCGESVDEH4MUXBG4WCJWNM9RKZ9CNJGV6M8C3JFRS2GBXJG4X88LXJUS22.xml")));
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(2));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(1));

            Assert.That(numberOfEventsRaised, Is.EqualTo(6));
        }

        private int numberOfFunctions(IArchive archive)
        {
            var functions = from unit in archive.FileUnits
                            from function in unit.Elements(SRC.Function)
                            select function;
            return functions.Count();
        }

        private void WriteTextAndRaiseEvent(IFileMonitor watchedFolder, string fileName, string source)
        {
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName) + ".xml";
            var eventType = (File.Exists(path) ? FileEventType.FileChanged : FileEventType.FileAdded);

            File.WriteAllText(path, source);

            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(path, eventType));
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(xmlPath, eventType));
        }

        private void DeleteSourceAndRaiseEvent(IFileMonitor watchedFolder, string fileName)
        {
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName);
            xmlPath += ".xml";

            File.Delete(path);
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(path, FileEventType.FileDeleted));
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(xmlPath, FileEventType.FileDeleted));
        }

        private void RenameSourceFileAndRaiseEvent(IFileMonitor watchedFolder, string oldFileName, string fileName)
        {
            var oldPath = Path.Combine(this.srcDirectoryInfo.Name, oldFileName);
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);

            var oldXmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", oldFileName) + ".xml";
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName) + ".xml";

            File.Move(oldPath, path);
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(path, oldPath, FileEventType.FileRenamed));
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(oldXmlPath, FileEventType.FileDeleted));
            watchedFolder.FileEventRaised += Raise.EventWith(new FileEventRaisedArgs(xmlPath, FileEventType.FileAdded));
        }
    }
}
