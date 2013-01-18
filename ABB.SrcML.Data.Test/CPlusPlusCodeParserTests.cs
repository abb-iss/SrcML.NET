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

        [TestFixtureSetUp]
        public void ClassSetup() {
            srcMLFormat = SrcMLFileUnitSetup.CreateFileUnitTemplate();
            codeParser = new CPlusPlusCodeParser();
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A {
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var actual = typeDefinitions.First();
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A : B,C,D {
            // };
            string xml = @"<class>class <name>A</name> <super>: <name>B</name>,<name>C</name>,<name>D</name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var definition = typeDefinitions.First();
            Assert.AreEqual("A", definition.Name);
            Assert.AreEqual(3, definition.Parents.Count);

            var parentNames = from parent in definition.Parents
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (expected, actual) => expected == actual
                );
            foreach(var parentMatchesExpected in tests) {
                Assert.That(parentMatchesExpected);
            }
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            Assert.AreEqual(2, typeDefinitions.Count());
            Assert.Fail("TODO add assertions to verify class in class");
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            string xml = @"";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.Java);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            Assert.Fail("TODO Need to add assertions to verify type in function");
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct() {
            // struct A {
            // };
            string xml = @"<struct>struct <name>A</name> <block>{<public type=""default"">
</public>}</block>;</struct>";

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var actual = typeDefinitions.First();
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Struct, actual.Kind);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);
            var actual = typeDefinitions.First();
            Assert.AreEqual(TypeKind.Union, actual.Kind);
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

            XElement xmlElement = SrcMLFileUnitSetup.GetFileUnitForXmlSnippet(srcMLFormat, xml, Language.CPlusPlus);
            var typeDefinitions = codeParser.CreateTypeDefinitions(xmlElement);

            Assert.AreEqual(2, typeDefinitions.Count());

            var outer = typeDefinitions.First();
            var inner = typeDefinitions.Last();

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.Namespace.Name);

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.Namespace.Name);
            Assert.Fail("TODO add assertions to verify class in class");
        }
    }
}
