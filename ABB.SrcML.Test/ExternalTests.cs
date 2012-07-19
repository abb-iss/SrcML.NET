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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using ABB.SrcML;
namespace ABB.SrcML.Test
{
	[TestClass]
	public class ExternalTests
	{
		[ClassInitialize]
		public static void ExternalTestInitialize(TestContext context)
		{
			Directory.CreateDirectory("external");
			Directory.CreateDirectory("external_xml");

			File.WriteAllBytes("external\\fileWithBom.cpp", new byte[3] { 0xEF, 0xBB, 0xBF });
			File.WriteAllText("external\\ClassWithConstructor.java", @"package external;

class ClassWithConstructor
{
	private int hidden = 0;
	
	public Test(int value)
	{
		hidden = value;
	}
	
	public int foo (char a)
	{
		return (int) a;
	}
}");
			File.WriteAllText(@"external\cpp_parsing_error.c", @"int testcase(int x)
{
	if(x < 0)
		printf(""x < 0\n"");
#if 1
	else
		printf(""x >= 0\n"");
#else
	else
		printf(""no really, x >= 0\n"");
#endif
	return x;
}
");
			File.WriteAllText("external\\MacroWithoutSemicolon.cpp", @"if (exists) {
	Py_BEGIN_ALLOW_THREADS
	fp = fopen(filename, ""r"" PY_STDIOTEXTMODE);
	Py_END_ALLOW_THREADS

	if (fp == NULL) {
		exists = 0;
	}
}");
			File.WriteAllText("external\\DestructorWithIfStatement.cpp", @"~Test()
{
	if(0)
	{
	}
}");
            File.WriteAllText("external\\MethodWithFunctionPointerParameters.cpp", @"void foo(int (*a)(char i), char b)
{
}");
		}

		[ClassCleanup]
		public static void SRCTestCleanup()
		{
			foreach (var file in Directory.GetFiles("external"))
			{
				File.Delete(file);
			}
			foreach (var file in Directory.GetFiles("external_xml"))
			{
				File.Delete(file);
			}
			Directory.Delete("external");
			Directory.Delete("external_xml");
		}

		[TestMethod]
		public void FileWithBom()
		{
			var srcmlObject = new ABB.SrcML.SrcML(TestConstants.SrcmlPath);

			var doc = srcmlObject.GenerateSrcMLFromFile("external\\fileWithBom.cpp", "external_xml\\fileWithBom.xml");
		}

		[TestMethod]
		public void JavaClassWithConstructor()
		{
			var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

			var doc = srcmlObject.GenerateSrcMLFromFile("external\\ClassWithConstructor.java", "external_xml\\ClassWithConstructor.java.xml");
			XElement classBlock = null;

			classBlock = doc.FileUnits.First().Element(SRC.Class).Element(SRC.Block);

			Assert.AreEqual(1, classBlock.Elements(SRC.Function).Count(), srcmlObject.ApplicationDirectory);
		}

		[TestMethod]
		public void DeclStmtWithTwoDecl()
		{
			var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);
			var source = "int x = 0, y = 2;";

			var xml = srcmlObject.GenerateSrcMLFromString(source);
			var element = XElement.Parse(xml);

            var decl = element.Element(SRC.DeclarationStatement).Element(SRC.Declaration);
            var nameCount = decl.Elements(SRC.Name).Count();
            var initCount = decl.Elements(SRC.Init).Count();
			Assert.AreEqual(2, nameCount, srcmlObject.ApplicationDirectory);
            Assert.AreEqual(2, initCount, srcmlObject.ApplicationDirectory);
		}

		[TestMethod]
		public void FunctionWithElseInCpp()
		{
			var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

			var doc = srcmlObject.GenerateSrcMLFromFile("external\\cpp_parsing_error.c", "external_xml\\cpp_parsing_error.c.xml");

			Assert.AreEqual(1, doc.FileUnits.First().Elements().Count(), srcmlObject.ApplicationDirectory);
		}

		[TestMethod]
		public void MacroWithoutSemicolon()
		{
			var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

			var doc = srcmlObject.GenerateSrcMLFromFile("external\\MacroWithoutSemicolon.cpp", "external_xml\\MacroWithoutSemicolon.cpp.xml");

			Assert.AreEqual(2, doc.FileUnits.First().Descendants(SRC.If).Count());
		}

		[TestMethod]
		public void DestructorWithIfStatement()
		{
			var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

			var doc = srcmlObject.GenerateSrcMLFromFile("external\\DestructorWithIfStatement.cpp", "external_xml\\DestructorWithIfStatement.cpp.xml");

			Assert.AreEqual(1, doc.FileUnits.First().Descendants(SRC.Destructor).Count());
		}

        [TestMethod]
        public void MethodWithFunctionPointerAsParameter()
        {
            var srcmlObject = new Src2SrcMLRunner(TestConstants.SrcmlPath);

            var doc = srcmlObject.GenerateSrcMLFromFile("external\\MethodWithFunctionPointerParameters.cpp", "external_xml\\MethodWithFunctionPointerParameters.cpp.xml");

            Assert.AreEqual(2, doc.FileUnits.First().Element(SRC.Function).Element(SRC.ParameterList).Elements(SRC.Parameter).Count());
        }
	}
}
