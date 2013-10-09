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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class CSharpCodeParserTests {
        private CSharpCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new CSharpCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CSharp);
        }

        [Test]
        public void TestConstructorWithBaseKeyword() {
            // B.cs namespace A { class B { public B() { } } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <block>{ }</block></constructor> }</block></class> }</block></namespace>";
            // C.cs namespace A { class C : B { public C() : base() { } } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <super>: <name>B</name></super> <block>{ <constructor><specifier>public</specifier> <name>C</name><parameter_list>()</parameter_list> <member_list>: <call><name>base</name><argument_list>()</argument_list></call> </member_list><block>{ }</block></constructor> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var constructors = from methodDefinition in globalScope.GetDescendantScopes<MethodDefinition>()
                               where methodDefinition.IsConstructor
                               select methodDefinition;

            var bConstructor = (from method in constructors
                                where method.GetParentScopes<TypeDefinition>().FirstOrDefault().Name == "B"
                                select method).FirstOrDefault();

            var cConstructor = (from method in constructors
                                where method.GetParentScopes<TypeDefinition>().FirstOrDefault().Name == "C"
                                select method).FirstOrDefault();

            var methodCall = (from scope in globalScope.GetDescendantScopes()
                              from call in scope.MethodCalls
                              select call).FirstOrDefault();

            Assert.IsNotNull(methodCall);
            Assert.That(methodCall.IsConstructor);
            Assert.AreSame(bConstructor, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructorWithThisKeyword() {
            // B.cs
            //namespace A {
            //    class B {
            //        public B() : this(0) { }
            //        public B(int i) { }
            //    }
            //}

            string bXml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{
        <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <member_list>: <call><name>this</name><argument_list>(<argument><expr><lit:literal type=""number"">0</lit:literal></expr></argument>)</argument_list></call> </member_list><block>{ }</block></constructor>
        <constructor><specifier>public</specifier> <name>B</name><parameter_list>(<param><decl><type><name>int</name></type> <name>i</name></decl></param>)</parameter_list> <block>{ }</block></constructor>
    }</block></class>
}</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");

            var globalScope = codeParser.ParseFileUnit(bUnit);

            var constructors = from methodDefinition in globalScope.GetDescendantScopes<MethodDefinition>()
                               where methodDefinition.IsConstructor
                               select methodDefinition;

            var defaultConstructor = (from method in constructors
                                      where method.Parameters.Count == 0
                                      select method).FirstOrDefault();

            var oneArgumentConstructor = (from method in constructors
                                          where method.Parameters.Count == 1
                                          select method).FirstOrDefault();

            var methodCall = (from scope in globalScope.GetDescendantScopes()
                              from call in scope.MethodCalls
                              select call).FirstOrDefault();

            Assert.IsNotNull(methodCall);
            Assert.That(methodCall.IsConstructor);
            Assert.AreSame(oneArgumentConstructor, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCreateAliasesForFiles_UsingNamespace() {
            // using x.y.z;
            string xml = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var actual = codeParser.ParseAliasElement(xmlElement.Element(SRC.Using), new ParserContext(xmlElement));

            Assert.IsNull(actual.ImportedNamedScope);
            Assert.That(actual.IsNamespaceImport);
            Assert.AreEqual("x", actual.ImportedNamespace.Name);
            Assert.AreEqual("y", actual.ImportedNamespace.ChildScopeUse.Name);
            Assert.AreEqual("z", actual.ImportedNamespace.ChildScopeUse.ChildScopeUse.Name);
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
            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreEqual("A.B", typeB.GetFullName());
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
            Assert.AreEqual("Foo", typeA.GetFirstParent<NamespaceDefinition>().GetFullName());
            Assert.AreEqual("Foo.A", typeA.GetFullName());

            Assert.AreEqual("B", typeB.Name);
            Assert.AreEqual("Foo", typeB.GetFirstParent<NamespaceDefinition>().GetFullName());
            Assert.AreEqual("Foo.A.B", typeB.GetFullName());
        }

        [Test]
        public void TestDeclarationWithTypeVarFromConstructor() {
            // B.cs namespace A { class B { public B() { }; } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <block>{ }</block></constructor><empty_stmt>;</empty_stmt> }</block></class> }</block></namespace>";
            // C.cs namespace A { class C { void main() { var b = new B(); } } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <block>{ <function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ <decl_stmt><decl><type><name>var</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");
            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var typeB = (from type in globalScope.GetDescendantScopes<TypeDefinition>()
                         where type.Name == "B"
                         select type).FirstOrDefault();

            var declaration = (from scope in globalScope.GetDescendantScopesAndSelf()
                               from decl in scope.DeclaredVariables
                               select decl).FirstOrDefault();

            Assert.IsNotNull(typeB);
            Assert.IsNotNull(declaration);
            Assert.AreSame(typeB, declaration.VariableType.FindFirstMatchingType());
        }

        [Test]
        public void TestDeclarationWithTypeVarFromMethod() {
            //namespace A {
            //    class B {
            //        public static void main() { var b = getB(); }
            //        public static B getB() { return new B(); }
            //    }
            //}
            string xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{
        <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ <decl_stmt><decl><type><name>var</name></type> <name>b</name> =<init> <expr><call><name>getB</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt> }</block></function>
        <function><type><specifier>public</specifier> <specifier>static</specifier> <name>B</name></type> <name>getB</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
            var scope = codeParser.ParseFileUnit(unit);

            var typeB = (from type in scope.GetDescendantScopes<TypeDefinition>()
                         where type.Name == "B"
                         select type).FirstOrDefault();

            var mainMethod = (from method in scope.GetDescendantScopes<MethodDefinition>()
                              where method.Name == "main"
                              select method).FirstOrDefault();

            var declaration = mainMethod.DeclaredVariables.FirstOrDefault();

            Assert.IsNotNull(declaration);
            Assert.AreSame(typeB, declaration.VariableType.FindFirstMatchingType());
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
        public void TestGenericVariableDeclaration() {
            //Dictionary<string,int> map;
            string xml = @"<decl_stmt><decl><type><name><name>Dictionary</name><argument_list>&lt;<argument><name>string</name></argument>,<argument><name>int</name></argument>&gt;</argument_list></name></type> <name>map</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cs");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.DeclaredVariables.First();
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("map", testDeclaration.Name);
            Assert.AreEqual("Dictionary", testDeclaration.VariableType.Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(2, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("string", testDeclaration.VariableType.TypeParameters.First().Name);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.Last().Name);
        }

        [Test]
        public void TestGenericVariableDeclarationWithPrefix() {
            //System.Collection.Dictionary<string,int> map;
            string xml = @"<decl_stmt><decl><type><name><name>System</name><op:operator>.</op:operator><name>Collection</name><op:operator>.</op:operator><name><name>Dictionary</name><argument_list>&lt;<argument><name>string</name></argument>,<argument><name>int</name></argument>&gt;</argument_list></name></name></type> <name>map</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cs");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.DeclaredVariables.First();
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("map", testDeclaration.Name);
            Assert.AreEqual("Dictionary", testDeclaration.VariableType.Name);
            Assert.AreEqual("System.Collection", testDeclaration.VariableType.Prefix.ToString());
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(2, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("string", testDeclaration.VariableType.TypeParameters.First().Name);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.Last().Name);
        }

        [Test]
        public void TestGetAccessModifierForMethod_InternalProtected() {
            //namespace Example {
            //    public class Foo {
            //        internal protected bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>internal</specifier> <specifier>protected</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForMethod(element));
        }

        [Test]
        public void TestGetAccessModifierForMethod_None() {
            //namespace Example {
            //    public class Foo {
            //        bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).FirstOrDefault();
            Assert.AreEqual(AccessModifier.None, codeParser.GetAccessModifierForMethod(element));
        }

        [Test]
        public void TestGetAccessModifierForMethod_Normal() {
            //namespace Example {
            //    public class Foo {
            //        public bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>public</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).FirstOrDefault();
            Assert.AreEqual(AccessModifier.Public, codeParser.GetAccessModifierForMethod(element));
        }

        [Test]
        public void TestGetAccessModifierForMethod_ProtectedInternal() {
            //namespace Example {
            //    public class Foo {
            //        protected internal bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>protected</specifier> <specifier>internal</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForMethod(element));
        }

        [Test]
        public void TestGetAccessModifierForMethod_ProtectedInternalStatic() {
            //namespace Example {
            //    public class Foo {
            //        protected static internal bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>protected</specifier> <specifier>static</specifier> <specifier>internal</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForMethod(element));
        }

        [Test]
        public void TestGetAccessModifierForType_InternalProtected() {
            //namespace Example {
            //    internal protected class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>internal</specifier> <specifier>protected</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForType(element));
        }

        [Test]
        public void TestGetAccessModifierForType_None() {
            //namespace Example {
            //    class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class>class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).FirstOrDefault();
            Assert.AreEqual(AccessModifier.None, codeParser.GetAccessModifierForType(element));
        }

        [Test]
        public void TestGetAccessModifierForType_Normal() {
            //namespace Example {
            //    public class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).FirstOrDefault();
            Assert.AreEqual(AccessModifier.Public, codeParser.GetAccessModifierForType(element));
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternal() {
            //namespace Example {
            //    protected internal class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>protected</specifier> <specifier>internal</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForType(element));
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternalStatic() {
            //namespace Example {
            //    protected static internal class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>protected</specifier> <specifier>static</specifier> <specifier>internal</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var element = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).FirstOrDefault();
            Assert.AreEqual(AccessModifier.ProtectedInternal, codeParser.GetAccessModifierForType(element));
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
        public void TestMethodCallWithBaseKeyword() {
            // B.cs namespace A { class B { public virtual void Foo() { } } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <function><type><specifier>public</specifier> <specifier>virtual</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class> }</block></namespace>";
            // C.cs namespace A { class C : B { public override void Foo() { base.Foo(); } } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <super>: <name>B</name></super> <block>{ <function><type><specifier>public</specifier> <specifier>override</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>base</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var fooMethods = from methodDefinition in globalScope.GetDescendantScopes<MethodDefinition>()
                             select methodDefinition;

            var bDotFoo = (from method in fooMethods
                           where method.GetParentScopes<TypeDefinition>().FirstOrDefault().Name == "B"
                           select method).FirstOrDefault();
            var cDotFoo = (from method in fooMethods
                           where method.GetParentScopes<TypeDefinition>().FirstOrDefault().Name == "C"
                           select method).FirstOrDefault();

            Assert.IsNotNull(bDotFoo);
            Assert.IsNotNull(cDotFoo);

            var methodCall = (from scope in globalScope.GetDescendantScopes()
                              from call in scope.MethodCalls
                              select call).FirstOrDefault();
            Assert.IsNotNull(methodCall);
            Assert.AreSame(bDotFoo, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodDefinitionWithReturnType() {
            //int Foo() { }
            string xml = @"<function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithReturnTypeAndWithSpecifier() {
            //static int Foo() { }
            string xml = @"<function><type><specifier>static</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithVoidReturn() {
            //void Foo() { }
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(method, "could not find the test method");

            Assert.IsNull(method.ReturnType, "return type should be null");
        }

        [Test]
        public void TestMultiVariableDeclarations() {
            //int a,b,c;
            string testXml = @"<decl_stmt><decl><type><name>int</name></type> <name>a</name><op:operator>,</op:operator><name>b</name><op:operator>,</op:operator><name>c</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cs");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            Assert.AreEqual(3, globalScope.DeclaredVariables.Count());

            var declaredVariableNames = from variable in globalScope.DeclaredVariables select variable.Name;
            var expectedVariableNames = new string[] { "a", "b", "c" };

            CollectionAssert.AreEquivalent(expectedVariableNames, declaredVariableNames);
        }

        [Test]
        public void TestProperty() {
            // namespace A { class B { int Foo { get; set; } } }
            string xml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <decl_stmt><decl><type><name>int</name></type> <name>Foo</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt> }</block></class> }</block></namespace>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var classB = testScope.GetDescendantScopes<TypeDefinition>().FirstOrDefault();

            Assert.IsNotNull(classB);
            Assert.AreEqual(1, classB.DeclaredVariables.Count());

            var fooProperty = classB.DeclaredVariables.First();
            Assert.AreEqual("Foo", fooProperty.Name);
            Assert.AreEqual("int", fooProperty.VariableType.Name);
        }

        [Test]
        public void TestPropertyAsCallingObject() {
            // B.cs
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <decl_stmt><decl><type><name>C</name></type> <name>Foo</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt> }</block></class> }</block></namespace>";

            // C.cs
            //namespace A {
            //	class C {
            //		static void main() {
            //			B b = new B();
            //			b.Foo.Bar();
            //		}
            //		void Bar() { }
            //	}
            //}
            string cXml = @"<namespace>namespace <name>A</name> <block>{
	<class>class <name>C</name> <block>{
		<function><type><specifier>static</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
			<decl_stmt><decl><type><name>B</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
			<expr_stmt><expr><call><name><name>b</name><op:operator>.</op:operator><name>Foo</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
		}</block></function>

		<function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
	}</block></class>
}</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");
            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);

            var globalScope = bScope.Merge(cScope);

            var classB = (from t in globalScope.GetDescendantScopes<TypeDefinition>()
                          where t.Name == "B"
                          select t).FirstOrDefault();

            var classC = (from t in globalScope.GetDescendantScopes<TypeDefinition>()
                          where t.Name == "C"
                          select t).FirstOrDefault();

            Assert.IsNotNull(classB);
            Assert.IsNotNull(classC);

            var mainMethod = classC.GetChildScopesWithId<MethodDefinition>("main").FirstOrDefault();
            var barMethod = classC.GetChildScopesWithId<MethodDefinition>("Bar").FirstOrDefault();

            Assert.IsNotNull(mainMethod);
            Assert.IsNotNull(barMethod);

            var callToBar = (from m in mainMethod.MethodCalls
                             where m.Name == "Bar"
                             select m).FirstOrDefault();

            Assert.IsNotNull(callToBar);
            Assert.AreSame(barMethod, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariablesWithSpecifiers() {
            //static int A;
            //public const int B;
            //public static readonly Foo C;
            //volatile  int D;
            string testXml = @"<decl_stmt><decl><type><specifier>static</specifier> <name>int</name></type> <name>A</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>public</specifier> <name>const</name> <name>int</name></type> <name>B</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>public</specifier> <specifier>static</specifier> <specifier>readonly</specifier> <name>Foo</name></type> <name>C</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>volatile</specifier>  <name>int</name></type> <name>D</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cs");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            var declaredVariableNames = from variable in globalScope.DeclaredVariables select variable.Name;
            var declaredVariableTypes = from variable in globalScope.DeclaredVariables select variable.VariableType.Name;

            var expectedVariableNames = new string[] { "A", "B", "C", "D" };
            var expectedVariableTypes = new string[] { "int", "Foo" };

            CollectionAssert.AreEquivalent(expectedVariableNames, declaredVariableNames);
            foreach(var declaration in globalScope.DeclaredVariables) {
                CollectionAssert.Contains(expectedVariableTypes, declaration.VariableType.Name);
            }
        }
    }
}