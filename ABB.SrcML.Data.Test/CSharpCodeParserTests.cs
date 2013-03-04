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
    }
}
