/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.IO;
using System.Linq;
using ABB.SrcML;
using NUnit.Framework;
using NSubstitute;

namespace ABB.SrcML.Test
{
    [TestFixture]
    class SrcMLArchiveTest
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

        [Test]
        public void FileCreationTest()
        {
            int numberOfEventsRaised = 0;
            ISourceFolder watchedFolder = Substitute.For<ISourceFolder>();

            watchedFolder.FullFolderPath = srcDirectoryInfo.FullName;

            var archive = new SrcMLArchive(watchedFolder);
            var xmlDirectory = new DirectoryInfo(archive.ArchivePath);

            archive.SourceFileChanged += (o, e) =>
                {
                    numberOfEventsRaised++;
                    Assert.That(e.SourceFilePath, Is.Not.SamePathOrUnder(xmlDirectory.Name));
                    Console.WriteLine("{0}: {1}", e.EventType, e.SourceFilePath);
                };

            WriteTextAndRaiseEvent(watchedFolder, "foo.c", @"int foo(int i) {
    return i + 1;
}");
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "foo.c.xml")));

            WriteTextAndRaiseEvent(watchedFolder, "bar.c", @"int bar(int i) {
    return i - 1;
}");
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "bar.c.xml")));

            Directory.CreateDirectory(Path.Combine(SOURCEDIRECTORY, "subdir"));
            WriteTextAndRaiseEvent(watchedFolder, Path.Combine("subdir", "component.c"), @"int are_equal(int i, int j) {
    return i == j;

}");
            Assert.That(File.Exists(Path.Combine(xmlDirectory.FullName, "subdir", "component.c.xml")));            
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(3));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(3));

            DeleteSourceAndRaiseEvent(watchedFolder, "bar.c");
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(2));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(2));

            WriteTextAndRaiseEvent(watchedFolder, Path.Combine("subdir", "component.c"), @"struct A {
    int a;
    char b;
}");
            Assert.That(archive.FileUnits.Count(), Is.EqualTo(2));
            Assert.That(numberOfFunctions(archive), Is.EqualTo(1));

            RenameSourceFileAndRaiseEvent(watchedFolder, "foo.c", "foo2.c");

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

        private void WriteTextAndRaiseEvent(ISourceFolder watchedFolder, string fileName, string source)
        {
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName) + ".xml";
            var eventType = (File.Exists(path) ? SourceEventType.Changed : SourceEventType.Added);

            File.WriteAllText(path, source);

            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(path, eventType));
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(xmlPath, eventType));
        }

        private void DeleteSourceAndRaiseEvent(ISourceFolder watchedFolder, string fileName)
        {
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName);
            xmlPath += ".xml";

            File.Delete(path);
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(path, SourceEventType.Deleted));
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(xmlPath, SourceEventType.Deleted));
        }

        private void RenameSourceFileAndRaiseEvent(ISourceFolder watchedFolder, string oldFileName, string fileName)
        {
            var oldPath = Path.Combine(this.srcDirectoryInfo.Name, oldFileName);
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            
            var oldXmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", oldFileName) + ".xml";
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName) + ".xml";

            File.Move(oldPath, path);
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(path, oldPath, SourceEventType.Renamed));
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(oldXmlPath, SourceEventType.Deleted));
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(xmlPath, SourceEventType.Added));
        }
    }
}
