/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    public class CSharpCodeParserTests {
        private CSharpCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new CSharpCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CSharp);
        }

        [Test]
        public void TestCreateTypeDefinition_Class() {
            ////Foo.cs
            //public class Foo {
            //    public int bar;
            //}
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Interface() {
            ////Foo.cs
            //public interface Foo {
            //    public int GetBar();
            //}
            string fooXml = @"<class type=""interface""><specifier>public</specifier> interface <name>Foo</name> <block>{
    <function_decl><type><specifier>public</specifier> <name>int</name></type> <name>GetBar</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Interface, foo.Kind);
            Assert.AreEqual(1, foo.ChildScopes.Count());
            Assert.AreEqual(0, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Struct() {
            ////Foo.cs
            //public struct Foo {
            //    public int bar;
            //}
            string fooXml = @"<struct><specifier>public</specifier> struct <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></struct>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Struct, foo.Kind);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Namespace() {
            ////Foo.cs
            //namespace Example {
            //    public class Foo {
            //        public int bar;
            //    }
            //}
            string fooXml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    }</block></class>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var example = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildScopes.Count());
            var foo = example.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_CompoundNamespace() {
            ////Foo.cs
            //namespace Example.Level2.Level3 {
            //    public class Foo {
            //        public int bar;
            //    }
            //}
            string fooXml = @"<namespace>namespace <name><name>Example</name><op:operator>.</op:operator><name>Level2</name><op:operator>.</op:operator><name>Level3</name></name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    }</block></class>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var example = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildScopes.Count());
            var level2 = example.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(level2);
            Assert.AreEqual("Level2", level2.Name);
            Assert.AreEqual(1, level2.ChildScopes.Count());
            var level3 = level2.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(level3);
            Assert.AreEqual("Level3", level3.Name);
            Assert.AreEqual(1, level3.ChildScopes.Count());
            var foo = level3.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_NestedCompoundNamespace() {
            ////Foo.cs
            //namespace Watermelon {
            //    namespace Example.Level2.Level3 {
            //        public class Foo {
            //            public int bar;
            //        }
            //    }
            //}
            string fooXml = @"<namespace>namespace <name>Watermelon</name> <block>{
    <namespace>namespace <name><name>Example</name><op:operator>.</op:operator><name>Level2</name><op:operator>.</op:operator><name>Level3</name></name> <block>{
        <class><specifier>public</specifier> class <name>Foo</name> <block>{
            <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
        }</block></class>
    }</block></namespace>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var watermelon = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(watermelon);
            Assert.AreEqual("Watermelon", watermelon.Name);
            Assert.AreEqual(1, watermelon.ChildScopes.Count());
            var example = watermelon.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildScopes.Count());
            var level2 = example.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(level2);
            Assert.AreEqual("Level2", level2.Name);
            Assert.AreEqual(1, level2.ChildScopes.Count());
            var level3 = level2.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(level3);
            Assert.AreEqual("Level3", level3.Name);
            Assert.AreEqual(1, level3.ChildScopes.Count());
            var foo = level3.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithParent() {
            ////Foo.cs
            //public class Foo : Baz {
            //    public int bar;
            //}
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <super>: <name>Baz</name></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
            Assert.AreEqual(1, foo.ParentTypes.Count);
            Assert.AreEqual("Baz", foo.ParentTypes.First().Name);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithQualifiedParent() {
            ////Foo.cs
            //public class Foo : Baz, System.IDisposable {
            //    public int bar;
            //}
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <super>: <name>Baz</name>, <name><name>System</name><op:operator>.</op:operator><name>IDisposable</name></name></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(0, foo.ChildScopes.Count());
            Assert.AreEqual(1, foo.DeclaredVariables.Count());
            Assert.AreEqual(2, foo.ParentTypes.Count);
            Assert.AreEqual("Baz", foo.ParentTypes[0].Name);
            Assert.AreEqual("IDisposable", foo.ParentTypes[1].Name);
            Assert.AreEqual("System", foo.ParentTypes[1].Prefix.Name);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            ////A.cs
            //class A {
            //    class B {}
            //}
            string xml = @"<class>class <name>A</name> <block>{
    <class>class <name>B</name> <block>{}</block></class>
}</block></class>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var typeA = globalScope.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildScopes.Count());
            var typeB = typeA.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(typeB);

            Assert.AreSame(typeA, typeB.ParentScope);
            Assert.AreEqual("A", typeA.FullName);
            Assert.AreEqual("A.B", typeB.FullName);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            ////A.cs
            //namespace Foo {
            //    class A {
            //        class B {}
            //    }
            //}
            string xml = @"<namespace>namespace <name>Foo</name> <block>{
    <class>class <name>A</name> <block>{
        <class>class <name>B</name> <block>{}</block></class>
    }</block></class>
}</block></namespace>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var foo = globalScope.ChildScopes.First() as NamespaceDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual(1, foo.ChildScopes.Count());
            var typeA = foo.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildScopes.Count());
            var typeB = typeA.ChildScopes.First() as TypeDefinition;
            Assert.IsNotNull(typeB);

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("Foo", typeA.NamespaceName);
            Assert.AreEqual("Foo.A", typeA.FullName);

            Assert.AreEqual("B", typeB.Name);
            Assert.AreEqual("Foo", typeB.NamespaceName);
            Assert.AreEqual("Foo.A.B", typeB.FullName);
        }

        [Test]
        public void TestFieldCreation() {
            //// A.cs
            //class A {
            //    public int Foo;
            //}
            string xml = @"<class>class <name>A</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>Foo</name></decl>;</decl_stmt>
}</block></class>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildScopes.Count());
            var typeA = globalScope.ChildScopes.First();
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.DeclaredVariables.Count());
            var foo = typeA.DeclaredVariables.First();
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual("int", foo.VariableType.Name);
        }

        [Test]
        public void TestMethodCallCreation() {
            //// A.cs
            //class A {
            //    public int Execute() {
            //        B b = new B();
            //        for(int i = 0; i < b.max(); i++) {
            //            try {
            //                PrintOutput(b.analyze(i));
            //            } catch(Exception e) {
            //                PrintError(e.ToString());
            //            }
            //        }
            //    }
            //}
            string xml = @"<class>class <name>A</name> <block>{
    <function><type><specifier>public</specifier> <name>int</name></type> <name>Execute</name><parameter_list>()</parameter_list> <block>{
        <decl_stmt><decl><type><name>B</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
        <for>for(<init><decl><type><name>int</name></type> <name>i</name> =<init> <expr><lit:literal type=""number"">0</lit:literal></expr></init></decl>;</init> <condition><expr><name>i</name> <op:operator>&lt;</op:operator> <call><name><name>b</name><op:operator>.</op:operator><name>max</name></name><argument_list>()</argument_list></call></expr>;</condition> <incr><expr><name>i</name><op:operator>++</op:operator></expr></incr>) <block>{
            <try>try <block>{
                <expr_stmt><expr><call><name>PrintOutput</name><argument_list>(<argument><expr><call><name><name>b</name><op:operator>.</op:operator><name>analyze</name></name><argument_list>(<argument><expr><name>i</name></expr></argument>)</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
            }</block> <catch>catch(<param><decl><type><name>Exception</name></type> <name>e</name></decl></param>) <block>{
                <expr_stmt><expr><call><name>PrintError</name><argument_list>(<argument><expr><call><name><name>e</name><op:operator>.</op:operator><name>ToString</name></name><argument_list>()</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
            }</block></catch></try>
        }</block></for>
    }</block></function>
}</block></class>";
            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var executeMethod = globalScope.ChildScopes.First().ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(executeMethod);

            Assert.AreEqual(1, executeMethod.MethodCalls.Count());
            var callToNewB = executeMethod.MethodCalls.First();
            Assert.AreEqual("B", callToNewB.Name);
            Assert.IsTrue(callToNewB.IsConstructor);
            Assert.IsFalse(callToNewB.IsDestructor);

            var forStatement = executeMethod.ChildScopes.First();
            Assert.AreEqual(1, forStatement.MethodCalls.Count());
            var callToMax = forStatement.MethodCalls.First();
            Assert.AreEqual("max", callToMax.Name);
            Assert.IsFalse(callToMax.IsDestructor);
            Assert.IsFalse(callToMax.IsConstructor);

            var forBlock = forStatement.ChildScopes.First();
            var tryStatement = forBlock.ChildScopes.First();
            var tryBlock = tryStatement.ChildScopes.First();

            Assert.AreEqual(2, tryBlock.MethodCalls.Count());
            var callToPrintOutput = tryBlock.MethodCalls.First();
            Assert.AreEqual("PrintOutput", callToPrintOutput.Name);
            Assert.IsFalse(callToPrintOutput.IsDestructor);
            Assert.IsFalse(callToPrintOutput.IsConstructor);

            var callToAnalyze = tryBlock.MethodCalls.Last();
            Assert.AreEqual("analyze", callToAnalyze.Name);
            Assert.IsFalse(callToAnalyze.IsDestructor);
            Assert.IsFalse(callToAnalyze.IsConstructor);

            var catchStatement = tryStatement.ChildScopes.Last();
            var catchBlock = catchStatement.ChildScopes.First();

            Assert.AreEqual(2, catchBlock.MethodCalls.Count());
            var callToPrintError = catchBlock.MethodCalls.First();
            Assert.AreEqual("PrintError", callToPrintError.Name);
            Assert.IsFalse(callToPrintError.IsDestructor);
            Assert.IsFalse(callToPrintError.IsConstructor);

            var callToToString = catchBlock.MethodCalls.Last();
            Assert.AreEqual("ToString", callToToString.Name);
            Assert.IsFalse(callToToString.IsDestructor);
            Assert.IsFalse(callToToString.IsConstructor);
        }

        [Test]
        public void TestCreateAliasesForFiles_UsingNamespace() {
            // using x.y.z;
            string xml = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var aliases = codeParser.CreateAliasesForFile(xmlElement);

            var actual = aliases.First();

            Assert.IsNull(actual.ImportedNamedScope);
            Assert.That(actual.IsNamespaceAlias);
            Assert.AreEqual("x", actual.ImportedNamespace.Name);
            Assert.AreEqual("y", actual.ImportedNamespace.ChildScopeUse.Name);
            Assert.AreEqual("z", actual.ImportedNamespace.ChildScopeUse.ChildScopeUse.Name);
        }
    }
}
