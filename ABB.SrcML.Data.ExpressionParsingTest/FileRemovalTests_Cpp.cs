using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ABB.SrcML.Test.Utilities;

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
            ////A.cpp
            //#include "A.h"
            //int Foo::Add(int b) {
            //  return this->a + b;
            //}
            string cppXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name><name>Foo</name><op:operator>::</op:operator><name>Add</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list> <block>{
  <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><name>a</name> <op:operator>+</op:operator> <name>b</name></expr>;</return>
}</block></function>";
            var cppFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(cppXml, "A.cpp");
            var beforeScope = CodeParser.ParseFileUnit(cppFileunit);
            ////A.h
            //class Foo {
            //  public:
            //    int a;
            //    int Add(int b);
            //};
            string hXml = @"<class>class <name>Foo</name> <block>{<private type=""default"">
  </private><public>public:
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
    <function_decl><type><name>int</name></type> <name>Add</name><parameter_list>(<param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list>;</function_decl>
</public>}</block>;</class>";
            var hFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(hXml, "A.h");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(hFileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.IsNotNull(afterScope.ChildStatements.First() as TypeDefinition);

            afterScope.RemoveFile("A.h");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemoveMethodDeclaration_Global() {
            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string defXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup.GetFileUnitForXmlSnippet(defXml, "Foo.cpp");
            var beforeScope = CodeParser.ParseFileUnit(fileUnitDef);

            ////Foo.h
            //int Foo(char bar);
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup.GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileunitDecl));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition) afterScope.ChildStatements.First()).Name);

            afterScope.RemoveFile("Foo.h");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemoveMethodDefinition_Class() {
            ////A.h
            //class Foo {
            //  public:
            //    int a;
            //    int Add(int b);
            //};
            string hXml = @"<class>class <name>Foo</name> <block>{<private type=""default"">
  </private><public>public:
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
    <function_decl><type><name>int</name></type> <name>Add</name><parameter_list>(<param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list>;</function_decl>
</public>}</block>;</class>";
            var hFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(hXml, "A.h");
            var beforeScope = CodeParser.ParseFileUnit(hFileunit);

            ////A.cpp
            //#include "A.h"
            //int Foo::Add(int b) {
            //  return this->a + b;
            //}
            string cppXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name><name>Foo</name><op:operator>::</op:operator><name>Add</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list> <block>{
  <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><name>a</name> <op:operator>+</op:operator> <name>b</name></expr>;</return>
}</block></function>";
            var cppFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(cppXml, "A.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(cppFileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var foo = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            //Assert.AreEqual(1, foo.DeclaredVariables.Count());

            afterScope.RemoveFile("A.cpp");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemoveMethodDefinition_Global() {
            ////Foo.h
            //int Foo(char bar);
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup.GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var beforeScope = CodeParser.ParseFileUnit(fileunitDecl);

            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string defXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup.GetFileUnitForXmlSnippet(defXml, "Foo.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileUnitDef));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition) afterScope.ChildStatements.First()).Name);

            afterScope.RemoveFile("Foo.cpp");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemoveMethodFromGlobal() {
            ////Foo.cpp
            //int Foo() { return 0; }
            string fooXml = @"<function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>";
            var fileunitFoo = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cpp");
            var beforeScope = CodeParser.ParseFileUnit(fileunitFoo);

            ////Baz.cpp
            //char* Baz() { return "Hello, World!"; }
            string bazXml = "<function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Baz</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=\"string\">\"Hello, World!\"</lit:literal></expr>;</return> }</block></function>";
            var fileunitBaz = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(fileunitBaz));
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<MethodDefinition>().Count());

            afterScope.RemoveFile("Baz.cpp");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemoveNamespace() {
            ////A.cpp
            //namespace A {
            //	int Foo(){ return 0;}
            //}
            string aXml = @"<namespace>namespace <name>A</name> <block>{
    <function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list><block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var aFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(aXml, "A.cpp");
            var beforeScope = CodeParser.ParseFileUnit(aFileunit);

            ////B.cpp
            //namespace B {
            //    char* Bar(){return "Hello, World!";}
            //}
            string bXml = @"<namespace>namespace <name>B</name> <block>{
    <function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Bar</name><parameter_list>()</parameter_list><block>{<return>return <expr><lit:literal type=""string"">""Hello, World!""</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bFileunit));

            Assert.AreEqual(2, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("B.cpp");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestRemovePartOfNamespace() {
            ////A1.cpp
            //namespace A {
            //	int Foo(){ return 0;}
            //}
            string a1Xml = @"<namespace>namespace <name>A</name> <block>{
    <function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list><block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cpp");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);

            ////A2.cpp
            //namespace A {
            //    char* Bar(){return "Hello, World!";}
            //}
            string a2Xml = @"<namespace>namespace <name>A</name> <block>{
    <function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Bar</name><parameter_list>()</parameter_list><block>{<return>return <expr><lit:literal type=""string"">""Hello, World!""</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var a2Fileunit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cpp");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2Fileunit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.First().ChildStatements.OfType<MethodDefinition>().Count());

            afterScope.RemoveFile("A2.cpp");

            Assert.IsTrue(TestHelper.StatementsAreEqual(beforeScope, afterScope));
        }

        [Test]
        public void TestTestHelper() {
            ////A.h
            //class Foo {
            //  public:
            //    int a;
            //    int Add(int b);
            //};
            string xml = @"<class>class <name>Foo</name> <block>{<private type=""default"">
  </private><public>public:
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
    <function_decl><type><name>int</name></type> <name>Add</name><parameter_list>(<param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list>;</function_decl>
</public>}</block>;</class>";
            var fileunit = FileUnitSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var scope1 = CodeParser.ParseFileUnit(fileunit);
            var scope2 = CodeParser.ParseFileUnit(fileunit);
            Assert.IsTrue(TestHelper.StatementsAreEqual(scope1, scope2));
        }
    }
}
