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

//        [Test]
//        public void TestConstructorWithCallToSelf() {
//            // test.h 
//            //class MyClass {
//            //public:
//            //   MyClass() : MyClass(0) { } 
//            //   MyClass(int foo) { } 
//            //};
//            string xml = @"<class>class <name>MyClass</name> <block>{<private type=""default"">
//</private><public>public:
//   <constructor><name>MyClass</name><parameter_list>()</parameter_list> <member_list>: <call><name>MyClass</name><argument_list>(<argument><expr><lit:literal type=""number"">0</lit:literal></expr></argument>)</argument_list></call> </member_list><block>{ }</block></constructor> 
//   <constructor><name>MyClass</name><parameter_list>(<param><decl><type><name>int</name></type> <name>foo</name></decl></param>)</parameter_list> <block>{ }</block></constructor> 
//</public>}</block>;</class>";
//            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
//            var globalScope = codeParser.ParseFileUnit(unit);

//            var constructors = globalScope.GetDescendants<MethodDefinition>().ToList();
//            var defaultConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 0);
//            var calledConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 1);

//            Assert.IsNotNull(defaultConstructor);
//            Assert.IsNotNull(calledConstructor);
//            Assert.AreEqual(1, defaultConstructor.MethodCalls.Count());

//            var constructorCall = defaultConstructor.MethodCalls.First();

//            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
//        }

//        [Test]
//        public void TestConstructorWithSuperClass() {
//            // test.h class SuperClass {
//            // public:
//            // SuperClass(int foo) { } }; class SubClass : public SuperClass {
//            // public:
//            // SubClass(int foo) : SuperClass(foo) { } };
//            string xml = @"<class>class <name>SuperClass</name> <block>{<private type=""default"">
//  </private><public>public:
//    <constructor><name>SuperClass</name><parameter_list>(<param><decl><type><name>int</name></type> <name>foo</name></decl></param>)</parameter_list> <block>{ }</block></constructor>
//</public>}</block>;</class>
//<class>class <name>SubClass</name> <super>: <specifier>public</specifier> <name>SuperClass</name></super> <block>{<private type=""default"">
//  </private><public>public:
//    <constructor><name>SubClass</name><parameter_list>(<param><decl><type><name>int</name></type> <name>foo</name></decl></param>)</parameter_list> <member_list>: <call><name>SuperClass</name><argument_list>(<argument><expr><name>foo</name></expr></argument>)</argument_list></call> </member_list><block>{ }</block></constructor>
//</public>}</block>;</class>";
//            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.h");
//            var globalScope = codeParser.ParseFileUnit(unit);

//            var constructors = globalScope.GetDescendants<MethodDefinition>();
//            var subClassConstructor = (from method in constructors
//                                       where method.GetAncestors<TypeDefinition>().First().Name == "SubClass"
//                                       select method).FirstOrDefault();

//            var calledConstructor = (from method in constructors
//                                     where method.GetAncestors<TypeDefinition>().First().Name == "SuperClass"
//                                     select method).FirstOrDefault();

//            Assert.IsNotNull(subClassConstructor);
//            Assert.IsNotNull(calledConstructor);
//            //TODO: fix test oracles
//            Assert.AreEqual(1, subClassConstructor.MethodCalls.Count());

//            var constructorCall = subClassConstructor.MethodCalls.First();

//            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
//        }

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
        public void TestGetAliases_Import() {
            //A.cpp
            //namespace x {
            //  namespace y {
            //    namespace z {}
            //  }
            //}
            string xmlA = @"<namespace>namespace <name>x</name> <block>{
  <namespace>namespace <name>y</name> <block>{
    <namespace>namespace <name>z</name> <block>{}</block></namespace>
  }</block></namespace>
}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using namespace x::y::z;
            //foo = 17;
            string xmlB = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>
<expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>";
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
        public void TestGetAliases_NestedImportNamespace() {
            //using namespace x::y::z;
            //if(bar) {
            //  using namespace std;
            //  foo = 17;
            //}
            string xml = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>
<if>if<condition>(<expr><name>bar</name></expr>)</condition><then> <block>{
  <using>using namespace <name>std</name>;</using>
  <expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
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
            string xmlA = @"<namespace>namespace <name>B</name> <block>{
  <class>class <name>Bar</name> <block>{<private type=""default""/>}</block>
<decl/></class>}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cpp");
            //B.cpp
            //using namespace x::y::z;
            //if(bar) {
            //  using B::Bar;
            //  foo = 17;
            //}
            string xmlB = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>
<if>if<condition>(<expr><name>bar</name></expr>)</condition><then> <block>{
  <using>using <name><name>B</name><op:operator>::</op:operator><name>Bar</name></name>;</using>
  <expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
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
            string xml = @"<using>using namespace <name><name>x</name><op:operator>::</op:operator><name>y</name><op:operator>::</op:operator><name>z</name></name>;</using>
<if>if<condition>(<expr><name>bar</name></expr>)</condition><then> <block>{
  <using>using <name>x</name> = <decl_stmt><decl><type><name><name>foo</name><op:operator>::</op:operator><name>bar</name><op:operator>::</op:operator><name>baz</name></name></type></decl>;</decl_stmt></using>
  <expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
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
        [Category("Todo")]
        public void TestMergeWithUsing() {
            // namespace A { class B { void Foo(); }; }
            string headerXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{<private type=""default""> <function_decl><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl> </private>}</block>;</class> }</block></namespace>";

            //using namespace A;
            //
            //void B::Foo() { }
            string implementationXml = @"<using>using namespace <name>A</name>;</using>

<function><type><name>void</name></type> <name><name>B</name><op:operator>::</op:operator><name>Foo</name></name><parameter_list>()</parameter_list> <block>{ }</block></function>";

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

            var globalScope_implementationFirst = implementationScope.Merge(headerScope);

            DataAssert.StatementsAreEqual(globalScope, globalScope_implementationFirst);
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

            var aDotBar = globalScope.GetNamedChildren("A").First().GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
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
            string xml = @"<function><type><name>void</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>)</parameter_list> <block>{ <expr_stmt><expr><call><name>printf</name><argument_list>(<argument><expr><name>a</name></expr></argument>)</argument_list></call></expr>;</expr_stmt> }</block></function>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <expr_stmt><expr><call><name>foo</name><argument_list>(<argument><expr><lit:literal type=""number"">5</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt>
    <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>
";

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
        public void TestMethodDefinitionWithReturnType() {
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
        public void TestMethodDefinitionWithReturnTypeAndWithSpecifier() {
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
        public void TestMethodDefinitionWithVoidParameter() {
            //void Foo(void) { }
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>(<param><decl><type><name>void</name></type></decl></param>)</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual(0, method.Parameters.Count);
        }

        [Test]
        public void TestMethodDefinitionWithVoidReturn() {
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
        public void TestMethodWithDefaultArguments() {
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
    }
}