using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture, Category("Build")]
    public class FileRemovalTests_CSharp {
        private CSharpCodeParser CodeParser;
        private SrcMLFileUnitSetup FileUnitSetup;

        [TestFixtureSetUp, Category("Build")]
        public void ClassSetup() {
            FileUnitSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
            CodeParser = new CSharpCodeParser();
        }

        [Test]
        public void TestRemoveClass_Global() {
            ////Foo.cs
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
            var fooFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var beforeScope = CodeParser.ParseFileUnit(fooFileUnit);
            ////Baz.cs
            //class Baz {
            //    public static int DoWork() { return 0; }
            //}
            string bazXml = @"<class>class <name>Baz</name> <block>{
    <function><type><specifier>public</specifier> <specifier>static</specifier> <name>int</name></type> <name>DoWork</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var bazFileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bazXml, "Baz.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bazFileUnit));

            Assert.AreEqual(0, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("Baz.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemoveNamespace() {
            ////A.cs
            //namespace A {
            //    class Foo { int bar; }
            //}
            string aXml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>Foo</name> <block>{ <decl_stmt><decl><type><name>int</name></type> <name>bar</name></decl>;</decl_stmt> }</block></class>
}</block></namespace>";
            var aFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(aXml, "A.cs");
            var beforeScope = CodeParser.ParseFileUnit(aFileunit);
            ////B.cs
            //namespace B {
            //    class Baz { public ulong xyzzy; }
            //}
            string bXml = @"<namespace>namespace <name>B</name> <block>{
    <class>class <name>Baz</name> <block>{ <decl_stmt><decl><type><specifier>public</specifier> <name>ulong</name></type> <name>xyzzy</name></decl>;</decl_stmt> }</block></class>
}</block></namespace>";
            var bFileunit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(bFileunit));

            Assert.AreEqual(2, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());

            afterScope.RemoveFile("B.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialClass() {
            ////A1.cs
            //public partial class A {
            //    public int Execute() {
            //        return 0;
            //    }
            //}
            string a1Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <name>int</name></type> <name>Execute</name><parameter_list>()</parameter_list> <block>{
        <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
    }</block></function>
}</block></class>";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //public partial class A {
            //    private bool Foo() {
            //        return true;
            //    }
            //}
            string a2Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>private</specifier> <name>bool</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{
        <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return>
    }</block></function>
}</block></class>";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(2, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Execute"));
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Foo"));

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialMethod_Declaration() {
            ////A2.cs
            //public partial class A {
            //    public partial int Foo() { return 42; }
            //}
            string a2Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">42</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var beforeScope = CodeParser.ParseFileUnit(a2FileUnit);
            ////A1.cs
            //public partial class A {
            //    public partial int Foo();
            //}
            string a1Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function_decl><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a1FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            var foo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.That(foo.IsPartial);

            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);

            afterScope.RemoveFile("A1.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartialMethod_Implementation() {
            ////A1.cs
            //public partial class A {
            //    public partial int Foo();
            //}
            string a1Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function_decl><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //public partial class A {
            //    public partial int Foo() { return 42; }
            //}
            string a2Xml = @"<class><specifier>public</specifier> <specifier>partial</specifier> class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <specifier>partial</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""number"">42</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.Count());
            var typeA = afterScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            var foo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);

            afterScope.RemoveFile("A2.cs");

           DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestRemovePartOfNamespace() {
            ////A1.cs
            //namespace A {
            //    class Foo { int bar; }
            //}
            string a1Xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>Foo</name> <block>{ <decl_stmt><decl><type><name>int</name></type> <name>bar</name></decl>;</decl_stmt> }</block></class>
}</block></namespace>";
            var a1FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a1Xml, "A1.cs");
            var beforeScope = CodeParser.ParseFileUnit(a1FileUnit);
            ////A2.cs
            //namespace A {
            //    class Baz { public ulong xyzzy; }
            //}
            string a2Xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>Baz</name> <block>{ <decl_stmt><decl><type><specifier>public</specifier> <name>ulong</name></type> <name>xyzzy</name></decl>;</decl_stmt> }</block></class>
}</block></namespace>";
            var a2FileUnit = FileUnitSetup.GetFileUnitForXmlSnippet(a2Xml, "A2.cs");
            var afterScope = beforeScope.Merge(CodeParser.ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, afterScope.ChildStatements.OfType<NamespaceDefinition>().Count());
            Assert.AreEqual(2, afterScope.ChildStatements.First().ChildStatements.OfType<TypeDefinition>().Count());

            afterScope.RemoveFile("A2.cs");

            DataAssert.StatementsAreEqual(beforeScope, afterScope);
        }

        [Test]
        public void TestFileRemovalWithDifferentCase() {
            // namespace A { class B { } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ }</block></class> }</block></namespace>";

            // namespace C { class D { } }
            string dXml = @"<namespace>namespace <name>C</name> <block>{ <class>class <name>D</name> <block>{ }</block></class> }</block></namespace>";

            var bUnit = FileUnitSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var dUnit = FileUnitSetup.GetFileUnitForXmlSnippet(dXml, "D.cs");

            var bScope = CodeParser.ParseFileUnit(bUnit);
            var dScope = CodeParser.ParseFileUnit(dUnit);
            var globalScope = bScope.Merge(dScope);

            globalScope.RemoveFile("b.cs");
            Assert.AreEqual(1, globalScope.ChildStatements.Count());
        }
    }
}
