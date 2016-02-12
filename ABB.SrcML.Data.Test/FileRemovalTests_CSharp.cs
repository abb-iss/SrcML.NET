using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture, Category("Build")]
    public class FileRemovalTests_CSharp {
        private CSharpCodeParser CodeParser;
        private SrcMLFileUnitSetup FileUnitSetup;

        [TestFixtureSetUp, Category("Build")]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CSharp);
            CodeParser = new CSharpCodeParser();
        }

        [Test]
        public void TestRemoveClass_Global() {
            string fooXml = @"class Foo {
                private int bar;
                public Foo() { bar = 42; }
                public int GetBar() { return bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.cs");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);

            string bazXml = @"class Baz {
              public static int DoWork() { return 0; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(bazXml, "Baz.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Baz.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(0, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("Baz.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            string aXml = @"namespace A {
                class Foo { int bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(aXml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var aFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "A.cs");
            var beforeScope = CodeParser.ParseFileUnit(aFileunit);

            string bXml = @"namespace B {
                class Baz { public ulong xyzzy; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(bXml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "B.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bFileunit));

            Assert.AreEqual(2, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("B.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialClass() {
            string a1Xml = @"public partial class A {
              public int Execute() {
                   return 0;
              }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);

            string a2Xml = @"public partial class A {
              private bool Foo() {
                return true;
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(2, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Execute"));
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Foo"));

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialMethod_Declaration() {
            string a2Xml = @"public partial class A {
              public partial int Foo() { return 42; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "A2.cs");
            var beforeScope = CodeParser.ParseFileUnit(a2FileUnit);

            string a1Xml = @"public partial class A {
              public partial int Foo();
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A1.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a1FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            var foo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.That(foo.IsPartial);

            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);

            afterScope.RemoveFile("A1.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialMethod_Implementation() {
            string a1Xml = @"public partial class A {
                public partial int Foo();
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);

            string a2Xml = @"public partial class A {
                public partial int Foo() { return 42; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            var foo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartOfNamespace() {
            string a1Xml = @"namespace A {
                class Foo { int bar; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);

            string a2Xml = @"
            namespace A {
                class Baz { public ulong xyzzy; }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.First().ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestFileRemovalWithDifferentCase() {

            string bXml = @"namespace A { class B { } }";

            string dXml = @"namespace C { class D { } }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(bXml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcML, "B.cs");
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(dXml, "D.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var dUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "D.cs");

            var bScope = CodeParser.ParseFileUnit(bUnit);
            var dScope = CodeParser.ParseFileUnit(dUnit);
            var globalScope = bScope.Merge(dScope);

            globalScope.RemoveFile("b.cs");
            Assert.AreEqual(1, globalScope.ChildStatements.Count());
        }
    }
}
