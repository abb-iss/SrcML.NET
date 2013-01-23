using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    class JavaCodeParserTests {
        private string srcMLFormat;
        private AbstractCodeParser codeParser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            srcMLFormat = SrcMLFileUnitSetup.CreateFileUnitTemplate();
            codeParser = new JavaCodeParser();
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A {
            // }
            string xml = @"<class>class <name>A</name> <block>{
}</block></class>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
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
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            // class A {
            //     class B {
            //     }
            // }
            string xml = @"<class>class <name>A</name> <block>{
	<class>class <name>B</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            Assert.Fail("TODO Need to add assertions to verify type in function");
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            string xml = @"<import>import <name>x</name> . <comment type=""block"">/*test */</comment> <name>y</name> . <name>z</name> . <comment type=""block"">/*test */</comment> * <comment type=""block"">/*test*/</comment>;</import>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
            
            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("x.y.z", actual.NamespaceName);
            Assert.That(actual.IsNamespaceAlias);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            string xml = @"<import>import <name>x</name>.<name>y</name>.<name>z</name>;</import>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);

            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.AreEqual("x.y", actual.NamespaceName);
            Assert.AreEqual("z", actual.Name);
            Assert.IsFalse(actual.IsNamespaceAlias);
        }
    }
}
