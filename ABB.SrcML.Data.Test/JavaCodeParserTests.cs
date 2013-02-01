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
using System.Diagnostics;

namespace ABB.SrcML.Data.Test {
    class JavaCodeParserTests {
        private string srcMLFormat;
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            srcMLFormat = SrcMLFileUnitSetup.CreateFileUnitTemplate();
            codeParser = new JavaCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.Java);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A {
            // }
            string xml = @"<class>class <name>A</name> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;
            Assert.AreEqual("A", actual.Name);
            Assert.That(actual.Namespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_Interface() {
            // interface A {
            // }
            string xml = @"<class type=""interface"">interface <name>A</name> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Interface, actual.Kind);
            Assert.That(actual.Namespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A implements B,C,D {
            // }
            string xml = @"<class>class <name>A</name> <super><implements>implements <name>B</name>,<name>C</name>,<name>D</name></implements></super> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.Parents.Count);
            Assert.That(actual.Namespace.IsGlobal);

            var parentNames = from parent in actual.Parents
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a);
            foreach(var test in tests) {
                Assert.That(test);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            // class D implements A.B.C {
            // }
            string xml = @"<class>class <name>D</name> <super><implements>implements <name>A</name><op:operator>.</op:operator><name>B</name><op:operator>.</op:operator><name>C</name></implements></super> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "D.java");

            var actual = SrcMLElementVisitor.Visit(xmlElement, codeParser).ChildScopes.First() as TypeDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.Parents.Count);
            Assert.That(actual.Namespace.IsGlobal);

            var parent = actual.Parents.First();

            Assert.AreEqual("C", parent.Name);
            var prefix_tests = Enumerable.Zip<string, string, bool>(new[] { "A", "B", "C" }, parent.Prefix, (e, a) => e == a);
            foreach(var prefixMatches in prefix_tests) {
                Assert.That(prefixMatches);
            }
        }
        
        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            // class A {
            //     class B {
            //     }
            // }
            string xml = @"<class>class <name>A</name> <block>{
	<class>class <name>B</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var scopes = VariableScopeIterator.Visit(SrcMLElementVisitor.Visit(xmlElement, codeParser));

            Assert.AreEqual(3, scopes.Count());
            Assert.Fail("TODO add assertions to verify class in class");
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            // package A;
            // class B {
            //     class C {
            //     }
            // }
            string xml = @"<package>package <name>A</name>;</package>
<class>class <name>B</name> <block>{
	<class>class <name>C</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "B.java");
            var scopes = VariableScopeIterator.Visit(SrcMLElementVisitor.Visit(xmlElement, codeParser));
            var typeDefinitions = from scope in scopes
                                  let definition = (scope as TypeDefinition)
                                  where definition != null
                                  select definition;

            Assert.AreEqual(2, typeDefinitions.Count());

            var outer = typeDefinitions.First() as TypeDefinition;
            var inner = typeDefinitions.Last() as TypeDefinition;

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.Namespace.Name);
            Assert.IsFalse(outer.Namespace.IsGlobal);

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.Namespace.Name);
            Assert.IsFalse(inner.Namespace.IsGlobal);

            Assert.Fail("TODO add assertions to verify class in class");
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            // class A {
            //     int foo() {
            //         class B {
            //         }
            //     }
            // }
            string xml = @"<class>class <name>A</name> <block>{
	<function><type><name>int</name></type> <name>foo</name><parameter_list>()</parameter_list> <block>{
		<class>class <name>B</name> <block>{
		}</block></class>
}</block></function>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var typeDefinitions = SrcMLElementVisitor.Visit(xmlElement, codeParser);
            Assert.Fail("TODO Need to add assertions to verify type in function");
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            string xml = @"<import>import <name>x</name> . <comment type=""block"">/*test */</comment> <name>y</name> . <name>z</name> . <comment type=""block"">/*test */</comment> * <comment type=""block"">/*test*/</comment>;</import>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");
            
            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("x.y.z", actual.NamespaceName);
            Assert.That(actual.IsNamespaceAlias);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            string xml = @"<import>import <name>x</name>.<name>y</name>.<name>z</name>;</import>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("x.y", actual.NamespaceName);
            Assert.AreEqual("z", actual.Name);
            Assert.IsFalse(actual.IsNamespaceAlias);
        }

        [Test]
        public void TestCreateVariableDeclaration() {
            // class A {
            //     private int X;
            //     int GetX() { return X; }
            // }
            string xml = @"<class>class <name>A</name> <block>{
	<decl_stmt><decl><type><specifier>private</specifier> <name>int</name></type> <name>X</name></decl>;</decl_stmt>
	<function><type><specifier>public</specifier> <name>int</name></type> <name>GetX</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>X</name></expr>;</return> }</block></function>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var declarationElement = xmlElement.Descendants(SRC.DeclarationStatement).First();

            var rootScope = SrcMLElementVisitor.Visit(xmlElement, codeParser);
            var scope = VariableScopeIterator.Visit(rootScope).Last();
            var useOfX = xmlElement.Descendants(SRC.Return).First().Descendants(SRC.Name).First();

            Assert.AreEqual(3, rootScope.GetScopesForPath(useOfX.GetXPath(false)).Count());

            var matchingDeclarations = rootScope.GetDeclarationsForVariableName("X", useOfX.GetXPath(false));
            var declaration = matchingDeclarations.First();

            Assert.AreEqual("int", declaration.VariableType.Name);
            Assert.AreEqual("X", declaration.Name);

            Assert.That(useOfX.GetXPath(false).StartsWith(declaration.Scope.XPath));
        }
    }
}
