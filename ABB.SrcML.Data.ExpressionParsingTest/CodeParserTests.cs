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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ABB.SrcML.Test.Utilities;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test
{
    [TestFixture]
    [Category("Build")]
    public class CodeParserTests {
        private Dictionary<Language, AbstractCodeParser> codeParsers;
        private Dictionary<Language, SrcMLFileUnitSetup> fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParsers = new Dictionary<Language, AbstractCodeParser>() {
                {Language.CPlusPlus, new CPlusPlusCodeParser()},
                {Language.CSharp, new CSharpCodeParser()},
                {Language.Java, new JavaCodeParser()}
            };
            fileSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                {Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus)},
                {Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp)},
                {Language.Java, new SrcMLFileUnitSetup(Language.Java)}
            };
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Java)]
        public void TestTwoVariableDeclarations(Language lang) {
            //int a,b;
            string testXml = @"<decl_stmt><decl><type><name>int</name></type> <name>a</name></decl><op:operator>,</op:operator><decl><type ref=""prev""/><name>b</name></decl>;</decl_stmt>";
            var testUnit = fileSetup[lang].GetFileUnitForXmlSnippet(testXml, "test.cpp");

            var globalScope = codeParsers[lang].ParseFileUnit(testUnit);

            var declStmt = globalScope.ChildStatements.First();
            var varDecls = declStmt.Content.Components.OfType<VariableDeclaration>().ToList();

            Assert.AreEqual(2, varDecls.Count);
            Assert.AreEqual("a", varDecls[0].Name);
            Assert.AreEqual("int", varDecls[0].VariableType.Name);
            Assert.AreEqual("b", varDecls[1].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[1].VariableType);
        }

        
        [TestCase(Language.CSharp)]
        [TestCase(Language.Java)]
        public void TestField(Language lang) {
            //class A {
            //  int Foo;
            //  Bar baz;
            //}
            string xml = @"<class>class <name>A</name> <block>{
  <decl_stmt><decl><type><name>int</name></type> <name>Foo</name></decl>;</decl_stmt>
  <decl_stmt><decl><type><name>Bar</name></type> <name>baz</name></decl>;</decl_stmt>
}</block></class>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            var declStmts = globalScope.GetDescendantsAndSelf<DeclarationStatement>().ToList();
            Assert.AreEqual(2, declStmts.Count);
            
            var foo = declStmts[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual("int", foo.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, foo.Accessibility);

            var baz = declStmts[1].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(baz);
            Assert.AreEqual("baz", baz.Name);
            Assert.AreEqual("Bar", baz.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, baz.Accessibility);
        }

        [TestCase(Language.CPlusPlus)]
        public void TestField_Cpp(Language lang) {
            //class A {
            //  int Foo;
            //  Bar baz;
            //}
            string xml = @"<class>class <name>A</name> <block>{<private type=""default"">
  <decl_stmt><decl><type><name>int</name></type> <name>Foo</name></decl>;</decl_stmt>
  <decl_stmt><decl><type><name>Bar</name></type> <name>baz</name></decl>;</decl_stmt>
</private>}</block><decl/></class>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            var declStmts = globalScope.GetDescendantsAndSelf<DeclarationStatement>().ToList();
            Assert.AreEqual(2, declStmts.Count);
            
            var foo = declStmts[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual("int", foo.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, foo.Accessibility);

            var baz = declStmts[1].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(baz);
            Assert.AreEqual("baz", baz.Name);
            Assert.AreEqual("Bar", baz.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, baz.Accessibility);
        }

        [TestCase(Language.CSharp)]
        [TestCase(Language.Java)]
        public void TestMethodCallCreation(Language lang) {
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
            var fileUnit = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");
            var globalScope = codeParsers[lang].ParseFileUnit(fileUnit);

            var executeMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(executeMethod);

            var callToNewB = executeMethod.ChildStatements.First().Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToNewB);
            Assert.AreEqual("B", callToNewB.Name);
            Assert.IsTrue(callToNewB.IsConstructor);
            Assert.IsFalse(callToNewB.IsDestructor);

            var forStatement = executeMethod.GetDescendants<ForStatement>().FirstOrDefault();
            Assert.IsNotNull(forStatement);
            var callToMax = forStatement.Condition.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToMax);
            Assert.AreEqual("max", callToMax.Name);
            Assert.IsFalse(callToMax.IsDestructor);
            Assert.IsFalse(callToMax.IsConstructor);

            var tryStatement = forStatement.GetDescendants<TryStatement>().FirstOrDefault();
            Assert.IsNotNull(tryStatement);

            var callToPrintOutput = tryStatement.ChildStatements.First().Content as MethodCall;
            Assert.IsNotNull(callToPrintOutput);
            Assert.AreEqual("PrintOutput", callToPrintOutput.Name);
            Assert.IsFalse(callToPrintOutput.IsDestructor);
            Assert.IsFalse(callToPrintOutput.IsConstructor);

            var callToAnalyze = callToPrintOutput.Arguments.First().GetDescendantsAndSelf<MethodCall>().First();
            Assert.IsNotNull(callToAnalyze);
            Assert.AreEqual("analyze", callToAnalyze.Name);
            Assert.IsFalse(callToAnalyze.IsDestructor);
            Assert.IsFalse(callToAnalyze.IsConstructor);

            var catchStatement = tryStatement.CatchStatements.FirstOrDefault();
            Assert.IsNotNull(catchStatement);

            var callToPrintError = catchStatement.ChildStatements.First().Content as MethodCall;
            Assert.IsNotNull(callToPrintError);
            Assert.AreEqual("PrintError", callToPrintError.Name);
            Assert.IsFalse(callToPrintError.IsDestructor);
            Assert.IsFalse(callToPrintError.IsConstructor);

            var callToToString = callToPrintError.Arguments.First().GetDescendantsAndSelf<MethodCall>().First();
            Assert.IsNotNull(callToToString);
            Assert.AreEqual("ToString", callToToString.Name);
            Assert.IsFalse(callToToString.IsDestructor);
            Assert.IsFalse(callToToString.IsConstructor);
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Java)]
        public void TestSimpleExpression(Language lang) {
            //foo = 2+3;
            string xml = @"<expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">2</lit:literal><op:operator>+</op:operator><lit:literal type=""number"">3</lit:literal></expr>;</expr_stmt>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(5, exp.Components.Count);
            var foo = exp.Components[0] as NameUse;
            Assert.IsNotNull(foo);
            Assert.AreEqual("foo", foo.Name);
            var equals = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(equals);
            Assert.AreEqual("=", equals.Text);
            var two = exp.Components[2] as LiteralUse;
            Assert.IsNotNull(two);
            Assert.AreEqual("2", two.Text);
            var plus = exp.Components[3] as OperatorUse;
            Assert.IsNotNull(plus);
            Assert.AreEqual("+", plus.Text);
            var three = exp.Components[4] as LiteralUse;
            Assert.IsNotNull(three);
            Assert.AreEqual("3", three.Text);
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.CSharp)]
        [TestCase(Language.Java)]
        public void TestSubExpression(Language lang) {
            //foo = (2+3)*5;
            string xml = @"<expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <op:operator>(</op:operator><lit:literal type=""number"">2</lit:literal><op:operator>+</op:operator><lit:literal type=""number"">3</lit:literal><op:operator>)</op:operator><op:operator>*</op:operator><lit:literal type=""number"">5</lit:literal></expr>;</expr_stmt>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(5, exp.Components.Count);
            var foo = exp.Components[0] as NameUse;
            Assert.IsNotNull(foo);
            Assert.AreEqual("foo", foo.Name);
            var equals = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(equals);
            Assert.AreEqual("=", equals.Text);

            var subExp = exp.Components[2];
            Assert.AreEqual(typeof(Expression), subExp.GetType());
            Assert.AreEqual(3, subExp.Components.Count);
            var two = subExp.Components[0] as LiteralUse;
            Assert.IsNotNull(two);
            Assert.AreEqual("2", two.Text);
            var plus = subExp.Components[1] as OperatorUse;
            Assert.IsNotNull(plus);
            Assert.AreEqual("+", plus.Text);
            var three = subExp.Components[2] as LiteralUse;
            Assert.IsNotNull(three);
            Assert.AreEqual("3", three.Text);

            var times = exp.Components[3] as OperatorUse;
            Assert.IsNotNull(times);
            Assert.AreEqual("*", times.Text);
            var five = exp.Components[4] as LiteralUse;
            Assert.IsNotNull(five);
            Assert.AreEqual("5", five.Text);
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestGetChildren_Statements(Language lang) {
            //if(foo == 0) {
            //  return;
            //  try {
            //    return;
            //  } catch(Exception e) {
            //    return;
            //  } 
            //} else {
            //  return;
            //}
            string xml = @"<if>if<condition>(<expr><name>foo</name> <op:operator>==</op:operator> <lit:literal type=""number"">0</lit:literal></expr>)</condition><then> <block>{
  <return>return;</return>
  <try>try <block>{
    <return>return;</return>
  }</block> <catch>catch<parameter_list>(<param><decl><type><name>Exception</name></type> <name>e</name></decl></param>)</parameter_list> <block>{
    <return>return;</return>
  }</block></catch></try> 
}</block></then> <else>else <block>{
  <return>return;</return>
}</block></else></if>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            Assert.AreEqual(4, globalScope.GetDescendantsAndSelf<ReturnStatement>().Count());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestGetChildren_Expressions(Language lang) {
            //Foo f = (bar + baz(qux(17))).Xyzzy();
            string xml = @"<decl_stmt><decl><type><name>Foo</name></type> <name>f</name> <init>= <expr><op:operator>(</op:operator><name>bar</name> <op:operator>+</op:operator> <call><name>baz</name><argument_list>(<argument><expr><call><name>qux</name><argument_list>(<argument><expr><lit:literal type=""number"">17</lit:literal></expr></argument>)</argument_list></call></expr></argument>)</argument_list></call><op:operator>)</op:operator><op:operator>.</op:operator><call><name>Xyzzy</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            Assert.AreEqual(3, globalScope.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().Count());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestResolveLocalVariable(Language lang) {
            //int Foo() {
            //  if(MethodCall()) {
            //    int bar = 17;
            //    bar = 42;
            //  }
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">()</parameter_list> <block pos:line=""1"" pos:column=""11"">{
  <if pos:line=""2"" pos:column=""3"">if<condition pos:line=""2"" pos:column=""5"">(<expr><call><name pos:line=""2"" pos:column=""6"">MethodCall</name><argument_list pos:line=""2"" pos:column=""16"">()</argument_list></call></expr>)</condition><then pos:line=""2"" pos:column=""19""> <block pos:line=""2"" pos:column=""20"">{
    <decl_stmt><decl><type><name pos:line=""3"" pos:column=""5"">int</name></type> <name pos:line=""3"" pos:column=""9"">bar</name> <init pos:line=""3"" pos:column=""13"">= <expr><lit:literal type=""number"" pos:line=""3"" pos:column=""15"">17</lit:literal></expr></init></decl>;</decl_stmt>
    <expr_stmt><expr><name pos:line=""4"" pos:column=""5"">bar</name> <op:operator pos:line=""4"" pos:column=""9"">=</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""11"">42</lit:literal></expr>;</expr_stmt>
  }</block></then></if>
}</block></function>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            var ifStmt = globalScope.GetDescendants<IfStatement>().First();
            Assert.AreEqual(2, ifStmt.ChildStatements.Count());

            var barDecl = ifStmt.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault(v => v.Name == "bar");
            Assert.IsNotNull(barDecl);
            var barUse = ifStmt.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "bar");
            Assert.IsNotNull(barUse);
            Assert.AreSame(barDecl, barUse.FindMatches().FirstOrDefault());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestResolveLocalVariable_ParentExpression(Language lang) {
            //int Foo() {
            //  for(int i = 0; i < bar; i++) {
            //    printf(i);
            //  }
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">()</parameter_list> <block pos:line=""1"" pos:column=""11"">{
  <for pos:line=""2"" pos:column=""3"">for(<init><decl><type><name pos:line=""2"" pos:column=""7"">int</name></type> <name pos:line=""2"" pos:column=""11"">i</name> <init pos:line=""2"" pos:column=""13"">= <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""15"">0</lit:literal></expr></init></decl>;</init> <condition><expr><name pos:line=""2"" pos:column=""18"">i</name> <op:operator pos:line=""2"" pos:column=""20"">&lt;</op:operator> <name pos:line=""2"" pos:column=""22"">bar</name></expr>;</condition> <incr><expr><name pos:line=""2"" pos:column=""27"">i</name><op:operator pos:line=""2"" pos:column=""28"">++</op:operator></expr></incr>) <block pos:line=""2"" pos:column=""32"">{
    <expr_stmt><expr><call><name pos:line=""3"" pos:column=""5"">printf</name><argument_list pos:line=""3"" pos:column=""11"">(<argument><expr><name pos:line=""3"" pos:column=""12"">i</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
  }</block></for>
}</block></function>";
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            var forStmt = globalScope.GetDescendants<ForStatement>().First();
            Assert.AreEqual(1, forStmt.ChildStatements.Count());

            var iDecl = forStmt.Initializer.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault(v => v.Name == "i");
            Assert.IsNotNull(iDecl);
            var iUse = forStmt.ChildStatements[0].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "i");
            Assert.IsNotNull(iUse);
            Assert.AreSame(iDecl, iUse.FindMatches().FirstOrDefault());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestResolveLocalVariable_Parameter(Language lang) {
            //int Foo(int num, bool option) {
            //  if(option) {
            //    printf(num);
            //  }
            //  return 0;
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Foo</name><parameter_list pos:line=""1"" pos:column=""8"">(<param><decl><type><name pos:line=""1"" pos:column=""9"">int</name></type> <name pos:line=""1"" pos:column=""13"">num</name></decl></param>, <param><decl><type><name pos:line=""1"" pos:column=""18"">bool</name></type> <name pos:line=""1"" pos:column=""23"">option</name></decl></param>)</parameter_list> <block pos:line=""1"" pos:column=""31"">{
  <if pos:line=""2"" pos:column=""3"">if<condition pos:line=""2"" pos:column=""5"">(<expr><name pos:line=""2"" pos:column=""6"">option</name></expr>)</condition><then pos:line=""2"" pos:column=""13""> <block pos:line=""2"" pos:column=""14"">{
    <expr_stmt><expr><call><name pos:line=""3"" pos:column=""5"">printf</name><argument_list pos:line=""3"" pos:column=""11"">(<argument><expr><name pos:line=""3"" pos:column=""12"">num</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
  }</block></then></if>
  <return pos:line=""5"" pos:column=""3"">return <expr><lit:literal type=""number"" pos:line=""5"" pos:column=""10"">0</lit:literal></expr>;</return>
}</block></function>";
            XElement xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(xml, "A.cpp");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            var foo = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Foo");
            Assert.AreEqual(2, foo.Parameters.Count);
            var numDecl = foo.Parameters[0];
            Assert.IsNotNull(numDecl);
            var optionDecl = foo.Parameters[1];
            Assert.IsNotNull(optionDecl);

            var optionUse = foo.GetDescendants().SelectMany(s => s.GetExpressions()).SelectMany(e => e.GetDescendantsAndSelf<NameUse>()).FirstOrDefault(n => n.Name == "option");
            Assert.IsNotNull(optionUse);
            Assert.AreSame(optionDecl, optionUse.FindMatches().FirstOrDefault());

            var numUse = foo.GetDescendants().SelectMany(s => s.GetExpressions()).SelectMany(e => e.GetDescendantsAndSelf<NameUse>()).FirstOrDefault(n => n.Name == "num");
            Assert.IsNotNull(numUse);
            Assert.AreSame(numDecl, numUse.FindMatches().FirstOrDefault());
        }
    }
}
