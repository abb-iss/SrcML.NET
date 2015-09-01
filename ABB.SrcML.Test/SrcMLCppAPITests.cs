using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NUnit.Framework;
using ABB.SrcML;
using System.Xml;
namespace ABB.SrcML.Test {
    [TestFixture]
    [Category("Build")]
    class SrcMLCppAPITests {
        /// <summary>
        /// This tests the creation of an archive called 'output.cpp.xml' using a list of source files
        /// from which the archive will be derived.
        /// </summary>
        [Test]
        public void TestCreateArchiveFromListOfFiles() {
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");
            Assert.True(SrcMLCppAPI.SrcmlCreateArchiveFromListOfFiles(l.ToArray(), l.Count(), "output.cpp.xml") == 0);
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
            //Further testing should check for units
        }
        /// TODO: This requires some modifications to be made to SrcMLFile so that it can take strings of xml. Going to do
        /// A pull of what I have so far before I go making those kinds of changes.
        /// <summary>
        /// Tests the creation of srcML to be returned as a string instead of being placed in af file.
        /// </summary>
        [Test]
        public void TestCreateArchiveInMemory() {
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");
            string s = SrcMLCppAPI.SrcmlCreateArchiveInMemory(l.ToArray(), l.Count());
            Assert.False(String.IsNullOrEmpty(s));
            //XmlReader reader = XmlReader.Create(new StringReader(s));
            Console.WriteLine(s);
        }
    }
}
