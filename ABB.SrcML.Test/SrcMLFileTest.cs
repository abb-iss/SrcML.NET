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
using System.Xml;
using NUnit.Framework;
using ABB.SrcML;
using System.Xml.Linq;

namespace ABB.SrcML.Test
{
    /// <summary>
    /// Tests for ABB.SrcML.SrcMLFile.
    /// </summary>
    [TestFixture]
    [Category("Build")]
    public class SrcMLFileTest
    {
        public SrcMLFileTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [TestFixtureSetUp]
        public static void SrcMLFileTestInitialize()
        {
            Directory.CreateDirectory("srcmlfiletest");

            File.WriteAllText("srcmlfiletest\\foo.c", String.Format(@"int foo() {{{0}printf(""hello world!"");{0}}}", Environment.NewLine));
            File.WriteAllText("srcmlfiletest\\bar.cpp", String.Format(@"int bar() {{{0}printf(""good bye, world"");{0}}}", Environment.NewLine));
        }
        
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [TestFixtureTearDown()]
        public static void SrcMLFileTestClassCleanup()
        {
            foreach (var file in Directory.GetFiles("srcmlfiletest"))
            {
                File.Delete(file);
            }
            Directory.Delete("srcmlfiletest");
        }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        [TearDown()]
        public void SrcMLFileTestCleanup()
        {
            if (File.Exists("test.xml"))
                File.Delete("test.xml");
        }
        //
        #endregion

        [Test]
        public void SingleFileTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""{0}"" xmlns:cpp=""{1}"" xmlns:op=""{2}"" xmlns:type=""{3}"" languageFilter=""C++"" dir="""" filename=""bar.c"">
</unit>", SRC.NS, CPP.NS, OP.NS, TYPE.NS));
            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count(), "A non-compound file should have only a single  file unit");
        }

        [Test]
        public void CompoundFileTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""{0}"">
<unit languageFilter=""C"" dir="""" filename=""foo.c""></unit>
<unit languageFilter=""C"" dir="""" filename=""bar.c""></unit>
</unit>", SRC.NS));

            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(2, doc.FileUnits.Count(), "This compound file should have 2 units.");
        }

        [Test]
        public void DisjunctMergedFileTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""{0}"">
<unit languageFilter=""C"" filename=""c:\Test\foo.c""></unit>
<unit languageFilter=""C"" filename=""z:\Release\bar.c""></unit>
</unit>", SRC.NS));

            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.IsNull(doc.ProjectDirectory, String.Format("The ProjectDirectory property should be null when there is no common path. ({0})", doc.ProjectDirectory));
        }

        [Test]
        public void WhitespaceInCompoundDocumentTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""{0}"">
<unit languageFilter=""C"" filename=""c:\Test\foo.c"">
<comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>
<unit languageFilter=""C"" filename=""z:\Test\bar.c""></unit>
</unit>", SRC.NS));

            var doc = new SrcMLFile("test.xml");

            var firstUnit = doc.FileUnits.First();

            var firstComment = firstUnit.Elements(SRC.Comment).First();
            var lastComment = firstUnit.Elements(SRC.Comment).Last();
            var nextNode = firstComment.NextNode;

            Assert.AreNotEqual(lastComment, nextNode);
        }

        [Test]
        public void WhitespaceInSingleDocumentTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<unit  xmlns=""{0}"" languageFilter=""C"" dir=""c:\Test"" filename=""foo.c""><comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>", SRC.NS));

            var doc = new SrcMLFile("test.xml");

            var firstUnit = doc.FileUnits.First();

            var firstComment = firstUnit.Elements(SRC.Comment).First();
            var lastComment = firstUnit.Elements(SRC.Comment).Last();
            var nextNode = firstComment.NextNode;

            Assert.AreNotEqual(lastComment, nextNode);
        }

        [Test]
        public void ExtraNewlinesInMergedDocumentTest()
        {
            var srcmlObject = new Src2SrcMLRunner(Path.Combine(SrcMLHelper.GetSrcMLRootDirectory(), SrcMLHelper.srcMLExecutableLocation));

            var doc = srcmlObject.GenerateSrcMLFromDirectory("srcmlfiletest", "test.xml");

            foreach (var unit in doc.FileUnits)
            {
                var path = doc.GetPathForUnit(unit);
                var firstElement = unit.Elements().First();

                Assert.AreEqual(1, firstElement.GetSrcLineNumber(), path);
            }
        }

        [Test]
        [Timeout(5000)]
        public void EmptyElementTest()
        {
            File.WriteAllText("test.xml", String.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
<unit  xmlns=""{0}"">
<unit languageFilter=""C"" dir=""c:\Test"" filename=""beforeBlank.c""><expr_stmt /></unit>
<unit languageFilter=""C"" dir=""c:\Test"" filename=""foo.c"" />
<unit languageFilter=""C"" dir=""c:\Test"" filename=""bar.c""><comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>
</unit>", SRC.NS));
            var doc = new SrcMLFile("test.xml");
            Assert.AreEqual(3, doc.FileUnits.Count());
        }
    }
}
