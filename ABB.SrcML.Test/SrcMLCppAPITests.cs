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
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");

            SrcMLCppAPI.ArchiveAdapter ad = new SrcMLCppAPI.ArchiveAdapter();
            
            ad.SetArchiveXmlEncoding("abc");
            ad.SetArchiveFilename("abc.cpp.xml");
            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveFtF(l.ToArray(), l.Count(), "output.cpp.xml", ptr) == 0);
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
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");
            SrcMLCppAPI.ArchiveAdapter ad = new SrcMLCppAPI.ArchiveAdapter();
            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);
            string s = SrcMLCppAPI.SrcmlCreateArchiveFtM(l.ToArray(), l.Count(), ptr);
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
            String str = "int main(){int c; c = 0; ++c;}";

            SrcMLCppAPI.ArchiveAdapter ad = new SrcMLCppAPI.ArchiveAdapter();
            ad.SetArchiveFilename("input.cpp");
            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveMtF(str, str.Length, "output.cpp.xml", ptr) == 0);
            Assert.True(File.Exists("output.cpp.xml"));

            SrcMLFile srcFile = new SrcMLFile("output.cpp.xml");
            Assert.IsNotNull(srcFile);

            var files = srcFile.FileUnits.ToList();
            Assert.AreEqual(1, files.Count());

            string file = "input.cpp";
            var f1 = (from ele in files
                      where ele.Attribute("filename").Value == file
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
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");
            String str = "int main(){int c; c = 0; ++c;}";

            SrcMLCppAPI.ArchiveAdapter ad = new SrcMLCppAPI.ArchiveAdapter();
            ad.SetArchiveFilename("input.cpp");
            IntPtr ptr = SrcMLCppAPI.CreatePtrFromStruct(ad);

            string s = SrcMLCppAPI.SrcmlCreateArchiveMtM(str, str.Length, ptr);
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
            Console.WriteLine(s);
        }

    }
}
