using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    public class FileRemovalTests_Java {
        private JavaCodeParser CodeParser;
        private SrcMLFileUnitSetup FileUnitSetup;

        [TestFixtureSetUp, Category("Build")]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
            CodeParser = new JavaCodeParser();
        }

        [Test]
        public void TestRemoveClass_Global() {
            ////Foo.java
            //class Foo {
            //    private int bar;
            //    public Foo() { bar = 42; }
            //    public int GetBar() { return bar; }
            //}
            string fooXml = @"<class>class <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>private</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    <constructor><specifier>public</specifier> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>bar</name> <op:operator>=</op:operator> <lit:literal type=""number"">42</lit:literal></expr>;</expr_stmt> }</block></constructor>
    <function><type><specifier>public</specifier> <name>int</name></type> <name>GetBar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>bar</name></expr>;</return> }</block></function>
}</block></class>";
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);
            ////Baz.java
            //class Baz {
            //    public static int DoWork() { return 0; }
            //}
            string bazXml = @"<class>class <name>Baz</name> <block>{
    <function><type><specifier>public</specifier> <specifier>static</specifier> <name>int</name></type> <name>DoWork</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(0, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveClass_Namespace() {
            ////Foo.java
            //package com.ABB.Example;
            //class Foo {
            //    private int bar;
            //    public Foo() { bar = 42; }
            //    public int GetBar() { return bar; }
            //}
            string fooXml = @"<package>package <name>com</name>.<name>ABB</name>.<name>Example</name>;</package>
<class>class <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>private</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    <constructor><specifier>public</specifier> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>bar</name> <op:operator>=</op:operator> <lit:literal type=""number"">42</lit:literal></expr>;</expr_stmt> }</block></constructor>
    <function><type><specifier>public</specifier> <name>int</name></type> <name>GetBar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>bar</name></expr>;</return> }</block></function>
}</block></class>";
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);
            ////Baz.java
            //package com.ABB.Example;
            //class Baz {
            //    public static int DoWork() { return 0; }
            //}
            string bazXml = @"<decl_stmt><decl><type><name>package</name></type> <name><name>com</name><op:operator>.</op:operator><name>ABB</name><op:operator>.</op:operator><name>Example</name></name></decl>;</decl_stmt>
<class>class <name>Baz</name> <block>{
    <function><type><specifier>public</specifier> <specifier>static</specifier> <name>int</name></type> <name>DoWork</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            ////Foo.java
            //package com.ABB.example;
            //class Foo {
            //    private int bar;
            //    public Foo() { bar = 42; }
            //    public int GetBar() { return bar; }
            //}
            string fooXml = @"<package>package <name><name>com</name><op:operator>.</op:operator><name>ABB</name><op:operator>.</op:operator><name>example</name></name>;</package>
<class>class <name>Foo</name> <block>{
	<decl_stmt><decl><type><specifier>private</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
	<constructor><specifier>public</specifier> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>bar</name> <op:operator>=</op:operator> <lit:literal type=""number"">42</lit:literal></expr>;</expr_stmt> }</block></constructor>
	<function><type><specifier>public</specifier> <name>int</name></type> <name>GetBar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>bar</name></expr>;</return> }</block></function>
}</block></class>";
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.java");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);
            ////Baz.java
            //package com.ABB.DifferentExample;
            //class Baz {
            //    public static int DoWork() { return 0; }
            //}
            string bazXml = @"<package>package <name><name>com</name><op:operator>.</op:operator><name>ABB</name><op:operator>.</op:operator><name>DifferentExample</name></name>;</package>
<class>class <name>Baz</name> <block>{
	<function><type><specifier>public</specifier> <specifier>static</specifier> <name>int</name></type> <name>DoWork</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.java");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            var comDotAbb = afterScope.ChildStatements.OfType<NamespaceDefinition>().First().ChildStatements.OfType<NamespaceDefinition>().First();
            Assert.AreEqual("com.ABB", comDotAbb.GetFullName());
            Assert.AreEqual(2, comDotAbb.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("Baz.java");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }
    }
}
