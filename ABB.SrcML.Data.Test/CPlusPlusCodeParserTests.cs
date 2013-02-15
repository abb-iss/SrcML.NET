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

            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A : B,C,D {
            // };
            string xml = @"<class>class <name>A</name> <super>: <name>B</name>,<name>C</name>,<name>D</name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

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
            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
            var globalNamespace = actual.ParentScope as NamespaceDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parent = actual.ParentTypes.First();

            Assert.AreEqual("C", parent.Name);
            var prefix_tests = Enumerable.Zip<string, string, bool>(new[] { "A", "B", "C" }, parent.Prefix, (e, a) => e == a);
            foreach(var prefixMatches in prefix_tests) {
                Assert.That(prefixMatches);
            }
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

            var globalScope = SrcMLElementVisitor.Visit(xmlElement, codeParser);
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
            var globalScope = SrcMLElementVisitor.Visit(xmlElement, codeParser);

            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            var typeB = typeA.ChildScopes.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentScope);
            Assert.AreEqual("A", typeA.FullName);
            Assert.AreEqual("A.B", typeB.FullName);
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
            var mainMethod = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as MethodDefinition;

            Assert.AreEqual("main", mainMethod.Name);

            var typeA = mainMethod.ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main.A", typeA.FullName);
            Assert.AreEqual(String.Empty, typeA.NamespaceName);
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct() {
            // struct A {
            // };
            string xml = @"<struct>struct <name>A</name> <block>{<public type=""default"">
</public>}</block>;</struct>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
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
            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
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
            var scopes = VariableScopeIterator.Visit(SrcMLElementVisitor.Visit(xmlElement, codeParser));

            Assert.AreEqual(4, scopes.Count());

            var typeDefinitions = from scope in scopes
                                  let definition = scope as TypeDefinition
                                  where definition != null
                                  select definition;

            var outer = typeDefinitions.First() as TypeDefinition;
            var inner = typeDefinitions.Last() as TypeDefinition;

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.NamespaceName);
            Assert.AreEqual("A.B", outer.FullName);

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.NamespaceName);
            Assert.AreEqual("A.B.C", inner.FullName);
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

            var globalScope = SrcMLElementVisitor.Visit(xmlElement, codeParser);
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

            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("A", actual.NamespaceName);
            Assert.IsFalse(actual.IsNamespaceAlias);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            // using namespace x::y::z;
            string xml = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("x.y.z", actual.NamespaceName);
            Assert.That(actual.IsNamespaceAlias);
        }
    }
}
