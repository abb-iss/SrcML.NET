using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    class GetScopeTests {
        private Dictionary<Language, AbstractCodeParser> parser;
        private Dictionary<Language,SrcMLFileUnitSetup> fileSetup;

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
        public void TestGlobal() {
            ////Example.cpp
            //char* bar = "Hello, world!";
            //int foo = 42;
            var xml = @"<decl_stmt><decl><type><name pos:line=""1"" pos:column=""1"">char</name><type:modifier pos:line=""1"" pos:column=""5"">*</type:modifier></type> <name pos:line=""1"" pos:column=""7"">bar</name> =<init pos:line=""1"" pos:column=""12""> <expr><lit:literal type=""string"" pos:line=""1"" pos:column=""13"">""Hello, world!""</lit:literal></expr></init></decl>;</decl_stmt>
<decl_stmt><decl><type><name pos:line=""2"" pos:column=""1"">int</name></type> <name pos:line=""2"" pos:column=""5"">foo</name> =<init pos:line=""2"" pos:column=""10""> <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""11"">42</lit:literal></expr></init></decl>;</decl_stmt>";
            var unit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "Example.cpp");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(unit);
            var actual = globalScope.GetScopeForLocation(new SourceLocation("Example.cpp", 2, 5, 2, 5, "", false));
            Assert.AreEqual(globalScope, actual);
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
            var expected = globalScope.ChildScopes.First().ChildScopes.First();
            var actual = globalScope.GetScopeForLocation(new SourceLocation("Foo.cs", 3, 14, 3, 14, "", true));
            Assert.AreEqual(expected, actual);
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
            NamedScope globalScope = parser[Language.CPlusPlus].ParseFileUnit(hUnit);
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

            var bar = globalScope.ChildScopes.First().ChildScopes.OfType<MethodDefinition>().First();
            Assert.AreEqual("bar", bar.Name);
            Assert.AreEqual(bar, globalScope.GetScopeForLocation(new SourceLocation("Foo.cpp", 3, 2, 3, 2, "", true)));
        }
    }
}
