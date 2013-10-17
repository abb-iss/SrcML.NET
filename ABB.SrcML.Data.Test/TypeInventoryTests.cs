/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class TypeInventoryTests {
        private Dictionary<Language, AbstractCodeParser> CodeParser;
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;

        [Test]
        public void BasicParentTest_Java() {
            // # A.java class A implements B { }
            string a_xml = @"<class>class <name>A</name> <super><implements>implements <name>B</name></implements></super> <block>{
}</block></class>";
            // # B.java class B { }
            string b_xml = @"<class>class <name>B</name> <block>{
}</block></class>";

            // # C.java class C { A a; }
            string c_xml = @"<class>class <name>C</name> <block>{
	<decl_stmt><decl><type><name>A</name></type> <name>a</name></decl>;</decl_stmt>
}</block></class>";

            var fileUnitA = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(a_xml, "A.java");
            var fileUnitB = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(b_xml, "B.java");
            var fileUnitC = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(c_xml, "C.java");

            var globalScope = CodeParser[Language.Java].ParseFileUnit(fileUnitA) as IScope;
            globalScope = globalScope.Merge(CodeParser[Language.Java].ParseFileUnit(fileUnitB) as IScope);
            globalScope = globalScope.Merge(CodeParser[Language.Java].ParseFileUnit(fileUnitC) as IScope);

            Assert.AreEqual(3, globalScope.ChildScopes.Count());

            var scopes = VariableScopeIterator.Visit(globalScope);
            var typeDefinitions = (from scope in scopes
                                   let typeDefinition = (scope as TypeDefinition)
                                   where typeDefinition != null
                                   orderby typeDefinition.Name
                                   select typeDefinition).ToList();

            var typeA = typeDefinitions[0];
            var typeB = typeDefinitions[1];
            var typeC = typeDefinitions[2];

            Assert.AreEqual("B", typeB.Name);

            var cDotA = typeC.DeclaredVariables.First();
            var parentOfA = typeA.ParentTypes.First();

            Assert.That(cDotA.VariableType.FindMatches().FirstOrDefault(), Is.SameAs(typeA));
            Assert.That(parentOfA.FindMatches().FirstOrDefault(), Is.SameAs(typeB));
        }

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                { Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
            };
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.CSharp, new CSharpCodeParser() },
                { Language.Java, new JavaCodeParser() },
            };
        }

        [Test]
        public void TestConstructorFromOtherNamespace_Java() {
            //package A.B;
            //class C {
            //	public C() { }
            //}
            string c_xml = @"<package>package <name>A</name>.<name>B</name>;</package>
<class>class <name>C</name> <block>{
	<constructor><specifier>public</specifier> <name>C</name><parameter_list>()</parameter_list> <block>{ }</block></constructor>
}</block></class>";

            //package A.D;
            //import A.B.*;
            //class E {
            //	public void main() {
            //		C c = new C();
            //	}
            //}
            string e_xml = @"<package>package <name>A</name>.<name>D</name>;</package>
<import>import <name>A</name>.<name>B</name>.*;</import>
<class>class <name>E</name> <block>{
	<function><type><specifier>public</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
		<decl_stmt><decl><type><name>C</name></type> <name>c</name> =<init> <expr><op:operator>new</op:operator> <call><name>C</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
	}</block></function>
}</block></class>";

            var cUnit = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(c_xml, "C.java");
            var eUnit = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(e_xml, "E.java");

            var cScope = CodeParser[Language.Java].ParseFileUnit(cUnit);
            var eScope = CodeParser[Language.Java].ParseFileUnit(eUnit);

            var globalScope = cScope.Merge(eScope);

            var typeC = (from typeDefinition in globalScope.GetDescendantScopesAndSelf<TypeDefinition>()
                         where typeDefinition.Name == "C"
                         select typeDefinition).First();

            var typeE = (from typeDefinition in globalScope.GetDescendantScopesAndSelf<TypeDefinition>()
                         where typeDefinition.Name == "E"
                         select typeDefinition).First();

            Assert.IsNotNull(typeC, "Could not find class C");
            Assert.IsNotNull(typeE, "Could not find class E");

            var constructorForC = typeC.GetChildScopes<MethodDefinition>().FirstOrDefault();

            Assert.IsNotNull(constructorForC, "C has no methods");
            Assert.AreEqual("C", constructorForC.Name);
            Assert.That(constructorForC.IsConstructor);

            var eDotMain = typeE.GetChildScopesWithId<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(eDotMain, "could not find main method in E");
            Assert.AreEqual("main", eDotMain.Name);

            var callToC = eDotMain.MethodCalls.First();
            Assert.IsNotNull(callToC, "main contains no calls");
            Assert.AreEqual("C", callToC.Name);
            Assert.That(callToC.IsConstructor);

            Assert.AreEqual(constructorForC, callToC.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestGetCallsTo() {
            //void foo() {
            //    bar();
            //    if(0) bar();
            //}
            //
            //void bar() { star(); }
            //
            //void star() { }
            string xml = @"<function><type><name>void</name></type> <name>foo</name><parameter_list>()</parameter_list> <block>{
    <expr_stmt><expr><call><name>bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <if>if<condition>(<expr><lit:literal type=""number"">0</lit:literal></expr>)</condition><then> <expr_stmt><expr><call><name>bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt></then></if>
}</block></function>

<function><type><name>void</name></type> <name>bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name>star</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>

<function><type><name>void</name></type> <name>star</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var unit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "test.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(unit);

            var methodFoo = globalScope.GetDescendantScopes<MethodDefinition>().FirstOrDefault();
            var methodBar = globalScope.GetDescendantScopes<MethodDefinition>().Skip(1).FirstOrDefault();
            var methodStar = globalScope.GetDescendantScopes<MethodDefinition>().LastOrDefault();

            Assert.IsNotNull(methodFoo, "could not find method foo");
            Assert.IsNotNull(methodBar, "could not find method bar");
            Assert.IsNotNull(methodStar, "could not find method star");

            Assert.AreEqual("foo", methodFoo.Name);
            Assert.AreEqual("bar", methodBar.Name);
            Assert.AreEqual("star", methodStar.Name);

            Assert.That(methodFoo.ContainsCallTo(methodBar));
            Assert.AreEqual(2, methodFoo.GetCallsTo(methodBar).Count());

            Assert.That(methodBar.ContainsCallTo(methodStar));
            Assert.AreEqual(1, methodBar.GetCallsTo(methodStar).Count());

            Assert.IsFalse(methodFoo.ContainsCallTo(methodStar));
            Assert.IsFalse(methodBar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodBar));
        }

        [Test]
        public void TestGlobalVariableUsedInFunction() {
            //class A {
            //	public:
            //		void Foo() { cout << "A.Foo"; }
            //};
            string headerXml = @"<class>class <name>A</name> <block>{<private type=""default"">
	</private><public>public:
		<function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>cout</name> <op:operator>&lt;&lt;</op:operator> <lit:literal type=""string"">""A.Foo""</lit:literal></expr>;</expr_stmt> }</block></function>
