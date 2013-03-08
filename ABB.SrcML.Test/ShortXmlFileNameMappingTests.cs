using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace ABB.SrcML.Test {
    [TestFixture]
    public class ShortXmlFileNameMappingTests {
        [TestFixtureSetUp]
        public void FixtureSetUp() {
            if(!Directory.Exists("mappingTest")) {
                Directory.CreateDirectory("mappingTest");
            } 
        }

        [SetUp]
        public void TestSetUp() {
            if(Directory.Exists("mappingTest")) {
                foreach(var file in Directory.GetFiles("mappingTest")) {
                    File.Delete(file);
                }
            }
        }

        [TestFixtureTearDown]
        public void FixtureTearDown() {
            if(Directory.Exists("mappingTest")) {
                Directory.Delete("mappingTest", true);
            } 
        }
        
        [Test]
        public void TestGetXmlPath_CorrectDir() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            var xmlPath = map.GetXmlPath("Example.cpp");
            Assert.That(xmlPath.StartsWith(map.XmlDirectory));
        }

        [Test]
        public void TestGetXmlPath_NonExistentDir() {
            Assert.That(!File.Exists("MissingDir"));
            var map = new ShortXmlFileNameMapping("MissingDir");
            var xmlPath = map.GetXmlPath("Example.cpp");
            Assert.That(xmlPath.StartsWith(map.XmlDirectory));
        }

        [Test]
        public void TestGetXmlPath_SameName() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            var xmlPath1 = map.GetXmlPath("Example.cpp");
            var xmlPath2 = map.GetXmlPath(@"Subdir\Example.cpp");
            Assert.AreNotEqual(xmlPath1, xmlPath2);
        }

        [Test]
        public void TestGetXmlPath_Repeated() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            var xmlPath1 = map.GetXmlPath("Example.cpp");
            var xmlPath2 = map.GetXmlPath("Example.cpp");
            Assert.AreEqual(xmlPath1, xmlPath2);
        }

        [Test]
        public void TestGetXmlPath_DuplicatesInMapFile() {
            string fileContents = @"C:\Foo\Bar\test.cs|C:\srcmlArchive\test.cs.1.xml
C:\Foo\Bar\Example.cs|C:\srcmlArchive\Example.cs.1.xml
C:\Foo\Bar\xyzzy\Example.cs|C:\srcmlArchive\Example.cs.2.xml";
            File.WriteAllText(@"mappingTest\mapping.txt", fileContents);
            var map = new ShortXmlFileNameMapping("mappingTest");
            var thirdName = map.GetXmlPath(@"C:\OtherDir\Example.cs");
            Assert.AreEqual(Path.Combine(Environment.CurrentDirectory, @"mappingTest\Example.cs.3.xml"), thirdName);
        }

        [Test]
        public void TestGetXmlPath_DifferentCase() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            var xmlPath1 = map.GetXmlPath("foo\\Example.cpp");
            var xmlPath2 = map.GetXmlPath("bar\\example.CPP");
            //If the file system is case insensitive, the paths should be Example.cpp.1.xml and example.CPP.2.xml
            //If the file system is case sensitive, the paths should be Example.cpp.1.xml and example.CPP.1.xml
            Assert.IsTrue(string.Compare(xmlPath1, xmlPath2, StringComparison.CurrentCultureIgnoreCase) != 0);
        }

        [Test]
        public void TestConstructor_CurrentDirectory() {
            var map = new ShortXmlFileNameMapping(".");
            var xmlPath = map.GetXmlPath("Foo.cs");
            Assert.That(xmlPath.StartsWith(Environment.CurrentDirectory));
        }

        [Test]
        public void TestRountrip() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            var xmlPath = map.GetXmlPath("Foo.cs");
            Assert.AreEqual(Path.GetFullPath("Foo.cs"), map.GetSourcePath(xmlPath));
        }

        [Test]
        public void TestGetSourcePath_FullPath() {
            string fileContents = @"C:\Foo\Bar\test.cs|C:\srcmlArchive\test.cs.1.xml
C:\Foo\Bar\Example.cs|C:\srcmlArchive\Example.cs.1.xml
C:\Foo\Bar\xyzzy\Example.cs|C:\srcmlArchive\Example.cs.2.xml";
            File.WriteAllText(@"mappingTest\mapping.txt", fileContents);
            
            var map = new ShortXmlFileNameMapping("mappingTest");
            var sourcePath = map.GetSourcePath(@"C:\srcmlArchive\Example.cs.1.xml");
            Assert.AreEqual(@"C:\Foo\Bar\Example.cs", sourcePath);
        }

        [Test]
        public void TestGetSourcePath_JustFileName() {
            string fileContents = @"C:\Foo\Bar\test.cs|{0}\mappingTest\test.cs.1.xml
C:\Foo\Bar\Example.cs|{0}\mappingTest\Example.cs.1.xml
C:\Foo\Bar\xyzzy\Example.cs|{0}\mappingTest\Example.cs.2.xml";
            File.WriteAllText(@"mappingTest\mapping.txt", string.Format(fileContents, Environment.CurrentDirectory));

            var map = new ShortXmlFileNameMapping("mappingTest");
            var sourcePath = map.GetSourcePath(@"Example.cs.2.xml");
            Assert.AreEqual(@"C:\Foo\Bar\xyzzy\Example.cs", sourcePath);
        }

        [Test]
        public void TestMappingFile() {
            string fileContents = @"C:\Foo\Bar\test.cs|C:\srcmlArchive\test.cs.1.xml
C:\Foo\Bar\Example.cs|C:\srcmlArchive\Example.cs.1.xml
C:\Foo\Bar\xyzzy\Example.cs|C:\srcmlArchive\Example.cs.2.xml";
            File.WriteAllText(@"mappingTest\mapping.txt", fileContents);
            var map = new ShortXmlFileNameMapping("mappingTest");
            Assert.AreEqual(@"C:\srcmlArchive\test.cs.1.xml", map.GetXmlPath(@"C:\Foo\Bar\test.cs"));
            Assert.AreEqual(@"C:\srcmlArchive\Example.cs.1.xml", map.GetXmlPath(@"C:\Foo\Bar\Example.cs"));
            Assert.AreEqual(@"C:\srcmlArchive\Example.cs.2.xml", map.GetXmlPath(@"C:\Foo\Bar\xyzzy\Example.cs"));
        }

        [Test]
        public void TestSaveMapping() {
            var map = new ShortXmlFileNameMapping("mappingTest");
            map.GetXmlPath("main.c");
            map.GetXmlPath("integer.cpp");
            map.SaveMapping();
            Assert.That(File.Exists(@"mappingTest\mapping.txt"));
            var lines = File.ReadAllLines(@"mappingTest\mapping.txt");
            Assert.AreEqual(2, lines.Length);
        }

        [Test]
        public void TestFolderWithXmlNoMappingFile() {
            File.Copy(@"..\..\TestInputs\function_def.xml", @"mappingTest\function_def.xml", true);
            File.Copy(@"..\..\TestInputs\method_def.xml", @"mappingTest\method_def.xml", true);
            File.Copy(@"..\..\TestInputs\nested_scopes.xml", @"mappingTest\nested_scopes.xml", true);

            var map = new ShortXmlFileNameMapping("mappingTest");
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\function_def.cpp", map.GetSourcePath(@"function_def.xml"));
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\method_def.cpp", map.GetSourcePath(@"method_def.xml"));
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\nested_scopes.c", map.GetSourcePath(@"nested_scopes.xml"));
        }

        [Test]
        public void TestFolderWithXmlNoMappingFile_NonSrcMLXml() {
            File.Copy(@"..\..\TestInputs\function_def.xml", @"mappingTest\function_def.xml", true);
            File.Copy(@"..\..\TestInputs\method_def.xml", @"mappingTest\method_def.xml", true);
            File.Copy(@"..\..\TestInputs\nested_scopes.xml", @"mappingTest\nested_scopes.xml", true);
            File.Copy(@"..\..\TestInputs\NotSrcML.xml", @"mappingTest\NotSrcML.xml", true);

            var map = new ShortXmlFileNameMapping("mappingTest");
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\function_def.cpp", map.GetSourcePath(@"function_def.xml"));
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\method_def.cpp", map.GetSourcePath(@"method_def.xml"));
            Assert.AreEqual(@"C:\Workspaces\SrcML.NET\TestInputs\nested_scopes.c", map.GetSourcePath(@"nested_scopes.xml"));
        }

        [Test]
        public void TestConcurrentAccess() {
            var sourceFiles1 = new[]
                               {
                                   Environment.CurrentDirectory+@"\xyzzy\Example.cs",
                                   Environment.CurrentDirectory+@"\foo\bar\MissingFile.cs",
                                   Environment.CurrentDirectory+@"\zork\Example.cs",
                                   Environment.CurrentDirectory+@"\path\to\file\data.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data1.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data2.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data3.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data4.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data5.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data6.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data7.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data8.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data9.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data10.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data11.c"

                               };
            var sourceFiles2 = new[]
                               {
                                   Environment.CurrentDirectory+@"\foo\bar\Example.cs",
                                   Environment.CurrentDirectory+@"\path\to\file\data.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data1.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data2.cpp",
                                   Environment.CurrentDirectory+@"\foo\bar\ImportantData.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data3.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data4.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data5.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data6.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data7.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data8.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data9.cpp",
                                   Environment.CurrentDirectory+@"\other\dir\Sample.h",
                                   Environment.CurrentDirectory+@"\path\to\file\data10.cpp",
                                   Environment.CurrentDirectory+@"\path\to\file\data11.c"
                               };
            var xmlFiles = new[]
                           {
                               Environment.CurrentDirectory+@"\mappingTest\Example.cs.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\MissingFile.cs.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\Example.cs.2.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data1.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data2.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data3.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data4.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data5.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data6.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data7.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data8.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data9.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data10.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\data11.c.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\Example.cs.3.xml",
                               Environment.CurrentDirectory+@"\mappingTest\ImportantData.cpp.1.xml",
                               Environment.CurrentDirectory+@"\mappingTest\Sample.h.1.xml",
                           };
            
            var map = new ShortXmlFileNameMapping("mappingTest");
            var worker = new Thread(() => ConcurrentWorker(map, sourceFiles2));
            worker.Start();
            foreach(var file in sourceFiles1) {
                map.GetXmlPath(file);
            }
            worker.Join(5000);
            map.SaveMapping();

            var obsSourceFiles = new HashSet<string>();
            var obsXmlFiles = new HashSet<string>();
            foreach(var entry in File.ReadAllLines("mappingTest\\mapping.txt")) {
                var fields = entry.Split('|');
                obsSourceFiles.Add(fields[0]);
                obsXmlFiles.Add(fields[1]);
            }
            var sourceFiles = sourceFiles1.Union(sourceFiles2);
            Assert.AreEqual(sourceFiles.Count(), obsSourceFiles.Count);
            Assert.AreEqual(xmlFiles.Count(), obsXmlFiles.Count);
            foreach(var file in obsSourceFiles) {
                Assert.IsTrue(sourceFiles.Contains(file));
            }
            foreach(var file in obsXmlFiles) {
                Assert.IsTrue(xmlFiles.Contains(file));
            }
        }

        private void ConcurrentWorker(ShortXmlFileNameMapping map, IEnumerable<string> sourceFiles) {
            foreach(var file in sourceFiles) {
                map.GetXmlPath(file);
            }
        }

        [Test]
        public void TestDispose() {
            Assert.IsTrue(!File.Exists("mappingTest\\mapping.txt"));
            using(var map = new ShortXmlFileNameMapping("mappingTest")) {
                map.GetXmlPath(@"foo\bar\Example.cs");
                map.GetXmlPath(@"foo\bar\Data.cs");
                map.GetXmlPath(@"foo\bar\baz\Example.cs");
            }
            Assert.IsTrue(File.Exists("mappingTest\\mapping.txt"));
        }
    }
}
