/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ABB.SrcML;
using System.Xml.Linq;
using ABB.SrcML.Utilities;

namespace ABB.SrcML.Test {
    [TestFixture]
    [Category("Build")]
    public class SrcMLGeneratorTests {
        private SrcMLGenerator generator;
        
        [TestFixtureSetUp]
        public static void FixtureInitialize() {
            Directory.CreateDirectory("srcmltest");
            Directory.CreateDirectory("srcml_xml");
            File.WriteAllText("srcmltest\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));

            File.WriteAllText("srcmltest\\bar.c", String.Format(@"int bar() {{{0}    printf(""goodbye, world!"");{0}}}", Environment.NewLine));

            File.WriteAllText("srcmltest\\CSHARP.cs", @"using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ABB.SrcML;

namespace LoggingTransformation
{
    public class FunctionEntryLogTransform : ITransform
    {
        public IEnumerable<XElement> Query(XElement element)
        {
            var functions = from method in element.Descendants(SRC.method)
                                select method;
            return functions;
        }

        public XElement Transform(XElement element)
        {
            var first = element.Element(SRC.Block).Elements().First();
            var functionName = element.Element(SRC.Name).Value;
            first.AddBeforeSelf(new XElement(SRC.ExpressionStatement, string.Format(""LOG_FUNCTION_ENTRY(\""{0}\"");\n\t"", functionName)));

            return element;
        }
    }
}
");
            File.WriteAllText("srcmltest\\File with spaces.cpp", String.Format(@"int foo() {{{0}    printf(""hello world!"");{0}}}", Environment.NewLine));

            Directory.CreateDirectory("badPathTest");
            Directory.CreateDirectory("badPathTest\\BadPath™");
            File.WriteAllText("badPathTest\\BadPath™\\badPathTest.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText("badPathTest\\fooBody.c", String.Format(@"int foo() {{{0}printf(""hello world!™"");{0}}}", Environment.NewLine));
        }

        [TestFixtureTearDown]
        public static void FixtureCleanup() {
            Directory.Delete("srcmltest", true);
            Directory.Delete("srcml_xml", true);
            Directory.Delete("badPathTest", true);
        }

        [SetUp]
        public void TestSetup() {
            generator = new SrcMLGenerator(TestConstants.SrcmlPath);
        }

        [Test]
        public void DifferentLanguageTest() {
            generator.GenerateSrcMLFromFile("srcmltest\\CSHARP.cs", "srcml_xml\\differentlanguage_java.xml", Language.Java);
            var doc = new SrcMLFile("srcml_xml\\differentlanguage_java.xml");
            Assert.IsNotNull(doc);
        }

        [Test, Category("SrcMLUpdate")]
        public void TestStrangeEncodings([Values(@"badPathTest\BadPath™\badPathTest.c", @"srcmltest\fooBody.c")] string sourceFileName) {
            var xmlFileName = Path.Combine("srcml_xml", Path.GetFileName(Path.ChangeExtension(sourceFileName, ".xml")));
            generator.GenerateSrcMLFromFile(sourceFileName, xmlFileName, Language.C);
            var doc = new SrcMLFile(xmlFileName);
            Assert.IsNotNull(doc);
        }

        [Test]
        public void SrcMLFromStringTest() {
            string sourceCode = @"int foo() {
printf(""hello world!"");
}";
            string xml = generator.GenerateSrcMLFromString(sourceCode, Language.C);

            XElement element = XElement.Parse(xml);

            Assert.IsNotNull(element);
        }

        [Test]
        public void InvalidLanguageTest() {
            generator.GenerateSrcMLFromFile("srcmltest\\foo.c", "srcml_xml\\invalidlanguage_java.xml", Language.Java);
            var doc = new SrcMLFile("srcml_xml\\invalidlanguage_java.xml");
            Assert.IsNotNull(doc);

            doc = null;
            generator.GenerateSrcMLFromFile("srcmltest\\foo.c", "srcml_xml\\invalidlanguage_cpp.xml", Language.CPlusPlus);
            doc = new SrcMLFile("srcml_xml\\invalidlanguage_cpp.xml");
            Assert.IsNotNull(doc);

            doc = null;
            generator.GenerateSrcMLFromFile("srcmltest\\foo.c", "srcml_xml\\invalidlanguage_c.xml", Language.C);
            doc = new SrcMLFile("srcml_xml\\invalidlanguage_c.xml");

            Assert.IsNotNull(doc);
        }

        [Test]
        public void SingleFileTest() {
            generator.GenerateSrcMLFromFile("srcmltest\\foo.c", "srcml_xml\\singlefile.xml");
            var unit = SrcMLElement.Load("srcml_xml\\singlefile.xml");
            Assert.IsNotNull(unit);
            Assert.AreEqual("srcmltest\\foo.c", unit.Attribute("filename").Value);
        }

        [Test]
        public void MultipleFilesTest() {
            var doc = generator.GenerateSrcMLFileFromFiles(new string[] { "srcmltest\\foo.c", "srcmltest\\bar.c" }, "srcml_xml\\multiplefile.xml");

            Assert.IsNotNull(doc);
            var files = doc.FileUnits.ToList();
            Assert.AreEqual(2, files.Count());
            Assert.AreEqual("srcmltest\\foo.c", files[0].Attribute("filename").Value);
            Assert.AreEqual("srcmltest\\bar.c", files[1].Attribute("filename").Value);
        }

        [Test]
        public void MultipleFilesTest_DifferentDirectories() {
            var doc = generator.GenerateSrcMLFileFromFiles(new string[] { "srcmltest\\foo.c", "srcmltest\\bar.c", "..\\..\\TestInputs\\baz.cpp" }, "srcml_xml\\multiplefile.xml");

            Assert.IsNotNull(doc);
            var files = doc.FileUnits.ToList();
            Assert.AreEqual(3, files.Count());
            Assert.AreEqual("srcmltest\\foo.c", files[0].Attribute("filename").Value);
            Assert.AreEqual("srcmltest\\bar.c", files[1].Attribute("filename").Value);
            Assert.AreEqual("TestInputs\\baz.cpp", files[2].Attribute("filename").Value);
        }

        [Test]
        public void MultipleFilesTest_Language() {
            generator.GenerateSrcMLFromFiles(new string[] { "srcmltest\\foo.c", "srcmltest\\bar.c" }, "srcml_xml\\multiplefile.xml", Language.CPlusPlus);
            var doc = new SrcMLFile("srcml_xml\\multiplefile.xml");

            Assert.IsNotNull(doc);
            var files = doc.FileUnits.ToList();
            Assert.AreEqual(2, files.Count());
            Assert.AreEqual("srcmltest\\foo.c", files[0].Attribute("filename").Value);
            Assert.AreEqual("C++", files[0].Attribute("language").Value);
            Assert.AreEqual("srcmltest\\bar.c", files[1].Attribute("filename").Value);
            Assert.AreEqual("C++", files[1].Attribute("language").Value);
        }

        [Test]
        public void ExclusionFilterTest() {
            var exclusionList = new List<string>();
            exclusionList.Add("srcmltest\\bar.c");
            exclusionList.Add("srcmltest\\BadPath™\\badPathTest.c");
            exclusionList.Add("srcmltest\\fooBody.c");

            var doc = generator.GenerateSrcMLFileFromDirectory("srcmltest", "srcml_xml\\exclusionfilter.xml", exclusionList, Language.C);

            var numFileUnits = doc.FileUnits.Count();
            string firstSourceFile = null;
            if(numFileUnits > 0) {
                firstSourceFile = doc.FileUnits.First().Attribute("filename").Value;
            }

            Assert.AreEqual(1, numFileUnits, "test.xml should have only one file in it");
            Assert.AreEqual(Path.GetFullPath("srcmltest\\foo.c"), firstSourceFile);
        }

        [Test]
        public void EmptyOutputFileTest() {
            File.WriteAllText("srcml_xml\\emptyFile.xml", "");
            Assert.IsTrue(File.Exists("srcml_xml\\emptyFile.xml"));

            generator.GenerateSrcMLFromFile("srcmltest\\foo.c", "srcml_xml\\emptyFile.xml");
            var doc = new SrcMLFile("srcml_xml\\emptyFile.xml");
            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
        }

        [Test]
        public void InputWithSpacesTest() {
            generator.GenerateSrcMLFromFile("srcmltest\\File with spaces.cpp", "srcml_xml\\input_with_spaces.xml");
            var doc = new SrcMLFile("srcml_xml\\input_with_spaces.xml");
            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
        }

        //[Test]
        //[ExpectedException(typeof(SrcMLException))]
        //public void TestGenerateSrcMLFromFile_UnRegisteredExtension() {
        //    //The default src2srcml can't parse c#, so this should fail
        //    generator.NonDefaultExecutables.Clear();
        //    generator.GenerateSrcMLFromFile(@"srcmltest\CSHARP.csx", @"srcml_xml\CSHARP.xml");
        //}

        [Test]
        public void TestGenerateSrcMLFromFile_NonDefaultExtension() {
            generator.GenerateSrcMLFromFile(@"srcmltest\CSHARP.cs", @"srcml_xml\CSHARP.xml");
            var doc = new SrcMLFile(@"srcml_xml\CSHARP.xml");
            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
        }

        [Test]
        public void TestGenerateSrcMLFromFiles_NonDefaultExtension() {
            var doc = generator.GenerateSrcMLFileFromFiles(new[] {@"srcmltest\File with spaces.cpp", @"srcmltest\CSHARP.cs", @"srcmltest\foo.c"}, @"srcml_xml\multiple_files_csharp.xml");
            Assert.IsNotNull(doc);
            Assert.AreEqual(3, doc.FileUnits.Count());
        }

        //[Test]
        //[ExpectedException(typeof(InvalidOperationException))]
        //public void TestRegisterExecutable_Duplicate() {
        //    generator.RegisterExecutable(Path.Combine(TestConstants.SrcmlPath, "csharp"), new[] {Language.CSharp});
        //}

        //[Test]
        //public void TestSupportedLanguages() {
        //    var langs = generator.SupportedLanguages.ToList();
        //    Assert.AreEqual(5, langs.Count);
        //    Assert.IsTrue(langs.Contains(Language.C));
        //    Assert.IsTrue(langs.Contains(Language.CPlusPlus));
        //    Assert.IsTrue(langs.Contains(Language.CSharp));
        //    Assert.IsTrue(langs.Contains(Language.Java));
        //    Assert.IsTrue(langs.Contains(Language.AspectJ));

        //    generator.NonDefaultExecutables.Clear();
        //    langs = generator.SupportedLanguages.ToList();
        //    Assert.AreEqual(4, langs.Count);
        //    Assert.IsFalse(langs.Contains(Language.CSharp));
        //}
    }
}
