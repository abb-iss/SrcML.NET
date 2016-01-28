using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
            CodeParser = new CSharpCodeParser();
        }

        [Test]
        public void TestRemoveClass_Global() {
            ////Foo.cs
            //class Foo {
            //    private int bar;
            //    public Foo() { bar = 42; }
            //    public int GetBar() { return bar; }
            //}
            string fooXml = @"class Foo {
                private int bar;
                public Foo() { bar = 42; }
                public int GetBar() { return bar; }
            }";
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);
            ////Baz.cs
            //class Baz {
            //    public static int DoWork() { return 0; }
            //}
            string bazXml = @"class Baz {
              public static int DoWork() { return 0; }
            }";
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(0, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("Baz.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            ////A.cs
            //namespace A {
            //    class Foo { int bar; }
            //}
            string aXml = @"namespace A {
                class Foo { int bar; }
            }";
            var aFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(aXml, "A.cs");
            var beforeScope = CodeParser.ParseFileUnit(aFileunit);
            ////B.cs
            //namespace B {
            //    class Baz { public ulong xyzzy; }
            //}
            string bXml = @"namespace B {
                class Baz { public ulong xyzzy; }
            }";
            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bFileunit));

            Assert.AreEqual(2, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("B.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialClass() {
            ////A1.cs
            //public partial class A {
            //    public int Execute() {
            //        return 0;
            //    }
            //}
            string a1Xml = @"public partial class A {
              public int Execute() {
                   return 0;
              }
            }";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //public partial class A {
            //    private bool Foo() {
            //        return true;
            //    }
            //}
            string a2Xml = @"public partial class A {
              private bool Foo() {
                return true;
              }
            }";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
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
            ////A2.cs
            //public partial class A {
            //    public partial int Foo() { return 42; }
            //}
            string a2Xml = @"public partial class A {
              public partial int Foo() { return 42; }
            }";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var beforeScope = CodeParser.ParseFileUnit(a2FileUnit);
            ////A1.cs
            //public partial class A {
            //    public partial int Foo();
            //}
            string a1Xml = @"public partial class A {
              public partial int Foo();
            }";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
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
            ////A1.cs
            //public partial class A {
            //    public partial int Foo();
            //}
            string a1Xml = @"public partial class A {
                public partial int Foo();
            }";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //public partial class A {
            //    public partial int Foo() { return 42; }
            //}
            string a2Xml = @"public partial class A {
                public partial int Foo() { return 42; }
            }";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
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
            ////A1.cs
            //namespace A {
            //    class Foo { int bar; }
            //}
            string a1Xml = @"namespace A {
                class Foo { int bar; }
            }";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //namespace A {
            //    class Baz { public ulong xyzzy; }
            //}
            string a2Xml = @"
            namespace A {
                class Baz { public ulong xyzzy; }
            }";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.First().ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestFileRemovalWithDifferentCase() {
            // namespace A { class B { } }
            string bXml = @"namespace A { class B { } }";

            // namespace C { class D { } }
            string dXml = @"namespace C { class D { } }";

            var bUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var dUnit = FileUnitSetup.GetFileUnitForXmlSnippet(dXml, "D.cs");

            var bScope = CodeParser.ParseFileUnit(bUnit);
            var dScope = CodeParser.ParseFileUnit(dUnit);
            var globalScope = bScope.Merge(dScope);

            globalScope.RemoveFile("b.cs");
            Assert.AreEqual(1, globalScope.ChildStatements.Count());
        }
    }
}
