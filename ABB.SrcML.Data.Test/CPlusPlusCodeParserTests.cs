/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - implementation and documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class CPlusPlusCodeParserTests {
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new CPlusPlusCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A { };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default""> </private>}</block>;</class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassDeclaration() {
            //class A;
            string xml = @"<class_decl>class <name>A</name>;</class_decl>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestClassWithDeclaredVariable() {
            //class A {
            //    int a;
            //};
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
</private>}</block>;</class>";

            var globalScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(xml, "A.h"));
            Assert.IsTrue(globalScope.IsGlobal);

            var classA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(classA);
            Assert.AreEqual("A", classA.Name);
            Assert.AreEqual(1, classA.ChildStatements.Count);

            var fieldStmt = classA.ChildStatements.First();
            Assert.IsNotNull(fieldStmt);
            var field = fieldStmt.Content as VariableDeclaration;
            Assert.IsNotNull(field);
            Assert.AreEqual("a", field.Name);
            Assert.AreEqual("int", field.VariableType.Name);
        }

        

        [Test]
        public void TestFreeStandingBlock() {
            //{
            //	int foo = 42;
            //	MethodCall(foo);
            //}
            string xml = @"<block>{
	<decl_stmt><decl><type><name>int</name></type> <name>foo</name> =<init> <expr><lit:literal type=""number"">42</lit:literal></expr></init></decl>;</decl_stmt>
	<expr_stmt><expr><call><name>MethodCall</name><argument_list>(<argument><expr><name>foo</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
}</block>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var firstChild = globalScope.ChildStatements.First();

            Assert.IsInstanceOf<BlockStatement>(firstChild);
            
            var actual = firstChild as BlockStatement;
            Assert.IsNull(actual.Content);
            Assert.AreEqual(2, actual.ChildStatements.Count);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestExternStatement_Single() {
            //extern "C" int MyGlobalVar;
            string xml = @"<extern>extern <lit:literal type=""string"">""C""</lit:literal> <decl_stmt><decl><type><name>int</name></type> <name>MyGlobalVar</name></decl>;</decl_stmt></extern>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as ExternStatement;

            Assert.IsNotNull(actual);
            Assert.AreEqual("\"C\"", actual.LinkageType);
            Assert.AreEqual(1, actual.ChildStatements.Count);
            
        }

        [Test]
        public void TestExternStatement_Block() {
            //extern "C" {
            //  int globalVar1;
            //  int globalVar2;
            //}
            string xml = @"<extern>extern <lit:literal type=""string"">""C""</lit:literal> <block>{
  <decl_stmt><decl><type><name>int</name></type> <name>globalVar1</name></decl>;</decl_stmt>
  <decl_stmt><decl><type><name>int</name></type> <name>globalVar2</name></decl>;</decl_stmt>
}</block></extern>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as ExternStatement;

            Assert.IsNotNull(actual);
            Assert.AreEqual("\"C\"", actual.LinkageType);
            Assert.AreEqual(2, actual.ChildStatements.Count);
            
        }

        [Test]
        public void TestConstructor_CallToSelf() {
            // test.h 
            //class MyClass {
            //public:
            //   MyClass() : MyClass(0) { } 
            //   MyClass(int foo) { } 
            //};
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">MyClass</name> <block pos:line=""1"" pos:column=""15"">{<private type=""default"" pos:line=""1"" pos:column=""16"">
</private><public pos:line=""2"" pos:column=""1"">public:
   <constructor><name pos:line=""3"" pos:column=""4"">MyClass</name><parameter_list pos:line=""3"" pos:column=""11"">()</parameter_list> <member_list pos:line=""3"" pos:column=""14"">: <call><name pos:line=""3"" pos:column=""16"">MyClass</name><argument_list pos:line=""3"" pos:column=""23"">(<argument><expr><lit:literal type=""number"" pos:line=""3"" pos:column=""24"">0</lit:literal></expr></argument>)</argument_list></call> </member_list><block pos:line=""3"" pos:column=""27"">{ }</block></constructor> 
   <constructor><name pos:line=""4"" pos:column=""4"">MyClass</name><parameter_list pos:line=""4"" pos:column=""11"">(<param><decl><type><name pos:line=""4"" pos:column=""12"">int</name></type> <name pos:line=""4"" pos:column=""16"">foo</name></decl></param>)</parameter_list> <block pos:line=""4"" pos:column=""21"">{ }</block></constructor> 
</public>}</block>;</class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
            var globalScope = codeParser.ParseFileUnit(unit);

            var constructors = globalScope.GetDescendants<MethodDefinition>().ToList();
            var defaultConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 0);
            var calledConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 1);

            Assert.IsNotNull(defaultConstructor);
            Assert.IsNotNull(calledConstructor);
            Assert.AreEqual(1, defaultConstructor.ConstructorInitializers.Count);

            var constructorCall = defaultConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(constructorCall);
            Assert.That(constructorCall.IsConstructor);
            Assert.That(constructorCall.IsConstructorInitializer);
            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructor_CallToSuperClass() {
            // test.h 
            // class SuperClass {
            // public:
            // SuperClass(int foo) { } }; 
            // class SubClass : public SuperClass {
            // public:
            // SubClass(int foo) : SuperClass(foo) { } };
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">SuperClass</name> <block pos:line=""1"" pos:column=""18"">{<private type=""default"" pos:line=""1"" pos:column=""19"">
</private><public pos:line=""2"" pos:column=""1"">public:
<constructor><name pos:line=""3"" pos:column=""1"">SuperClass</name><parameter_list pos:line=""3"" pos:column=""11"">(<param><decl><type><name pos:line=""3"" pos:column=""12"">int</name></type> <name pos:line=""3"" pos:column=""16"">foo</name></decl></param>)</parameter_list> <block pos:line=""3"" pos:column=""21"">{ }</block></constructor> </public>}</block>;</class> 
<class pos:line=""4"" pos:column=""1"">class <name pos:line=""4"" pos:column=""7"">SubClass</name> <super pos:line=""4"" pos:column=""16"">: <specifier pos:line=""4"" pos:column=""18"">public</specifier> <name pos:line=""4"" pos:column=""25"">SuperClass</name></super> <block pos:line=""4"" pos:column=""36"">{<private type=""default"" pos:line=""4"" pos:column=""37"">
</private><public pos:line=""5"" pos:column=""1"">public:
<constructor><name pos:line=""6"" pos:column=""1"">SubClass</name><parameter_list pos:line=""6"" pos:column=""9"">(<param><decl><type><name pos:line=""6"" pos:column=""10"">int</name></type> <name pos:line=""6"" pos:column=""14"">foo</name></decl></param>)</parameter_list> <member_list pos:line=""6"" pos:column=""19"">: <call><name pos:line=""6"" pos:column=""21"">SuperClass</name><argument_list pos:line=""6"" pos:column=""31"">(<argument><expr><name pos:line=""6"" pos:column=""32"">foo</name></expr></argument>)</argument_list></call> </member_list><block pos:line=""6"" pos:column=""37"">{ }</block></constructor> </public>}</block>;</class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
            var globalScope = codeParser.ParseFileUnit(unit);

            var calledConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "SuperClass" && m.IsConstructor);
            var subClassConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "SubClass" && m.IsConstructor);
            Assert.IsNotNull(subClassConstructor);
            Assert.IsNotNull(calledConstructor);
            Assert.AreEqual(1, subClassConstructor.ConstructorInitializers.Count);

            var constructorCall = subClassConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(constructorCall);
            Assert.That(constructorCall.IsConstructor);
            Assert.That(constructorCall.IsConstructorInitializer);
            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructor_InitializeBuiltinTypeField() {
            //test.h
            //class Quux
            //{
            //    int _my_int;
            //public:
            //    Quux() : _my_int(5) {  }
            //};
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Quux</name>
<block pos:line=""2"" pos:column=""1"">{<private type=""default"" pos:line=""2"" pos:column=""2"">
    <decl_stmt><decl><type><name pos:line=""3"" pos:column=""5"">int</name></type> <name pos:line=""3"" pos:column=""9"">_my_int</name></decl>;</decl_stmt>
</private><public pos:line=""4"" pos:column=""1"">public:
    <constructor><name pos:line=""5"" pos:column=""5"">Quux</name><parameter_list pos:line=""5"" pos:column=""9"">()</parameter_list> <member_list pos:line=""5"" pos:column=""12"">: <call><name pos:line=""5"" pos:column=""14"">_my_int</name><argument_list pos:line=""5"" pos:column=""21"">(<argument><expr><lit:literal type=""number"" pos:line=""5"" pos:column=""22"">5</lit:literal></expr></argument>)</argument_list></call> </member_list><block pos:line=""5"" pos:column=""25"">{  }</block></constructor>
</public>}</block>;</class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var quux = globalScope.GetNamedChildren<TypeDefinition>("Quux").First();
            var field = quux.GetNamedChildren<VariableDeclaration>("_my_int").First();
            var fieldType = field.VariableType.ResolveType().FirstOrDefault();
            Assert.IsNotNull(fieldType);

            var constructor = quux.GetNamedChildren<MethodDefinition>("Quux").First();
            Assert.AreEqual(1, constructor.ConstructorInitializers.Count);
            var fieldCall = constructor.ConstructorInitializers[0];
            Assert.IsNotNull(fieldCall);
            Assert.That(fieldCall.IsConstructor);
            Assert.That(fieldCall.IsConstructorInitializer);
            Assert.IsEmpty(fieldCall.FindMatches());
        }

        [Test]
        public void TestConstructor_InitializeField() {
            //test.h
            //class Foo
            //{
            //public:
            //    Foo(int a) { }
            //};
            //class Bar
            //{
            //    Foo baz;
            //public:
            //    Bar() : baz(42) { }
            //};
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Foo</name>
<block pos:line=""2"" pos:column=""1"">{<private type=""default"" pos:line=""2"" pos:column=""2"">
</private><public pos:line=""3"" pos:column=""1"">public:
    <constructor><name pos:line=""4"" pos:column=""5"">Foo</name><parameter_list pos:line=""4"" pos:column=""8"">(<param><decl><type><name pos:line=""4"" pos:column=""9"">int</name></type> <name pos:line=""4"" pos:column=""13"">a</name></decl></param>)</parameter_list> <block pos:line=""4"" pos:column=""16"">{ }</block></constructor>
</public>}</block>;</class>
<class pos:line=""6"" pos:column=""1"">class <name pos:line=""6"" pos:column=""7"">Bar</name>
<block pos:line=""7"" pos:column=""1"">{<private type=""default"" pos:line=""7"" pos:column=""2"">
    <decl_stmt><decl><type><name pos:line=""8"" pos:column=""5"">Foo</name></type> <name pos:line=""8"" pos:column=""9"">baz</name></decl>;</decl_stmt>
</private><public pos:line=""9"" pos:column=""1"">public:
    <constructor><name pos:line=""10"" pos:column=""5"">Bar</name><parameter_list pos:line=""10"" pos:column=""8"">()</parameter_list> <member_list pos:line=""10"" pos:column=""11"">: <call><name pos:line=""10"" pos:column=""13"">baz</name><argument_list pos:line=""10"" pos:column=""16"">(<argument><expr><lit:literal type=""number"" pos:line=""10"" pos:column=""17"">42</lit:literal></expr></argument>)</argument_list></call> </member_list><block pos:line=""10"" pos:column=""21"">{ }</block></constructor>
</public>}</block>;</class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var fooConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Foo" && m.IsConstructor);
            var barConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar" && m.IsConstructor);
            Assert.AreEqual(1, barConstructor.ConstructorInitializers.Count);
            var fieldCall = barConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(fieldCall);
            Assert.That(fieldCall.IsConstructor);
            Assert.That(fieldCall.IsConstructorInitializer);
            Assert.AreSame(fooConstructor, fieldCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            // using A::Foo;
            string xml = @"<using>using <name><name>A</name><op:operator>::</op:operator><name>Foo</name></name>;</using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("Foo", actual.AliasName);
            Assert.AreEqual("A::Foo", actual.Target.ToString());

        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            // using namespace x::y::z;
            string xml = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x :: y :: z", actual.ImportedNamespace.ToString());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestCreateAliasesForFiles_TypeAlias() {
            // using x = foo::bar::baz;
            string xml = @"<using>using <name>x</name> = <decl_stmt><decl><type><name><name>foo</name><op:operator>::</op:operator><name>bar</name><op:operator>::</op:operator><name>baz</name></name></type></decl>;</decl_stmt></using>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x", actual.AliasName, "TODO fix once srcml is updated");
            Assert.AreEqual("foo::bar::baz", actual.Target.ToString());
        }

        [Test]
        public void TestGetImports() {
            //A.cpp
            //namespace x {
            //  namespace y {
            //    namespace z {}
            //  }
            //}
            string xmlA = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">x</name> <block pos:line=""1"" pos:column=""13"">{
  <namespace pos:line=""2"" pos:column=""3"">namespace <name pos:line=""2"" pos:column=""13"">y</name> <block pos:line=""2"" pos:column=""15"">{
    <namespace pos:line=""3"" pos:column=""5"">namespace <name pos:line=""3"" pos:column=""15"">z</name> <block pos:line=""3"" pos:column=""17"">{}</block></namespace>
  }</block></namespace>
}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using namespace x::y::z;
            //foo = 17;
            string xmlB = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">x</name><op:operator pos:line=""1"" pos:column=""18"">::</op:operator><name pos:line=""1"" pos:column=""20"">y</name><op:operator pos:line=""1"" pos:column=""21"">::</op:operator><name pos:line=""1"" pos:column=""23"">z</name></name>;</using>
