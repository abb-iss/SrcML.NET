using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ABB.SrcML.Test.Utilities;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data.Test {
    [TestFixture, Category("Build")]
    public class FileRemovalTests_Cpp {
        private CPlusPlusCodeParser CodeParser;
        private SrcMLFileUnitSetup FileUnitSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
            CodeParser = new CPlusPlusCodeParser();
        }

        [Test]
        public void TestRemoveClassDefinition() {

            string cppXml = @"int Foo::Add(int b) {
              return this->a + b;
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(cppXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cppFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var beforeScope = CodeParser.ParseFileUnit(cppFileunit);

            string hXml = @"class Foo {
              public:
                int a;
                int Add(int b);
            };";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(hXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var hFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "A.h");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(hFileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.IsNotNull(afterScope.ChildStatements.First() as TypeDefinition);

            afterScope.RemoveFile("A.h");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveMethodFromClass() {
            string cppXml = @"int Foo::Add(int b) {
              return this->a + b;
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(cppXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cppFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            string hXml = @"class Foo {
              public:
                int a;
                int Add(int b);
            };";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(hXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var hFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "A.h");

            var beforeScope = CodeParser.ParseFileUnit(hFileunit);
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(cppFileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.IsNotNull(afterScope.ChildStatements.First() as TypeDefinition);

            afterScope.RemoveFile("A.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveMethodDeclaration_Global() {
            string defXml = "int Foo(char bar) { return 0; }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(defXml, "Foo.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileUnitDef = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.cpp");
            var beforeScope = CodeParser.ParseFileUnit(fileUnitDef);

            string declXml = "int Foo(char bar);";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(declXml, "Foo.h", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileunitDecl = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "Foo.h");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileunitDecl));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition)afterScope.ChildStatements.First()).Name);

            afterScope.RemoveFile("Foo.h");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveMethodDefinition_Class() {
            string hXml = @"class Foo {
              public:
                int a;
                int Add(int b);
            };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(hXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var hFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var beforeScope = CodeParser.ParseFileUnit(hFileunit);

            string cppXml = @"int Foo::Add(int b) {
              return this->a + b;
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(cppXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cppFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "A.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(cppFileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var foo = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            //Assert.AreEqual(1, foo.DeclaredVariables.Count());

            afterScope.RemoveFile("A.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveMethodDefinition_Global() {
            string declXml = "int Foo(char bar);";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(declXml, "Foo.h", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileunitDecl = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.h");
            var beforeScope = CodeParser.ParseFileUnit(fileunitDecl);

            //int Foo(char bar) { return 0; }
            string defXml = "int Foo(char bar) { return 0; }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(defXml, "Foo.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileUnitDef = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "Foo.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileUnitDef));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition)afterScope.ChildStatements.First()).Name);

            afterScope.RemoveFile("Foo.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveMethodFromGlobal() {
            string fooXml = @"int Foo() { return 0; }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(fooXml, "Foo.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileunitFoo = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.cpp");
            var beforeScope = CodeParser.ParseFileUnit(fileunitFoo);

            string bazXml = "char* Baz() { return \"Hello, World!\"; }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bazXml, "Baz.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileunitBaz = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "Baz.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileunitBaz));
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<MethodDefinition>().Count());

            afterScope.RemoveFile("Baz.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            string aXml = @"namespace A {
              int Foo(){ return 0;}
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(aXml, "A.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var aFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var beforeScope = CodeParser.ParseFileUnit(aFileunit);

            string bXml = @"namespace B {
                char* Bar(){return 'Hello, World!';}
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bXml, "B.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bFileunit));

            Assert.AreEqual(2, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("B.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartOfNamespace() {

            string a1Xml = @"namespace A {
              int Foo(){ return 0;}
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a1Xml, "A1.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A1.cpp");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);

            string a2Xml = @"namespace A {
                char* Bar(){return 'Hello, World!';}
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(a2Xml, "A2.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var a2Fileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "A2.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2Fileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.First().ChildStatements.OfType<MethodDefinition>().Count());

            afterScope.RemoveFile("A2.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestCppRemovalWithNamespaceAndClass() {
            string hXml = @"namespace A {
              class Foo {
                  public:
                      int Bar(int b);
              };
            }";

            string cppXml = @"int A::Foo::Bar(int b) { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(hXml, "Foo.h", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var hFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.h");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(cppXml, "Foo.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cppFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "Foo.cpp");

            var beforeScope = CodeParser.ParseFileUnit(hFileUnit);
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(cppFileUnit));

            afterScope.RemoveFile("Foo.cpp");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestHeaderRemovalWithNamespaceAndClass() {
            string hXml = @"namespace A {
              class Foo {
                  public:
                      int Bar(int b);
              };
            }";

            string cppXml = @"int A::Foo::Bar(int b) { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(hXml, "Foo.h", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var hFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.h");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(cppXml, "Foo.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cppFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLB, "Foo.cpp");

            var beforeScope = CodeParser.ParseFileUnit(cppFileUnit);
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(hFileUnit));

            afterScope.RemoveFile("Foo.h");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestTestHelper() {
            string xml = @"class Foo {
              public:
                int a;
                int Add(int b);
            };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileunit = FileUnitSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var scope1 = CodeParser.ParseFileUnit(fileunit);
            var scope2 = CodeParser.ParseFileUnit(fileunit);
            DataAssert.StatementsAreEqual(scope1, scope2);
        }
    }
}
