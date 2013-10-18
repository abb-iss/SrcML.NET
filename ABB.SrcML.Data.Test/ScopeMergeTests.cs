/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class ScopeMergeTests {
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

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader) as INamedScope;
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl) as INamedScope;
            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var namespaceA = globalScope.ChildScopes.First() as INamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildScopes.Count());

            var typeB = namespaceA.ChildScopes.First() as ITypeDefinition;
            Assert.AreEqual("A.B", typeB.GetFullName());
            Assert.AreEqual(1, typeB.ChildScopes.Count());

            var methodFoo = typeB.ChildScopes.First() as IMethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.GetFullName());
            Assert.AreEqual(0, methodFoo.ChildScopes.Count());

            Assert.AreSame(globalScope, namespaceA.ParentScope);
            Assert.AreSame(namespaceA, typeB.ParentScope);
            Assert.AreSame(typeB, methodFoo.ParentScope);
        }

        [Test]
        public void TestMethodDefinitionMerge_Cpp() {
            // # A.h class A { int Foo(); };
            string header_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
</private>}</block>;</class>";

            // # A.cpp #include "A.h" int A::Foo() { int bar = 1; return bar; }
            string impl_xml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>int</name></type> <name>bar</name> =<init> <expr><lit:literal type=""number"">1</lit:literal></expr></init></decl>;</decl_stmt>
    <return>return <expr><name>bar</name></expr>;</return>
}</block></function>";

            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(header_xml, "A.h");
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(impl_xml, "A.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(header) as INamedScope;
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(implementation) as INamedScope;

            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(1, typeA.ChildScopes.Count());
            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);

            var methodFoo = typeA.ChildScopes.First() as IMethodDefinition;
            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual(0, methodFoo.ChildScopes.Count());
            Assert.AreEqual(1, methodFoo.DeclaredVariables.Count());
            Assert.AreEqual("A.cpp", methodFoo.PrimaryLocation.SourceFileName);
            Assert.AreEqual(AccessModifier.Private, methodFoo.Accessibility);
        }

        [Test]
        public void TestMethodDefinitionMerge_NoParameterName() {
            ////Foo.h
            //int Foo(char);
            string declXml = "<function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type></decl></param>)</parameter_list>;</function_decl>";
            var fileunitDecl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(declXml, "Foo.h");
            var declarationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileunitDecl) as INamedScope;

            ////Foo.cpp
            //int Foo(char bar) { return 0; }
            string defXml = "<function><type><name>int</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>char</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><lit:literal type=\"number\">0</lit:literal></expr>;</return> }</block></function>";
            var fileUnitDef = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(defXml, "Foo.cpp");
            var definitionScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitDef) as INamedScope;
            var globalScope = declarationScope.Merge(definitionScope);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            Assert.AreEqual("Foo", ((IMethodDefinition) globalScope.ChildScopes.First()).Name);
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

            var scopeD = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitD) as INamedScope;
            var scopeE = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitE) as INamedScope;
            var scopeF = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitF) as INamedScope;
            var globalScope = scopeD.Merge(scopeE).Merge(scopeF);

            Assert.AreEqual(2, globalScope.ChildScopes.Count());

            var namespaceA = globalScope.ChildScopes.First() as INamespaceDefinition;
            var namespaceD = globalScope.ChildScopes.Last() as INamespaceDefinition;

            Assert.AreEqual(1, namespaceA.ChildScopes.Count());
            Assert.AreEqual(1, namespaceD.ChildScopes.Count());
            Assert.AreEqual("A", namespaceA.GetFullName());
            Assert.AreEqual("D", namespaceD.GetFullName());

            var namespaceB = namespaceA.ChildScopes.First() as INamespaceDefinition;
            var typeF = namespaceD.ChildScopes.First() as ITypeDefinition;

            Assert.AreEqual("B", namespaceB.Name);
            Assert.AreEqual("F", typeF.Name);
            Assert.AreEqual(1, namespaceB.ChildScopes.Count());

            var namespaceC = namespaceB.ChildScopes.First() as INamespaceDefinition;
            Assert.AreEqual("C", namespaceC.Name);
            Assert.AreEqual("A.B", namespaceC.GetFirstParent<INamespaceDefinition>().GetFullName());
            Assert.AreEqual(2, namespaceC.ChildScopes.Count());
            var typeD = namespaceC.ChildScopes.First() as ITypeDefinition;
            var typeE = namespaceC.ChildScopes.Last() as ITypeDefinition;

            Assert.That(typeD.ParentScope == typeE.ParentScope);
            Assert.That(typeD.ParentScope == namespaceC);

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

            var globalScopeD = CodeParser[Language.Java].ParseFileUnit(fileUnitD) as INamespaceDefinition;
            var globalScopeE = CodeParser[Language.Java].ParseFileUnit(fileUnitE) as INamespaceDefinition;
            var globalScopeF = CodeParser[Language.Java].ParseFileUnit(fileUnitF) as INamespaceDefinition;
            var globalScope = globalScopeD.Merge(globalScopeE).Merge(globalScopeF) as INamespaceDefinition;

            Assert.AreEqual(2, globalScope.ChildScopes.Count());

            var packageA = globalScope.ChildScopes.First() as INamespaceDefinition;
            var packageD = globalScope.ChildScopes.Last() as INamespaceDefinition;

            Assert.AreEqual("A", packageA.Name);
            Assert.AreEqual("D", packageD.Name);

            var packageAB = packageA.ChildScopes.First() as INamespaceDefinition;
            Assert.AreEqual("B", packageAB.Name);
            Assert.AreEqual("A.B", packageAB.GetFullName());

            var packageABC = packageAB.ChildScopes.First() as INamespaceDefinition;
            Assert.AreEqual("C", packageABC.Name);

            Assert.AreEqual("C", packageABC.Name);
            Assert.AreEqual("A.B.C", packageABC.GetFullName());

            var typeD = packageABC.ChildScopes.First() as ITypeDefinition;
            var typeE = packageABC.ChildScopes.Last() as ITypeDefinition;
            var typeF = packageD.ChildScopes.First() as ITypeDefinition;

            Assert.AreEqual("D", typeD.Name);
            Assert.AreEqual("E", typeE.Name);
            Assert.That(typeD.ParentScope == typeE.ParentScope);

            Assert.That(typeD.ParentScope != typeF.ParentScope);
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
            var globalScope = CodeParser[Language.CSharp].ParseFileUnit(a1FileUnit) as INamedScope;
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
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(a2FileUnit)) as INamedScope;

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(2, typeA.ChildScopes.OfType<IMethodDefinition>().Count());
            Assert.IsTrue(typeA.ChildScopes.OfType<IMethodDefinition>().Any(m => m.Name == "Execute"));
            Assert.IsTrue(typeA.ChildScopes.OfType<IMethodDefinition>().Any(m => m.Name == "Foo"));
        }

        [Test]
        public void TestPartialMethodMerge_CSharp() {
            ////A1.cs
            //public partial class A {
            //    public partial int Foo();
            //}
            string a1Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function_decl><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";
            var a1FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var globalScope = CodeParser[Language.CSharp].ParseFileUnit(a1FileUnit) as INamedScope;
            ////A2.cs
            //public partial class A {
            //    public partial int Foo() { return 42; }
            //}
            string a2Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">42</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var a2FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(a2FileUnit)) as INamedScope;

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildScopes.OfType<IMethodDefinition>().Count());
            var foo = typeA.ChildScopes.First() as IMethodDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
        }

        [Test]
        public void TestUnresolvedParentMerge_ClassEncounteredFirst_Cpp() {
            // # A.cpp int A::Foo() { return 0; }
            //
            // int A::Bar() { return 0; }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Bar</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            // # A.h class A { };
            string xmlh = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "A.cpp");
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "A.h");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader) as INamedScope;
            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(0, typeA.ChildScopes.Count());

            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl)) as INamedScope;
            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.AreEqual(2, typeA.ChildScopes.Count());

            var aDotFoo = typeA.ChildScopes.First() as IMethodDefinition;
            var aDotBar = typeA.ChildScopes.Last() as IMethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.GetFullName());
            Assert.AreEqual("A.Bar", aDotBar.GetFullName());

            Assert.AreSame(typeA, aDotFoo.ParentScope);
            Assert.AreSame(typeA, aDotFoo.ParentScope);
            Assert.AreSame(globalScope, typeA.ParentScope);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestUnresolvedParentMerge_MethodsEncounteredFirst_Cpp() {
            // # A.cpp int A::Foo() { return 0; }
            //
            // int A::Bar() { return 0; }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Bar</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            // # A.h class A { };
            string xmlh = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "A.cpp");
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "A.h");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl) as INamedScope;
            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var scopeA = globalScope.ChildScopes.FirstOrDefault() as INamedScope;
            Assert.AreEqual("A", scopeA.Name);
            Assert.AreEqual(2, scopeA.ChildScopes.Count());

            var methodFoo = scopeA.ChildScopes.First() as IMethodDefinition;
            var methodBar = scopeA.ChildScopes.Last() as IMethodDefinition;

            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual("A.Foo", methodFoo.GetFullName());
            Assert.AreEqual("Bar", methodBar.Name);
            Assert.AreEqual("A.Bar", methodBar.GetFullName());

            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader)) as INamedScope;

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var typeA = globalScope.ChildScopes.First() as ITypeDefinition;
            Assert.AreEqual(2, typeA.ChildScopes.Count());

            var aDotFoo = typeA.ChildScopes.First() as IMethodDefinition;
            var aDotBar = typeA.ChildScopes.Last() as IMethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.GetFullName());
            Assert.AreEqual("A.Bar", aDotBar.GetFullName());

            Assert.AreSame(methodFoo, aDotFoo);
            Assert.AreSame(methodBar, aDotBar);

            Assert.AreSame(typeA, methodFoo.ParentScope);
            Assert.AreSame(typeA, methodBar.ParentScope);
            Assert.AreSame(globalScope, typeA.ParentScope);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }
    }
}