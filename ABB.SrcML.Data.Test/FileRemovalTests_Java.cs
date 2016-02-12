using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    public class FileRemovalTests_Java {
        private JavaCodeParser CodeParser;
        private SrcMLFileUnitSetup FileUnitSetup;

        [TestFixtureSetUp, Category("Build")]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.Java);
            CodeParser = new JavaCodeParser();
        }

        [Test]
        public void TestRemoveClass_Global() {
            string fooXml = @"class Foo {
                private int bar;
                public Foo() { bar = 42; }
                public int GetBar() { return bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(fooXml, "Foo.java", Language.Java, new Collection<UInt32>() { }, false);
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);

            string bazXml = @"class Baz {
                public static int DoWork() { return 0; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(bazXml, "Baz.java", Language.Java, new Collection<UInt32>() { }, false);
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(0, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveClass_Namespace() {
            string fooXml = @"package com.ABB.Example;
            class Foo {
                private int bar;
                public Foo() { bar = 42; }
                public int GetBar() { return bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(fooXml, "Foo.java", Language.Java, new Collection<UInt32>() { }, false);
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);

            string bazXml = @"package com.ABB.Example;
            class Baz {
                public static int DoWork() { return 0; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(bazXml, "Baz.java", Language.Java, new Collection<UInt32>() { }, false);
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            string fooXml = @"package com.ABB.example;
            class Foo {
                private int bar;
                public Foo() { bar = 42; }
                public int GetBar() { return bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(fooXml, "Foo.java", Language.Java, new Collection<UInt32>() { }, false);
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);

            string bazXml = @"package com.ABB.DifferentExample;
            class Baz {
                public static int DoWork() { return 0; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(bazXml, "Baz.java", Language.Java, new Collection<UInt32>() { }, false);
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            var comDotAbb = afterScope.ChildStatements.OfType<NamespaceDefinition>().First().ChildStatements.OfType<NamespaceDefinition>().First();
            Assert.AreEqual("com.ABB", comDotAbb.GetFullName());
            Assert.AreEqual(2, comDotAbb.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }
    }
}
