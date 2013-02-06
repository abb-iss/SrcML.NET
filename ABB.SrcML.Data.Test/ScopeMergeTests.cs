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

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                {Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
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

            var codeParser = new JavaCodeParser();

            var scopeD = SrcMLElementVisitor.Visit(fileUnitD, codeParser);
            var scopeE = SrcMLElementVisitor.Visit(fileUnitE, codeParser);
            var scopeF = SrcMLElementVisitor.Visit(fileUnitF, codeParser);

            scopeD.Merge(scopeE);
            scopeD.Merge(scopeF);

            Assert.AreEqual(2, scopeD.ChildScopes.Count());
            Assert.AreEqual(1, scopeF.ChildScopes.Count());

            var packageABC = scopeD as NamespaceDefinition;
            var packageD = scopeF as NamespaceDefinition;

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

            var fileUnitD = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(d_xml, "D.h");
            var fileUnitE = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(e_xml, "E.h");
            var fileUnitF = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(f_xml, "F.h");

            var codeParser = new CPlusPlusCodeParser();

            var globalScope = SrcMLElementVisitor.Visit(fileUnitD, codeParser);

            globalScope.Merge(SrcMLElementVisitor.Visit(fileUnitE, codeParser));
            globalScope.Merge(SrcMLElementVisitor.Visit(fileUnitF, codeParser));

            Assert.AreEqual(2, globalScope.ChildScopes.Count());
            
            var namespaceA = globalScope.ChildScopes.First() as NamespaceDefinition;
            var namespaceD = globalScope.ChildScopes.Last() as NamespaceDefinition;
            
            Assert.AreEqual(1, namespaceA.ChildScopes.Count());
            Assert.AreEqual(1, namespaceD.ChildScopes.Count());

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
    }
}
