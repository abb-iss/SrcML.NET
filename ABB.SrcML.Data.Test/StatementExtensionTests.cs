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

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class StatementExtensionTests {
        private Dictionary<Language, AbstractCodeParser> CodeParser;
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;

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
        public void TestGetCallsTo_Simple() {
            //void foo() {
            //  printf("Hello");
            //}
            //
            //int main() {
            //  foo();
            //  return 0;
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">void</name></type> <name pos:line=""1"" pos:column=""6"">foo</name><parameter_list pos:line=""1"" pos:column=""9"">()</parameter_list> <block pos:line=""1"" pos:column=""12"">{
  <expr_stmt><expr><call><name pos:line=""2"" pos:column=""3"">printf</name><argument_list pos:line=""2"" pos:column=""9"">(<argument><expr><lit:literal type=""string"" pos:line=""2"" pos:column=""10"">""Hello""</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt>
}</block></function>

<function><type><name pos:line=""5"" pos:column=""1"">int</name></type> <name pos:line=""5"" pos:column=""5"">main</name><parameter_list pos:line=""5"" pos:column=""9"">()</parameter_list> <block pos:line=""5"" pos:column=""12"">{
  <expr_stmt><expr><call><name pos:line=""6"" pos:column=""3"">foo</name><argument_list pos:line=""6"" pos:column=""6"">()</argument_list></call></expr>;</expr_stmt>
  <return pos:line=""7"" pos:column=""3"">return <expr><lit:literal type=""number"" pos:line=""7"" pos:column=""10"">0</lit:literal></expr>;</return>
}</block></function>";
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "foo.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlElement);
            var fooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").First();
            var mainMethod = globalScope.GetNamedChildren<MethodDefinition>("main").First();

            Assert.That(mainMethod.ContainsCallTo(fooMethod));
            var fooCalls = mainMethod.GetCallsTo(fooMethod, true).ToList();
            Assert.AreEqual(1, fooCalls.Count);
            var expectedFooCall = mainMethod.FindExpressions<MethodCall>(true).First(mc => mc.Name == "foo");
            Assert.AreSame(expectedFooCall, fooCalls[0]);

            var callsToFoo = fooMethod.GetCallsToSelf().ToList();
            Assert.AreEqual(1, callsToFoo.Count);
            Assert.AreSame(expectedFooCall, callsToFoo[0]);
        }

        [Test]
        public void TestGetCallsTo_Multiple() {
            //void star() { }
            //
            //void bar() { star(); }
            //
            //void foo() {
            //    bar();
            //    if(0) bar();
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">void</name></type> <name pos:line=""1"" pos:column=""6"">star</name><parameter_list pos:line=""1"" pos:column=""10"">()</parameter_list> <block pos:line=""1"" pos:column=""13"">{ }</block></function>

<function><type><name pos:line=""3"" pos:column=""1"">void</name></type> <name pos:line=""3"" pos:column=""6"">bar</name><parameter_list pos:line=""3"" pos:column=""9"">()</parameter_list> <block pos:line=""3"" pos:column=""12"">{ <expr_stmt><expr><call><name pos:line=""3"" pos:column=""14"">star</name><argument_list pos:line=""3"" pos:column=""18"">()</argument_list></call></expr>;</expr_stmt> }</block></function>

<function><type><name pos:line=""5"" pos:column=""1"">void</name></type> <name pos:line=""5"" pos:column=""6"">foo</name><parameter_list pos:line=""5"" pos:column=""9"">()</parameter_list> <block pos:line=""5"" pos:column=""12"">{
    <expr_stmt><expr><call><name pos:line=""6"" pos:column=""5"">bar</name><argument_list pos:line=""6"" pos:column=""8"">()</argument_list></call></expr>;</expr_stmt>
    <if pos:line=""7"" pos:column=""5"">if<condition pos:line=""7"" pos:column=""7"">(<expr><lit:literal type=""number"" pos:line=""7"" pos:column=""8"">0</lit:literal></expr>)</condition><then pos:line=""7"" pos:column=""10""> <expr_stmt><expr><call><name pos:line=""7"" pos:column=""11"">bar</name><argument_list pos:line=""7"" pos:column=""14"">()</argument_list></call></expr>;</expr_stmt></then></if>
}</block></function>";

            var unit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "test.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(unit);

            var methodFoo = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "foo");
            var methodBar = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "bar");
            var methodStar = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "star");

            Assert.That(methodFoo.ContainsCallTo(methodBar));
            Assert.AreEqual(2, methodFoo.GetCallsTo(methodBar, true).Count());

            Assert.That(methodBar.ContainsCallTo(methodStar));
            Assert.AreEqual(1, methodBar.GetCallsTo(methodStar, true).Count());

            Assert.IsFalse(methodFoo.ContainsCallTo(methodStar));
            Assert.IsFalse(methodBar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodBar));
        }

        [Test]
        public void TestGetCallsTo_Masking() {
            //void foo() { printf("Global foo"); }
            //
            //class Bar {
            //public:
            //  void foo() { printf("Bar::foo"); }
            //  void baz() { foo(); }
            //};
            var xml = @"<function><type><name pos:line=""1"" pos:column=""1"">void</name></type> <name pos:line=""1"" pos:column=""6"">foo</name><parameter_list pos:line=""1"" pos:column=""9"">()</parameter_list> <block pos:line=""1"" pos:column=""12"">{ <expr_stmt><expr><call><name pos:line=""1"" pos:column=""14"">printf</name><argument_list pos:line=""1"" pos:column=""20"">(<argument><expr><lit:literal type=""string"" pos:line=""1"" pos:column=""21"">""Global foo""</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt> }</block></function>

<class pos:line=""3"" pos:column=""1"">class <name pos:line=""3"" pos:column=""7"">Bar</name> <block pos:line=""3"" pos:column=""11"">{<private type=""default"" pos:line=""3"" pos:column=""12"">
</private><public pos:line=""4"" pos:column=""1"">public:
  <function><type><name pos:line=""5"" pos:column=""3"">void</name></type> <name pos:line=""5"" pos:column=""8"">foo</name><parameter_list pos:line=""5"" pos:column=""11"">()</parameter_list> <block pos:line=""5"" pos:column=""14"">{ <expr_stmt><expr><call><name pos:line=""5"" pos:column=""16"">printf</name><argument_list pos:line=""5"" pos:column=""22"">(<argument><expr><lit:literal type=""string"" pos:line=""5"" pos:column=""23"">""Bar::foo""</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt> }</block></function>
  <function><type><name pos:line=""6"" pos:column=""3"">void</name></type> <name pos:line=""6"" pos:column=""8"">baz</name><parameter_list pos:line=""6"" pos:column=""11"">()</parameter_list> <block pos:line=""6"" pos:column=""14"">{ <expr_stmt><expr><call><name pos:line=""6"" pos:column=""16"">foo</name><argument_list pos:line=""6"" pos:column=""19"">()</argument_list></call></expr>;</expr_stmt> }</block></function>
</public>}</block>;</class>";
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "Bar.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlElement);
            var globalFooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").First();
            var bar = globalScope.GetNamedChildren<TypeDefinition>("Bar").First();
            var barFooMethod = bar.GetNamedChildren<MethodDefinition>("foo").First();
            var bazMethod = bar.GetNamedChildren<MethodDefinition>("baz").First();

            Assert.That(bazMethod.ContainsCallTo(barFooMethod));
            Assert.IsFalse(bazMethod.ContainsCallTo(globalFooMethod));
            var fooCalls = bazMethod.GetCallsTo(barFooMethod, true).ToList();
            Assert.AreEqual(1, fooCalls.Count);
            var expectedFooCall = bazMethod.FindExpressions<MethodCall>(true).First(mc => mc.Name == "foo");
            Assert.AreSame(expectedFooCall, fooCalls[0]);

            Assert.IsEmpty(globalFooMethod.GetCallsToSelf());
            Assert.AreEqual(1, barFooMethod.GetCallsToSelf().Count());
        }

        [Test]
        public void TestGetCallsTo_NonRecursive() {
            //int Qux() { return 42; }
            //int Xyzzy() { return 17; }
            //
            //void foo() {
            //  if(Qux()) {
            //    print(Xyzzy());
            //  }
            //}
            string xml = @"<function><type><name pos:line=""1"" pos:column=""1"">int</name></type> <name pos:line=""1"" pos:column=""5"">Qux</name><parameter_list pos:line=""1"" pos:column=""8"">()</parameter_list> <block pos:line=""1"" pos:column=""11"">{ <return pos:line=""1"" pos:column=""13"">return <expr><lit:literal type=""number"" pos:line=""1"" pos:column=""20"">42</lit:literal></expr>;</return> }</block></function>
<function><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">Xyzzy</name><parameter_list pos:line=""2"" pos:column=""10"">()</parameter_list> <block pos:line=""2"" pos:column=""13"">{ <return pos:line=""2"" pos:column=""15"">return <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""22"">17</lit:literal></expr>;</return> }</block></function>

<function><type><name pos:line=""4"" pos:column=""1"">void</name></type> <name pos:line=""4"" pos:column=""6"">foo</name><parameter_list pos:line=""4"" pos:column=""9"">()</parameter_list> <block pos:line=""4"" pos:column=""12"">{
  <if pos:line=""5"" pos:column=""3"">if<condition pos:line=""5"" pos:column=""5"">(<expr><call><name pos:line=""5"" pos:column=""6"">Qux</name><argument_list pos:line=""5"" pos:column=""9"">()</argument_list></call></expr>)</condition><then pos:line=""5"" pos:column=""12""> <block pos:line=""5"" pos:column=""13"">{
    <expr_stmt><expr><call><name pos:line=""6"" pos:column=""5"">print</name><argument_list pos:line=""6"" pos:column=""10"">(<argument><expr><call><name pos:line=""6"" pos:column=""11"">Xyzzy</name><argument_list pos:line=""6"" pos:column=""16"">()</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
  }</block></then></if>
}</block></function>";
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "foo.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlElement);
            var quxMethod = globalScope.GetNamedChildren<MethodDefinition>("Qux").First();
            var xyzzyMethod = globalScope.GetNamedChildren<MethodDefinition>("Xyzzy").First();
            var ifStmt = globalScope.GetDescendants<IfStatement>().First();

            Assert.That(ifStmt.ContainsCallTo(quxMethod));
            Assert.That(ifStmt.ContainsCallTo(xyzzyMethod));

            Assert.AreEqual(1, ifStmt.GetCallsTo(quxMethod, false).Count());
            Assert.AreEqual(0, ifStmt.GetCallsTo(xyzzyMethod, false).Count());
        }
    }
}