using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class GetStatementTests {
        private Dictionary<Language, SrcMLFileUnitSetup> fileSetup;
        private Dictionary<Language, AbstractCodeParser> parser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            parser = new Dictionary<Language, AbstractCodeParser>
                      {
                          {Language.CSharp, new CSharpCodeParser()},
                          {Language.CPlusPlus, new CPlusPlusCodeParser()}
                      };
            fileSetup = new Dictionary<Language, SrcMLFileUnitSetup>
                        {
                            {Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp)},
                            {Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus)}
                        };
        }

        [Test]
        public void TestGlobalStatement() {
            ////Example.cpp
            //char* bar = "Hello, world!";
            //int foo = 42;
            var xml = @"<decl_stmt><decl><type><name pos:line=""1"" pos:column=""1"">char</name><type:modifier pos:line=""1"" pos:column=""5"">*</type:modifier></type> <name pos:line=""1"" pos:column=""7"">bar</name> =<init pos:line=""1"" pos:column=""12""> <expr><lit:literal type=""string"" pos:line=""1"" pos:column=""13"">""Hello, world!""</lit:literal></expr></init></decl>;</decl_stmt>
<decl_stmt><decl><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">foo</name> =<init pos:line=""2"" pos:column=""10""> <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""11"">42</lit:literal></expr></init></decl>;</decl_stmt>";
            var unit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "Example.cpp");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(unit);
            var actual = globalScope.GetStatementForLocation(new SourceLocation("Example.cpp", 2, 5));
            Assert.AreSame(globalScope.ChildStatements[1], actual);
        }

        [Test]
        public void TestLocationInClass_CSharp() {
            ////Foo.cs
            //namespace Example {
            //    class Foo {
            //        int bar = 42;
            //    }
            //}
            var xml = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">Example</name> <block pos:line=""1"" pos:column=""19"">{
    <class pos:line=""2"" pos:column=""5"">class <name pos:line=""2"" pos:column=""11"">Foo</name> <block pos:line=""2"" pos:column=""15"">{
        <decl_stmt><decl><type><name pos:line=""3"" pos:column=""9"">int</name></type> <name pos:line=""3"" pos:column=""13"">bar</name> =<init pos:line=""3"" pos:column=""18""> <expr><lit:literal type=""number"" pos:line=""3"" pos:column=""19"">42</lit:literal></expr></init></decl>;</decl_stmt>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup[Language.CSharp].GetFileUnitForXmlSnippet(xml, "Foo.cs");
            var globalScope = parser[Language.CSharp].ParseFileUnit(unit);

            var foo = globalScope.GetDescendants<TypeDefinition>().First(t => t.Name == "Foo");
            var stmt = foo.ChildStatements[0];
            var stmtActual = globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 3, 14));
            Assert.AreSame(stmt, stmtActual);

            var fooActual = globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 2, 6));
            Assert.AreSame(foo, fooActual);
        }

        [Test]
        public void TestLocationInMain_Cpp() {
            //#include <iostream>
            //
            //char* MyFunction(int foo) {
            //    if(foo > 0) {
            //        return "Hello world!";
            //    } else {
            //        return "Goodbye cruel world!";
            //    }
            //}
            //
            //int main(int argc, char* argv[]) {
            //    std::cout<<MyFunction(42);
            //    return 0;
            //}
            var xml = @"<cpp:include pos:line=""1"" pos:column=""1"">#<cpp:directive pos:line=""1"" pos:column=""2"">include</cpp:directive> <cpp:file pos:line=""1"" pos:column=""10"">&lt;iostream&gt;</cpp:file></cpp:include>

<function><type><name pos:line=""3"" pos:column=""1"">char</name><type:modifier pos:line=""3"" pos:column=""5"">*</type:modifier></type> <name pos:line=""3"" pos:column=""7"">MyFunction</name><parameter_list pos:line=""3"" pos:column=""17"">(<param><decl><type><name pos:line=""3"" pos:column=""18"">int</name></type> <name pos:line=""3"" pos:column=""22"">foo</name></decl></param>)</parameter_list> <block pos:line=""3"" pos:column=""27"">{
    <if pos:line=""4"" pos:column=""5"">if<condition pos:line=""4"" pos:column=""7"">(<expr><name pos:line=""4"" pos:column=""8"">foo</name> <op:operator pos:line=""4"" pos:column=""12"">&gt;</op:operator> <lit:literal type=""number"" pos:line=""4"" pos:column=""14"">0</lit:literal></expr>)</condition><then pos:line=""4"" pos:column=""16""> <block pos:line=""4"" pos:column=""17"">{
        <return pos:line=""5"" pos:column=""9"">return <expr><lit:literal type=""string"" pos:line=""5"" pos:column=""16"">""Hello world!""</lit:literal></expr>;</return>
    }</block></then> <else pos:line=""6"" pos:column=""7"">else <block pos:line=""6"" pos:column=""12"">{
        <return pos:line=""7"" pos:column=""9"">return <expr><lit:literal type=""string"" pos:line=""7"" pos:column=""16"">""Goodbye cruel world!""</lit:literal></expr>;</return>
    }</block></else></if>
}</block></function>

<function><type><name pos:line=""11"" pos:column=""1"">int</name></type> <name pos:line=""11"" pos:column=""5"">main</name><parameter_list pos:line=""11"" pos:column=""9"">(<param><decl><type><name pos:line=""11"" pos:column=""10"">int</name></type> <name pos:line=""11"" pos:column=""14"">argc</name></decl></param>, <param><decl><type><name pos:line=""11"" pos:column=""20"">char</name><type:modifier pos:line=""11"" pos:column=""24"">*</type:modifier></type> <name><name pos:line=""11"" pos:column=""26"">argv</name><index pos:line=""11"" pos:column=""30"">[]</index></name></decl></param>)</parameter_list> <block pos:line=""11"" pos:column=""34"">{
    <expr_stmt><expr><name><name pos:line=""12"" pos:column=""5"">std</name><op:operator pos:line=""12"" pos:column=""8"">::</op:operator><name pos:line=""12"" pos:column=""10"">cout</name></name><op:operator pos:line=""12"" pos:column=""14"">&lt;&lt;</op:operator><call><name pos:line=""12"" pos:column=""16"">MyFunction</name><argument_list pos:line=""12"" pos:column=""26"">(<argument><expr><lit:literal type=""number"" pos:line=""12"" pos:column=""27"">42</lit:literal></expr></argument>)</argument_list></call></expr>;</expr_stmt>
    <return pos:line=""13"" pos:column=""5"">return <expr><lit:literal type=""number"" pos:line=""13"" pos:column=""12"">0</lit:literal></expr>;</return>
}</block></function>";
            var fileUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "function_def.cpp");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(fileUnit);

            var main = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "main");
            Assert.AreSame(main.ChildStatements[0], globalScope.GetStatementForLocation(new SourceLocation("function_def.cpp", 12, 20)));
        }

        [Test]
        public void TestLocationInMethodDefinition_Cpp() {
            ////Foo.h
            //class Foo {
            //public:
            //    int bar(int);
            //}
            var hXml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Foo</name> <block pos:line=""1"" pos:column=""11"">{<private type=""default"" pos:line=""1"" pos:column=""12"">
</private><public pos:line=""2"" pos:column=""1"">public:
    <function_decl><type><name pos:line=""3"" pos:column=""5"">int</name></type> <name pos:line=""3"" pos:column=""9"">bar</name><parameter_list pos:line=""3"" pos:column=""12"">(<param><decl><type><name pos:line=""3"" pos:column=""13"">int</name></type></decl></param>)</parameter_list>;</function_decl>
</public>}</block><decl/></class>";
            var hUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(hXml, "Foo.h");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(hUnit);
            ////Foo.cpp
            //#include "Foo.h"
            //int Foo::bar(int baz) {
            //    return baz + 1;
            //}
            var cppXml = @"<cpp:include pos:line=""1"" pos:column=""1"">#<cpp:directive pos:line=""1"" pos:column=""2"">include</cpp:directive> <cpp:file><lit:literal type=""string"" pos:line=""1"" pos:column=""10"">""Foo.h""</lit:literal></cpp:file></cpp:include>
<function><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name><name pos:line=""2"" pos:column=""5"">Foo</name><op:operator pos:line=""2"" pos:column=""8"">::</op:operator><name pos:line=""2"" pos:column=""10"">bar</name></name><parameter_list pos:line=""2"" pos:column=""13"">(<param><decl><type><name pos:line=""2"" pos:column=""14"">int</name></type> <name pos:line=""2"" pos:column=""18"">baz</name></decl></param>)</parameter_list> <block pos:line=""2"" pos:column=""23"">{
    <return pos:line=""3"" pos:column=""5"">return <expr><name pos:line=""3"" pos:column=""12"">baz</name> <op:operator pos:line=""3"" pos:column=""16"">+</op:operator> <lit:literal type=""number"" pos:line=""3"" pos:column=""18"">1</lit:literal></expr>;</return>
}</block></function>";
            var cppUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(cppXml, "Foo.cpp");
            globalScope = globalScope.Merge(parser[Language.CPlusPlus].ParseFileUnit(cppUnit));

            var bar = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            Assert.AreEqual(bar.ChildStatements[0], globalScope.GetStatementForLocation(new SourceLocation("Foo.cpp", 3, 8)));
        }

        [Test]
        public void TestLocationInForLoop() {
            //for(int i = 0; i < foo.Count; i++) {
            //    Bar(i);
            //}
            var xml = @"<for pos:line=""1"" pos:column=""1"">for(<init><decl><type><name pos:line=""1"" pos:column=""5"">int</name></type> <name pos:line=""1"" pos:column=""9"">i</name> <init pos:line=""1"" pos:column=""11"">= <expr><lit:literal type=""number"" pos:line=""1"" pos:column=""13"">0</lit:literal></expr></init></decl>;</init> <condition><expr><name pos:line=""1"" pos:column=""16"">i</name> <op:operator pos:line=""1"" pos:column=""18"">&lt;</op:operator> <name><name pos:line=""1"" pos:column=""20"">foo</name><op:operator pos:line=""1"" pos:column=""23"">.</op:operator><name pos:line=""1"" pos:column=""24"">Count</name></name></expr>;</condition> <incr><expr><name pos:line=""1"" pos:column=""31"">i</name><op:operator pos:line=""1"" pos:column=""32"">++</op:operator></expr></incr>) <block pos:line=""1"" pos:column=""36"">{
    <expr_stmt><expr><call><name pos:line=""2"" pos:column=""5"">Bar</name><argument_list pos:line=""2"" pos:column=""8"">(<argument><expr><name pos:line=""2"" pos:column=""9"">i</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
}</block></for>";
            var xmlElement = fileSetup[Language.CSharp].GetFileUnitForXmlSnippet(xml, "Foo.cs");
            var globalScope = parser[Language.CSharp].ParseFileUnit(xmlElement);

            var forLoop = globalScope.GetDescendants<ForStatement>().First();
            Assert.AreSame(forLoop, globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 1, 12)));
        }
    }
}