using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using ABB.SrcML;
using System.Xml;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Collections.ObjectModel;
namespace ABB.SrcML.Test {

    [TestFixture]
    [Category("Build")]
    class LibSrcMLRunnerTests {
        private const string TestInputPath = @"..\..\TestInputs";
        [TearDown]
        public void Cleanup() {
            File.Delete("output0.cpp.xml");
            File.Delete("output1.cpp.xml");
        }
        /// <summary>
        /// This tests the creation of an archive using a list of source files
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveFtF() {
            using (Archive srcmlarchive = new Archive(), srcmlarchive2 = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitFilename(Path.Combine(TestInputPath, "input.cpp"));
                    srcmlarchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();

                    srcmlunit.SetUnitFilename(Path.Combine(TestInputPath, "input2.cpp"));
                    srcmlarchive2.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive2.AddUnit(srcmlunit);
                    srcmlarchive2.ArchivePack();

                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();
                    IntPtr structPtr2 = srcmlarchive2.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    structArrayPtr.Add(structPtr2);

                    try {
                        Assert.True(LibSrcMLRunner.SrcmlCreateArchiveFtF(structArrayPtr.ToArray(), structArrayPtr.Count(), "output") == 0);
                    }
                    catch (SrcMLException e) {
                        Console.WriteLine(e.Message);
                    }
                }
                {
                    Assert.True(File.Exists("output0.cpp.xml"));
                    SrcMLFile srcFile = new SrcMLFile("output0.cpp.xml");
                    Assert.IsNotNull(srcFile);

                    var files = srcFile.FileUnits.ToList();
                    Assert.AreEqual(1, files.Count());

                    string file = Path.Combine(TestInputPath, "input.cpp");
                    var f1 = (from ele in files
                              where ele.Attribute("filename").Value == file
                              select ele);
                    Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);
                }
                {
                    Assert.True(File.Exists("output1.cpp.xml"));
                    SrcMLFile srcFile = new SrcMLFile("output1.cpp.xml");
                    Assert.IsNotNull(srcFile);

                    var files = srcFile.FileUnits.ToList();
                    Assert.AreEqual(1, files.Count());

                    string file1 = Path.Combine(TestInputPath, "input2.cpp");
                    var f2 = (from ele in files
                              where ele.Attribute("filename").Value == file1
                              select ele);
                    Assert.AreEqual(file1, f2.FirstOrDefault().Attribute("filename").Value);
                }
            }
        }

        /// <summary>
        /// Tests the creation of srcML from a file and returned via string
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveFtM() {
            IntPtr s = new IntPtr(0);
            List<String> documents = new List<String>();
            using (Archive srcmlarchive = new Archive(), srcmlarchive2 = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitFilename(Path.Combine(TestInputPath, "input.cpp"));
                    srcmlunit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();

                    srcmlunit.SetUnitFilename(Path.Combine(TestInputPath, "input2.cpp"));
                    srcmlunit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive2.AddUnit(srcmlunit);
                    srcmlarchive2.ArchivePack();

                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();
                    IntPtr structPtr2 = srcmlarchive2.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    structArrayPtr.Add(structPtr2);

                    try {
                        s = LibSrcMLRunner.SrcmlCreateArchiveFtM(structArrayPtr.ToArray(), structArrayPtr.Count());
                    }
                    catch (Exception e) {
                        throw new SrcMLException(e.Message, e);
                    }
                    for (int i = 0; i < 2; ++i) {
                        IntPtr docptr = Marshal.ReadIntPtr(s);
                        String docstr = Marshal.PtrToStringAnsi(docptr);
                        Marshal.FreeHGlobal(docptr);
                        documents.Add(docstr);
                        s += Marshal.SizeOf(typeof(IntPtr));
                    }
                }
                Assert.False(String.IsNullOrEmpty(documents.ElementAt(0)));
                XDocument doc = XDocument.Parse(documents.ElementAt(0));
                var units = from unit in doc.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                            where unit.Attribute("filename") != null
                            select unit;
                string file = Path.Combine(TestInputPath, "input.cpp"); ;
                var f1 = (from ele in units
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);

                Assert.False(String.IsNullOrEmpty(documents.ElementAt(1)));
                XDocument doc2 = XDocument.Parse(documents.ElementAt(1));
                var units2 = from unit in doc2.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                             where unit.Attribute("filename") != null
                             select unit;
                string file2 = Path.Combine(TestInputPath, "input2.cpp"); ;
                var f2 = (from ele in units2
                          where ele.Attribute("filename").Value == file2
                          select ele);
                Assert.AreEqual(file2, f2.FirstOrDefault().Attribute("filename").Value);
            }
        }

        /// <summary>
        /// Tests the creation of srcML from code in memory and placed in a file
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtF() {
            using (Archive srcmlarchive = new Archive()) {
                List<String> BufferList = new List<String>();
                List<String> FileList = new List<String>();

                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";

                FileList.Add("input.cpp");
                FileList.Add("input2.cpp");

                BufferList.Add(str);
                BufferList.Add(str2);

                var buffandfile = BufferList.Zip(FileList, (b, f) => new { buf = b, file = f });
                foreach (var pair in buffandfile) {
                    using (Unit srcmlunit = new Unit()) {
                        srcmlunit.SetUnitBuffer(pair.buf);
                        srcmlunit.SetUnitFilename(pair.file);

                        srcmlarchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                        srcmlarchive.AddUnit(srcmlunit);
                    }
                }
                srcmlarchive.ArchivePack();

                IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                List<IntPtr> structArrayPtr = new List<IntPtr>();
                structArrayPtr.Add(structPtr);

                Assert.True(LibSrcMLRunner.SrcmlCreateArchiveMtF(structArrayPtr.ToArray(), structArrayPtr.Count(), "output") == 0);
                Assert.True(File.Exists("output0.cpp.xml"));

                SrcMLFile srcFile = new SrcMLFile("output0.cpp.xml");
                Assert.IsNotNull(srcFile);

                var files = srcFile.FileUnits.ToList();
                Assert.AreEqual(2, files.Count());


                string file = "input.cpp";
                var f1 = (from ele in files
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);

                string file2 = "input2.cpp";
                var f2 = (from ele in files
                          where ele.Attribute("filename").Value == file2
                          select ele);
                Assert.AreEqual(file2, f2.FirstOrDefault().Attribute("filename").Value);
            }
        }

        /// <summary>
        /// Tests the creation of srcML from code in memory and returned as a string
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtM() {
            using (Archive srcmlarchive = new Archive()) {
                List<String> BufferList = new List<String>();
                List<String> FileList = new List<String>();
                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";
                BufferList.Add(str);
                BufferList.Add(str2);

                FileList.Add("input.cpp");
                FileList.Add("input2.cpp");

                var buffandfile = BufferList.Zip(FileList, (b, f) => new { buf = b, file = f });
                foreach (var pair in buffandfile) {
                    using (Unit srcmlunit = new Unit()) {
                        srcmlunit.SetUnitBuffer(pair.buf);
                        srcmlunit.SetUnitFilename(pair.file);

                        srcmlunit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                        srcmlarchive.AddUnit(srcmlunit);
                    }
                }

                srcmlarchive.ArchivePack();
                IntPtr structPtr = srcmlarchive.GetPtrToStruct();
                List<IntPtr> structArrayPtr = new List<IntPtr>();
                structArrayPtr.Add(structPtr);
                IntPtr s = new IntPtr(0);
                try {
                    s = LibSrcMLRunner.SrcmlCreateArchiveMtM(structArrayPtr.ToArray(), structArrayPtr.Count());
                }
                catch (Exception e) {
                    Console.WriteLine("EXCEPTION: {0}", e.Message);
                    Assert.True(false);
                }

                List<String> documents = new List<String>();
                for (int i = 0; i < 1; ++i) {
                    IntPtr docptr = Marshal.ReadIntPtr(s);
                    String docstr = Marshal.PtrToStringAnsi(docptr);
                    Marshal.FreeHGlobal(docptr);
                    documents.Add(docstr);
                    s += Marshal.SizeOf(typeof(IntPtr));
                }

                Assert.False(String.IsNullOrEmpty(documents.ElementAt(0)));
                XDocument doc = XDocument.Parse(documents.ElementAt(0));
                var units = from unit in doc.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                            where unit.Attribute("filename") != null
                            select unit;

                string file = "input.cpp";
                var f1 = (from ele in units
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);

                string file2 = "input2.cpp";
                var f2 = (from ele in units
                          where ele.Attribute("filename").Value == file2
                          select ele);
                Assert.AreEqual("input2.cpp", f2.FirstOrDefault().Attribute("filename").Value);
                Console.WriteLine(s);
            }
        }
        [Test]
        public void TestGenerateSrcMLFromString() {
            LibSrcMLRunner run = new LibSrcMLRunner();
            try {
                string b = run.GenerateSrcMLFromString("int main(){int x;}", "input.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_MODIFIER }, false);

                Assert.False(String.IsNullOrEmpty(b));

                XDocument doc = XDocument.Parse(b);
                var units = from unit in doc.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                            where unit.Attribute("filename") != null
                            select unit;

                string file = "input.cpp";
                var f1 = (from ele in units
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);
            }
            catch (SrcMLException e) {
                throw e;
            }
        }
        [Test]
        public void TestGenerateSrcMLFromStrings() {
            List<String> BufferList = new List<String>();
            List<String> FileList = new List<String>();

            String str = "int main(){int c; c = 0; ++c;}";
            String str2 = "int foo(){int c; c = 0; ++c;}";

            FileList.Add("input.cpp");
            FileList.Add("input2.cpp");
            BufferList.Add(str);
            BufferList.Add(str2);

            LibSrcMLRunner run = new LibSrcMLRunner();

            try {
                List<string> b = run.GenerateSrcMLFromStrings(BufferList, FileList, Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_MODIFIER }, false).ToList<string>();
                Assert.True(Convert.ToBoolean(b.Count));
                XDocument doc = XDocument.Parse(b.ElementAt(0));
                var units = from unit in doc.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                            where unit.Attribute("filename") != null
                            select unit;

                string file = "input.cpp";
                var f1 = (from ele in units
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);

                string file2 = "input2.cpp";
                var f2 = (from ele in units
                          where ele.Attribute("filename").Value == file2
                          select ele);
                Assert.AreEqual(file2, f2.FirstOrDefault().Attribute("filename").Value);
            }
            catch (SrcMLException e) {
                throw e;
            }
        }
        [Test]
        public void TestGenerateSrcMLFromFile() {
            LibSrcMLRunner run = new LibSrcMLRunner();
            try {
                run.GenerateSrcMLFromFile(Path.Combine(TestInputPath, "input.cpp"), "output", Language.CPlusPlus, new List<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_MODIFIER }, new Dictionary<string, Language>() { });

                Assert.True(File.Exists("output0.cpp.xml"));
                SrcMLFile srcFile = new SrcMLFile("output0.cpp.xml");
                Assert.IsNotNull(srcFile);

                var files = srcFile.FileUnits.ToList();
                Assert.AreEqual(1, files.Count());

                string file = Path.Combine(TestInputPath, "input.cpp");
                var f1 = (from ele in files
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);
            }
            catch (SrcMLException e) {
                throw new SrcMLException(e.Message, e);
            }
        }
        [Test]
        public void TestGenerateSrcMLFromFiles() {
            LibSrcMLRunner run = new LibSrcMLRunner();
            List<string> fileList = new List<string>() { Path.Combine(TestInputPath, "input.cpp"), Path.Combine(TestInputPath, "input2.cpp") };
            try {
                run.GenerateSrcMLFromFiles(fileList, "output", Language.CPlusPlus, new List<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_MODIFIER }, new Dictionary<string, Language>() { });

                Assert.True(File.Exists("output0.cpp.xml"));
                SrcMLFile srcFile = new SrcMLFile("output0.cpp.xml");
                Assert.IsNotNull(srcFile);

                var files = srcFile.FileUnits.ToList();
                Assert.AreEqual(2, files.Count());

                string file = Path.Combine(TestInputPath, "input.cpp");
                var f1 = (from ele in files
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual(file, f1.FirstOrDefault().Attribute("filename").Value);

                string file2 = Path.Combine(TestInputPath, "input2.cpp");
                var f2 = (from ele in files
                          where ele.Attribute("filename").Value == file2
                          select ele);
                Assert.AreEqual(file2, f2.FirstOrDefault().Attribute("filename").Value);
            }
            catch (SrcMLException e) {
                throw e;
            }
        }

        #region WrapperTests
        [Test]
        public void TestArchiveSetSrcEncoding() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveSrcEncoding("ISO-8859-1");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();

                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetSrcEncoding(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveXmlEncoding() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveXmlEncoding("ISO-8859-1");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetXmlEncoding(structArrayPtr.ToArray()));
                }
            }
        }

        [Test]
        public void TestArchiveSetLanguage() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetLanguage(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetUrl() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveUrl("http://www.srcml.org/");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetUrl(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetVersion() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveSrcVersion("1.0");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetVersion(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetOptions() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetOptions(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetOptions(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveEnableOption() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.EnableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveEnableOption(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveDisableOption() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetOptions(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlarchive.DisableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveDisableOption(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetTabstop() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetArchiveTabstop(2);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetTabstop(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterFileExtension() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.RegisterFileExtension("h", LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterFileExtension(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterNamespace() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.RegisterNamespace("abb", "www.abb.com");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterNamespace(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetProcessingInstruction() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.SetProcessingInstruction("hpp", "data");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetProcessingInstruction(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterMacro() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlarchive.RegisterMacro("Token", "type");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterMacro(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetFilename() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitFilename("Bleep.cpp");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetFilename(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetLanguage() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetLanguage(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetSrcEncoding() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitSrcEncoding("UTF-8");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetSrcEncoding(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetUrl() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitUrl("www.abb.com");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetUrl(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetVersion() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitSrcVersion("1.0");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetVersion(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetTimestamp() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetUnitTimestamp("0800");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetTimestamp(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetHash() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.SetHash("hash");
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetHash(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitUnparseSetEol() {
            using (Archive srcmlarchive = new Archive()) {
                using (Unit srcmlunit = new Unit()) {
                    srcmlunit.UnparseSetEol(50);
                    srcmlarchive.AddUnit(srcmlunit);
                    srcmlarchive.ArchivePack();
                    IntPtr structPtr = srcmlarchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitUnparseSetEol(structArrayPtr.ToArray()));
                }
            }
        }
        #endregion
    }
}
