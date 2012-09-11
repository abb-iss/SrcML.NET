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
            
            int numberOfFilesAdded = 0;

            ISourceFolder watchedFolder = Substitute.For<ISourceFolder>();

            watchedFolder.FullFolderPath = srcDirectoryInfo.FullName;

            var archive = new SrcMLArchive(watchedFolder);
            var xmlDirectory = new DirectoryInfo(archive.XmlDirectory);

            archive.SourceFileChanged += (o, e) =>
                {
                    Assert.That(e.EventType, Is.EqualTo(SourceEventType.Added));
                    Assert.That(e.SourceFilePath, Is.Not.SamePathOrUnder(xmlDirectory.Name));
                    numberOfFilesAdded++;
                    Console.WriteLine("ADDED: {0}", e.SourceFilePath);
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

            Assert.That(numberOfFilesAdded, Is.EqualTo(3));
        }

        private void WriteTextAndRaiseEvent(ISourceFolder watchedFolder, string fileName, string source)
        {
            var path = Path.Combine(this.srcDirectoryInfo.Name, fileName);
            var xmlPath = Path.Combine(this.srcDirectoryInfo.Name, ".srcml", fileName);
            xmlPath += ".xml";

            File.WriteAllText(path, source);

            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(path, SourceEventType.Added));
            watchedFolder.SourceFileChanged += Raise.EventWith(new SourceEventArgs(xmlPath, SourceEventType.Added));
        }
    }
}
