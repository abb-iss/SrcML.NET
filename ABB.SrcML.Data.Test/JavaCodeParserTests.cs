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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var actual = typeDefinitions.First();
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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var actual = typeDefinitions.First();

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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var definition = typeDefinitions.First();

            Assert.AreEqual("A", definition.Name);
            Assert.AreEqual(3, definition.Parents.Count);
            Assert.That(definition.Namespace.IsGlobal);

            var parentNames = from parent in definition.Parents
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (expected, actual) => expected == actual);
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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var definition = typeDefinitions.First();

            Assert.AreEqual("D", definition.Name);
            Assert.AreEqual(1, definition.Parents.Count);
            Assert.That(definition.Namespace.IsGlobal);

            var parent = definition.Parents.First();

            Assert.AreEqual("C", parent.Name);
            var prefix_tests = Enumerable.Zip<string, string, bool>(new[] { "A", "B", "C" }, parent.Prefix, (expected, actual) => expected == actual);
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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);

            Assert.AreEqual(2, typeDefinitions.Count());
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
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);

            Assert.AreEqual(2, typeDefinitions.Count());

            var outer = typeDefinitions.First();
            var inner = typeDefinitions.Last();

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

            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
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

            var scope = codeParser.CreateScopeFromContainer(xmlElement, xmlElement);
            
            var useOfX = xmlElement.Descendants(SRC.Return).First().Descendants(SRC.Name).First();

            Assert.AreEqual(4, scope.GetScopesForPath(useOfX.GetXPath(false)).Count());

            var matchingDeclarations = scope.GetDeclarationsForVariableName("X", useOfX.GetXPath(false));
            var declaration = matchingDeclarations.First();

            Assert.AreEqual("int", declaration.VariableType.Name);
            Assert.AreEqual("X", declaration.Name);

            Assert.That(useOfX.GetXPath(false).StartsWith(declaration.Scope.XPath));
        }
    }
}
