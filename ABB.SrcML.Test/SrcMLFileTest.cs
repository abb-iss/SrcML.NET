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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ABB.SrcML;
using System.Xml.Linq;

namespace ABB.SrcML.Test
{
    /// <summary>
    /// Tests for ABB.SrcML.SrcMLFile.
    /// </summary>
    [TestClass]
    public class SrcMLFileTest
    {
        public SrcMLFileTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void SrcMLFileTestInitialize(TestContext testContext)
        {
            Directory.CreateDirectory("srcmlfiletest");

            File.WriteAllText("srcmlfiletest\\foo.c", @"int foo() {
printf(""hello world!"");
}");
            File.WriteAllText("srcmlfiletest\\bar.cpp", @"int bar() {
printf(""good bye, world"");
}");
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
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
        [TestCleanup()]
        public void SrcMLFileTestCleanup()
        {
            if (File.Exists("test.xml"))
                File.Delete("test.xml");
        }
        //
        #endregion

        [TestMethod]
        public void SingleFileTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""http://www.sdml.info/srcML/src"" xmlns:cpp=""http://www.sdml.info/srcML/cpp"" xmlns:op=""http://www.sdml.info/srcML/operator"" xmlns:type=""http://www.sdml.info/srcML/modifier"" languageFilter=""C++"" dir="""" filename=""bar.c"">
</unit>");
            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(1, doc.FileUnits.Count(), "A non-compound file should have only a single  file unit");
        }

        [TestMethod]
        public void CompoundFileTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""http://www.sdml.info/srcML/src"">
<unit languageFilter=""C"" dir="""" filename=""foo.c""></unit>
<unit languageFilter=""C"" dir="""" filename=""bar.c""></unit>
</unit>");

            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.AreEqual(2, doc.FileUnits.Count(), "This compound file should have 2 units.");
        }

        [TestMethod]
        public void DisjunctMergedFileTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""http://www.sdml.info/srcML/src"">
<unit languageFilter=""C"" filename=""c:\Test\foo.c""></unit>
<unit languageFilter=""C"" filename=""z:\Release\bar.c""></unit>
</unit>");

            var doc = new SrcMLFile("test.xml");

            Assert.IsNotNull(doc);
            Assert.IsNull(doc.ProjectDirectory, String.Format("The ProjectDirectory property should be null when there is no common path. ({0})", doc.ProjectDirectory));
        }

        [TestMethod]
        public void WhitespaceInCompoundDocumentTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?><unit xmlns=""http://www.sdml.info/srcML/src"">
<unit languageFilter=""C"" filename=""c:\Test\foo.c"">
<comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>
<unit languageFilter=""C"" filename=""z:\Test\bar.c""></unit>
</unit>");

            var doc = new SrcMLFile("test.xml");

            var firstUnit = doc.FileUnits.First();

            var firstComment = firstUnit.Elements(SRC.Comment).First();
            var lastComment = firstUnit.Elements(SRC.Comment).Last();
            var nextNode = firstComment.NextNode;

            Assert.AreNotEqual(lastComment, nextNode);
        }

        [TestMethod]
        public void WhitespaceInSingleDocumentTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?>
<unit  xmlns=""http://www.sdml.info/srcML/src"" languageFilter=""C"" dir=""c:\Test"" filename=""foo.c""><comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>");

            var doc = new SrcMLFile("test.xml");

            var firstUnit = doc.FileUnits.First();

            var firstComment = firstUnit.Elements(SRC.Comment).First();
            var lastComment = firstUnit.Elements(SRC.Comment).Last();
            var nextNode = firstComment.NextNode;

            Assert.AreNotEqual(lastComment, nextNode);
        }

        [TestMethod]
        public void ExtraNewlinesInMergedDocumentTest()
        {
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromDirectory("srcmlfiletest", "test.xml");

            foreach (var unit in doc.FileUnits)
            {
                var path = SrcMLFile.GetPathForUnit(unit);
                var firstElement = unit.Elements().First();

                Assert.AreEqual(1, firstElement.GetSrcLineNumber(), path);
            }
        }

        [TestMethod]
        [Timeout(5000)]
        public void EmptyElementTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?>
<unit  xmlns=""http://www.sdml.info/srcML/src"">
<unit languageFilter=""C"" dir=""c:\Test"" filename=""beforeBlank.c""><expr_stmt /></unit>
<unit languageFilter=""C"" dir=""c:\Test"" filename=""foo.c"" />
<unit languageFilter=""C"" dir=""c:\Test"" filename=""bar.c""><comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>
</unit>");
            var doc = new SrcMLFile("test.xml");
            Assert.AreEqual(3, doc.FileUnits.Count());
        }
    }
}
