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

namespace ABB.SrcML.Test
{
    [TestFixture]
    [Category("Build")]
    public class SrcDiffRunnerTests
    {
        [TestFixtureSetUp]
        public static void SrcMLTestInitialize()
        {
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
        }

        [TestFixtureTearDown]
        public static void SrcMLTestCleanup()
        {
            /*
            foreach (var file in Directory.GetFiles("srcmltest"))
            {
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles("srcml_xml"))
            {
                File.Delete(file);
            }
            Directory.Delete("srcmltest");
            Directory.Delete("srcml_xml");
            */
        }

        [Test]
        public void DifferentLanguageTest()
        {
            var srcmlObject = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));

            var doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\CSHARP.cs", "srcmltest\\CSHARP.cs", "srcml_xml\\differentlanguage_java.xml", Language.Java);

            Assert.IsNotNull(doc);
        }

        [Test]
        public void InvalidLanguageTest()
        {
            var srcmlObject = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));

            var doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\invalidlanguage_java.xml", Language.Java);
            Assert.IsNotNull(doc);

            doc = null;
            doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\invalidlanguage_cpp.xml", Language.CPlusPlus);
            Assert.IsNotNull(doc);

            doc = null;
            doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\invalidlanguage_c.xml", Language.C);

            Assert.IsNotNull(doc);
        }

        [Test]
        public void SingleFileTest()
        {
            var srcmlObject = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));

            var doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\singlefile.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
            Assert.AreEqual("srcmltest\\foo.c", doc.FileUnits.First().Attribute("filename").Value);
        }

        /// <summary>
        /// Added by JZ on 12/4/2012.
        /// Unit test for SrcDiffRunner.GenerateSrcDiffAndXElementFromFile()
        /// </summary>
        [Test]
        public void SingleFileToFileAndXElementTest()
        {
            var srcmlObject = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));

            XElement xElement = srcmlObject.GenerateSrcDiffAndXElementFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\singlefile.xml");

            Assert.IsNotNull(xElement);
            //Assert.AreEqual(1, doc.FileUnits.Count());
            //Assert.AreEqual("srcmltest\\foo.c", doc.FileUnits.First().Attribute("filename").Value);
            Assert.That(File.Exists("srcml_xml\\singlefile.xml"));
        }

        [Test]
        public void EmptyOutputFileTest()
        {
            var srcmlObject = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));
            File.WriteAllText("srcml_xml\\emptyFile.xml", "");
            Assert.IsTrue(File.Exists("srcml_xml\\emptyFile.xml"));

            var doc = srcmlObject.GenerateSrcDiffFromFile("srcmltest\\foo.c", "srcmltest\\foo.c", "srcml_xml\\emptyFile.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
        }

        [Test]
        public void InputWithSpacesTest()
        {
            var runner = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));
            var doc = runner.GenerateSrcDiffFromFile("srcmltest\\File with spaces.cpp", "srcmltest\\File with spaces.cpp", "srcml_xml\\input_with_spaces.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count());
        }

        [Test]
        public void MyTestMethod()
        {
            var runner = new SrcDiffRunner(Path.Combine("..\\..\\External\\", "SrcDiff"));
            runner.GenerateSrcDiffFromFile("srcmltest\\File with spaces.cpp", "srcmltest\\File with spaces.cpp", "testfile.xml");
        }

    }
}
