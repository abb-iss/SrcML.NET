using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    public class FileRemovalTests_Cpp {
        private SrcMLFileUnitSetup FileUnitSetup;
        private CPlusPlusCodeParser CodeParser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
            CodeParser = new CPlusPlusCodeParser();
        }

        [Test]
        public void TestRemoveMethodFromGlobal() {
            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string fooXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileunitFoo = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cpp");
            var globalScope = SrcMLElementVisitor.Visit(fileunitFoo, CodeParser);
            ////Baz.cpp
            //char* Baz() { return "Hello, World!"; }
            string bazXml = "<function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Baz</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=\"string\">\"Hello, World!\"</lit:literal></expr>;</return> }</block></function>";
            var fileunitBaz = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.cpp");
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(fileunitBaz, CodeParser));

            Assert.AreEqual(2, globalScope.ChildScopes.OfType<MethodDefinition>().Count());

            globalScope.RemoveFile("Foo.cpp");
            Assert.AreEqual(1, globalScope.ChildScopes.OfType<MethodDefinition>().Count());
            var bazFunc = globalScope.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual("Baz", bazFunc.Name);
        }

        [Test]
        public void TestRemoveMethodDefinition_Global() {
            ////Foo.h
            //int Foo(char);
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup.GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var globalScope = SrcMLElementVisitor.Visit(fileunitDecl, CodeParser);
            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string defXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup.GetFileUnitForXmlSnippet(defXml, "Foo.cpp");
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(fileUnitDef, CodeParser));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            Assert.AreEqual("Foo", ((MethodDefinition)globalScope.ChildScopes.First()).Name);

            globalScope.RemoveFile("Foo.cpp");
            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual("Foo", foo.Name);
            Assert.IsFalse(foo.DefinitionLocations.Any());
            Assert.AreEqual("Foo.h", foo.Locations.First().SourceFileName);
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
            var globalScope = SrcMLElementVisitor.Visit(hFileunit, CodeParser);
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
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(cppFileunit, CodeParser));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            Assert.IsNotNull(globalScope.ChildScopes.First() as TypeDefinition);
            
            globalScope.RemoveFile("A.cpp");

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(1, foo.ChildScopes.Count());
            var add = foo.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(add);
            Assert.AreEqual(0, add.DefinitionLocations.Count());
            Assert.IsFalse(add.Locations.Any(l => l.SourceFileName == "A.cpp"));
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
            var globalScope = SrcMLElementVisitor.Visit(aFileunit, CodeParser);
            ////B.cpp
            //namespace B {
            //    char* Bar(){return "Hello, World!";}
            //}
            string bXml = @"<namespace>namespace <name>B</name> <block>{
    <function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Bar</name><parameter_list>()</parameter_list><block>{<return>return <expr><lit:literal type=""string"">""Hello, World!""</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cpp");
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(bFileunit, CodeParser));

            Assert.AreEqual(2, globalScope.ChildScopes.OfType<NamespaceDefinition>().Count());
            globalScope.RemoveFile("A.cpp");
            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var first = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(first);
            Assert.AreEqual("B", first.Name);
            Assert.AreEqual(1, first.ChildScopes.Count());
            var bChild = first.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(bChild);
            Assert.AreEqual("Bar", bChild.Name);
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
            var globalScope = SrcMLElementVisitor.Visit(a1FileUnit, CodeParser);
            ////A2.cpp
            //namespace A {
            //    char* Bar(){return "Hello, World!";}
            //}
            string a2Xml = @"<namespace>namespace <name>A</name> <block>{
    <function><type><name>char</name><type:modifier>*</type:modifier></type> <name>Bar</name><parameter_list>()</parameter_list><block>{<return>return <expr><lit:literal type=""string"">""Hello, World!""</lit:literal></expr>;</return>}</block></function>
}</block></namespace>";
            var a2Fileunit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cpp");
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(a2Fileunit, CodeParser));

            Assert.AreEqual(1, globalScope.ChildScopes.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, globalScope.ChildScopes.First().ChildScopes.OfType<MethodDefinition>().Count());
            globalScope.RemoveFile("A1.cpp");
            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var first = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(first);
            Assert.AreEqual("A", first.Name);
            Assert.AreEqual(1, first.ChildScopes.Count());
            var aChild = first.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(aChild);
            Assert.AreEqual("Bar", aChild.Name);
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
            var beforeScope = SrcMLElementVisitor.Visit(cppFileunit, CodeParser);
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
            var afterScope = beforeScope.Merge(SrcMLElementVisitor.Visit(hFileunit, CodeParser));

            Assert.AreEqual(1, afterScope.ChildScopes.Count());
            Assert.IsNotNull(afterScope.ChildScopes.First() as TypeDefinition);

            afterScope.RemoveFile("A.h");

            Assert.IsTrue(TestHelper.ScopesAreEqual(beforeScope, afterScope));
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
            var scope1 = SrcMLElementVisitor.Visit(fileunit, CodeParser);
            var scope2 = SrcMLElementVisitor.Visit(fileunit, CodeParser);
            Assert.IsTrue(TestHelper.ScopesAreEqual(scope1, scope2));
        }


    }
}
