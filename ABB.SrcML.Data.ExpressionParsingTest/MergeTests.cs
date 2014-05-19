/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ABB.SrcML.Test.Utilities;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    public class MergeTests {
        private Dictionary<Language, AbstractCodeParser> CodeParser;
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                { Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
                { Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp) }
            };
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
                { Language.CSharp, new CSharpCodeParser() }
            };
        }

        [Test]
        public void TestCreateMethodDefinition_TwoUnresolvedParents() {
            // # B.h namespace A { class B { }; }
            string xmlh = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{<private type=""default"">
    </private>}</block>;</class>
}</block></namespace>";

            // # B.cpp int A::B::Foo() { return 0; }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
}</block></function>";

            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "B.h");
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "B.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl);
            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(2, namespaceA.Locations.Count);

            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A.B", typeB.GetFullName());
            Assert.AreEqual(1, typeB.ChildStatements.Count());
            Assert.AreEqual(2, typeB.Locations.Count);

            var methodFoo = typeB.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.GetFullName());
            Assert.AreEqual(0, methodFoo.ChildStatements.Count());
            Assert.AreEqual(1, methodFoo.Locations.Count);

            Assert.AreSame(globalScope, namespaceA.ParentStatement);
            Assert.AreSame(namespaceA, typeB.ParentStatement);
            Assert.AreSame(typeB, methodFoo.ParentStatement);
        }

        [Test]
        public void TestCreateMethodDefinition_TwoUnresolvedParentsWithPrototype() {
            // # B.h namespace A { class B { int Foo(); }; }
            string xmlh = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{<private type=""default""> <function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl> </private>}</block>;</class> }</block></namespace>
