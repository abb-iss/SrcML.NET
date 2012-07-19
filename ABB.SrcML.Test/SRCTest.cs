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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ABB.SrcML;
using System.Xml.Linq;

namespace ABB.SrcML.Test
{
    [TestClass]
    public class SRCTest
    {
        [ClassInitialize]
        public static void SRCTestInitialize(TestContext context)
        {
            Directory.CreateDirectory("srctest");
            Directory.CreateDirectory("srctest_xml");

            File.WriteAllText(@"srctest\foo.c", @"int foo() {
    printf(""hello world!""); bool x = 5 < 3; printd(""what?"");
}

void bar() {
    while(1)
    {
        printg(""bar"");
    }
}");
            File.WriteAllText("srctest\\bar.c", @"int bar() {
printf(""good bye, world"");
}");
        }

        [ClassCleanup]
        public static void SRCTestCleanup()
        {
            foreach (var file in Directory.GetFiles("srctest"))
            {
                File.Delete(file);
            }
            foreach (var file in Directory.GetFiles("srctest_xml"))
            {
                File.Delete(file);
            }
            Directory.Delete("srctest");
            Directory.Delete("srctest_xml");
        }

        [TestMethod]
        public void CheckPositionNumberWithSingleUnit()
        {
            var srcmlObject = new ABB.SrcML.SrcML(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromFile(@"srctest\foo.c", @"srctest_xml\singleunit_position.xml");

            var firstUnit = doc.FileUnits.First();

            Assert.AreEqual(1, firstUnit.GetSrcLinePosition());
            Assert.AreEqual(1, firstUnit.Descendants(SRC.Type).First().Element(SRC.Name).GetSrcLinePosition());
            Assert.AreEqual(1, firstUnit.Element(SRC.Function).GetSrcLinePosition());
            Assert.AreEqual(5, firstUnit.Descendants(SRC.Name).First(n => n.Value == "foo").GetSrcLinePosition());
            Assert.AreEqual(5, firstUnit.Descendants(SRC.Name).First(n => n.Value == "printf").GetSrcLinePosition());
            Assert.AreEqual(45, firstUnit.Descendants(SRC.Name).First(n => n.Value == "printd").GetSrcLinePosition());

            Assert.AreEqual(6, firstUnit.Descendants(SRC.Name).First(n => n.Value == "bar").GetSrcLinePosition());
            Assert.AreEqual(10, firstUnit.Descendants(SRC.Condition).First().GetSrcLinePosition());
            Assert.AreEqual(9, firstUnit.Descendants(SRC.Name).First(n => n.Value == "printg").GetSrcLinePosition());
        }

        [TestMethod]
        public void GetSrcLineNumberWithSingleUnit()
        {
            File.WriteAllText("srctest\\singleunitlinenum.c", @"int foo() {
printf(""hello world!"");
}");
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromFile("srctest\\singleunitlinenum.c", @"srctest_xml\singleunit_linenumber.xml");

            var unit = doc.FileUnits.First();

            Assert.AreEqual(1, unit.GetSrcLineNumber());
            Assert.AreEqual(1, unit.Element(SRC.Function).GetSrcLineNumber());
            Assert.AreEqual(2, unit.Descendants(SRC.Call).First().GetSrcLineNumber());
        }

        [TestMethod]
        public void GetLineInfoWithString()
        {
            var source = @"int foo() {
printf(""hello world!"");
}";
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var xml = srcmlObject.GenerateSrcMLFromString(source);

            var element = XElement.Parse(xml);

            Assert.AreEqual(-1, element.GetSrcLineNumber());
            Assert.AreEqual(-1, element.GetSrcLinePosition());
        }

        [TestMethod]
        public void GetSrcLineNumberWithMultipleUnit()
        {
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromDirectory("srctest", "srctest_xml\\multipleunit_linenumber.xml");
            
            var firstUnit = doc.FileUnits.First();

            Assert.AreEqual(1, firstUnit.Element(SRC.Function).GetSrcLineNumber());
            Assert.AreEqual(2, firstUnit.Descendants(SRC.Call).First().GetSrcLineNumber());
        }

        [TestMethod]
        public void ToSourceTest()
        {
            var text = File.ReadAllText("srctest\\foo.c");

            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromFile("srctest\\foo.c", "srctest_xml\\srctest_tosource.xml");

            var firstUnit = doc.FileUnits.First();

            string contentsFromXml = firstUnit.ToSource();

            Assert.AreEqual(text, contentsFromXml);
        }

        [TestMethod]
        public void ParentStatementTest()
        {
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromFile("srctest\\foo.c", "srctest_xml\\srctest_parentstatement.xml");
            var firstUnit = doc.FileUnits.First();
            var function = firstUnit.Element(SRC.Function);
            
            var expression = function.Element(SRC.Block).Element(SRC.ExpressionStatement);
            var call = expression.Descendants(SRC.Call).First();

            var declaration = function.Element(SRC.Block).Element(SRC.DeclarationStatement);
            var variable = declaration.Element(SRC.Declaration).Element(SRC.Name);

            var unitParent = firstUnit.ParentStatement();
            var functionParent = function.ParentStatement();
            var callParent = call.ParentStatement();
            var variableParent = variable.ParentStatement();

            Assert.IsNull(unitParent);
            Assert.IsNull(functionParent);
            Assert.AreEqual(callParent, expression);
            Assert.AreEqual(declaration, variableParent);            
        }

        [TestMethod]
        public void ContainsCallToTest()
        {
            string source = @"int foo() {
    printf(""hello world!"");
    int x = 5;
}";
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);
            var xml = srcmlObject.GenerateSrcMLFromString(source);

            var element = XElement.Parse(xml);
            var expression = element.Descendants(SRC.DeclarationStatement).First();

            Assert.IsTrue(element.ContainsCallTo("printf"));
            Assert.IsFalse(expression.ContainsCallTo("printf"));
        }
    }
}
