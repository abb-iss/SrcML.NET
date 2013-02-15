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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    class ScopeMergeTests {
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;
        private Dictionary<Language, AbstractCodeParser> CodeParser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                { Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
            };
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
            };
        }

        [Test]
        public void TestNamespaceMerge_Java() {
            // # D.java
            // package A.B.C;
            // class D {
            //     public void Foo() { }
            // }
            string d_xml = @"<package>package <name>A</name>.<name>B</name>.<name>C</name>;</package>
<class>class <name>D</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            // # E.java
            // package A.B.C;
            // class E {
            //     public void Bar() { }
            // }
            string e_xml = @"<package>package <name>A</name>.<name>B</name>.<name>C</name>;</package>
<class>class <name>E</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            // # F.java
            // package D;
            // class F {
            //     public void Oof() { }
            // }
            string f_xml = @"<package>package <name>D</name>;</package>
<class>class <name>F</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>Oof</name><parameter_list>()</parameter_list> <block>{ }</block></function>
}</block></class>";

            var fileUnitD = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(d_xml, "D.java");
            var fileUnitE = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(e_xml, "E.java");
            var fileUnitF = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(f_xml, "F.java");


            var scopeD = SrcMLElementVisitor.Visit(fileUnitD, CodeParser[Language.Java]);
            var scopeE = SrcMLElementVisitor.Visit(fileUnitE, CodeParser[Language.Java]);
            var scopeF = SrcMLElementVisitor.Visit(fileUnitF, CodeParser[Language.Java]);

            NamespaceDefinition globalScope = new NamespaceDefinition() {
                ProgrammingLanguage = Language.Java,
            };

            globalScope.AddChildScope(scopeD);
            globalScope.AddChildScope(scopeE);
            globalScope.AddChildScope(scopeF);

            Assert.AreEqual(2, globalScope.ChildScopes.Count());
            
            var packageABC = globalScope.ChildScopes.First() as NamespaceDefinition;
            var packageD = globalScope.ChildScopes.Last() as NamespaceDefinition;

            Assert.AreEqual("A.B.C", packageABC.Name);
            Assert.AreEqual("D", packageD.Name);

            var typeD = packageABC.ChildScopes.First() as TypeDefinition;
            var typeE = packageABC.ChildScopes.Last() as TypeDefinition;
            var typeF = packageD.ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("D", typeD.Name);
            Assert.AreEqual("E", typeE.Name);
            Assert.That(typeD.ParentScope == typeE.ParentScope);

            Assert.That(typeD.ParentScope != typeF.ParentScope);
        }

        [Test]
        public void TestNamespaceMerge_Cpp() {
            // # D.h
            // namespace A {
            //     namespace B {
            //         namespace C {
            //             class D { };
            //         }
            //     }
            // }
            string d_xml = @"<namespace>namespace <name>A</name> <block>{
    <namespace>namespace <name>B</name> <block>{
        <namespace>namespace <name>C</name> <block>{
             <class>class <name>D</name> <block>{<private type=""default""> </private>}</block>;</class>
         }</block></namespace>
    }</block></namespace>
}</block></namespace>";

            // # E.h
            // namespace A {
            //     namespace B {
            //         namespace C {
            //             class E { };
            //         }
            //     }
            // }
            string e_xml = @"<namespace>namespace <name>A</name> <block>{
    <namespace>namespace <name>B</name> <block>{
        <namespace>namespace <name>C</name> <block>{
             <class>class <name>E</name> <block>{<private type=""default""> </private>}</block>;</class>
         }</block></namespace>
    }</block></namespace>
}</block></namespace>";

            // # F.h
            // namespace D {
            //     class F { };
            // }
            string f_xml = @"<namespace>namespace <name>D</name> <block>{
    <class>class <name>F</name> <block>{<private type=""default""> </private>}</block>;</class>
}</block></namespace>";

            var fileUnitD = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(d_xml, "D.h");
            var fileUnitE = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(e_xml, "E.h");
            var fileUnitF = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(f_xml, "F.h");

            var globalScope = SrcMLElementVisitor.Visit(fileUnitD, CodeParser[Language.CPlusPlus]);

            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(fileUnitE, CodeParser[Language.CPlusPlus]));
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(fileUnitF, CodeParser[Language.CPlusPlus]));

            Assert.AreEqual(2, globalScope.ChildScopes.Count());
            
            var namespaceA = globalScope.ChildScopes.First() as NamespaceDefinition;
            var namespaceD = globalScope.ChildScopes.Last() as NamespaceDefinition;
            
            Assert.AreEqual(1, namespaceA.ChildScopes.Count());
            Assert.AreEqual(1, namespaceD.ChildScopes.Count());
            Assert.AreEqual("A", namespaceA.FullName);
            Assert.AreEqual("D", namespaceD.FullName);

            var namespaceB = namespaceA.ChildScopes.First() as NamespaceDefinition;
            var typeF = namespaceD.ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("B", namespaceB.Name);
            Assert.AreEqual("F", typeF.Name);
            Assert.AreEqual(1, namespaceB.ChildScopes.Count());

            var namespaceC = namespaceB.ChildScopes.First() as NamespaceDefinition;
            Assert.AreEqual("C", namespaceC.Name);
            Assert.AreEqual("A.B", namespaceC.NamespaceName);
            Assert.AreEqual(2, namespaceC.ChildScopes.Count());
            var typeD = namespaceC.ChildScopes.First() as TypeDefinition;
            var typeE = namespaceC.ChildScopes.Last() as TypeDefinition;

            Assert.That(typeD.ParentScope == typeE.ParentScope);
            Assert.That(typeD.ParentScope == namespaceC);

            Assert.AreEqual("A.B.C.D", typeD.FullName);
            Assert.AreEqual("A.B.C.E", typeE.FullName);
            Assert.AreEqual("D.F", typeF.FullName);
        }

        [Test]
        public void TestUnresolvedParentMerge_MethodsEncounteredFirst_Cpp() {
            // # A.cpp
            // int A::Foo() {
            //     return 0;
            // }
            // 
            // int A::Bar() {
            //     return 0;
            // }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Bar</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            // # A.h
            // class A {
            // };
            string xmlh = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            var xmlImpl =  FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "A.cpp");
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "A.h");

            var globalScope = SrcMLElementVisitor.Visit(xmlImpl, CodeParser[Language.CPlusPlus]);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var scopeA = globalScope.ChildScopes.FirstOrDefault() as NamedScope;
            Assert.AreEqual("A", scopeA.Name);
            Assert.AreEqual(2, scopeA.ChildScopes.Count());

            var methodFoo = scopeA.ChildScopes.First() as MethodDefinition;
            var methodBar = scopeA.ChildScopes.Last() as MethodDefinition;

            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual("A.Foo", methodFoo.FullName);
            Assert.AreEqual("Bar", methodBar.Name);
            Assert.AreEqual("A.Bar", methodBar.FullName);

            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(xmlHeader, CodeParser[Language.CPlusPlus]));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual(2, typeA.ChildScopes.Count());

            var aDotFoo = typeA.ChildScopes.First() as MethodDefinition;
            var aDotBar = typeA.ChildScopes.Last() as MethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.FullName);
            Assert.AreEqual("A.Bar", aDotBar.FullName);

            Assert.AreSame(methodFoo, aDotFoo);
            Assert.AreSame(methodBar, aDotBar);

            Assert.AreSame(typeA, methodFoo.ParentScope);
            Assert.AreSame(typeA, methodBar.ParentScope);
            Assert.AreSame(globalScope, typeA.ParentScope);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestUnresolvedParentMerge_ClassEncounteredFirst_Cpp() {
            // # A.cpp
            // int A::Foo() {
            //     return 0;
            // }
            // 
            // int A::Bar() {
            //     return 0;
            // }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Bar</name></name><parameter_list>()</parameter_list> <block>{
     <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            // # A.h
            // class A {
            // };
            string xmlh = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "A.cpp");
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "A.h");

            var globalScope = SrcMLElementVisitor.Visit(xmlHeader, CodeParser[Language.CPlusPlus]);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(0, typeA.ChildScopes.Count());

            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(xmlImpl, CodeParser[Language.CPlusPlus]));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            
            typeA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual(2, typeA.ChildScopes.Count());

            var aDotFoo = typeA.ChildScopes.First() as MethodDefinition;
            var aDotBar = typeA.ChildScopes.Last() as MethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.FullName);
            Assert.AreEqual("A.Bar", aDotBar.FullName);

            Assert.AreSame(typeA, aDotFoo.ParentScope);
            Assert.AreSame(typeA, aDotFoo.ParentScope);
            Assert.AreSame(globalScope, typeA.ParentScope);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestCreateMethodDefinition_TwoUnresolvedParents() {
            // # B.h
            // namespace A {
            //     class B {
            //     };
            // }
            string xmlh = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{<private type=""default"">
    </private>}</block>;</class>
}</block></namespace>";

            // # B.cpp
            // int A::B::Foo() {
            //     return 0;
            // }
            string xmlcpp = @"<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
}</block></function>";

            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlh, "B.h");
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xmlcpp, "B.cpp");

            var globalScope = SrcMLElementVisitor.Visit(xmlHeader, CodeParser[Language.CPlusPlus]);
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(xmlImpl, CodeParser[Language.CPlusPlus]));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());

            var namespaceA = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildScopes.Count());

            var typeB = namespaceA.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A.B", typeB.FullName);
            Assert.AreEqual(1, typeB.ChildScopes.Count());

            var methodFoo = typeB.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.FullName);
            Assert.AreEqual(0, methodFoo.ChildScopes.Count());

            Assert.AreSame(globalScope, namespaceA.ParentScope);
            Assert.AreSame(namespaceA, typeB.ParentScope);
            Assert.AreSame(typeB, methodFoo.ParentScope);
        }

        [Test]
        public void TestMethodDefinitionMerge_Cpp() {
            // # A.h
            // class A {
            //     int Foo();
            // };
            string header_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
</private>}</block>;</class>";

            // # A.cpp
            // #include "A.h"
            // int A::Foo() {
            //     int bar = 1;
            //     return bar;
            // }
            string impl_xml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>

<function><type><name>int</name></type> <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>int</name></type> <name>bar</name> =<init> <expr><lit:literal type=""number"">1</lit:literal></expr></init></decl>;</decl_stmt>
    <return>return <expr><name>bar</name></expr>;</return>
}</block></function>";

            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(header_xml, "A.h");
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(impl_xml, "A.cpp");

            var globalScope = SrcMLElementVisitor.Visit(header, CodeParser[Language.CPlusPlus]);
            globalScope = globalScope.Merge(SrcMLElementVisitor.Visit(implementation, CodeParser[Language.CPlusPlus]));

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            
            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(1, typeA.ChildScopes.Count());

            var methodFoo = typeA.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual(0, methodFoo.ChildScopes.Count());
            Assert.AreEqual(1, methodFoo.DeclaredVariables.Count());
            Assert.AreEqual("A.cpp", methodFoo.PrimaryLocation.SourceFileName);
            Assert.AreEqual(AccessModifier.Private, methodFoo.Accessibility);
        }
    }
}