";

            // # B.cpp int A::B::Foo() { return 0; }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
}</block></function>";

            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "B.h");
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "B.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl);
            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(2, namespaceA.Locations.Count);

            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A.B", typeB.GetFullName());
            Assert.AreEqual(1, typeB.ChildStatements.Count());
            Assert.AreEqual(2, typeB.Locations.Count);

            var methodFoo = typeB.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.GetFullName());
            Assert.AreEqual(0, methodFoo.ChildStatements.Count());
            Assert.AreEqual(2, methodFoo.Locations.Count);

            Assert.AreSame(globalScope, namespaceA.ParentStatement);
            Assert.AreSame(namespaceA, typeB.ParentStatement);
            Assert.AreSame(typeB, methodFoo.ParentStatement);
        }
        [Test]
        public void TestMethodDefinitionMerge_NoParameters() {
            ////Foo.h
            //int Foo();
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var declarationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileunitDecl);

            ////Foo.cpp
            //int Foo() { return 0; }
            string defXml = @"<function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(defXml, "Foo.cpp");
            var definitionScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitDef);

            var globalScope = declarationScope.Merge(definitionScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition) globalScope.ChildStatements.First()).Name);
        }
        [Test]
        public void TestMethodDefinitionMerge_NoParameterName() {
            ////Foo.h
            //int Foo(char);
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type></decl></param>)</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var declarationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileunitDecl);

            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string defXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(defXml, "Foo.cpp");

            var globalScope = new NamespaceDefinition();
            var definitionScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitDef);

            globalScope = globalScope.Merge(declarationScope) as NamespaceDefinition;
            globalScope = globalScope.Merge(definitionScope) as NamespaceDefinition;

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition) globalScope.ChildStatements.First()).Name);
        }

        [Test]
        public void TestNamespaceMerge_Cpp() {
            // # D.h namespace A { namespace B { namespace C { class D { }; } } }
            string d_xml = @"<namespace>namespace <name>A</name> <block>{
    <namespace>namespace <name>B</name> <block>{
        <namespace>namespace <name>C</name> <block>{
             <class>class <name>D</name> <block>{<private type=""default""> </private>}</block>;</class>
         }</block></namespace>
    }</block></namespace>
}</block></namespace>";

            // # E.h namespace A { namespace B { namespace C { class E { }; } } }
            string e_xml = @"<namespace>namespace <name>A</name> <block>{
    <namespace>namespace <name>B</name> <block>{
        <namespace>namespace <name>C</name> <block>{
             <class>class <name>E</name> <block>{<private type=""default""> </private>}</block>;</class>
         }</block></namespace>
    }</block></namespace>
}</block></namespace>";

            // # F.h namespace D { class F { }; }
            string f_xml = @"<namespace>namespace <name>D</name> <block>{
    <class>class <name>F</name> <block>{<private type=""default""> </private>}</block>;</class>
}</block></namespace>";

            var fileUnitD = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(d_xml, "D.h");
            var fileUnitE = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(e_xml, "E.h");
            var fileUnitF = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(f_xml, "F.h");

            var globalScope = new NamespaceDefinition();
            var scopeD = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitD);
            var scopeE = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitE);
            var scopeF = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitF);

            globalScope = globalScope.Merge(scopeD).Merge(scopeE).Merge(scopeF) as NamespaceDefinition;

            Assert.AreEqual(2, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var namespaceD = globalScope.ChildStatements.Last() as NamespaceDefinition;

            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(1, namespaceD.ChildStatements.Count());
            Assert.AreEqual("A", namespaceA.GetFullName());
            Assert.AreEqual("D", namespaceD.GetFullName());

            var namespaceB = namespaceA.ChildStatements.First() as NamespaceDefinition;
            var typeF = namespaceD.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("B", namespaceB.Name);
            Assert.AreEqual("F", typeF.Name);
            Assert.AreEqual(1, namespaceB.ChildStatements.Count());

            var namespaceC = namespaceB.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("C", namespaceC.Name);
            Assert.AreEqual("A.B", namespaceC.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual(2, namespaceC.ChildStatements.Count());
            var typeD = namespaceC.ChildStatements.First() as TypeDefinition;
            var typeE = namespaceC.ChildStatements.Last() as TypeDefinition;

            Assert.That(typeD.ParentStatement == typeE.ParentStatement);
            Assert.That(typeD.ParentStatement == namespaceC);

            Assert.AreEqual("A.B.C.D", typeD.GetFullName());
            Assert.AreEqual("A.B.C.E", typeE.GetFullName());
            Assert.AreEqual("D.F", typeF.GetFullName());
        }

        [Test]
        public void TestNamespaceMerge_Java() {
            // # D.java package A.B.C; class D { public void Foo() { } }
            string d_xml = @"<package>package <name>A</name>.<name>B</name>.<name>C</name>;</package>
<class>class <name>D</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            // # E.java package A.B.C; class E { public void Bar() { } }
            string e_xml = @"<package>package <name>A</name>.<name>B</name>.<name>C</name>;</package>
<class>class <name>E</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            // # F.java package D; class F { public void Oof() { } }
            string f_xml = @"<package>package <name>D</name>;</package>
<class>class <name>F</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Oof</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            var fileUnitD = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(d_xml, "D.java");
            var fileUnitE = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(e_xml, "E.java");
            var fileUnitF = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(f_xml, "F.java");

            var globalScopeD = CodeParser[Language.Java].ParseFileUnit(fileUnitD);
            var globalScopeE = CodeParser[Language.Java].ParseFileUnit(fileUnitE);
            var globalScopeF = CodeParser[Language.Java].ParseFileUnit(fileUnitF);
            var globalScope = globalScopeD.Merge(globalScopeE).Merge(globalScopeF);

            Assert.AreEqual(2, globalScope.ChildStatements.Count());

            var packageA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var packageD = globalScope.ChildStatements.Last() as NamespaceDefinition;

            Assert.AreEqual("A", packageA.Name);
            Assert.AreEqual("D", packageD.Name);

            var packageAB = packageA.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("B", packageAB.Name);
            Assert.AreEqual("A.B", packageAB.GetFullName());

            var packageABC = packageAB.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("C", packageABC.Name);

            Assert.AreEqual("C", packageABC.Name);
            Assert.AreEqual("A.B.C", packageABC.GetFullName());

            var typeD = packageABC.ChildStatements.First() as TypeDefinition;
            var typeE = packageABC.ChildStatements.Last() as TypeDefinition;
            var typeF = packageD.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("D", typeD.Name);
            Assert.AreEqual("E", typeE.Name);
            Assert.That(typeD.ParentStatement == typeE.ParentStatement);

            Assert.That(typeD.ParentStatement != typeF.ParentStatement);
        }

        [Test]
        public void TestMethodDefinitionMerge_DifferentPrefixes() {
            ////A.cpp
            // int A::Foo() { return 0; }
            string aCpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitA = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(aCpp, "A.cpp");
            var aScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitA);

            ////B.cpp
            // int B::Foo() { return 1; }
            string bCpp = @"<function><type><name>int</name></type> <name><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">1</lit:literal></expr>;</return> }</block></function>";
            var fileUnitB = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(bCpp, "B.cpp");
            var bScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitB);

            var globalScope = aScope.Merge(bScope);

            Assert.AreEqual(2, globalScope.ChildStatements.Count);
        }
        [Test]
        public void TestPartialClassMerge_CSharp() {
            ////A1.cs
            //public partial class A {
            //    public int Execute() {
            //        return 0;
            //    }
            //}
            string a1Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <name>int</name></type> <name>Execute</name><parameter_list>()</parameter_list> <block>{
        <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
    }</block></function>
}</block></class>";
            var a1FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var globalScope = CodeParser[Language.CSharp].ParseFileUnit(a1FileUnit);
            ////A2.cs
            //public partial class A {
            //    private bool Foo() {
            //        return true;
            //    }
            //}
            string a2Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>private</specifier> <name>bool</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{
        <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return>
    }</block></function>
}</block></class>";
            var a2FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(2, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Execute"));
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Foo"));
        }
    }
}