<expr_stmt><expr><name pos:line=""2"" pos:column=""1"">foo</name> <op:operator pos:line=""2"" pos:column=""5"">=</op:operator> <lit:literal type=""number"" pos:line=""2"" pos:column=""7"">17</lit:literal></expr>;</expr_stmt>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);
            var foo = globalScope.ChildStatements[2].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());

            var zDef = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(ns => ns.Name == "z");
            Assert.IsNotNull(zDef);
            var zUse = imports[0].ImportedNamespace.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "z");
            Assert.AreSame(zDef, zUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestGetImports_NestedImportNamespace() {
            //using namespace x::y::z;
            //if(bar) {
            //  using namespace std;
            //  foo = 17;
            //}
            string xml = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">x</name><op:operator pos:line=""1"" pos:column=""18"">::</op:operator><name pos:line=""1"" pos:column=""20"">y</name><op:operator pos:line=""1"" pos:column=""21"">::</op:operator><name pos:line=""1"" pos:column=""23"">z</name></name>;</using>
<if pos:line=""2"" pos:column=""1"">if<condition pos:line=""2"" pos:column=""3"">(<expr><name pos:line=""2"" pos:column=""4"">bar</name></expr>)</condition><then pos:line=""2"" pos:column=""8""> <block pos:line=""2"" pos:column=""9"">{
  <using pos:line=""3"" pos:column=""3"">using namespace <name pos:line=""3"" pos:column=""19"">std</name>;</using>
  <expr_stmt><expr><name pos:line=""4"" pos:column=""3"">foo</name> <op:operator pos:line=""4"" pos:column=""7"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""9"">17</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");
            
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var foo = globalScope.ChildStatements[1].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(2, imports.Count);
            Assert.AreEqual("std", imports[0].ImportedNamespace.ToString());
            Assert.AreEqual("x :: y :: z", imports[1].ImportedNamespace.ToString());
        }

        [Test]
        public void TestGetAliases_NestedImportClass() {
            //A.cpp
            //namespace B {
            //  class Bar {}
            //}
            string xmlA = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">B</name> <block pos:line=""1"" pos:column=""13"">{
  <class pos:line=""2"" pos:column=""3"">class <name pos:line=""2"" pos:column=""9"">Bar</name> <block pos:line=""2"" pos:column=""13"">{<private type=""default""/>}</block>
<decl/></class>}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using namespace x::y::z;
            //if(bar) {
            //  using B::Bar;
            //  foo = 17;
            //}
            string xmlB = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">x</name><op:operator pos:line=""1"" pos:column=""18"">::</op:operator><name pos:line=""1"" pos:column=""20"">y</name><op:operator pos:line=""1"" pos:column=""21"">::</op:operator><name pos:line=""1"" pos:column=""23"">z</name></name>;</using>
<if pos:line=""2"" pos:column=""1"">if<condition pos:line=""2"" pos:column=""3"">(<expr><name pos:line=""2"" pos:column=""4"">bar</name></expr>)</condition><then pos:line=""2"" pos:column=""8""> <block pos:line=""2"" pos:column=""9"">{
  <using pos:line=""3"" pos:column=""3"">using <name><name pos:line=""3"" pos:column=""9"">B</name><op:operator pos:line=""3"" pos:column=""10"">::</op:operator><name pos:line=""3"" pos:column=""12"">Bar</name></name>;</using>
  <expr_stmt><expr><name pos:line=""4"" pos:column=""3"">foo</name> <op:operator pos:line=""4"" pos:column=""7"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""9"">17</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cpp");
            
            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            var foo = globalScope.ChildStatements[2].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetAliases().ToList();
            Assert.AreEqual(1, aliases.Count);
            Assert.AreEqual("B::Bar", aliases[0].Target.ToString());
            Assert.AreEqual("Bar", aliases[0].AliasName);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());

            var barDef = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(ns => ns.Name == "Bar");
            Assert.IsNotNull(barDef);
            var barUse = aliases[0].Target.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "Bar");
            Assert.AreSame(barDef, barUse.FindMatches().FirstOrDefault());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestGetAliases_NestedTypeAlias() {
            //using namespace x::y::z;
            //if(bar) {
            //  using x = foo::bar::baz;
            //  foo = 17;
            //}
            string xml = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">x</name><op:operator pos:line=""1"" pos:column=""18"">::</op:operator><name pos:line=""1"" pos:column=""20"">y</name><op:operator pos:line=""1"" pos:column=""21"">::</op:operator><name pos:line=""1"" pos:column=""23"">z</name></name>;</using>
<if pos:line=""2"" pos:column=""1"">if<condition pos:line=""2"" pos:column=""3"">(<expr><name pos:line=""2"" pos:column=""4"">bar</name></expr>)</condition><then pos:line=""2"" pos:column=""8""> <block pos:line=""2"" pos:column=""9"">{
  <using pos:line=""3"" pos:column=""3"">using <name pos:line=""3"" pos:column=""9"">x</name> = <decl_stmt><decl><type><name><name pos:line=""3"" pos:column=""13"">foo</name><op:operator pos:line=""3"" pos:column=""16"">::</op:operator><name pos:line=""3"" pos:column=""18"">bar</name><op:operator pos:line=""3"" pos:column=""21"">::</op:operator><name pos:line=""3"" pos:column=""23"">baz</name></name></type></decl>;</decl_stmt></using>
  <expr_stmt><expr><name pos:line=""4"" pos:column=""3"">foo</name> <op:operator pos:line=""4"" pos:column=""7"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""9"">17</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");
            
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var foo = globalScope.ChildStatements[1].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetAliases().ToList();
            Assert.AreEqual(1, aliases.Count);
            Assert.AreEqual("foo :: bar :: baz", aliases[0].Target.ToString());
            Assert.AreEqual("x", aliases[0].AliasName);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());
        }

        [Test]
        public void TestImport_NameResolution() {
            //A.cpp
            //using namespace Foo::Bar;
            //
            //namespace A {
            //  class Robot {
            //  public: 
            //    Baz GetThingy() { 
            //      Baz* b = new Baz();
            //      return *b;
            //    }
            //  }
            //}
            string xmlA = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">Foo</name><op:operator pos:line=""1"" pos:column=""20"">::</op:operator><name pos:line=""1"" pos:column=""22"">Bar</name></name>;</using>

<namespace pos:line=""3"" pos:column=""1"">namespace <name pos:line=""3"" pos:column=""11"">A</name> <block pos:line=""3"" pos:column=""13"">{
  <class pos:line=""4"" pos:column=""3"">class <name pos:line=""4"" pos:column=""9"">Robot</name> <block pos:line=""4"" pos:column=""15"">{<private type=""default"" pos:line=""4"" pos:column=""16"">
  </private><public pos:line=""5"" pos:column=""3"">public: 
    <function><type><name pos:line=""6"" pos:column=""5"">Baz</name></type> <name pos:line=""6"" pos:column=""9"">GetThingy</name><parameter_list pos:line=""6"" pos:column=""18"">()</parameter_list> <block pos:line=""6"" pos:column=""21"">{ 
      <decl_stmt><decl><type><name pos:line=""7"" pos:column=""7"">Baz</name><type:modifier pos:line=""7"" pos:column=""10"">*</type:modifier></type> <name pos:line=""7"" pos:column=""12"">b</name> <init pos:line=""7"" pos:column=""14"">= <expr><op:operator pos:line=""7"" pos:column=""16"">new</op:operator> <call><name pos:line=""7"" pos:column=""20"">Baz</name><argument_list pos:line=""7"" pos:column=""23"">()</argument_list></call></expr></init></decl>;</decl_stmt>
      <return pos:line=""8"" pos:column=""7"">return <expr><op:operator pos:line=""8"" pos:column=""14"">*</op:operator><name pos:line=""8"" pos:column=""15"">b</name></expr>;</return>
    }</block></function>
  </public>}</block>
<decl/></class>}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //namespace Foo {
            //  namespace Bar {
            //    class Baz {
            //    public:
            //      Baz() { }
            //  }
            //}
            string xmlB = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">Foo</name> <block pos:line=""1"" pos:column=""15"">{
  <namespace pos:line=""2"" pos:column=""3"">namespace <name pos:line=""2"" pos:column=""13"">Bar</name> <block pos:line=""2"" pos:column=""17"">{
    <class pos:line=""3"" pos:column=""5"">class <name pos:line=""3"" pos:column=""11"">Baz</name> <block pos:line=""3"" pos:column=""15"">{<private type=""default"" pos:line=""3"" pos:column=""16"">
    </private><public pos:line=""4"" pos:column=""5"">public:
      <constructor><name pos:line=""5"" pos:column=""7"">Baz</name><parameter_list pos:line=""5"" pos:column=""10"">()</parameter_list> <block pos:line=""5"" pos:column=""13"">{ }</block></constructor>
  </public>}</block>
<decl/></class>}</block></namespace></block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cpp");
            
            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var baz = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "Baz");
            Assert.IsNotNull(baz);

            var thingy = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "GetThingy");
            Assert.IsNotNull(thingy);
            var thingyTypes = thingy.ReturnType.FindMatches().ToList();
            Assert.AreEqual(1, thingyTypes.Count);
            Assert.AreSame(baz, thingyTypes[0]);

            var bazDef = baz.GetNamedChildren<MethodDefinition>("Baz").First();
            var bazCall = thingy.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Baz");
            Assert.IsNotNull(bazCall);
            Assert.AreSame(bazDef, bazCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestAlias_NameResolution_ImportType() {
            //A.cpp
            //namespace Foo {
            //  namespace Bar {
            //    class Baz {
            //    public:
            //      static void DoTheThing() { }
            //  }
            //}
            string xmlA = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">Foo</name> <block pos:line=""1"" pos:column=""15"">{
  <namespace pos:line=""2"" pos:column=""3"">namespace <name pos:line=""2"" pos:column=""13"">Bar</name> <block pos:line=""2"" pos:column=""17"">{
    <class pos:line=""3"" pos:column=""5"">class <name pos:line=""3"" pos:column=""11"">Baz</name> <block pos:line=""3"" pos:column=""15"">{<private type=""default"" pos:line=""3"" pos:column=""16"">
    </private><public pos:line=""4"" pos:column=""5"">public:
      <function><type><specifier pos:line=""5"" pos:column=""7"">static</specifier> <name pos:line=""5"" pos:column=""14"">void</name></type> <name pos:line=""5"" pos:column=""19"">DoTheThing</name><parameter_list pos:line=""5"" pos:column=""29"">()</parameter_list> <block pos:line=""5"" pos:column=""32"">{ }</block></function>
  </public>}</block>
<decl/></class>}</block></namespace></block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using Foo::Bar::Baz;
            //namespace A {
            //  class B {
            //  public:
            //    B() {
            //      Baz::DoTheThing();
            //    }
            //  }
            //}
            string xmlB = @"<using pos:line=""1"" pos:column=""1"">using <name><name pos:line=""1"" pos:column=""7"">Foo</name><op:operator pos:line=""1"" pos:column=""10"">::</op:operator><name pos:line=""1"" pos:column=""12"">Bar</name><op:operator pos:line=""1"" pos:column=""15"">::</op:operator><name pos:line=""1"" pos:column=""17"">Baz</name></name>;</using>
<namespace pos:line=""2"" pos:column=""1"">namespace <name pos:line=""2"" pos:column=""11"">A</name> <block pos:line=""2"" pos:column=""13"">{
  <class pos:line=""3"" pos:column=""3"">class <name pos:line=""3"" pos:column=""9"">B</name> <block pos:line=""3"" pos:column=""11"">{<private type=""default"" pos:line=""3"" pos:column=""12"">
  </private><public pos:line=""4"" pos:column=""3"">public:
    <constructor><name pos:line=""5"" pos:column=""5"">B</name><parameter_list pos:line=""5"" pos:column=""6"">()</parameter_list> <block pos:line=""5"" pos:column=""9"">{
      <expr_stmt><expr><call><name><name pos:line=""6"" pos:column=""7"">Baz</name><op:operator pos:line=""6"" pos:column=""10"">::</op:operator><name pos:line=""6"" pos:column=""12"">DoTheThing</name></name><argument_list pos:line=""6"" pos:column=""22"">()</argument_list></call></expr>;</expr_stmt>
    }</block></constructor>
  </public>}</block>
<decl/></class>}</block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cpp");
            
            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var thingDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "DoTheThing");
            Assert.IsNotNull(thingDef);
            Assert.AreEqual("Baz", ((TypeDefinition)thingDef.ParentStatement).Name);

            var bDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "B");
            Assert.IsNotNull(bDef);
            Assert.AreEqual(1, bDef.ChildStatements.Count);
            var thingCall = bDef.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(thingCall);
            Assert.AreSame(thingDef, thingCall.FindMatches().First());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestAlias_NameResolution_TypeAlias() {
            //A.cpp
            //namespace Foo {
            //  namespace Bar {
            //    class Baz {
            //    public:
            //      static void DoTheThing() { }
            //  }
            //}
            string xmlA = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">Foo</name> <block pos:line=""1"" pos:column=""15"">{
  <namespace pos:line=""2"" pos:column=""3"">namespace <name pos:line=""2"" pos:column=""13"">Bar</name> <block pos:line=""2"" pos:column=""17"">{
    <class pos:line=""3"" pos:column=""5"">class <name pos:line=""3"" pos:column=""11"">Baz</name> <block pos:line=""3"" pos:column=""15"">{<private type=""default"" pos:line=""3"" pos:column=""16"">
    </private><public pos:line=""4"" pos:column=""5"">public:
      <function><type><specifier pos:line=""5"" pos:column=""7"">static</specifier> <name pos:line=""5"" pos:column=""14"">void</name></type> <name pos:line=""5"" pos:column=""19"">DoTheThing</name><parameter_list pos:line=""5"" pos:column=""29"">()</parameter_list> <block pos:line=""5"" pos:column=""32"">{ }</block></function>
  </public>}</block>
<decl/></class>}</block></namespace></block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using X = Foo::Bar::Baz;
            //namespace A {
            //  class B {
            //  public:
            //    B() {
            //      X::DoTheThing();
            //    }
            //  }
            //}
            string xmlB = @"<using pos:line=""1"" pos:column=""1"">using <name pos:line=""1"" pos:column=""7"">X</name> = <decl_stmt><decl><type><name><name pos:line=""1"" pos:column=""11"">Foo</name><op:operator pos:line=""1"" pos:column=""14"">::</op:operator><name pos:line=""1"" pos:column=""16"">Bar</name><op:operator pos:line=""1"" pos:column=""19"">::</op:operator><name pos:line=""1"" pos:column=""21"">Baz</name></name></type></decl>;</decl_stmt></using>
<namespace pos:line=""2"" pos:column=""1"">namespace <name pos:line=""2"" pos:column=""11"">A</name> <block pos:line=""2"" pos:column=""13"">{
  <class pos:line=""3"" pos:column=""3"">class <name pos:line=""3"" pos:column=""9"">B</name> <block pos:line=""3"" pos:column=""11"">{<private type=""default"" pos:line=""3"" pos:column=""12"">
  </private><public pos:line=""4"" pos:column=""3"">public:
    <constructor><name pos:line=""5"" pos:column=""5"">B</name><parameter_list pos:line=""5"" pos:column=""6"">()</parameter_list> <block pos:line=""5"" pos:column=""9"">{
      <expr_stmt><expr><call><name><name pos:line=""6"" pos:column=""7"">X</name><op:operator pos:line=""6"" pos:column=""8"">::</op:operator><name pos:line=""6"" pos:column=""10"">DoTheThing</name></name><argument_list pos:line=""6"" pos:column=""20"">()</argument_list></call></expr>;</expr_stmt>
    }</block></constructor>
  </public>}</block>
<decl/></class>}</block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cpp");
            
            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var thingDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "DoTheThing");
            Assert.IsNotNull(thingDef);
            Assert.AreEqual("Baz", ((TypeDefinition)thingDef.ParentStatement).Name);

            var bDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "B");
            Assert.IsNotNull(bDef);
            Assert.AreEqual(1, bDef.ChildStatements.Count);
            var thingCall = bDef.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(thingCall);
            Assert.AreSame(thingDef, thingCall.FindMatches().First());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassInNamespace() {
            // namespace A { class B { }; }
            string xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{<private type=""default"">
    </private>}</block>;</class>
}</block></namespace>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "B.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", namespaceA.Name);
            Assert.IsFalse(namespaceA.IsGlobal);

            Assert.AreEqual("B", typeB.Name);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithMethodDeclaration() {
            // class A {
            // public:
            // int foo(int a); };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
</private><public>public:
    <function_decl><type><name>int</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>)</parameter_list>;</function_decl>
