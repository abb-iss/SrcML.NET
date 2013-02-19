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
    }
}
