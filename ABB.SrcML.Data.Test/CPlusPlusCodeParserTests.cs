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
using System.Xml;
using System.Xml.Linq;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    class CPlusPlusCodeParserTests {
        private string srcMLFormat;
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            srcMLFormat = SrcMLFileUnitSetup.CreateFileUnitTemplate();
            codeParser = new CPlusPlusCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A {
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentScope);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A : B,C,D {
            // };
            string xml = @"<class>class <name>A</name> <super>: <name>B</name>,<name>C</name>,<name>D</name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypes.Count);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentScope);

            var parentNames = from parent in actual.ParentTypes
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a
                );
            foreach(var parentMatchesExpected in tests) {
                Assert.That(parentMatchesExpected);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            // class D : A::B::C {
            // }
            string xml = @"<class>class <name>D</name> <super>: <name><name>A</name><op:operator>::</op:operator><name>B</name><op:operator>::</op:operator><name>C</name></name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "D.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parent = actual.ParentTypes.First();

            Assert.AreEqual("C", parent.Name);
            TestHelper.VerifyPrefixValues(new[] { "A", "B" }, parent.Prefix);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassInNamespace() {
            // namespace A {
            //     class B {
            //     };
            // }
            string xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{<private type=""default"">
    </private>}</block>;</class>
}</block></namespace>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "B.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var namespaceA = globalScope.ChildScopes.First() as NamespaceDefinition;
            var typeB = namespaceA.ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("A", namespaceA.Name);
            Assert.IsFalse(namespaceA.IsGlobal);

            Assert.AreEqual("B", typeB.Name);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            // class A {
            //     class B {
            //     };
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
	<class>class <name>B</name> <block>{<private type=""default"">
	</private>}</block>;</class>
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            var typeB = typeA.ChildScopes.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentScope);
            Assert.AreEqual("A", typeA.GetFullName());
            
            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            // int main() {
            //     class A {
            //     };
            // }
            string xml = @"<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
	<class>class <name>A</name> <block>{<private type=""default"">
	</private>}</block>;</class>
}</block></function>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "main.cpp");
            var mainMethod = codeParser.ParseFileUnit(xmlElement).ChildScopes.First() as MethodDefinition;

            Assert.AreEqual("main", mainMethod.Name);

            var typeA = mainMethod.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main.A", typeA.GetFullName());
            Assert.AreEqual(String.Empty, typeA.GetFirstScope<NamespaceDefinition>().GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct() {
            // struct A {
            // };
            string xml = @"<struct>struct <name>A</name> <block>{<public type=""default"">
</public>}</block>;</struct>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Struct, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_Union() {
            // union A {
            //     int a;
            //     char b;
            //};
            string xml = @"<union>union <name>A</name> <block>{<public type=""default"">
	<decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
	<decl_stmt><decl><type><name>char</name></type> <name>b</name></decl>;</decl_stmt>
</public>}</block>;</union>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;
            Assert.AreEqual(TypeKind.Union, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            // namespace A {
            //     class B {
            //         class C {
            //         };
            //     };
            // }
            string xml = @"<namespace>namespace <name>A</name> <block>{
	<class>class <name>B</name> <block>{<private type=""default"">
		<class>class <name>C</name> <block>{<private type=""default"">
		</private>}</block>;</class>
	</private>}</block>;</class>
}</block></namespace>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var scopes = VariableScopeIterator.Visit(codeParser.ParseFileUnit(xmlElement));

            Assert.AreEqual(4, scopes.Count());

            var typeDefinitions = from scope in scopes
                                  let definition = scope as TypeDefinition
                                  where definition != null
                                  select definition;

            var outer = typeDefinitions.First() as TypeDefinition;
            var inner = typeDefinitions.Last() as TypeDefinition;

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.GetFirstScope<NamespaceDefinition>().GetFullName());
            Assert.AreEqual("A.B", outer.GetFullName());

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.GetFirstScope<NamespaceDefinition>().GetFullName());
            Assert.AreEqual("A.B.C", inner.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithMethodDeclaration() {
            // class A {
            // public:
            //     int foo(int a);   
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
</private><public>public:
    <function_decl><type><name>int</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>)</parameter_list>;</function_decl>
</public>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var scopes = VariableScopeIterator.Visit(globalScope);

            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            var methodFoo = typeA.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual(3, scopes.Count());

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("foo", methodFoo.Name);

            Assert.AreEqual(1, methodFoo.Parameters.Count);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            // using A::Foo;
            string xml = @"<using>using <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name>;</using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var actual = codeParser.ParseAliasElement(xmlElement.Element(SRC.Using), new ParserContext(xmlElement));

            Assert.AreEqual("Foo", actual.ImportedNamedScope.Name);
            Assert.AreEqual("A", actual.ImportedNamespace.Name);
            Assert.IsFalse(actual.IsNamespaceImport);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            // using namespace x::y::z;
            string xml = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var actual = codeParser.ParseAliasElement(xmlElement.Element(SRC.Using), new ParserContext(xmlElement));

            Assert.IsNull(actual.ImportedNamedScope);
            Assert.That(actual.IsNamespaceImport);
            Assert.AreEqual("x", actual.ImportedNamespace.Name);
            Assert.AreEqual("y", actual.ImportedNamespace.ChildScopeUse.Name);
            Assert.AreEqual("z", actual.ImportedNamespace.ChildScopeUse.ChildScopeUse.Name);
        }

        [Test]
        public void TestMethodCallCreation_WithThisKeyword() {
            //class A {
            //    void Bar() { }
            //    class B {
            //        int a;
            //        void Foo() { this->Bar(); }
            //        void Bar() { return this->a; }
            //    };
            //};
            string a_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
    <class>class <name>B</name> <block>{<private type=""default"">
        <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
        <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><call><name>Bar</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
        <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><name>a</name></expr>;</return> }</block></function>
    </private>}</block>;</class>