</public>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual(1, typeA.ChildStatements.Count);
            var methodFoo = typeA.ChildStatements.First() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("foo", methodFoo.Name);

            Assert.AreEqual(1, methodFoo.Parameters.Count);
        }

        [Test]
        public void TestCreateTypeDefinition_StaticMethod() {
            //class Example {
            //public:
            //    static int Example::Foo(int bar) { return bar+1; }
            //};
            string xml = @"<class>class <name>Example</name> <block>{<private type=""default"">
</private><public>public:
    <function><type><name>static</name> <name>int</name></type> <name><name>Example</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{ <return>return <expr><name>bar</name><op:operator>+</op:operator><lit:literal type=""number"">1</lit:literal></expr>;</return> }</block></function>
</public>}</block>;</class>";
            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "static_method.h");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var example = globalScope.ChildStatements.OfType<TypeDefinition>().FirstOrDefault();
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildStatements.Count());
            var foo = example.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
        }


        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            // int main() { class A { }; }
            string xml = @"<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <class>class <name>A</name> <block>{<private type=""default"">
    </private>}</block>;</class>
}</block></function>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "main.cpp");
            var mainMethod = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as MethodDefinition;

            Assert.AreEqual("main", mainMethod.Name);

            var typeA = mainMethod.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main.A", typeA.GetFullName());
            Assert.AreEqual(string.Empty, typeA.GetAncestors<NamespaceDefinition>().First().GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            // class A { class B { }; };
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <class>class <name>B</name> <block>{<private type=""default"">
    </private>}</block>;</class>
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var typeB = typeA.ChildStatements.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentStatement);
            Assert.AreEqual("A", typeA.GetFullName());

            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A : B,C,D { };
            string xml = @"<class>class <name>A</name> <super>: <name>B</name>,<name>C</name>,<name>D</name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypeNames.Count);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);

            var parentNames = from parent in actual.ParentTypeNames
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a
                );
            foreach(var parentMatchesExpected in tests) {
                Assert.That(parentMatchesExpected);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            // class D : A::B::C { }
            string xml = @"<class>class <name>D</name> <super>: <name><name>A</name><op:operator>::</op:operator><name>B</name><op:operator>::</op:operator><name>C</name></name></super> <block>{<private type=""default"">
</private>}</block>;</class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "D.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.ParentTypeNames.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parent = actual.ParentTypeNames.First();

            Assert.AreEqual("C", parent.Name);

            var prefixNames = parent.Prefix.Names.ToList();
            Assert.AreEqual(2, prefixNames.Count);
            Assert.AreEqual("A", prefixNames[0].Name);
            Assert.AreEqual("B", prefixNames[1].Name);

        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            // namespace A { class B { class C { }; }; }
            string xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{<private type=""default"">
        <class>class <name>C</name> <block>{<private type=""default"">
        </private>}</block>;</class>
    </private>}</block>;</class>
}</block></namespace>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeDefinitions = globalScope.GetDescendants<TypeDefinition>().ToList();
            Assert.AreEqual(2, typeDefinitions.Count);

            var outer = typeDefinitions.First();
            var inner = typeDefinitions.Last();

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("A.B", outer.GetFullName());

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("A.B.C", inner.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct() {
            // struct A { };
            string xml = @"<struct>struct <name>A</name> <block>{<public type=""default"">
</public>}</block>;</struct>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Struct, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_Union() {
            // union A { int a; char b;
            //};
            string xml = @"<union>union <name>A</name> <block>{<public type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
    <decl_stmt><decl><type><name>char</name></type> <name>b</name></decl>;</decl_stmt>
</public>}</block>;</union>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.AreEqual(TypeKind.Union, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestGenericVariableDeclaration() {
            //vector<int> a;
            string xml = @"<decl_stmt><decl><type><name><name>vector</name><argument_list>&lt;<argument><name>int</name></argument>&gt;</argument_list></name></type> <name>a</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("a", testDeclaration.Name);
            Assert.AreEqual("vector", testDeclaration.VariableType.Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(1, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.First().Name);
        }

        [Test]
        public void TestGenericVariableDeclarationWithPrefix() {
            //std::vector<int> a;
            string xml = @"<decl_stmt><decl><type><name><name>std</name><op:operator>::</op:operator><name><name>vector</name><argument_list>&lt;<argument><name>int</name></argument>&gt;</argument_list></name></name></type> <name>a</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("a", testDeclaration.Name);
            Assert.AreEqual("vector", testDeclaration.VariableType.Name);
            Assert.AreEqual(1, testDeclaration.VariableType.Prefix.Names.Count());
            Assert.AreEqual("std", testDeclaration.VariableType.Prefix.Names.First().Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(1, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.First().Name);
        }

        [Test]
        public void TestMethodCallCreation_LengthyCallingExpression() {
            //a->b.Foo();
            string xml = @"<expr_stmt><expr><call><name><name>a</name><op:operator>-&gt;</op:operator><name>b</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(5, exp.Components.Count);
            var a = exp.Components[0] as NameUse;
            Assert.IsNotNull(a);
            Assert.AreEqual("a", a.Name);
            var arrow = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(arrow);
            Assert.AreEqual("->", arrow.Text);
            var b = exp.Components[2] as NameUse;
            Assert.IsNotNull(b);
            Assert.AreEqual("b", b.Name);
            var dot = exp.Components[3] as OperatorUse;
            Assert.IsNotNull(dot);
            Assert.AreEqual(".", dot.Text);
            var foo = exp.Components[4] as MethodCall;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.Arguments.Count);
            Assert.AreEqual(0, foo.TypeArguments.Count);
        }

        [Test]
        public void TestMergeWithUsing() {
            // namespace A { class B { void Foo(); }; }
            string headerXml = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">A</name> <block pos:line=""1"" pos:column=""13"">{ <class pos:line=""1"" pos:column=""15"">class <name pos:line=""1"" pos:column=""21"">B</name> <block pos:line=""1"" pos:column=""23"">{<private type=""default"" pos:line=""1"" pos:column=""24""> <function_decl><type><name pos:line=""1"" pos:column=""25"">void</name></type> <name pos:line=""1"" pos:column=""30"">Foo</name><parameter_list pos:line=""1"" pos:column=""33"">()</parameter_list>;</function_decl> </private>}</block>;</class> }</block></namespace>";

            //using namespace A;
            //
            //void B::Foo() { }
            string implementationXml = @"<using pos:line=""1"" pos:column=""1"">using namespace <name pos:line=""1"" pos:column=""17"">A</name>;</using>

<function><type><name pos:line=""3"" pos:column=""1"">void</name></type> <name><name pos:line=""3"" pos:column=""6"">B</name><op:operator pos:line=""3"" pos:column=""7"">::</op:operator><name pos:line=""3"" pos:column=""9"">Foo</name></name><parameter_list pos:line=""3"" pos:column=""12"">()</parameter_list> <block pos:line=""3"" pos:column=""15"">{ }</block></function>";
            
            var headerScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(headerXml, "A.h"));
            var implementationScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(implementationXml, "A.cpp"));

            var globalScope = headerScope.Merge(implementationScope);
            Assert.AreEqual(1, globalScope.ChildStatements.OfType<NamedScope>().Count());

            var namespaceA = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(n => n.Name == "A");
            Assert.IsNotNull(namespaceA);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count);

            var typeB = namespaceA.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            Assert.AreEqual(1, typeB.ChildStatements.Count);

            var methodFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(methodFoo);
            Assert.AreEqual(0, methodFoo.ChildStatements.Count);
            Assert.AreEqual(2, methodFoo.Locations.Count);

            headerScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(headerXml, "A.h"));
            implementationScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(implementationXml, "A.cpp"));

            var globalScope_implementationFirst = implementationScope.Merge(headerScope);

            namespaceA = globalScope_implementationFirst.GetDescendants<NamespaceDefinition>().FirstOrDefault(n => n.Name == "A");
            Assert.IsNotNull(namespaceA);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count);

            typeB = namespaceA.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            Assert.AreEqual(1, typeB.ChildStatements.Count);

            methodFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(methodFoo);
            Assert.AreEqual(0, methodFoo.ChildStatements.Count);
            Assert.AreEqual(2, methodFoo.Locations.Count);
        }

        [Test]
        public void TestMethodCallCreation_WithConflictingMethodNames() {
            //# A.h
            //class A {
            //    B b;
            //public:
            //    bool Contains() { b.Contains(); }
            //};
            string a_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>B</name></type> <name>b</name></decl>;</decl_stmt>
</private><public>public:
    <function><type><name>bool</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>b</name><op:operator>.</op:operator><call><name>Contains</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
</public>}</block>;</class>";

            //# B.h
            //class B {
            //public:
            //    bool Contains() { return true; }
            //};
            string b_xml = @"<class>class <name>B</name> <block>{<private type=""default"">
