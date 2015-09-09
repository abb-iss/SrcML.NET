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
        /// This tests the creation of an archive called 'output.cpp.xml' using a list of source files
        /// from which the archive will be derived.
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

            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);
            IntPtr ptr2 = SrcMLCppAPI.CreatePtrFromStruct(bc);

            List<IntPtr> ptrptr = new List<IntPtr>();
            ptrptr.Add(ptr);
            ptrptr.Add(ptr2);

            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveFtF(ptrptr.ToArray(), ptrptr.Count(), "output.cpp.xml") == 0);
            Assert.True(File.Exists("output.cpp.xml"));

            SrcMLFile srcFile = new SrcMLFile("output.cpp.xml");
            Assert.IsNotNull(srcFile);

            var files = srcFile.FileUnits.ToList();
            Assert.AreEqual(2, files.Count());

            string file = "input.cpp";
            var f1 = (from ele in files
                      where ele.Attribute("filename").Value == file
                      select ele);
            Assert.AreEqual("input.cpp", f1.FirstOrDefault().Attribute("filename").Value);

            string file1 = "input2.cpp";
            var f2 = (from ele in files
                      where ele.Attribute("filename").Value == file1
                      select ele);
            Assert.AreEqual("input2.cpp", f2.FirstOrDefault().Attribute("filename").Value);
        }
        /// TODO: This requires some modifications to be made to SrcMLFile so that it can take strings of xml. Going to do
        /// A pull of what I have so far before I go making those kinds of changes.
        /// <summary>
        /// Tests the creation of srcML to be returned as a string instead of being placed in af file.
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

            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);
            IntPtr ptr2 = SrcMLCppAPI.CreatePtrFromStruct(bc);

            List<IntPtr> ptrptr = new List<IntPtr>();
            ptrptr.Add(ptr);
            ptrptr.Add(ptr2);

            string s = SrcMLCppAPI.SrcmlCreateArchiveFtM(ptrptr.ToArray(), ptrptr.Count());

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

            //XmlReader reader = XmlReader.Create(new StringReader(s));
            Console.WriteLine(s);
        }
        /// TODO: This requires some modifications to be made to SrcMLFile so that it can take strings of xml. Going to do
        /// A pull of what I have so far before I go making those kinds of changes.
        /// <summary>
        /// Tests the creation of srcML to be returned as a string instead of being placed in af file.
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtF() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();

            List<String> fileL = new List<String>();
            List<String> fileL2 = new List<String>();
            String str = "int main(){int c; c = 0; ++c;}";
            String str2 = "int foo(){int c; c = 0; ++c;}";
            fileL.Add(str);
            fileL.Add(str2);
            ad.SetArchiveBuffer(fileL);

            
            fileL2.Add("input.cpp");
            fileL2.Add("input2.cpp");
            ad.SetArchiveFilename(fileL2);

            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            List<IntPtr> ptrptr = new List<IntPtr>();
            ptrptr.Add(ptr);

            //Console.WriteLine("check: {0}", Marshal.PtrToStringAnsi(ad.buffer[0]));
            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveMtF(ptrptr.ToArray(), ptrptr.Count(), "test.xml") == 0);
            Assert.True(File.Exists("test.xml"));
            
            SrcMLFile srcFile = new SrcMLFile("test.xml");
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
            
        }
        /// TODO: This requires some modifications to be made to SrcMLFile so that it can take strings of xml. Going to do
        /// A pull of what I have so far before I go making those kinds of changes.
        /// <summary>
        /// Tests the creation of srcML to be returned as a string instead of being placed in af file.
        /// </summary>
        [Test]
        public void TestCreateSrcMLArchiveMtM() {
            SrcMLCppAPI.SourceData ad = new SrcMLCppAPI.SourceData();

            List<String> fileL = new List<String>();
            List<String> fileL2 = new List<String>();
            String str = "int main(){int c; c = 0; ++c;}";
            String str2 = "int foo(){int c; c = 0; ++c;}";
            fileL.Add(str);
            fileL.Add(str2);
            ad.SetArchiveBuffer(fileL);


            fileL2.Add("input.cpp");
            fileL2.Add("input2.cpp");
            ad.SetArchiveFilename(fileL2);

            ad.SetArchiveLanguage(SrcMLCppAPI.SrcMLOptions.SRCML_LANGUAGE_CXX);

            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            List<IntPtr> ptrptr = new List<IntPtr>();
            ptrptr.Add(ptr);

            string s = SrcMLCppAPI.SrcmlCreateArchiveMtM(ptrptr.ToArray(), ptrptr.Count());

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
        }

    }
}
