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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    public class TypeInventoryTests {
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;
        private Dictionary<Language, AbstractCodeParser> CodeParser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                { Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
            };
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
            };
        }

        [Test]
        public void BasicParentTest_Java() {
            // # A.java
            // class A implements B {
            // }
            string a_xml = @"<class>class <name>A</name> <super><implements>implements <name>B</name></implements></super> <block>{
}</block></class>";
            // # B.java
            // class B {
            // }
            string b_xml = @"<class>class <name>B</name> <block>{
}</block></class>";

            // # C.java
            // class C {
            //     A a;
            // }
            string c_xml = @"<class>class <name>C</name> <block>{
	<decl_stmt><decl><type><name>A</name></type> <name>a</name></decl>;</decl_stmt>
}</block></class>";

            var fileUnitA = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(a_xml, "A.java");
            var fileUnitB = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(b_xml, "B.java");
            var fileUnitC = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(c_xml, "C.java");

            var globalScope = CodeParser[Language.Java].ParseFileUnit(fileUnitA) as NamedScope;
            globalScope = globalScope.Merge(CodeParser[Language.Java].ParseFileUnit(fileUnitB));
            globalScope = globalScope.Merge(CodeParser[Language.Java].ParseFileUnit(fileUnitC));

            var scopes = VariableScopeIterator.Visit(globalScope);
            var typeDefinitions = (from scope in scopes
                                   let typeDefinition = (scope as TypeDefinition)
                                   where typeDefinition != null
                                   orderby typeDefinition.Name
                                   select typeDefinition).ToList();

            var typeA = typeDefinitions[0];
            var typeB = typeDefinitions[1];
            var typeC = typeDefinitions[2];

            Assert.That(typeC.DeclaredVariables.First().VariableType.FindMatches().First(), Is.SameAs(typeA));
            Assert.That(typeA.ParentTypes.First().FindMatches().First(), Is.SameAs(typeB));
        }

        [Test]
        public void TestMethodCallFindMatches() {
            // # A.h
            // class A {
            //     int state;
            //     public:
            //         A();
            // };
            string headerXml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>state</name></decl>;</decl_stmt>
    </private><public>public:
        <constructor_decl><name>A</name><parameter_list>()</parameter_list>;</constructor_decl>
</public>}</block>;</class>";

            // # A.cpp
            // #include "A.h"
            // A::A() {
            // }
            string implementationXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<constructor><name><name>A</name><op:operator>::</op:operator><name>A</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>val</name></decl></param>)</parameter_list> <block>{
}</block></constructor>";

            // # main.cpp
            // #include "A.h"
            // int main() {
            //     A a = A();a.b().c();
            //     return 0;
            // }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>A</name></type> <name>a</name> =<init> <expr><op:operator>new</op:operator> <call><name>A</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
    <return>return <expr><lit:literal type=""number"">0</lit:literal></expr>;</return>
}</block></function>";

            var headerElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = CodeParser[Language.CPlusPlus].ParseFileUnit(headerElement) as NamedScope;
            var implementation = CodeParser[Language.CPlusPlus].ParseFileUnit(implementationElement) as NamedScope;
            var main = CodeParser[Language.CPlusPlus].ParseFileUnit(mainElement) as NamedScope;

            var unmergedMainMethod = main.ChildScopes.First() as MethodDefinition;
            Assert.That(unmergedMainMethod.MethodCalls.First().FindMatches(), Is.Empty);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from child in globalScope.ChildScopes
                                let namedChild = child as NamedScope
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
            // # A.h
            // class A {
            //     int state;
            //     public:
            //         A(int value);
            // };
            string headerXml = @"<class>class <name>A</name> <block>{<private type=""default"">
    <decl_stmt><decl><type><name>int</name></type> <name>state</name></decl>;</decl_stmt>
</private><public>public:
    <constructor_decl><name>A</name><parameter_list>(<param><decl><type><name>int</name></type> <name>value</name></decl></param>)</parameter_list>;</constructor_decl>
</public>}</block>;</class>";

            // # A.cpp
            // #include "A.h"
            // A::A(int value) {
            //     state = value;
            // }
            string implementationXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<constructor><name><name>A</name><op:operator>::</op:operator><name>A</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>value</name></decl></param>)</parameter_list> <block>{
    <expr_stmt><expr><name>state</name> <op:operator>=</op:operator> <name>value</name></expr>;</expr_stmt>
}</block></constructor>";

            // # main.cpp
            // #include "A.h"
            // int main() {
            //     int startingState = 0;
            //     A *a = new A(startingState);
            //     return startingState;
            // }
            string mainXml = @"<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file><lit:literal type=""string"">""A.h""</lit:literal></cpp:file></cpp:include>
<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
    <decl_stmt><decl><type><name>int</name></type> <name>startingState</name> =<init> <expr><lit:literal type=""number"">0</lit:literal></expr></init></decl>;</decl_stmt>
    <decl_stmt><decl><type><name>A</name> <type:modifier>*</type:modifier></type><name>a</name> =<init> <expr><op:operator>new</op:operator> <call><name>A</name><argument_list>(<argument><expr><name>startingState</name></expr></argument>)</argument_list></call></expr></init></decl>;</decl_stmt>
    <return>return <expr><name>startingState</name></expr>;</return></block></function>";

            var headerElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(headerXml, "A.h");
            var implementationElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(implementationXml, "A.cpp");
            var mainElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(mainXml, "main.cpp");

            var header = CodeParser[Language.CPlusPlus].ParseFileUnit(headerElement) as NamedScope;
            var implementation = CodeParser[Language.CPlusPlus].ParseFileUnit(implementationElement) as NamedScope;
            var main = CodeParser[Language.CPlusPlus].ParseFileUnit(mainElement) as NamedScope;

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from child in globalScope.ChildScopes
                                let namedChild = child as NamedScope
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
    }
}