</private><public>public:
    <function><type><name>bool</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
</public>}</block>;</class>";

            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.h");
            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(b_xml, "B.h");

            var scopeForA = codeParser.ParseFileUnit(fileUnitA);
            var scopeForB = codeParser.ParseFileUnit(fileUnitB);
            var globalScope = scopeForA.Merge(scopeForB);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var classA = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "A");
            Assert.IsNotNull(classA);
            var classB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(classB);

            var aDotContains = classA.GetNamedChildren<MethodDefinition>("Contains").FirstOrDefault();
            Assert.IsNotNull(aDotContains);
            var bDotContains = classB.GetNamedChildren<MethodDefinition>("Contains").FirstOrDefault();
            Assert.IsNotNull(bDotContains);

            Assert.AreEqual(1, aDotContains.ChildStatements.Count);
            var methodCall = aDotContains.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(methodCall);

            Assert.AreSame(bDotContains, methodCall.FindMatches().FirstOrDefault());
            Assert.AreNotSame(aDotContains, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_WithThisKeyword() {
            //class A {
            //    void Bar() { }
            //    class B {
            //        int a;
            //        void Foo() { this->Bar(); }
            //        void Bar() { return this->a; }
            //    };
            //};
            string a_xml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
    <class>class <name>B</name> <block>{<private type=""default"">
        <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
        <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><call><name>Bar</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
        <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>this</name><op:operator>-&gt;</op:operator><name>a</name></expr>;</return> }</block></function>
    </private>}</block>;</class>
</private>}</block>;</class>";
            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var aDotBar = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(aDotBar);
            var classB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(classB);
            var aDotBDotFoo = classB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(aDotBDotFoo);
            var aDotBDotBar = classB.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(aDotBDotBar);

            Assert.AreEqual(1, aDotBDotFoo.ChildStatements.Count);
            var barCall = aDotBDotFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(barCall);
            Assert.AreSame(aDotBDotBar, barCall.FindMatches().FirstOrDefault());
            Assert.AreNotSame(aDotBar, barCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_GlobalFunction() {
            //void foo(int a) { printf(a); }
            //int main() {
            //    foo(5);
            //    return 0;
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">void</name></type> <name pos:line=""1"" pos:column=""6"">foo</name><parameter_list pos:line=""1"" pos:column=""9"">(<param><decl><type><name pos:line=""1"" pos:column=""10"">int</name></type> <name pos:line=""1"" pos:column=""14"">a</name></decl></param>)</parameter_list> <block pos:line=""1"" pos:column=""17"">{ <expr_stmt><expr><call><name pos:line=""1"" pos:column=""19"">printf</name><argument_list pos:line=""1"" pos:column=""25"">(<argument><expr><name pos:line=""1"" pos:column=""26"">a</name></expr></argument>)</argument_list></call></expr>;</expr_stmt> }</block></function>
<function><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">main</name><parameter_list pos:line=""2"" pos:column=""9"">()</parameter_list> <block pos:line=""2"" pos:column=""12"">{
    <expr_stmt><expr><call><name pos:line=""3"" pos:column=""5"">foo</name><argument_list pos:line=""3"" pos:column=""8"">(<argument><expr><lit:literal type=""number"" pos:line=""3"" pos:column=""9"">5</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt>
    <return pos:line=""4"" pos:column=""5"">return <expr><lit:literal type=""number"" pos:line=""4"" pos:column=""12"">0</lit:literal></expr>;</return>
}</block></function>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").FirstOrDefault();
            var mainMethod = globalScope.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(mainMethod);
            //Assert.AreEqual(2, mainMethod.MethodCalls.Count());

            Assert.AreEqual(2, mainMethod.ChildStatements.Count);

            var fiveCall = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(fiveCall);
            var matches = fiveCall.FindMatches();
            Assert.AreSame(fooMethod, matches.FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_CallGlobalNamespace() {
            //void Foo() {
            //    std::cout<<"global::Foo"<<std::endl;
            //}
            //namespace A
            //{
            //    void Foo() {
            //        std::cout<<"A::Foo"<<std::endl;
            //    }
            //    void print()
            //    {
            //         Foo();
            //         ::Foo();
            //    }
            //}
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{
    <expr_stmt><expr><name><name>std</name><op:operator>::</op:operator><name>cout</name></name><op:operator>&lt;&lt;</op:operator><lit:literal type=""string"">""global::Foo""</lit:literal><op:operator>&lt;&lt;</op:operator><name><name>std</name><op:operator>::</op:operator><name>endl</name></name></expr>;</expr_stmt>
}</block></function>
<namespace>namespace <name>A</name>
<block>{
    <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{
        <expr_stmt><expr><name><name>std</name><op:operator>::</op:operator><name>cout</name></name><op:operator>&lt;&lt;</op:operator><lit:literal type=""string"">""A::Foo""</lit:literal><op:operator>&lt;&lt;</op:operator><name><name>std</name><op:operator>::</op:operator><name>endl</name></name></expr>;</expr_stmt>
    }</block></function>
    <function><type><name>void</name></type> <name>print</name><parameter_list>()</parameter_list>
    <block>{
         <expr_stmt><expr><call><name>Foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
         <expr_stmt><expr><call><name><op:operator>::</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></function>
}</block></namespace>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var globalFoo = globalScope.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            var aFoo = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            var print = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<MethodDefinition>("print").FirstOrDefault();

            Assert.IsNotNull(globalFoo);
            Assert.IsNotNull(aFoo);
            Assert.IsNotNull(print);

            Assert.AreEqual(2, print.ChildStatements.Count);

            var aCall = print.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(aCall);
            var aCallMatches = aCall.FindMatches().ToList();
            //Assert.AreEqual(1, matches.Count);
            Assert.AreSame(aFoo, aCallMatches.FirstOrDefault());

            var globalCall = print.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(globalCall);
            var globalCallMatches = globalCall.FindMatches().ToList();
            Assert.AreEqual(1, globalCallMatches.Count);
            Assert.AreSame(globalFoo, globalCallMatches.FirstOrDefault());
        }

        [Test]
        public void TestMethodCallFindMatches() {
            // # A.h class A { int context;
            // public:
            // A(); };
            string headerXml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>context</name></decl>;</decl_stmt>
    </private><public>public:
        <constructor_decl><name>A</name><parameter_list>()</parameter_list>;</constructor_decl>
</public>}</block>;</class>";

            // # A.cpp #include "A.h"
            // A: :A() {
            // }
            string implementationXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<constructor><name><name>A</name><op:operator>::</op:operator><name>A</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>val</name></decl></param>)</parameter_list> <block>{
}</block></constructor>";

            // # main.cpp #include "A.h" int main() { A a = A(); return 0; }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>A</name></type> <name>a</name> =<init> <expr><op:operator>new</op:operator> <call><name>A</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
    <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            var headerElement = fileSetup.GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = fileSetup.GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = fileSetup.GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = codeParser.ParseFileUnit(headerElement);
            var implementation = codeParser.ParseFileUnit(implementationElement);
            var main = codeParser.ParseFileUnit(mainElement);

            var unmergedMainMethod = main.ChildStatements.First() as MethodDefinition;
            Assert.That(unmergedMainMethod.FindExpressions<MethodCall>(true).First().FindMatches(), Is.Empty);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from namedChild in globalScope.ChildStatements.OfType<INamedEntity>()
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.FindExpressions<MethodCall>(true).First();
            var constructor = typeA.ChildStatements.OfType<MethodDefinition>().FirstOrDefault() as MethodDefinition;

            Assert.IsTrue(callInMain.IsConstructor);
            Assert.IsTrue(constructor.IsConstructor);
            Assert.AreSame(constructor, callInMain.FindMatches().First());
        }

        [Test]
        public void TestMethodCallFindMatches_WithArguments() {
            // # A.h class A { int context;
            // public:
            // A(int value); };
            string headerXml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>context</name></decl>;</decl_stmt>
</private><public>public:
    <constructor_decl><name>A</name><parameter_list>(<param><decl><type><name>int</name></type> <name>value</name></decl></param>)</parameter_list>;</constructor_decl>
</public>}</block>;</class>";

            // # A.cpp #include "A.h"
            // A: :A(int value) { context = value;
            // }
            string implementationXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<constructor><name><name>A</name><op:operator>::</op:operator><name>A</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>value</name></decl></param>)</parameter_list> <block>{
    <expr_stmt><expr><name>context</name> <op:operator>=</op:operator> <name>value</name></expr>;</expr_stmt>
}</block></constructor>";

            // # main.cpp #include "A.h" int main() { int startingState = 0; A *a = new
            // A(startingState); return startingState; }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>int</name></type> <name>startingState</name> =<init> <expr><lit:literal type=""number"">0</lit:literal></expr></init></decl>;</decl_stmt>
    <decl_stmt><decl><type><name>A</name> <type:modifier>*</type:modifier></type><name>a</name> =<init> <expr><op:operator>new</op:operator> <call><name>A</name><argument_list>(<argument><expr><name>startingState</name></expr></argument>)</argument_list></call></expr></init></decl>;</decl_stmt>
    <return>return <expr><name>startingState</name></expr>;</return></block></function>";

            var headerElement = fileSetup.GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = fileSetup.GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = fileSetup.GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = codeParser.ParseFileUnit(headerElement);
            var implementation = codeParser.ParseFileUnit(implementationElement);
            var main = codeParser.ParseFileUnit(mainElement);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from namedChild in globalScope.ChildStatements.OfType<INamedEntity>()
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.FindExpressions<MethodCall>(true).First();
            var constructor = typeA.ChildStatements.OfType<MethodDefinition>().FirstOrDefault() as MethodDefinition;

            Assert.IsTrue(callInMain.IsConstructor);
            Assert.IsTrue(constructor.IsConstructor);
            Assert.AreSame(constructor, callInMain.FindMatches().First());
        }

        [Test]
        public void TestMethodCallMatchToParameter() {
            //void CallFoo(B b) { b.Foo(); }
            //class B { void Foo() { } }
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">B</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10""> <function><type><name pos:line=""1"" pos:column=""11"">void</name></type> <name pos:line=""1"" pos:column=""16"">Foo</name><parameter_list pos:line=""1"" pos:column=""19"">()</parameter_list> <block pos:line=""1"" pos:column=""22"">{ }</block></function> </private>}</block>;</class>
<function><type><name pos:line=""2"" pos:column=""1"">void</name></type> <name pos:line=""2"" pos:column=""6"">CallFoo</name><parameter_list pos:line=""2"" pos:column=""13"">(<param><decl><type><name pos:line=""2"" pos:column=""14"">B</name></type> <name pos:line=""2"" pos:column=""16"">b</name></decl></param>)</parameter_list> <block pos:line=""2"" pos:column=""19"">{ <expr_stmt><expr><call><name><name pos:line=""2"" pos:column=""21"">b</name><op:operator pos:line=""2"" pos:column=""22"">.</op:operator><name pos:line=""2"" pos:column=""23"">Foo</name></name><argument_list pos:line=""2"" pos:column=""26"">()</argument_list></call></expr>;</expr_stmt> }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var methodCallFoo = testScope.GetNamedChildren<MethodDefinition>("CallFoo").FirstOrDefault();
            var classB = testScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();

            Assert.IsNotNull(methodCallFoo, "can't find CallFoo");
            Assert.IsNotNull(classB, "can't find class B");

            var bDotFoo = classB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "can't find B.Foo()");

            var callToFoo = methodCallFoo.FindExpressions<MethodCall>(true).FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find a call to Foo()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodDefinition_ReturnType() {
            //int Foo() { }
            string xml = @"<function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);
            Assert.AreEqual("int", method.ReturnType.Name);
            Assert.AreEqual(0, method.Parameters.Count);
            Assert.IsFalse(method.IsConstructor);
            Assert.IsFalse(method.IsDestructor);
            Assert.IsFalse(method.IsPartial);
        }

        [Test]
        public void TestMethodDefinition_ReturnTypeAndSpecifier() {
            //static int Foo() { }
            string xml = @"<function><type><name>static</name> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinition_Parameters() {
            //int Foo(int bar, char baz) { }
            var xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">(<param><decl><type><name pos:line=""1"" pos:column=""9"">int</name></type> <name pos:line=""1"" pos:column=""13"">bar</name></decl></param>, <param><decl><type><name pos:line=""1"" pos:column=""18"">char</name></type> <name pos:line=""1"" pos:column=""23"">baz</name></decl></param>)</parameter_list> <block pos:line=""1"" pos:column=""28"">{ }</block></function>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var foo = testScope.GetNamedChildren<MethodDefinition>("Foo").First();
            Assert.AreEqual("int", foo.ReturnType.Name);
            Assert.AreEqual(2, foo.Parameters.Count);
            Assert.AreEqual("int", foo.Parameters[0].VariableType.Name);
            Assert.AreEqual("bar", foo.Parameters[0].Name);
            Assert.AreEqual("char", foo.Parameters[1].VariableType.Name);
            Assert.AreEqual("baz", foo.Parameters[1].Name);
        }

        [Test]
        public void TestMethodDefinition_VoidParameter() {
            //void Foo(void) { }
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>void</name></type></decl></param>)</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual(0, method.Parameters.Count);
        }

        [Test]
        public void TestMethodDefinition_FunctionPointerParameter() {
            //int Foo(char bar, int (*pInit)(Quux *theQuux)) {}
            var xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">(<param><decl><type><name pos:line=""1"" pos:column=""9"">char</name></type> <name pos:line=""1"" pos:column=""14"">bar</name></decl></param>, <param><function_decl><type><name pos:line=""1"" pos:column=""19"">int</name></type> (<type:modifier pos:line=""1"" pos:column=""24"">*</type:modifier><name pos:line=""1"" pos:column=""25"">pInit</name>)<parameter_list pos:line=""1"" pos:column=""31"">(<param><decl><type><name pos:line=""1"" pos:column=""32"">Quux</name> <type:modifier pos:line=""1"" pos:column=""37"">*</type:modifier></type><name pos:line=""1"" pos:column=""38"">theQuux</name></decl></param>)</parameter_list></function_decl></param>)</parameter_list> <block pos:line=""1"" pos:column=""48"">{}</block></function>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var foo = testScope.GetNamedChildren<MethodDefinition>("Foo").First();
            Assert.AreEqual("int", foo.ReturnType.Name);
            Assert.AreEqual(2, foo.Parameters.Count);
            Assert.AreEqual("char", foo.Parameters[0].VariableType.Name);
            Assert.AreEqual("bar", foo.Parameters[0].Name);
            Assert.AreEqual("int", foo.Parameters[1].VariableType.Name);
            Assert.AreEqual("pInit", foo.Parameters[1].Name);
        }

        [Test]
        public void TestMethodDefinition_VoidReturn() {
            //void Foo() { }
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);
            Assert.IsNull(method.ReturnType, "return type should be null");
        }

        [Test]
        public void TestMethodDefinition_DefaultArguments() {
            //void foo(int a = 0);
            //
            //int main() {
            //    foo();
            //    foo(5);
            //    return 0;
            //}
            //
            //void foo(int a) { }
            string xml = @"<function_decl><type><name>void</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name> =<init> <expr><lit:literal type=""number"">0</lit:literal></expr></init></decl></param>)</parameter_list>;</function_decl>

<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <expr_stmt><expr><call><name>foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><call><name>foo</name><argument_list>(<argument><expr><lit:literal type=""number"">5</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt>
    <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>

<function><type><name>void</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>)</parameter_list> <block>{ }</block></function>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit).Merge(new NamespaceDefinition());
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").FirstOrDefault();
            var mainMethod = globalScope.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(mainMethod);

            Assert.AreEqual(3, mainMethod.ChildStatements.Count);
            var defaultCall = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(defaultCall);
            Assert.AreSame(fooMethod, defaultCall.FindMatches().FirstOrDefault());

            var fiveCall = mainMethod.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(fiveCall);
            Assert.AreSame(fooMethod, fiveCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTwoVariableDeclarations() {
            //int a,b;
            string testXml = @"<decl_stmt><decl><type><name>int</name></type> <name>a</name></decl><op:operator>,</op:operator><decl><type ref=""prev""/><name>b</name></decl>;</decl_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            var declStmt = globalScope.ChildStatements.First();
            var varDecls = declStmt.Content.Components.OfType<VariableDeclaration>().ToList();

            Assert.AreEqual(2, varDecls.Count);
            Assert.AreEqual("a", varDecls[0].Name);
            Assert.AreEqual("int", varDecls[0].VariableType.Name);
            Assert.AreEqual("b", varDecls[1].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[1].VariableType);
        }

        [Test]
        public void TestThreeVariableDeclarations() {
            //int a,b,c;
            string testXml = @"<decl_stmt><decl><type><name>int</name></type> <name>a</name></decl><op:operator>,</op:operator><decl><type ref=""prev""/><name>b</name></decl><op:operator>,</op:operator><decl><type ref=""prev""/><name>c</name></decl>;</decl_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            var declStmt = globalScope.ChildStatements.First();
            var varDecls = declStmt.Content.Components.OfType<VariableDeclaration>().ToList();

            Assert.AreEqual(3, varDecls.Count);
            Assert.AreEqual("a", varDecls[0].Name);
            Assert.AreEqual("int", varDecls[0].VariableType.Name);
            Assert.AreEqual("b", varDecls[1].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[1].VariableType);
            Assert.AreEqual("c", varDecls[2].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[2].VariableType);
        }

        [Test]
        public void TestVariablesWithSpecifiers() {
            //const int A;
            //static int B;
            //static const Foo C;
            //extern Foo D;
            string testXml = @"<decl_stmt><decl><type><specifier>const</specifier> <name>int</name></type> <name>A</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>static</specifier> <name>int</name></type> <name>B</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>static</specifier> <specifier>const</specifier> <name>Foo</name></type> <name>C</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>extern</specifier> <name>Foo</name></type> <name>D</name></decl>;</decl_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(4, globalScope.ChildStatements.Count);

            var declA = globalScope.ChildStatements[0].Content as VariableDeclaration;
            Assert.IsNotNull(declA);
            Assert.AreEqual("A", declA.Name);
            Assert.AreEqual("int", declA.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declA.Accessibility);

            var declB = globalScope.ChildStatements[1].Content as VariableDeclaration;
            Assert.IsNotNull(declB);
            Assert.AreEqual("B", declB.Name);
            Assert.AreEqual("int", declB.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declB.Accessibility);

            var declC = globalScope.ChildStatements[2].Content as VariableDeclaration;
            Assert.IsNotNull(declC);
            Assert.AreEqual("C", declC.Name);
            Assert.AreEqual("Foo", declC.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declC.Accessibility);

            var declD = globalScope.ChildStatements[3].Content as VariableDeclaration;
            Assert.IsNotNull(declD);
            Assert.AreEqual("D", declD.Name);
            Assert.AreEqual("Foo", declD.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declD.Accessibility);
        }

        [Test]
        public void TestLiteralUse() {
            //a = 17;
            //foo = "watermelon";
            //if(true) { 
            //  c = 'h';
            //}
            string xml = @"<expr_stmt><expr><name>a</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
<expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""string"">""watermelon""</lit:literal></expr>;</expr_stmt>
<if>if<condition>(<expr><lit:literal type=""boolean"">true</lit:literal></expr>)</condition><then> <block>{ 
  <expr_stmt><expr><name>c</name> <op:operator>=</op:operator> <lit:literal type=""char"">'h'</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var numLit = globalScope.ChildStatements[0].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(numLit);
            Assert.AreEqual("17", numLit.Text);
            Assert.AreEqual(LiteralKind.Number, numLit.Kind);

            var stringLit = globalScope.ChildStatements[1].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(stringLit);
            Assert.AreEqual("\"watermelon\"", stringLit.Text);
            Assert.AreEqual(LiteralKind.String, stringLit.Kind);

            var ifStmt = globalScope.ChildStatements[2] as IfStatement;
            Assert.IsNotNull(ifStmt);

            var boolLit = ifStmt.Condition as LiteralUse;
            Assert.IsNotNull(boolLit);
            Assert.AreEqual("true", boolLit.Text);
            Assert.AreEqual(LiteralKind.Boolean, boolLit.Kind);

            var charLit = ifStmt.ChildStatements[0].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(charLit);
            Assert.AreEqual("\'h\'", charLit.Text);
            Assert.AreEqual(LiteralKind.Character, charLit.Kind);
        }

        [Test]
        public void TestIfElse() {
            //if(a==b) {
            //  i = 17;
            //} else {
            //  i = 42;
            //  ReportError();
            //}
            string xml = @"<if>if<condition>(<expr><name>a</name><op:operator>==</op:operator><name>b</name></expr>)</condition><then> <block>{
  <expr_stmt><expr><name>i</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
}</block></then> <else>else <block>{
  <expr_stmt><expr><name>i</name> <op:operator>=</op:operator> <lit:literal type=""number"">42</lit:literal></expr>;</expr_stmt>
  <expr_stmt><expr><call><name>ReportError</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></else></if>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var ifStmt = globalScope.ChildStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt);
            Assert.IsNull(ifStmt.Content);
            Assert.IsNotNull(ifStmt.Condition);
            Assert.AreEqual(1, ifStmt.ChildStatements.Count);
            Assert.AreEqual(2, ifStmt.ElseStatements.Count);
        }

        [Test]
        public void TestIfElseIf() {
            //if(a==b) {
            //  i = 17;
            //} else if(a==c) {
            //  i = 42;
            //  foo();
            //} else {
            //  ReportError();
            //}
            string xml = @"<if>if<condition>(<expr><name>a</name><op:operator>==</op:operator><name>b</name></expr>)</condition><then> <block>{
  <expr_stmt><expr><name>i</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
}</block></then> <else>else <if>if<condition>(<expr><name>a</name><op:operator>==</op:operator><name>c</name></expr>)</condition><then> <block>{
  <expr_stmt><expr><name>i</name> <op:operator>=</op:operator> <lit:literal type=""number"">42</lit:literal></expr>;</expr_stmt>
  <expr_stmt><expr><call><name>foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></then> <else>else <block>{
  <expr_stmt><expr><call><name>ReportError</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></else></if></else></if>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var ifStmt = globalScope.ChildStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt);
            Assert.IsNull(ifStmt.Content);
            Assert.IsNotNull(ifStmt.Condition);
            Assert.AreEqual(1, ifStmt.ChildStatements.Count);
            Assert.AreEqual(1, ifStmt.ElseStatements.Count);

            var ifStmt2 = ifStmt.ElseStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt2);
            Assert.IsNull(ifStmt2.Content);
            Assert.IsNotNull(ifStmt2.Condition);
            Assert.AreEqual(2, ifStmt2.ChildStatements.Count);
            Assert.AreEqual(1, ifStmt2.ElseStatements.Count);
        }

        [Test]
        public void TestEmptyStatement() {
            // ;
            string xml = @"<empty_stmt>;</empty_stmt>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.h");
            
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0];
            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.ChildStatements.Count);
            Assert.IsNull(actual.Content);
        }

        [Test]
        public void TestVariableUse_Index() {
            //foo.bar[17];
            string xml = @"<expr_stmt><expr><name><name>foo</name><op:operator>.</op:operator><name>bar</name><index>[<expr><lit:literal type=""number"">17</lit:literal></expr>]</index></name></expr>;</expr_stmt>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "a.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(3, exp.Components.Count);
            var foo = exp.Components[0] as NameUse;
            Assert.IsNotNull(foo);
            Assert.AreEqual("foo", foo.Name);
            var op = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(op);
            Assert.AreEqual(".", op.Text);
            var bar = exp.Components[2] as VariableUse;
            Assert.IsNotNull(bar);
            Assert.AreEqual("bar", bar.Name);
            var index = bar.Index as LiteralUse;
            Assert.IsNotNull(index);
            Assert.AreEqual("17", index.Text);
            Assert.AreEqual(LiteralKind.Number, index.Kind);
        }

        [Test]
        public void TestResolveVariable_Field() {
            //class A {
            //public:
            //  int Foo;
            //  A() { Foo = 42; }
            //};
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">A</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10"">
</private><public pos:line=""2"" pos:column=""1"">public:
  <decl_stmt><decl><type><name pos:line=""3"" pos:column=""3"">int</name></type> <name pos:line=""3"" pos:column=""7"">Foo</name></decl>;</decl_stmt>
  <constructor><name pos:line=""4"" pos:column=""3"">A</name><parameter_list pos:line=""4"" pos:column=""4"">()</parameter_list> <block pos:line=""4"" pos:column=""7"">{ <expr_stmt><expr><name pos:line=""4"" pos:column=""9"">Foo</name> <op:operator pos:line=""4"" pos:column=""13"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""15"">42</lit:literal></expr>;</expr_stmt> }</block></constructor>
</public>}</block>;</class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(1, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_FieldInParent() {
            //class B {
            //public:
            //  int Foo;
            //};
            //class A : public B {
            //public:
            //  A() { Foo = 42; }
            //};
            var xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">B</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10"">
</private><public pos:line=""2"" pos:column=""1"">public:
  <decl_stmt><decl><type><name pos:line=""3"" pos:column=""3"">int</name></type> <name pos:line=""3"" pos:column=""7"">Foo</name></decl>;</decl_stmt>
</public>}</block>;</class>
<class pos:line=""5"" pos:column=""1"">class <name pos:line=""5"" pos:column=""7"">A</name> <super pos:line=""5"" pos:column=""9"">: <specifier pos:line=""5"" pos:column=""11"">public</specifier> <name pos:line=""5"" pos:column=""18"">B</name></super> <block pos:line=""5"" pos:column=""20"">{<private type=""default"" pos:line=""5"" pos:column=""21"">
</private><public pos:line=""6"" pos:column=""1"">public:
  <constructor><name pos:line=""7"" pos:column=""3"">A</name><parameter_list pos:line=""7"" pos:column=""4"">()</parameter_list> <block pos:line=""7"" pos:column=""7"">{ <expr_stmt><expr><name pos:line=""7"" pos:column=""9"">Foo</name> <op:operator pos:line=""7"" pos:column=""13"">=</op:operator> <lit:literal type=""number"" pos:line=""7"" pos:column=""15"">42</lit:literal></expr>;</expr_stmt> }</block></constructor>
</public>}</block>;</class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("B").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(1, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_Global() {
            //int Foo;
            //int Bar() {
            //  Foo = 17;
            //}
            var xml = @"<decl_stmt><decl><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name></decl>;</decl_stmt>
<function><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">Bar</name><parameter_list pos:line=""2"" pos:column=""8"">()</parameter_list> <block pos:line=""2"" pos:column=""11"">{
  <expr_stmt><expr><name pos:line=""3"" pos:column=""3"">Foo</name> <op:operator pos:line=""3"" pos:column=""7"">=</op:operator> <lit:literal type=""number"" pos:line=""3"" pos:column=""9"">17</lit:literal></expr>;</expr_stmt>
}</block></function>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<VariableDeclaration>("Foo").First();
            var bar = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            var fooUse = bar.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_VarInNamespace() {
            //namespace A {
            //  int Foo;
            //  int Bar() {
            //    Foo = 17;
            //  }
            //}
            var xml = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">A</name> <block pos:line=""1"" pos:column=""13"">{
  <decl_stmt><decl><type><name pos:line=""2"" pos:column=""3"">int</name></type> <name pos:line=""2"" pos:column=""7"">Foo</name></decl>;</decl_stmt>
  <function><type><name pos:line=""3"" pos:column=""3"">int</name></type> <name pos:line=""3"" pos:column=""7"">Bar</name><parameter_list pos:line=""3"" pos:column=""10"">()</parameter_list> <block pos:line=""3"" pos:column=""13"">{
    <expr_stmt><expr><name pos:line=""4"" pos:column=""5"">Foo</name> <op:operator pos:line=""4"" pos:column=""9"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""11"">17</lit:literal></expr>;</expr_stmt>
  }</block></function>
}</block></namespace>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var bar = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            var fooUse = bar.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_Masking() {
            //int foo = 17;
            //int main(int argc, char** argv)
            //{
            //    std::cout<<foo<<std::endl;
            //    float foo = 42.0;
            //    std::cout<<foo<<std::endl;
            //    return 0;
            //}
            var xml = @"<decl_stmt><decl><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">foo</name> <init pos:line=""1"" pos:column=""9"">= <expr><lit:literal type=""number"" pos:line=""1"" pos:column=""11"">17</lit:literal></expr></init></decl>;</decl_stmt>
<function><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">main</name><parameter_list pos:line=""2"" pos:column=""9"">(<param><decl><type><name pos:line=""2"" pos:column=""10"">int</name></type> <name pos:line=""2"" pos:column=""14"">argc</name></decl></param>, <param><decl><type><name pos:line=""2"" pos:column=""20"">char</name><type:modifier pos:line=""2"" pos:column=""24"">*</type:modifier><type:modifier pos:line=""2"" pos:column=""25"">*</type:modifier></type> <name pos:line=""2"" pos:column=""27"">argv</name></decl></param>)</parameter_list>
<block pos:line=""3"" pos:column=""1"">{
    <expr_stmt><expr><name><name pos:line=""4"" pos:column=""5"">std</name><op:operator pos:line=""4"" pos:column=""8"">::</op:operator><name pos:line=""4"" pos:column=""10"">cout</name></name><op:operator pos:line=""4"" pos:column=""14"">&lt;&lt;</op:operator><name pos:line=""4"" pos:column=""16"">foo</name><op:operator pos:line=""4"" pos:column=""19"">&lt;&lt;</op:operator><name><name pos:line=""4"" pos:column=""21"">std</name><op:operator pos:line=""4"" pos:column=""24"">::</op:operator><name pos:line=""4"" pos:column=""26"">endl</name></name></expr>;</expr_stmt>
    <decl_stmt><decl><type><name pos:line=""5"" pos:column=""5"">float</name></type> <name pos:line=""5"" pos:column=""11"">foo</name> <init pos:line=""5"" pos:column=""15"">= <expr><lit:literal type=""number"" pos:line=""5"" pos:column=""17"">42.0</lit:literal></expr></init></decl>;</decl_stmt>
    <expr_stmt><expr><name><name pos:line=""6"" pos:column=""5"">std</name><op:operator pos:line=""6"" pos:column=""8"">::</op:operator><name pos:line=""6"" pos:column=""10"">cout</name></name><op:operator pos:line=""6"" pos:column=""14"">&lt;&lt;</op:operator><name pos:line=""6"" pos:column=""16"">foo</name><op:operator pos:line=""6"" pos:column=""19"">&lt;&lt;</op:operator><name><name pos:line=""6"" pos:column=""21"">std</name><op:operator pos:line=""6"" pos:column=""24"">::</op:operator><name pos:line=""6"" pos:column=""26"">endl</name></name></expr>;</expr_stmt>
    <return pos:line=""7"" pos:column=""5"">return <expr><lit:literal type=""number"" pos:line=""7"" pos:column=""12"">0</lit:literal></expr>;</return>
}</block></function>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);
            var globalFoo = globalScope.GetNamedChildren<VariableDeclaration>("foo").First();
            var main = globalScope.GetNamedChildren<MethodDefinition>("main").First();
            Assert.AreEqual(4, main.ChildStatements.Count);

            var globalFooUse = main.ChildStatements[0].Content.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "foo");
            var globalFooUseMatches = globalFooUse.FindMatches().ToList();
            Assert.AreEqual(1, globalFooUseMatches.Count);
            Assert.AreSame(globalFoo, globalFooUseMatches[0]);

            var localFoo = main.GetNamedChildren<VariableDeclaration>("foo").First();
            var localFooUse = main.ChildStatements[2].Content.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "foo");
            var localFooUseMatches = localFooUse.FindMatches().ToList();
            Assert.AreEqual(1, localFooUseMatches.Count);
            Assert.AreSame(localFoo, localFooUseMatches[0]);
        }

        [Test]
        public void TestVariableDeclaredInCallingObjectWithParentClass() {
            //class A { B b; };
            string a_xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">A</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10""> <decl_stmt><decl><type><name pos:line=""1"" pos:column=""11"">B</name></type> <name pos:line=""1"" pos:column=""13"">b</name></decl>;</decl_stmt> </private>}</block>;</class>";

            //class B { void Foo() { } };
            string b_xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">B</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10""> <function><type><name pos:line=""1"" pos:column=""11"">void</name></type> <name pos:line=""1"" pos:column=""16"">Foo</name><parameter_list pos:line=""1"" pos:column=""19"">()</parameter_list> <block pos:line=""1"" pos:column=""22"">{ }</block></function> </private>}</block>;</class>";

            //class C : A { };
            string c_xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">C</name> <super pos:line=""1"" pos:column=""9"">: <name pos:line=""1"" pos:column=""11"">A</name></super> <block pos:line=""1"" pos:column=""13"">{<private type=""default"" pos:line=""1"" pos:column=""14""> </private>}</block>;</class>";

            //class D {
            //	C c;
            //	void Bar() { c.b.Foo(); }
            //};
            string d_xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">D</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10"">
    <decl_stmt><decl><type><name pos:line=""2"" pos:column=""5"">C</name></type> <name pos:line=""2"" pos:column=""7"">c</name></decl>;</decl_stmt>
    <function><type><name pos:line=""3"" pos:column=""5"">void</name></type> <name pos:line=""3"" pos:column=""10"">Bar</name><parameter_list pos:line=""3"" pos:column=""13"">()</parameter_list> <block pos:line=""3"" pos:column=""16"">{ <expr_stmt><expr><call><name><name pos:line=""3"" pos:column=""18"">c</name><op:operator pos:line=""3"" pos:column=""19"">.</op:operator><name pos:line=""3"" pos:column=""20"">b</name><op:operator pos:line=""3"" pos:column=""21"">.</op:operator><name pos:line=""3"" pos:column=""22"">Foo</name></name><argument_list pos:line=""3"" pos:column=""25"">()</argument_list></call></expr>;</expr_stmt> }</block></function>
</private>}</block>;</class>";

            var aUnit = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.h");
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(b_xml, "B.h");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(c_xml, "C.h");
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(d_xml, "D.h");

            var globalScope = codeParser.ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(bUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(cUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(dUnit));

            var typeA = globalScope.GetNamedChildren<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetNamedChildren<TypeDefinition>("C").FirstOrDefault();
            var typeD = globalScope.GetNamedChildren<TypeDefinition>("D").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");
            Assert.IsNotNull(typeD, "could not find class D");

            var adotB = typeA.GetNamedChildren<VariableDeclaration>("b").FirstOrDefault();
            Assert.IsNotNull(adotB, "could not find variable A.b");
            Assert.AreEqual("b", adotB.Name);

            var bDotFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "could not method B.Foo()");

            var dDotBar = typeD.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(dDotBar, "could not find method D.Bar()");

            var callToFoo = dDotBar.FindExpressions<MethodCall>(true).FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in D.Bar()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveArrayVariable_Local() {
            //int Foo() {
            //  if(MethodCall()) {
            //    int* bar = malloc(SIZE);
            //    bar[0] = 42;
            //  }
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">()</parameter_list> <block pos:line=""1"" pos:column=""11"">{
  <if pos:line=""2"" pos:column=""3"">if<condition pos:line=""2"" pos:column=""5"">(<expr><call><name pos:line=""2"" pos:column=""6"">MethodCall</name><argument_list pos:line=""2"" pos:column=""16"">()</argument_list></call></expr>)</condition><then pos:line=""2"" pos:column=""19""> <block pos:line=""2"" pos:column=""20"">{
    <decl_stmt><decl><type><name pos:line=""3"" pos:column=""5"">int</name><type:modifier pos:line=""3"" pos:column=""8"">*</type:modifier></type> <name pos:line=""3"" pos:column=""10"">bar</name> <init pos:line=""3"" pos:column=""14"">= <expr><call><name pos:line=""3"" pos:column=""16"">malloc</name><argument_list pos:line=""3"" pos:column=""22"">(<argument><expr><name pos:line=""3"" pos:column=""23"">SIZE</name></expr></argument>)</argument_list></call></expr></init></decl>;</decl_stmt>
    <expr_stmt><expr><name><name pos:line=""4"" pos:column=""5"">bar</name><index pos:line=""4"" pos:column=""8"">[<expr><lit:literal type=""number"" pos:line=""4"" pos:column=""9"">0</lit:literal></expr>]</index></name> <op:operator pos:line=""4"" pos:column=""12"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""14"">42</lit:literal></expr>;</expr_stmt>
  }</block></then></if>
}</block></function>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "a.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var ifStmt = globalScope.GetDescendants<IfStatement>().First();
            Assert.AreEqual(2, ifStmt.ChildStatements.Count());

            var barDecl = ifStmt.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault(v => v.Name == "bar");
            Assert.IsNotNull(barDecl);
            var barUse = ifStmt.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "bar");
            Assert.IsNotNull(barUse);
            Assert.AreSame(barDecl, barUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveArrayVariable_Field() {
            //class A {
            //public:
            //  char* Foo;
            //  A() { 
            //    Foo = malloc(SIZE);
            //    Foo[17] = 'x';
            //  }
            //}
            string xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">A</name> <block pos:line=""1"" pos:column=""9"">{<private type=""default"" pos:line=""1"" pos:column=""10"">
</private><public pos:line=""2"" pos:column=""1"">public:
  <decl_stmt><decl><type><name pos:line=""3"" pos:column=""3"">char</name><type:modifier pos:line=""3"" pos:column=""7"">*</type:modifier></type> <name pos:line=""3"" pos:column=""9"">Foo</name></decl>;</decl_stmt>
  <constructor><name pos:line=""4"" pos:column=""3"">A</name><parameter_list pos:line=""4"" pos:column=""4"">()</parameter_list> <block pos:line=""4"" pos:column=""7"">{ 
    <expr_stmt><expr><name pos:line=""5"" pos:column=""5"">Foo</name> <op:operator pos:line=""5"" pos:column=""9"">=</op:operator> <call><name pos:line=""5"" pos:column=""11"">malloc</name><argument_list pos:line=""5"" pos:column=""17"">(<argument><expr><name pos:line=""5"" pos:column=""18"">SIZE</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><name><name pos:line=""6"" pos:column=""5"">Foo</name><index pos:line=""6"" pos:column=""8"">[<expr><lit:literal type=""number"" pos:line=""6"" pos:column=""9"">17</lit:literal></expr>]</index></name> <op:operator pos:line=""6"" pos:column=""13"">=</op:operator> <lit:literal type=""char"" pos:line=""6"" pos:column=""15"">'x'</lit:literal></expr>;</expr_stmt>
  }</block></constructor>
</public>}</block><decl/></class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(2, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[1].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveCallOnArrayVariable() {
            //#include <iostream>
            //const int SIZE = 5;
            //class Foo {
            //public:
            //    int GetNum() { return 42; }
            //};
            //class Bar {
            //public:
            //    Foo FooArray[SIZE];
            //};
            //int main(int argc, char** argv) {
            //    Bar myBar;
            //    std::cout<< myBar.FooArray[0].GetNum() << std::endl;
            //    return 0;
            //}
            string xml = @"<cpp:include pos:line=""1"" pos:column=""1"">#<cpp:directive pos:line=""1"" pos:column=""2"">include</cpp:directive> <cpp:file pos:line=""1"" pos:column=""10"">&lt;iostream&gt;</cpp:file></cpp:include>
<decl_stmt><decl><type><specifier pos:line=""2"" pos:column=""1"">const</specifier> <name pos:line=""2"" pos:column=""7"">int</name></type> <name pos:line=""2"" pos:column=""11"">SIZE</name> <init pos:line=""2"" pos:column=""16"">= <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""18"">5</lit:literal></expr></init></decl>;</decl_stmt>
<class pos:line=""3"" pos:column=""1"">class <name pos:line=""3"" pos:column=""7"">Foo</name> <block pos:line=""3"" pos:column=""11"">{<private type=""default"" pos:line=""3"" pos:column=""12"">
</private><public pos:line=""4"" pos:column=""1"">public:
    <function><type><name pos:line=""5"" pos:column=""5"">int</name></type> <name pos:line=""5"" pos:column=""9"">GetNum</name><parameter_list pos:line=""5"" pos:column=""15"">()</parameter_list> <block pos:line=""5"" pos:column=""18"">{ <return pos:line=""5"" pos:column=""20"">return <expr><lit:literal type=""number"" pos:line=""5"" pos:column=""27"">42</lit:literal></expr>;</return> }</block></function>
</public>}</block>;</class>
<class pos:line=""7"" pos:column=""1"">class <name pos:line=""7"" pos:column=""7"">Bar</name> <block pos:line=""7"" pos:column=""11"">{<private type=""default"" pos:line=""7"" pos:column=""12"">
</private><public pos:line=""8"" pos:column=""1"">public:
    <decl_stmt><decl><type><name pos:line=""9"" pos:column=""5"">Foo</name></type> <name><name pos:line=""9"" pos:column=""9"">FooArray</name><index pos:line=""9"" pos:column=""17"">[<expr><name pos:line=""9"" pos:column=""18"">SIZE</name></expr>]</index></name></decl>;</decl_stmt>
</public>}</block>;</class>
<function><type><name pos:line=""11"" pos:column=""1"">int</name></type> <name pos:line=""11"" pos:column=""5"">main</name><parameter_list pos:line=""11"" pos:column=""9"">(<param><decl><type><name pos:line=""11"" pos:column=""10"">int</name></type> <name pos:line=""11"" pos:column=""14"">argc</name></decl></param>, <param><decl><type><name pos:line=""11"" pos:column=""20"">char</name><type:modifier pos:line=""11"" pos:column=""24"">*</type:modifier><type:modifier pos:line=""11"" pos:column=""25"">*</type:modifier></type> <name pos:line=""11"" pos:column=""27"">argv</name></decl></param>)</parameter_list> <block pos:line=""11"" pos:column=""33"">{
    <decl_stmt><decl><type><name pos:line=""12"" pos:column=""5"">Bar</name></type> <name pos:line=""12"" pos:column=""9"">myBar</name></decl>;</decl_stmt>
    <expr_stmt><expr><name><name pos:line=""13"" pos:column=""5"">std</name><op:operator pos:line=""13"" pos:column=""8"">::</op:operator><name pos:line=""13"" pos:column=""10"">cout</name></name><op:operator pos:line=""13"" pos:column=""14"">&lt;&lt;</op:operator> <name><name pos:line=""13"" pos:column=""17"">myBar</name><op:operator pos:line=""13"" pos:column=""22"">.</op:operator><name pos:line=""13"" pos:column=""23"">FooArray</name><index pos:line=""13"" pos:column=""31"">[<expr><lit:literal type=""number"" pos:line=""13"" pos:column=""32"">0</lit:literal></expr>]</index></name><op:operator pos:line=""13"" pos:column=""34"">.</op:operator><call><name pos:line=""13"" pos:column=""35"">GetNum</name><argument_list pos:line=""13"" pos:column=""41"">()</argument_list></call> <op:operator pos:line=""13"" pos:column=""44"">&lt;&lt;</op:operator> <name><name pos:line=""13"" pos:column=""47"">std</name><op:operator pos:line=""13"" pos:column=""50"">::</op:operator><name pos:line=""13"" pos:column=""52"">endl</name></name></expr>;</expr_stmt>
    <return pos:line=""14"" pos:column=""5"">return <expr><lit:literal type=""number"" pos:line=""14"" pos:column=""12"">0</lit:literal></expr>;</return>
}</block></function>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var getNum = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "GetNum");
            Assert.IsNotNull(getNum);
            var main = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(main);
            Assert.AreEqual(3, main.ChildStatements.Count);

            var getNumCall = main.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().First(mc => mc.Name == "GetNum");
            var matches = getNumCall.FindMatches().ToList();
            Assert.AreEqual(1, matches.Count);
            Assert.AreSame(getNum, matches.First());
        }

        [Test]
        public void TestTypeUseForOtherNamespace() {
            //namespace A {
            //    namespace B {
            //        class C {
            //            int Foo() { }
            //        };
            //    }
            //}
            string c_xml = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">A</name> <block pos:line=""1"" pos:column=""13"">{
    <namespace pos:line=""2"" pos:column=""5"">namespace <name pos:line=""2"" pos:column=""15"">B</name> <block pos:line=""2"" pos:column=""17"">{
        <class pos:line=""3"" pos:column=""9"">class <name pos:line=""3"" pos:column=""15"">C</name> <block pos:line=""3"" pos:column=""17"">{<private type=""default"" pos:line=""3"" pos:column=""18"">
            <function><type><name pos:line=""4"" pos:column=""13"">int</name></type> <name pos:line=""4"" pos:column=""17"">Foo</name><parameter_list pos:line=""4"" pos:column=""20"">()</parameter_list> <block pos:line=""4"" pos:column=""23"">{ }</block></function>
        </private>}</block>;</class>
    }</block></namespace>
}</block></namespace>";

            //using namespace A::B;
            //namespace D {
            //    class E {
            //        void main() {
            //            C c = new C();
            //            c.Foo();
            //        }
            //    };
            //}
            string e_xml = @"<using pos:line=""1"" pos:column=""1"">using namespace <name><name pos:line=""1"" pos:column=""17"">A</name><op:operator pos:line=""1"" pos:column=""18"">::</op:operator><name pos:line=""1"" pos:column=""20"">B</name></name>;</using>
<namespace pos:line=""2"" pos:column=""1"">namespace <name pos:line=""2"" pos:column=""11"">D</name> <block pos:line=""2"" pos:column=""13"">{
    <class pos:line=""3"" pos:column=""5"">class <name pos:line=""3"" pos:column=""11"">E</name> <block pos:line=""3"" pos:column=""13"">{<private type=""default"" pos:line=""3"" pos:column=""14"">
        <function><type><name pos:line=""4"" pos:column=""9"">void</name></type> <name pos:line=""4"" pos:column=""14"">main</name><parameter_list pos:line=""4"" pos:column=""18"">()</parameter_list> <block pos:line=""4"" pos:column=""21"">{
            <decl_stmt><decl><type><name pos:line=""5"" pos:column=""13"">C</name></type> <name pos:line=""5"" pos:column=""15"">c</name> <init pos:line=""5"" pos:column=""17"">= <expr><op:operator pos:line=""5"" pos:column=""19"">new</op:operator> <call><name pos:line=""5"" pos:column=""23"">C</name><argument_list pos:line=""5"" pos:column=""24"">()</argument_list></call></expr></init></decl>;</decl_stmt>
            <expr_stmt><expr><call><name><name pos:line=""6"" pos:column=""13"">c</name><op:operator pos:line=""6"" pos:column=""14"">.</op:operator><name pos:line=""6"" pos:column=""15"">Foo</name></name><argument_list pos:line=""6"" pos:column=""18"">()</argument_list></call></expr>;</expr_stmt>
        }</block></function>
    </private>}</block>;</class>
}</block></namespace>";

            var cUnit = fileSetup.GetFileUnitForXmlSnippet(c_xml, "C.cpp");
            var eUnit = fileSetup.GetFileUnitForXmlSnippet(e_xml, "E.cpp");

            NamespaceDefinition globalScope = codeParser.ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(eUnit));

            var typeC = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.FindExpressions<VariableDeclaration>(true).FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.ResolveType().FirstOrDefault());

            var callToCConstructor = mainMethod.FindExpressions<MethodCall>(true).FirstOrDefault();
            var callToFoo = mainMethod.FindExpressions<MethodCall>(true).LastOrDefault();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }
    }
}