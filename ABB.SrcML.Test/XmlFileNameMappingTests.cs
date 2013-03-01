using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Test {
    [TestFixture]
    public class XmlFileNameMappingTests {
        [Test]
        public void TestGetXmlPath_CorrectDir() {
            var map = new XmlFileNameMapping("srcml_xml");
            var xmlPath = map.GetXMLPath("Example.cpp");
            Assert.That(xmlPath.StartsWith(map.XmlDirectory));
        }

        [Test]
        public void TestGetXmlPath_NonExistentDir() {
            Assert.That(!File.Exists("MissingDir"));
            var map = new XmlFileNameMapping("MissingDir");
            var xmlPath = map.GetXMLPath("Example.cpp");
            Assert.That(xmlPath.StartsWith(map.XmlDirectory));
        }

        [Test]
        public void TestGetXmlPath_SameName() {
            var map = new XmlFileNameMapping("srcml_xml");
            var xmlPath1 = map.GetXMLPath("Example.cpp");
            var xmlPath2 = map.GetXMLPath(@"Subdir\Example.cpp");
            Assert.AreNotEqual(xmlPath1, xmlPath2);
        }

        [Test]
        public void TestGetXmlPath_Repeated() {
            var map = new XmlFileNameMapping("srcml_xml");
            var xmlPath1 = map.GetXMLPath("Example.cpp");
            var xmlPath2 = map.GetXMLPath("Example.cpp");
            Assert.AreEqual(xmlPath1, xmlPath2);
        }

        [Test]
        public void TestConstructor_CurrentDirectory() {
            var map = new XmlFileNameMapping(".");
            var xmlPath = map.GetXMLPath("Foo.cs");
            Assert.That(xmlPath.StartsWith(Environment.CurrentDirectory));
        }

        [Test]
        public void TestRountrip() {
            var map = new XmlFileNameMapping("srcml_xml");
            var xmlPath = map.GetXMLPath("Foo.cs");
            Assert.AreEqual(Path.GetFullPath("Foo.cs"), map.GetSourcePath(xmlPath));
        }

        [Test]
        public void TestGetSourcePath_FullPath() {
            var map = new XmlFileNameMapping("srcml_xml");
            var sourcePath = map.GetSourcePath(@"C:\Foo\bar\C=-source-GridView_2010_06_03 (7.0)-AbsUpdater.h.xml");
            Assert.AreEqual(@"C:\source\GridView_2010_06_03 (7.0)\AbsUpdater.h", sourcePath);
        }

        [Test]
        public void TestGetSourcePath_JustFileName() {
            var map = new XmlFileNameMapping("srcml_xml");
            var sourcePath = map.GetSourcePath(@"C=-source-GridView_2010_06_03 (7.0)-AbsUpdater.h.xml");
            Assert.AreEqual(@"C:\source\GridView_2010_06_03 (7.0)\AbsUpdater.h", sourcePath);
        }
        
    }
}