</private>}</block>;</class>";

            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.java");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var aDotBar = globalScope.ChildScopes.First().ChildScopes.First() as MethodDefinition;
            var aDotBDotFoo = globalScope.ChildScopes.First().ChildScopes.Last().ChildScopes.First() as MethodDefinition;
            var aDotBDotBar = globalScope.ChildScopes.First().ChildScopes.Last().ChildScopes.Last() as MethodDefinition;

            Assert.AreEqual("A.Bar", aDotBar.GetFullName());
            Assert.AreEqual("A.B.Foo", aDotBDotFoo.GetFullName());
            Assert.AreEqual("A.B.Bar", aDotBDotBar.GetFullName());

            Assert.AreSame(aDotBDotBar, aDotBDotFoo.MethodCalls.First().FindMatches().First());
            Assert.AreEqual(1, aDotBDotFoo.MethodCalls.First().FindMatches().Count());
        }

        [Test]
        public void TestClassWithDeclaredVariable() {
            //class A {
            //    int a;
            //};
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
</private>}</block>;</class>";

            var globalScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(xml, "A.h"));

            var classA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", classA.Name);
            Assert.AreEqual(1, classA.DeclaredVariables.Count());
        }

        [Test]
        public void TestMethodCallCreation_WithConflictingMethodNames() {
            //# A.h
            //class A {
            //    B b;
            //public:
            //    bool Contains() { b.Contains(); }
            //};
            string a_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>B</name></type> <name>b</name></decl>;</decl_stmt>
</private><public>public:
    <function><type><name>bool</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>b</name><op:operator>.</op:operator><call><name>Contains</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
</public>}</block>;</class>";

            //# B.h
            //class B {
            //public:
            //    bool Contains() { return true; }
            //};
            string b_xml = @"<class>class <name>B</name> <block>{<private type=""default"">
</private><public>public:
    <function><type><name>bool</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
</public>}</block>;</class>";

            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.h");
            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(b_xml, "B.h");

            var scopeForA = codeParser.ParseFileUnit(fileUnitA);
            var scopeForB = codeParser.ParseFileUnit(fileUnitB);
            var globalScope = scopeForA.Merge(scopeForB);

            var classA = globalScope.ChildScopes.First() as TypeDefinition;
            var classB = globalScope.ChildScopes.Last() as TypeDefinition;
            Assert.AreEqual("A", classA.Name);
            Assert.AreEqual("B", classB.Name);

            var aDotContains = classA.ChildScopes.First() as MethodDefinition;
            var bDotContains = classB.ChildScopes.First() as MethodDefinition;

            Assert.AreEqual("A.Contains", aDotContains.GetFullName());
            Assert.AreEqual("B.Contains", bDotContains.GetFullName());

            var methodCall = aDotContains.MethodCalls.First();
            var variableB = classA.DeclaredVariables.First();

            Assert.AreEqual("b", (methodCall.CallingObject as VariableUse).Name);
            Assert.AreEqual("b", variableB.Name);
            Assert.AreSame(variableB, (methodCall.CallingObject as VariableUse).FindMatches().First());

            Assert.AreSame(bDotContains, methodCall.FindMatches().First());
            Assert.AreNotSame(aDotContains, methodCall.FindMatches().First());
        }
        [Test]
        public void TestMergeWithUsing() {
            // namespace A { class B { void Foo(); }; }
            string headerXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{<private type=""default""> <function_decl><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl> </private>}</block>;</class> }</block></namespace>";

            //using namespace A;
            //
            //void B::Foo() { }
            string implementationXml = @"<using>using namespace <name>A</name>;</using>

<function><type><name>void</name></type> <name><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var headerScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(headerXml, "A.h"));
            var implementationScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(implementationXml, "A.cpp"));

            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            
            var namespaceA = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildScopes.Count());
            
            var typeB = namespaceA.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("B", typeB.Name);
            Assert.AreEqual(1, typeB.ChildScopes.Count());

            var methodFoo = typeB.ChildScopes.First() as MethodDefinition;
            Assert.AreEqual("Foo", methodFoo);
            Assert.IsEmpty(typeB.ChildScopes);

            var globalScope_implementationFirst = implementationScope.Merge(headerScope);

            TestHelper.ScopesAreEqual(globalScope, globalScope_implementationFirst);
        }
    }
}
