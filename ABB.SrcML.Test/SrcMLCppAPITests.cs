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
    class SrcMLCppAPITests {
        /// <summary>
        /// This tests the creation of an archive using a list of source files
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveFtF() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();
            ad.SetArchiveFilename("input.cpp");
            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            SrcMLCppAPI.SourceData bc = new SrcMLCppAPI.SourceData();
            bc.SetArchiveFilename("input2.cpp");
            bc.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            List<SrcMLCppAPI.SourceData> data = new List<SrcMLCppAPI.SourceData>();
            data.Add(ad);
            data.Add(bc);

            IntPtr structPtr = SrcMLCppAPI.CreatePtrFromStruct(ad);
            IntPtr structPtr2 = SrcMLCppAPI.CreatePtrFromStruct(bc);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            structArrayPtr.Add(structPtr2);
            try {
                Assert.True(SrcMLCppAPI.SrcmlCreateArchiveFtF(structArrayPtr.ToArray(), structArrayPtr.Count(), "output") == 0);
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

                string file = "input.cpp";
                var f1 = (from ele in files
                          where ele.Attribute("filename").Value == file
                          select ele);
                Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);
            }
            {
                Assert.True(File.Exists("output1.cpp.xml"));
                SrcMLFile srcFile = new SrcMLFile("output1.cpp.xml");
                Assert.IsNotNull(srcFile);

                var files = srcFile.FileUnits.ToList();
                Assert.AreEqual(1, files.Count());

                string file1 = "input2.cpp";
                var f2 = (from ele in files
                          where ele.Attribute("filename").Value == file1
                          select ele);
                Assert.AreEqual("input2.cpp", f2.FirstOrDefault().Attribute("filename").Value);
            }
            ad.Dispose();
            bc.Dispose();
        }

        /// <summary>
        /// Tests the creation of srcML from a file and returned via string
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveFtM() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();
            ad.SetArchiveFilename("input.cpp");
            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            SrcMLCppAPI.SourceData bc = new SrcMLCppAPI.SourceData();
            bc.SetArchiveFilename("input2.cpp");
            bc.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            List<SrcMLCppAPI.SourceData> data = new List<SrcMLCppAPI.SourceData>();
            data.Add(ad);
            data.Add(bc);

            IntPtr structPtr = SrcMLCppAPI.CreatePtrFromStruct(ad);
            IntPtr structPtr2 = SrcMLCppAPI.CreatePtrFromStruct(bc);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);
            structArrayPtr.Add(structPtr2);

            string s = SrcMLCppAPI.SrcmlCreateArchiveFtM(structArrayPtr.ToArray(), structArrayPtr.Count());

            Assert.False(String.IsNullOrEmpty(s));
            XDocument doc = XDocument.Parse(s);
            var units = from unit in doc.Descendants(XName.Get("unit", "http://www.srcML.org/srcML/src"))
                        where unit.Attribute("filename") != null
                        select unit;
            /*TODO: FIX.
            string file = "input.cpp";
            var f1 = (from ele in units
                      where ele.Attribute("filename").Value == file
                      select ele);
            Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);
            */
            string file2 = "input2.cpp";
            var f2 = (from ele in units
                      where ele.Attribute("filename").Value == file2
                      select ele);
            Assert.AreEqual("input2.cpp", f2.FirstOrDefault().Attribute("filename").Value);

            //XmlReader reader = XmlReader.Create(new StringReader(s));
            Console.WriteLine(s);

            ad.Dispose();
            bc.Dispose();
        }

        /// <summary>
        /// Tests the creation of srcML from code in memory and placed in a file
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtF() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();

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

            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            IntPtr structPtr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);

            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveMtF(structArrayPtr.ToArray(), structArrayPtr.Count(), "test") == 0);
            Assert.True(File.Exists("test0.cpp.xml"));
            
            SrcMLFile srcFile = new SrcMLFile("test0.cpp.xml");
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
            Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);

            ad.Dispose();
        }

        /// <summary>
        /// Tests the creation of srcML from code in memory and returned as a string
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtM() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();

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

            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            IntPtr structPtr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);

            string s = SrcMLCppAPI.SrcmlCreateArchiveMtM(structArrayPtr.ToArray(), structArrayPtr.Count());

            Assert.False(String.IsNullOrEmpty(s));
            XDocument doc = XDocument.Parse(s);
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
            ad.Dispose();
        }
      
    }
}
