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
            using (Archive srcmlArchive = new Archive(), srcmlArchive2 = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitFilename(Path.Combine(TestInputPath, "input.cpp"));
                    srcmlArchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.SetOutputFile("output");
                    srcmlArchive.ArchivePack();

                    srcmlUnit.SetUnitFilename(Path.Combine(TestInputPath, "input2.cpp"));
                    srcmlArchive2.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive2.AddUnit(srcmlUnit);
                    srcmlArchive2.SetOutputFile("output");
                    srcmlArchive2.ArchivePack();

                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();
                    IntPtr structPtr2 = srcmlArchive2.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    structArrayPtr.Add(structPtr2);

                    try {
                        Assert.True(LibSrcMLRunner.SrcmlCreateArchiveFtF(structArrayPtr.ToArray(), structArrayPtr.Count()) == IntPtr.Zero);
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
            using (Archive srcmlArchive = new Archive(), srcmlArchive2 = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitFilename(Path.Combine(TestInputPath, "input.cpp"));
                    srcmlUnit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();

                    srcmlUnit.SetUnitFilename(Path.Combine(TestInputPath, "input2.cpp"));
                    srcmlUnit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive2.AddUnit(srcmlUnit);
                    srcmlArchive2.ArchivePack();

                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();
                    IntPtr structPtr2 = srcmlArchive2.GetPtrToStruct();

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
            using (Archive srcmlArchive = new Archive()) {
                List<String> bufferList = new List<String>();
                List<String> fileList = new List<String>();

                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";

                fileList.Add("input.cpp");
                fileList.Add("input2.cpp");

                bufferList.Add(str);
                bufferList.Add(str2);

                var buffandfile = bufferList.Zip(fileList, (b, f) => new { buf = b, file = f });
                foreach (var pair in buffandfile) {
                    using (Unit srcmlUnit = new Unit()) {
                        srcmlUnit.SetUnitBuffer(pair.buf);
                        srcmlUnit.SetUnitFilename(pair.file);

                        srcmlArchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                        srcmlArchive.AddUnit(srcmlUnit);
                    }
                }
                srcmlArchive.SetOutputFile("output");
                srcmlArchive.ArchivePack();

                IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                List<IntPtr> structArrayPtr = new List<IntPtr>();
                structArrayPtr.Add(structPtr);

                Assert.True(LibSrcMLRunner.SrcmlCreateArchiveMtF(structArrayPtr.ToArray(), structArrayPtr.Count()) == IntPtr.Zero);
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
            using (Archive srcmlArchive = new Archive()) {
                List<String> bufferList = new List<String>();
                List<String> fileList = new List<String>();
                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";
                bufferList.Add(str);
                bufferList.Add(str2);

                fileList.Add("input.cpp");
                fileList.Add("input2.cpp");

                var buffandfile = bufferList.Zip(fileList, (b, f) => new { buf = b, file = f });
                foreach (var pair in buffandfile) {
                    using (Unit srcmlUnit = new Unit()) {
                        srcmlUnit.SetUnitBuffer(pair.buf);
                        srcmlUnit.SetUnitFilename(pair.file);

                        srcmlUnit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                        srcmlArchive.AddUnit(srcmlUnit);
                    }
                }

                srcmlArchive.ArchivePack();
                IntPtr structPtr = srcmlArchive.GetPtrToStruct();
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
            List<String> bufferList = new List<String>();
            List<String> fileList = new List<String>();

            String str = "int main(){int c; c = 0; ++c;}";
            String str2 = "int foo(){int c; c = 0; ++c;}";

            fileList.Add("input.cpp");
            fileList.Add("input2.cpp");
            bufferList.Add(str);
            bufferList.Add(str2);

            LibSrcMLRunner run = new LibSrcMLRunner();

            try {
                List<string> b = run.GenerateSrcMLFromStrings(bufferList, fileList, Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_MODIFIER }, false).ToList<string>();
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
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveSrcEncoding("ISO-8859-1");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();

                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetSrcEncoding(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveXmlEncoding() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveXmlEncoding("ISO-8859-1");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetXmlEncoding(structArrayPtr.ToArray()));
                }
            }
        }

        [Test]
        public void TestArchiveSetLanguage() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetLanguage(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetUrl() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveUrl("http://www.srcml.org/");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetUrl(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetVersion() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveSrcVersion("1.0");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetVersion(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetOptions() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetOptions(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetOptions(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveEnableOption() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.EnableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveEnableOption(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveDisableOption() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetOptions(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlArchive.DisableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveDisableOption(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetTabstop() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetArchiveTabstop(2);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetTabstop(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterFileExtension() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.RegisterFileExtension("h", LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterFileExtension(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterNamespace() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.RegisterNamespace("abb", "www.abb.com");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterNamespace(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveSetProcessingInstruction() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.SetProcessingInstruction("hpp", "data");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveSetProcessingInstruction(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestArchiveRegisterMacro() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlArchive.RegisterMacro("Token", "type");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterMacro(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetFilename() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitFilename("Bleep.cpp");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetFilename(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetLanguage() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetLanguage(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetSrcEncoding() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitSrcEncoding("UTF-8");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetSrcEncoding(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetUrl() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitUrl("www.abb.com");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetUrl(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetVersion() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitSrcVersion("1.0");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetVersion(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetTimestamp() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetUnitTimestamp("0800");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetTimestamp(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitSetHash() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.SetHash("hash");
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitSetHash(structArrayPtr.ToArray()));
                }
            }
        }
        [Test]
        public void TestUnitUnparseSetEol() {
            using (Archive srcmlArchive = new Archive()) {
                using (Unit srcmlUnit = new Unit()) {
                    srcmlUnit.UnparseSetEol(50);
                    srcmlArchive.AddUnit(srcmlUnit);
                    srcmlArchive.ArchivePack();
                    IntPtr structPtr = srcmlArchive.GetPtrToStruct();

                    List<IntPtr> structArrayPtr = new List<IntPtr>();
                    structArrayPtr.Add(structPtr);
                    Assert.IsTrue(LibSrcMLRunner.TestUnitUnparseSetEol(structArrayPtr.ToArray()));
                }
            }
        }
        #endregion
    }
}