</public>}</block>;</class>";

            //#include "A.h"
            //A a;
            //int main() { a.Foo(); return 0; }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<decl_stmt><decl><type><name>A</name></type> <name>a</name></decl>;</decl_stmt>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>a</name><op:operator>.</op:operator><call><name>Foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt> <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return> }</block></function>";

            var headerUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(headerXml, "A.h");
            var mainUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(headerUnit);
            var mainScope = CodeParser[Language.CPlusPlus].ParseFileUnit(mainUnit);

            var globalScope = headerScope.Merge(mainScope);

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            Assert.IsNotNull(typeA, "could not find class A");

            var globalAObject = globalScope.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(globalAObject, "could not find any global variables");
            Assert.AreEqual("a", globalAObject.Name);
            Assert.AreEqual("A", globalAObject.VariableType.Name);
            Assert.AreEqual(typeA, globalAObject.VariableType.FindFirstMatchingType());

            var mainFunction = globalScope.GetChildScopesWithId<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(mainFunction, "could not find main function");

            var callToADotFoo = mainFunction.MethodCalls.FirstOrDefault();
            Assert.IsNotNull(callToADotFoo, "could not find call to a.Foo()");

            Assert.AreEqual("Foo", callToADotFoo.Name);
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

            // # main.cpp #include "A.h" int main() { A a = A();a.b().c(); return 0; }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>A</name></type> <name>a</name> =<init> <expr><op:operator>new</op:operator> <call><name>A</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
    <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            var headerElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = CodeParser[Language.CPlusPlus].ParseFileUnit(headerElement) as INamedScope;
            var implementation = CodeParser[Language.CPlusPlus].ParseFileUnit(implementationElement) as INamedScope;
            var main = CodeParser[Language.CPlusPlus].ParseFileUnit(mainElement) as INamedScope;

            var unmergedMainMethod = main.ChildScopes.First() as MethodDefinition;
            Assert.That(unmergedMainMethod.MethodCalls.First().FindMatches(), Is.Empty);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from child in globalScope.ChildScopes
                                let namedChild = child as INamedScope
                                where namedChild != null
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.MethodCalls.First();
            var constructor = typeA.ChildScopes.First() as MethodDefinition;

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

            var headerElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = CodeParser[Language.CPlusPlus].ParseFileUnit(headerElement) as INamedScope;
            var implementation = CodeParser[Language.CPlusPlus].ParseFileUnit(implementationElement) as INamedScope;
            var main = CodeParser[Language.CPlusPlus].ParseFileUnit(mainElement) as INamedScope;

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from child in globalScope.ChildScopes
                                let namedChild = child as INamedScope
                                where namedChild != null
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.MethodCalls.First();
            var constructor = typeA.ChildScopes.First() as MethodDefinition;

            Assert.IsTrue(callInMain.IsConstructor);
            Assert.IsTrue(constructor.IsConstructor);
            Assert.AreSame(constructor, callInMain.FindMatches().First());
        }

        [Test]
        public void TestMethodCallMatchToParameter() {
            //void CallFoo(B b) { b.Foo(); }
            //class B { void Foo() { } }
            string xml = @"<function><type><name>void</name></type> <name>CallFoo</name><parameter_list>(<param><decl><type><name>B</name></type> <name>b</name></decl></param>)</parameter_list> <block>{ <expr_stmt><expr><name>b</name><op:operator>.</op:operator><call><name>Foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
<class>class <name>B</name> <block>{<private type=""default""> <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> </private>}</block>;</class>";

            var testUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = CodeParser[Language.CPlusPlus].ParseFileUnit(testUnit);

            var methodCallFoo = testScope.GetChildScopesWithId<MethodDefinition>("CallFoo").FirstOrDefault();
            var classB = testScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();

            Assert.IsNotNull(methodCallFoo, "can't find CallFoo");
            Assert.IsNotNull(classB, "can't find class B");

            var bDotFoo = classB.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "can't find B.Foo()");

            var callToFoo = methodCallFoo.MethodCalls.FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find a call to Foo()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallToParentOfCallingObject_CSharp() {
            //class A { void Foo() { } }
            string a_xml = @"<class>class <name>A</name> <block>{ <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>";

            //class B : A { void Bar() { } }
            string b_xml = @"<class>class <name>B</name> <super>: <name>A</name></super> <block>{ <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>";

            //class C {
            //	private B b;
            //	void main() {
            //		b.Foo();
            //	}
            //}
            string c_xml = @"<class>class <name>C</name> <block>{
	<decl_stmt><decl><type><specifier>private</specifier> <name>B</name></type> <name>b</name></decl>;</decl_stmt>
	<function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
		<expr_stmt><expr><call><name><name>b</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
	}</block></function>
}</block></class>";

            var aUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a_xml, "A.cs");
            var bUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(b_xml, "B.cs");
            var cUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(c_xml, "C.cs");

            INamespaceDefinition globalScope = CodeParser[Language.CSharp].ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(bUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(cUnit)) as INamespaceDefinition;

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetChildScopesWithId<TypeDefinition>("C").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");

            var aDotFoo = typeA.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            var cDotMain = typeC.GetChildScopesWithId<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(aDotFoo, "could not find method A.Foo()");
            Assert.IsNotNull(cDotMain, "could not find method C.main()");

            var callToFooFromC = cDotMain.MethodCalls.FirstOrDefault();

            Assert.IsNotNull(callToFooFromC, "could not find any calls in C.main()");
            Assert.AreEqual("Foo", callToFooFromC.Name);
            Assert.AreEqual("b", (callToFooFromC.CallingObject as VariableUse).Name);

            Assert.AreEqual(typeB, callToFooFromC.CallingObject.FindFirstMatchingType());
            Assert.AreEqual(aDotFoo, callToFooFromC.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallToParentType_CSharp() {
            //class A { void Foo() { } }
            string a_xml = @"<class>class <name>A</name> <block>{ <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>";

            //class B : A { void Bar() { Foo() } }
            string b_xml = @"<class>class <name>B</name> <super>: <name>A</name></super> <block>{ <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name>Foo</name><argument_list>()</argument_list></call></expr></expr_stmt> }</block></function> }</block></class>";

            var aUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a_xml, "A.cs");
            var bUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(b_xml, "B.cs");

            INamespaceDefinition globalScope = CodeParser[Language.CSharp].ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(bUnit)) as INamespaceDefinition;

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");

            var aDotFoo = typeA.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            var bDotBar = typeB.GetChildScopesWithId<MethodDefinition>("Bar").FirstOrDefault();

            Assert.IsNotNull(aDotFoo, "could not find method A.Foo()");
            Assert.IsNotNull(bDotBar, "could not find method B.Bar()");

            var callToFooFromB = bDotBar.MethodCalls.FirstOrDefault();

            Assert.IsNotNull(callToFooFromB, "could not find any calls in B.Bar()");
            Assert.AreEqual("Foo", callToFooFromB.Name);

            Assert.AreEqual(aDotFoo, callToFooFromB.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTypeUseForOtherNamespace_Cpp() {
            //namespace A {
            //    namespace B {
            //        class C {
            //            int Foo() { }
            //        };
            //    }
            //}
            string c_xml = @"<namespace>namespace <name>A</name> <block>{
    <namespace>namespace <name>B</name> <block>{
        <class>class <name>C</name> <block>{<private type=""default"">
            <function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>
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
            string e_xml = @"<using>using namespace <name><name>A</name><op:operator>::</op:operator><name>B</name></name>;</using>
<namespace>namespace <name>D</name> <block>{
    <class>class <name>E</name> <block>{<private type=""default"">
        <function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
            <decl_stmt><decl><type><name>C</name></type> <name>c</name> =<init> <expr><op:operator>new</op:operator> <call><name>C</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
            <expr_stmt><expr><name>c</name><op:operator>.</op:operator><call><name>Foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        }</block></function>
    </private>}</block>;</class>
}</block></namespace>";

            var cUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(c_xml, "C.cpp");
            var eUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(e_xml, "E.cpp");

            INamespaceDefinition globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(eUnit)) as INamespaceDefinition;

            var typeC = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetChildScopes<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.FindFirstMatchingType());

            var callToCConstructor = mainMethod.MethodCalls.First();
            var callToFoo = mainMethod.MethodCalls.Last();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTypeUseForOtherNamespace_CSharp() {
            //namespace A.B {
            //    class C {
            //        int Foo() { }
            //    }
            //}
            string c_xml = @"<namespace>namespace <name><name>A</name><op:operator>.</op:operator><name>B</name></name> <block>{
    <class>class <name>C</name> <block>{
        <function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>
    }</block></class>
}</block></namespace>";

            //using A.B;
            //namespace D {
            //    class E {
            //        void main() {
            //            C c = new C();
            //            c.Foo();
            //        }
            //    }
            //}
            string e_xml = @"<using>using <name><name>A</name><op:operator>.</op:operator><name>B</name></name>;</using>
<namespace>namespace <name>D</name> <block>{
    <class>class <name>E</name> <block>{
        <function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
            <decl_stmt><decl><type><name>C</name></type> <name>c</name> =<init> <expr><op:operator>new</op:operator> <call><name>C</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
            <expr_stmt><expr><call><name><name>c</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        }</block></function>
    }</block></class>
}</block></namespace>";

            var cUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(c_xml, "C.cpp");
            var eUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(e_xml, "E.cpp");

            INamespaceDefinition globalScope = CodeParser[Language.CSharp].ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(eUnit)) as INamespaceDefinition;

            var typeC = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetChildScopes<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.FindFirstMatchingType());

            var callToCConstructor = mainMethod.MethodCalls.First();
            var callToFoo = mainMethod.MethodCalls.Last();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTypeUseForOtherNamespace_Java() {
            //package A.B;
            //class C {
            //    int Foo();
            //}
            string c_xml = @"<package>package <name>A</name>.<name>B</name>;</package>
<class>class <name>C</name> <block>{
    <function_decl><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";

            //package D;
            //import A.B.*;
            //class E {
            //    public static void main() {
            //        C c = new C();
            //        c.Foo();
            //    }
            //}
            string e_xml = @"<package>package <name>D</name>;</package>
<import>import <name>A</name>.<name>B</name>.*;</import>
<class>class <name>E</name> <block>{
    <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
        <decl_stmt><decl><type><name>C</name></type> <name>c</name> =<init> <expr><op:operator>new</op:operator> <call><name>C</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
        <expr_stmt><expr><call><name><name>c</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></function>
}</block></class>";

            var cUnit = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(c_xml, "C.java");
            var eUnit = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(e_xml, "E.java");

            INamespaceDefinition globalScope = CodeParser[Language.Java].ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(CodeParser[Language.Java].ParseFileUnit(eUnit)) as INamespaceDefinition;

            var typeC = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendantScopes<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildScopes.First() as MethodDefinition;
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetChildScopes<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.FindFirstMatchingType());

            var callToCConstructor = mainMethod.MethodCalls.First();
            var callToFoo = mainMethod.MethodCalls.Last();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariableDeclaredInCallingObjectWithParentClass_Cpp() {
            //class A { B b; }
            string a_xml = @"<class>class <name>A</name> <block>{<private type=""default""> <decl_stmt><decl><type><name>B</name></type> <name>b</name></decl>;</decl_stmt> </private>}</block><decl/></class>";

            //class B { void Foo() { } }
            string b_xml = @"<class>class <name>B</name> <block>{<private type=""default""> <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> </private>}</block><decl/></class>";

            //class C : A { }
            string c_xml = @"<class>class <name>C</name> <super>: <name>A</name></super> <block>{<private type=""default""> </private>}</block><decl/></class>";

            //class D {
            //	C c;
            //	void Bar() { c.b.Foo(); }
            //}
            string d_xml = @"<class>class <name>D</name> <block>{<private type=""default"">
	<decl_stmt><decl><type><name>C</name></type> <name>c</name></decl>;</decl_stmt>
	<function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><name>c</name><op:operator>.</op:operator><name>b</name><op:operator>.</op:operator><call><name>Foo</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
</private>}</block><decl/></class>";

            var aUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(a_xml, "A.h");
            var bUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(b_xml, "B.h");
            var cUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(c_xml, "C.h");
            var dUnit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(d_xml, "D.h");

            INamespaceDefinition globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(bUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(cUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(dUnit)) as INamespaceDefinition;

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetChildScopesWithId<TypeDefinition>("C").FirstOrDefault();
            var typeD = globalScope.GetChildScopesWithId<TypeDefinition>("D").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");
            Assert.IsNotNull(typeD, "could not find class D");

            var adotB = typeA.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(adotB, "could not find variable A.b");
            Assert.AreEqual("b", adotB.Name);

            var bDotFoo = typeB.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "could not method B.Foo()");

            var dDotBar = typeD.GetChildScopesWithId<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(dDotBar, "could not find method D.Bar()");

            var callToFoo = dDotBar.MethodCalls.FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in D.Bar()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariableDeclaredInCallingObjectWithParentClass_CSharp() {
            //class A { B b; }
            string a_xml = @"<class>class <name>A</name> <block>{ <decl_stmt><decl><type><name>B</name></type> <name>b</name></decl>;</decl_stmt> }</block></class>";

            //class B { void Foo() { } }
            string b_xml = @"<class>class <name>B</name> <block>{ <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>";

            //class C : A { }
            string c_xml = @"<class>class <name>C</name> <super>: <name>A</name></super> <block>{ }</block></class>";

            //class D {
            //	C c;
            //	void Bar() { c.b.Foo(); }
            //}
            string d_xml = @"<class>class <name>D</name> <block>{
	<decl_stmt><decl><type><name>C</name></type> <name>c</name></decl>;</decl_stmt>
	<function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>c</name><op:operator>.</op:operator><name>b</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
}</block></class>";

            var aUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a_xml, "A.cs");
            var bUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(b_xml, "B.cs");
            var cUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(c_xml, "C.cs");
            var dUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(d_xml, "D.cs");

            INamespaceDefinition globalScope = CodeParser[Language.CSharp].ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(bUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(cUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(dUnit)) as INamespaceDefinition;

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetChildScopesWithId<TypeDefinition>("C").FirstOrDefault();
            var typeD = globalScope.GetChildScopesWithId<TypeDefinition>("D").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");
            Assert.IsNotNull(typeD, "could not find class D");

            var adotB = typeA.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(adotB, "could not find variable A.b");
            Assert.AreEqual("b", adotB.Name);

            var bDotFoo = typeB.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "could not method B.Foo()");

            var dDotBar = typeD.GetChildScopesWithId<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(dDotBar, "could not find method D.Bar()");

            var callToFoo = dDotBar.MethodCalls.FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in D.Bar()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariableDeclaredInParentClass() {
            //class A { void Foo() { } }
            string a_xml = @"<class>class <name>A</name> <block>{ <function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>";

            //class B { protected A a; }
            string b_xml = @"<class>class <name>B</name> <block>{ <decl_stmt><decl><type><specifier>protected</specifier> <name>A</name></type> <name>a</name></decl>;</decl_stmt> }</block></class>";

            //class C : B { void Bar() { a.Foo(); } }
            string c_xml = @"<class>class <name>C</name> <super>: <name>B</name></super> <block>{ <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>a</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class>";

            var aUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(a_xml, "A.cs");
            var bUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(b_xml, "B.cs");
            var cUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(c_xml, "C.cs");

            INamespaceDefinition globalScope = CodeParser[Language.CSharp].ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(bUnit)) as INamespaceDefinition;
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(cUnit)) as INamespaceDefinition;

            var typeA = globalScope.GetChildScopesWithId<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetChildScopesWithId<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetChildScopesWithId<TypeDefinition>("C").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");

            var aDotFoo = typeA.GetChildScopesWithId<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(aDotFoo, "could not find method A.Foo()");

            var cDotBar = typeC.GetChildScopesWithId<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(cDotBar, "could not find method C.Bar()");

            var bDotA = typeB.DeclaredVariables.FirstOrDefault();
            Assert.IsNotNull(bDotA, "could not find variable B.a");
            Assert.AreEqual("a", bDotA.Name);

            var callToFoo = cDotBar.MethodCalls.FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in C.Bar()");
            Assert.AreEqual("Foo", callToFoo.Name);

            Assert.AreEqual(aDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }
    }
}