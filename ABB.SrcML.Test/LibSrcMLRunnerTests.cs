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
            using (LibSrcMLRunner.SourceData bc = new LibSrcMLRunner.SourceData(), ad = new LibSrcMLRunner.SourceData()) {
                ad.SetArchiveFilename(Path.Combine(TestInputPath, "input.cpp"));
                ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);


                bc.SetArchiveFilename(Path.Combine(TestInputPath, "input2.cpp"));
                bc.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);

                IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);
                IntPtr structPtr2 = LibSrcMLRunner.CreatePtrFromStruct(bc);

                List<IntPtr> structArrayPtr = new List<IntPtr>();
                structArrayPtr.Add(structPtr);
                structArrayPtr.Add(structPtr2);
                try {
                    Assert.True(LibSrcMLRunner.SrcmlCreateArchiveFtF(structArrayPtr.ToArray(), structArrayPtr.Count(), "output") == 0);
                }
                catch (SrcMLException e) {
                    Console.WriteLine(e.Message);
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
            using (LibSrcMLRunner.SourceData bc = new LibSrcMLRunner.SourceData(), ad = new LibSrcMLRunner.SourceData()) {
                ad.SetArchiveFilename(Path.Combine(TestInputPath, "input.cpp"));
                ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);

                bc.SetArchiveFilename(Path.Combine(TestInputPath, "input2.cpp"));
                bc.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);

                IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);
                IntPtr structPtr2 = LibSrcMLRunner.CreatePtrFromStruct(bc);

                List<IntPtr> structArrayPtr = new List<IntPtr>();
                structArrayPtr.Add(structPtr);
                structArrayPtr.Add(structPtr2);
                IntPtr s = new IntPtr(0);
                try {
                    s = LibSrcMLRunner.SrcmlCreateArchiveFtM(structArrayPtr.ToArray(), structArrayPtr.Count());
                }
                catch (Exception e) {
                    throw new SrcMLException(e.Message, e);
                }

                List<String> documents = new List<String>();
                for (int i = 0; i < 2; ++i) {
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
            using (LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData()) {

                List<String> BufferList = new List<String>();
                List<String> FileList = new List<String>();
                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";
                BufferList.Add(str);
                BufferList.Add(str2);
                ad.SetArchiveBuffer(BufferList);


                FileList.Add("input.cpp");
                FileList.Add("input2.cpp");
                ad.SetArchiveFilename(FileList);

                ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);

                IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

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
            using (LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData()) {
                List<String> BufferList = new List<String>();
                List<String> FileList = new List<String>();
                String str = "int main(){int c; c = 0; ++c;}";
                String str2 = "int foo(){int c; c = 0; ++c;}";
                BufferList.Add(str);
                BufferList.Add(str2);
                ad.SetArchiveBuffer(BufferList);


                FileList.Add("input.cpp");
                FileList.Add("input2.cpp");
                ad.SetArchiveFilename(FileList);

                ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);

                IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

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
        #region WrapperTests
        [Test]
        public void TestArchiveSetSrcEncoding() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveSrcEncoding("ISO-8859-1");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetSrcEncoding(structArrayPtr.ToArray()));
        }

        [Test]
        public void TestArchiveXmlEncoding() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveXmlEncoding("ISO-8859-1");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetXmlEncoding(structArrayPtr.ToArray()));
        }

        [Test]
        public void TestArchiveSetLanguage() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetLanguage(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveSetUrl() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveUrl("http://www.srcml.org/");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetUrl(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveSetVersion() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveSrcVersion("1.0");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetVersion(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveSetOptions() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetOptions(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetOptions(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveEnableOption() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.EnableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveEnableOption(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveDisableOption() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.DisableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_LITERAL);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveDisableOption(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveSetTabstop() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveTabstop(2);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetTabstop(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveRegisterFileExtension() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.RegisterFileExtension("h", LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterFileExtension(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveRegisterNamespace() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.RegisterNamespace("abb", "www.abb.com");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterNamespace(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveSetProcessingInstruction() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetProcessingInstruction("hpp", "data");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveSetProcessingInstruction(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestArchiveRegisterMacro() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.RegisterMacro("Token", "type");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestArchiveRegisterMacro(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetFilename() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveFilename("Bleep.cpp");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetFilename(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetLanguage() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveLanguage(LibSrcMLRunner.SrcMLOptions.SRCML_LANGUAGE_CXX);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetLanguage(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetSrcEncoding() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveSrcEncoding("UTF-8");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetSrcEncoding(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetUrl() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveUrl("www.abb.com");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetUrl(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetVersion() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveSrcVersion("1.0");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetVersion(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetTimestamp() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetArchiveTimestamp("0800");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetTimestamp(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitSetHash() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.SetHash("hash");
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitSetHash(structArrayPtr.ToArray()));
        }
        [Test]
        public void TestUnitUnparseSetEol() {
            LibSrcMLRunner.SourceData ad = new LibSrcMLRunner.SourceData();
            ad.UnparseSetEol(50);
            IntPtr structPtr = LibSrcMLRunner.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            Assert.IsTrue(LibSrcMLRunner.TestUnitUnparseSetEol(structArrayPtr.ToArray()));
        }
        #endregion
    }
}
