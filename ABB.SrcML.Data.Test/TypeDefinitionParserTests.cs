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
    class TypeDefinitionParserTests {
        private string srcMLFormat;

        [TestFixtureSetUp]
        public void ClassSetup() {
            //construct the necessary srcML wrapper unit tags
            XmlNamespaceManager xnm = SrcML.NamespaceManager;
            StringBuilder namespaceDecls = new StringBuilder();
            foreach(string prefix in xnm) {
                if(prefix != string.Empty && !prefix.StartsWith("xml", StringComparison.InvariantCultureIgnoreCase)) {
                    if(prefix.Equals("src", StringComparison.InvariantCultureIgnoreCase)) {
                        namespaceDecls.AppendFormat("xmlns=\"{0}\" ", xnm.LookupNamespace(prefix));
                    } else {
                        namespaceDecls.AppendFormat("xmlns:{0}=\"{1}\" ", prefix, xnm.LookupNamespace(prefix));
                    }
                }
            }
            srcMLFormat = string.Format("<unit {0} language=\"{{1}}\">{{0}}</unit>", namespaceDecls.ToString());
        }

        [Test]
        public void TestCreateTypeDefinitions_Class_Cpp() {
            // class A {
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class_Java() {
            // class A {
            // }
            string xml = @"<class>class <name>A</name> <block>{
}</block></class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents_Cpp() {
            // class A : B,C,D {
            // };
            string xml = @"<class>class <name>A</name> <super>: <name>B</name>,<name>C</name>,<name>D</name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents_Java() {
            // class A implements B,C,D {
            // }
            string xml = @"<class>class <name>A</name> <super><implements>implements <name>B</name>,<name>C</name>,<name>D</name></implements></super> <block>{
}</block></class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass_Cpp() {
            // class A {
            //     class B {
            //     };
            // };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
	<class>class <name>B</name> <block>{<private type=""default"">
	</private>}</block>;</class>
</private>}</block>;</class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass_Java() {
            // class A {
            //     class B {
            //     }
            // }
            string xml = @"<class>class <name>A</name> <block>{
	<class>class <name>B</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction_Cpp() {
            string xml = @"";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction_Java() {
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

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct_Cpp() {
            // struct A {
            // };
            string xml = @"<struct>struct <name>A</name> <block>{<public type=""default"">
</public>}</block>;</struct>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_Union_Cpp() {
            // union A {
            //     int a;
            //     char b;
            //};
            string xml = @"<union>union <name>A</name> <block>{<public type=""default"">
	<decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
	<decl_stmt><decl><type><name>char</name></type> <name>b</name></decl>;</decl_stmt>
</public>}</block>;</union>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace_Cpp() {
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

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.CPlusPlus);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace_Java() {
            // package A;
            // class B {
            //     class C {
            //     }
            // }
            string xml = @"<package>package <name>A</name>;</package>
<class>class <name>B</name> <block>{
	<class>class <name>B</name> <block>{
}</block></class>
}</block></class>";

            XElement xmlElement = GetFileUnitForXmlSnippet(xml, Language.Java);
            var typeDefinitions = CodeParser.CreateTypeDefinitions(xmlElement);
        }

        private XElement GetFileUnitForXmlSnippet(string xmlSnippet, Language language) {
            var xml = String.Format(srcMLFormat, xmlSnippet, KsuAdapter.GetLanguage(language));
            var fileUnit = XElement.Parse(xml);
            return fileUnit;
        }
    }
}
