/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class JavaCodeParserTests {
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new JavaCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.Java);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            //import x.y.z;
            string xml = @"<import>import <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</import>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("z", actual.AliasName);
            Assert.AreEqual("x . y . z", actual.Target.ToString());
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            // import x . /*test */ y . z . /*test */ * /*test*/;
            string xml = @"<import>import <name><name>x</name> <op:operator>.</op:operator> <comment type=""block"">/*test */</comment> <name>y</name> <op:operator>.</op:operator> <name>z</name> <op:operator>.</op:operator></name> <comment type=""block"">/*test */</comment> * <comment type=""block"">/*test*/</comment>;</import>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x . y . z", actual.ImportedNamespace.ToString());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassInPackage() {
            //package A.B.C;
            //public class D { }
            string xml = @"<package>package <name><name>A</name><op:operator>.</op:operator><name>B</name><op:operator>.</op:operator><name>C</name></name>;</package>
<class><specifier>public</specifier> class <name>D</name> <block>{ }</block></class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "D.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.IsTrue(globalScope.IsGlobal);
            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var packageA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageA);
            Assert.AreEqual("A", packageA.Name);
            Assert.IsFalse(packageA.IsGlobal);
            Assert.AreEqual(1, packageA.ChildStatements.Count());

            var packageAB = packageA.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageAB);
            Assert.AreEqual("B", packageAB.Name);
            Assert.IsFalse(packageAB.IsGlobal);
            Assert.AreEqual(1, packageAB.ChildStatements.Count());

            var packageABC = packageAB.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageABC);
            Assert.AreEqual("C", packageABC.Name);
            Assert.IsFalse(packageABC.IsGlobal);
            Assert.AreEqual(1, packageABC.ChildStatements.Count());

            var typeD = packageABC.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeD, "Type D is not a type definition");
            Assert.AreEqual("D", typeD.Name);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            // class A { }
            string xml = @"<class>class <name>A</name> <block>{
}</block></class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.IsTrue(globalScope.IsGlobal);

            var actual = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(0, actual.ChildStatements.Count);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            // class A { int foo() { class B { } } }
            string xml = @"<class>class <name>A</name> <block>{
	<function><type><name>int</name></type> <name>foo</name><parameter_list>()</parameter_list> <block>{
		<class>class <name>B</name> <block>{
		}</block></class>
}</block></function>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var fooMethod = typeA.ChildStatements.First() as MethodDefinition;
            var typeB = fooMethod.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreSame(typeA, fooMethod.ParentStatement);
            Assert.AreEqual("A.foo", fooMethod.GetFullName());
            Assert.AreSame(fooMethod, typeB.ParentStatement);
            Assert.AreEqual("A.foo.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithExtendsAndImplements() {
            //Foo.java
            //public class Foo extends xyzzy implements A, B, C {
            //    public int bar;
            //}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <super><extends>extends <name>xyzzy</name></extends> <implements>implements <name>A</name>, <name>B</name>, <name>C</name></implements></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.IsNotNull(globalNamespace);
            Assert.AreEqual("Foo", actual.Name);
            Assert.AreEqual(4, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parentNames = from parent in actual.ParentTypes
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "xyzzy", "A", "B", "C" }, parentNames, (e, a) => e == a);
            foreach(var test in tests) {
                Assert.That(test);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            // class A { class B { } }
            string xml = @"<class>class <name>A</name> <block>{
	<class>class <name>B</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var typeB = typeA.ChildStatements.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentStatement);
            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            // class A implements B,C,D { }
            string xml = @"<class>class <name>A</name> <super><implements>implements <name>B</name>,<name>C</name>,<name>D</name></implements></super> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parentNames = from parent in actual.ParentTypes
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a);
            foreach(var test in tests) {
                Assert.That(test);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            // class D implements A.B.C { }
            string xml = @"<class>class <name>D</name> <super><implements>implements <name><name>A</name><op:operator>.</op:operator><name>B</name><op:operator>.</op:operator><name>C</name></name></implements></super> <block>{ }</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "D.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.ParentTypes.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parent = actual.ParentTypes.First();

            Assert.AreEqual("C", parent.Name);
            
            var prefixNames = parent.Prefix.Names.ToList();
            Assert.AreEqual(2, prefixNames.Count);
            Assert.AreEqual("A", prefixNames[0].Name);
            Assert.AreEqual("B", prefixNames[1].Name);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithSuperClass() {
            //Foo.java
            //public class Foo extends xyzzy {
            //    public int bar;
            //}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <super><extends>extends <name>xyzzy</name></extends></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            Assert.AreEqual("Foo", actual.Name);
            Assert.AreEqual(1, actual.ParentTypes.Count);
            Assert.AreEqual("xyzzy", actual.ParentTypes.First().Name);
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.IsNotNull(globalNamespace);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            // package A; class B { class C { } }
            string xml = @"<package>package <name>A</name>;</package>
<class>class <name>B</name> <block>{
	<class>class <name>C</name> <block>{
	}</block></class>
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "B.java");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeDefinitions = globalScope.GetDescendants<TypeDefinition>().ToList();
            Assert.AreEqual(2, typeDefinitions.Count());

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
        public void TestCreateTypeDefinitions_Interface() {
            // interface A { }
            string xml = @"<class type=""interface"">interface <name>A</name> <block>{
}</block></class>";

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Interface, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

//        [Test]
//        public void TestCreateVariableDeclaration() {
//            // class A { private int X; int GetX() { return X; } }
//            string xml = @"<class>class <name>A</name> <block>{
//	<decl_stmt><decl><type><specifier>private</specifier> <name>int</name></type> <name>X</name></decl>;</decl_stmt>
//	<function><type><specifier>public</specifier> <name>int</name></type> <name>GetX</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>X</name></expr>;</return> }</block></function>
//}</block></class>";

//            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

//            var declarationElement = xmlElement.Descendants(SRC.DeclarationStatement).First();

//            var rootScope = codeParser.ParseFileUnit(xmlElement);
//            var scope = VariableScopeIterator.Visit(rootScope).Last();
//            var useOfX = xmlElement.Descendants(SRC.Return).First().Descendants(SRC.Name).First();

//            Assert.AreEqual(3, rootScope.GetScopeForLocation(useOfX.GetXPath()).GetParentScopesAndSelf().Count());

//            var matchingDeclarations = rootScope.GetDeclarationsForVariableName("X", useOfX.GetXPath());
//            var declaration = matchingDeclarations.First();

//            Assert.AreEqual("int", declaration.VariableType.Name);
//            Assert.AreEqual("X", declaration.Name);

//            Assert.That(useOfX.GetXPath().StartsWith(declaration.ParentScope.PrimaryLocation.XPath));
//        }

        [Test]
        public void TestFieldCreation() {
            // # A.java class A { int B; }
            string xml = @"<class>class <name>A</name> <block>{
    <decl_stmt><decl><type><name>int</name></type> <name>B</name></decl>;</decl_stmt>
}</block></class>";

            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First();
            Assert.AreEqual(1, typeA.ChildStatements.Count);

            var fieldB = typeA.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(fieldB);
            Assert.AreEqual("B", fieldB.Name);
            Assert.AreEqual("int", fieldB.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, fieldB.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_None() {
            //public class Foo {
            //    bool Bar() { return true; }
            //}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{
    <function><type><name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>true</name></expr>;</return> }</block></function>
}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.None, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_Normal() {
            //public class Foo {
            //    public bool Bar() { return true; }
            //}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{
    <function><type><specifier>public</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>true</name></expr>;</return> }</block></function>
}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_Static() {
            //public class Foo {
            //    static public bool Bar() { return true; }
            //}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{
    <function><type><specifier>static</specifier> <specifier>public</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>true</name></expr>;</return> }</block></function>
}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_None() {
            //class Foo {}
            string xml = @"<class>class <name>Foo</name> <block>{}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.None, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Normal() {
            //public class Foo {}
            string xml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Static() {
            //static public class Foo {}
            string xml = @"<class><specifier>static</specifier> <specifier>public</specifier> class <name>Foo</name> <block>{}</block></class>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }

//        [Test]
//        public void TestMethodCallCreation() {
//            // # A.java class A { public int Execute() { B b = new B(); for(int i = 0; i
//            // < b.max(); i++) { try { PrintOutput(b.analyze(i)); } catch(Exception e) {
//            // PrintError(e.ToString()); } } } }
//            string xml = @"<class>class <name>A</name> <block>{
//<function><type><specifier>public</specifier> <name>int</name></type> <name>Execute</name><parameter_list>()</parameter_list> <block>{
//    <decl_stmt><decl><type><name>B</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
//    <for>for(<init><decl><type><name>int</name></type> <name>i</name> =<init> <expr><lit:literal type=""number"">0</lit:literal></expr></init></decl>;</init> <condition><expr><name>i</name> <op:operator>&lt;</op:operator> <call><name><name>b</name><op:operator>.</op:operator><name>max</name></name><argument_list>()</argument_list></call></expr>;</condition> <incr><expr><name>i</name><op:operator>++</op:operator></expr></incr>) <block>{
//        <try>try <block>{
//            <expr_stmt><expr><call><name>PrintOutput</name><argument_list>(<argument><expr><call><name><name>b</name><op:operator>.</op:operator><name>analyze</name></name><argument_list>(<argument><expr><name>i</name></expr></argument>)</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
//        }</block> <catch>catch(<param><decl><type><name>Exception</name></type> <name>e</name></decl></param>) <block>{
//            <expr_stmt><expr><call><name>PrintError</name><argument_list>(<argument><expr><call><name><name>e</name><op:operator>.</op:operator><name>ToString</name></name><argument_list>()</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
//        }</block></catch></try>
//    }</block></for>
//}</block></function>
//}</block></class>";

//            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.java");

//            var globalScope = codeParser.ParseFileUnit(fileUnit);

//            var executeMethod = globalScope.ChildScopes.First().ChildScopes.First() as IMethodDefinition;

//            var callToNewB = executeMethod.MethodCalls.First();
//            Assert.AreEqual("B", callToNewB.Name);
//            Assert.IsTrue(callToNewB.IsConstructor);
//            Assert.IsFalse(callToNewB.IsDestructor);

//            var forStatement = executeMethod.ChildScopes.First();
//            var callToMax = forStatement.MethodCalls.First();
//            Assert.AreEqual("max", callToMax.Name);
//            Assert.IsFalse(callToMax.IsDestructor);
//            Assert.IsFalse(callToMax.IsConstructor);

//            var forBlock = forStatement.ChildScopes.First();
//            var tryStatement = forBlock.ChildScopes.First();
//            var tryBlock = tryStatement.ChildScopes.First();

//            var callToPrintOutput = tryBlock.MethodCalls.First();
//            Assert.AreEqual("PrintOutput", callToPrintOutput.Name);

//            var callToAnalyze = tryBlock.MethodCalls.Last();
//            Assert.AreEqual("analyze", callToAnalyze.Name);

//            var catchStatement = tryStatement.ChildScopes.Last();
//            var catchBlock = catchStatement.ChildScopes.First();

//            var callToPrintError = catchBlock.MethodCalls.First();
//            Assert.AreEqual("PrintError", callToPrintError.Name);

//            var callToToString = catchBlock.MethodCalls.Last();
//            Assert.AreEqual("ToString", callToToString.Name);
//        }

//        [Test]
//        public void TestMethodCallCreation_WithConflictingMethodNames() {
//            //# A.java
//            //class A {
//            //    B b;
//            //    boolean Contains() { b.Contains(); }
//            //}
//            string a_xml = @"<class>class <name>A</name> <block>{
//    <decl_stmt><decl><type><name>B</name></type> <name>b</name></decl>;</decl_stmt>
//    <function><type><name>boolean</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>b</name><op:operator>.</op:operator><name>Contains</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
//}</block></class>";

//            //class B {
//            //    boolean Contains() { return true; }
//            //}
//            string b_xml = @"<class>class <name>B</name> <block>{
//    <function><type><name>boolean</name></type> <name>Contains</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
//}</block></class>";

//            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.java");
//            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(b_xml, "B.java");

//            var scopeForA = codeParser.ParseFileUnit(fileUnitA);
//            var scopeForB = codeParser.ParseFileUnit(fileUnitB);
//            var globalScope = scopeForA.Merge(scopeForB);

//            var aDotContains = globalScope.ChildScopes.First().ChildScopes.First() as IMethodDefinition;
//            var bDotContains = globalScope.ChildScopes.Last().ChildScopes.First() as IMethodDefinition;

//            Assert.AreEqual("A.Contains", aDotContains.GetFullName());
//            Assert.AreEqual("B.Contains", bDotContains.GetFullName());

//            Assert.AreSame(bDotContains, aDotContains.MethodCalls.First().FindMatches().First());
//            Assert.AreNotSame(aDotContains, aDotContains.MethodCalls.First().FindMatches().First());
//        }

//        [Test]
//        public void TestMethodCallCreation_WithThisKeyword() {
//            //class A {
//            //    void Bar() { }
//            //    class B {
//            //        int a;
//            //        void Foo() { this.Bar(); }
//            //        void Bar() { return this.a; }
//            //    }
//            //}
//            string a_xml = @"<class>class <name>A</name> <block>{
//    <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
//    <class>class <name>B</name> <block>{
//        <decl_stmt><decl><type><name>int</name></type> <name>a</name></decl>;</decl_stmt>
//        <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>this</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
//        <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><name>this</name><op:operator>.</op:operator><name>a</name></expr>;</return> }</block></function>
//    }</block></class>
//}</block></class>";

//            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(a_xml, "A.java");
//            var globalScope = codeParser.ParseFileUnit(fileUnit);

//            var aDotBar = globalScope.ChildScopes.First().ChildScopes.First() as IMethodDefinition;
//            var aDotBDotFoo = globalScope.ChildScopes.First().ChildScopes.Last().ChildScopes.First() as IMethodDefinition;
//            var aDotBDotBar = globalScope.ChildScopes.First().ChildScopes.Last().ChildScopes.Last() as IMethodDefinition;

//            Assert.AreEqual("A.Bar", aDotBar.GetFullName());
//            Assert.AreEqual("A.B.Foo", aDotBDotFoo.GetFullName());
//            Assert.AreEqual("A.B.Bar", aDotBDotBar.GetFullName());

//            Assert.AreSame(aDotBDotBar, aDotBDotFoo.MethodCalls.First().FindMatches().First());
//            Assert.AreEqual(1, aDotBDotFoo.MethodCalls.First().FindMatches().Count());
//        }

        
        [Test]
        public void TestVariablesWithSpecifiers() {
            //public static int A;
            //public final int B;
            //private static final Foo C;
            string testXml = @"<decl_stmt><decl><type><specifier>public</specifier> <specifier>static</specifier> <name>int</name></type> <name>A</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>public</specifier> <specifier>final</specifier> <name>int</name></type> <name>B</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>private</specifier> <specifier>static</specifier> <specifier>final</specifier> <name>Foo</name></type> <name>C</name></decl>;</decl_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.java");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var declA = globalScope.ChildStatements[0].Content as VariableDeclaration;
            Assert.IsNotNull(declA);
            Assert.AreEqual("A", declA.Name);
            Assert.AreEqual("int", declA.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, declA.Accessibility);

            var declB = globalScope.ChildStatements[1].Content as VariableDeclaration;
            Assert.IsNotNull(declB);
            Assert.AreEqual("B", declB.Name);
            Assert.AreEqual("int", declB.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, declB.Accessibility);

            var declC = globalScope.ChildStatements[2].Content as VariableDeclaration;
            Assert.IsNotNull(declC);
            Assert.AreEqual("C", declC.Name);
            Assert.AreEqual("Foo", declC.VariableType.Name);
            Assert.AreEqual(AccessModifier.Private, declC.Accessibility);
        }
    }
}