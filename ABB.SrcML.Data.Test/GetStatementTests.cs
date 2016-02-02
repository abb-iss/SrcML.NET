using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var xml = @"char* bar = 'Hello, world!';
            int foo = 42;";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Example.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Example.cpp");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(unit);
            var actual = globalScope.GetStatementForLocation(new SourceLocation("Example.cpp", 2, 5));
            Assert.AreSame(globalScope.ChildStatements[0], actual);
        }

        [Test]
        public void TestLocationInClass_CSharp() {
            var xml = @"namespace Example {
                class Foo {
                    int bar = 42;
                }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcML, "Foo.cs");
            var globalScope = parser[Language.CSharp].ParseFileUnit(unit);

            var foo = globalScope.GetDescendants<TypeDefinition>().First(t => t.Name == "Foo");
            var stmt = foo.ChildStatements[0];
            var stmtActual = globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 3, 14));
            Assert.AreSame(stmt, stmtActual.ChildStatements[0]);

            var fooActual = globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 3, 14));
            Assert.AreSame(foo, fooActual);
        }

        [Test]
        public void TestLocationInMain_Cpp() {
            var xml = @"#include <iostream>
            char* MyFunction(int foo) {
                if(foo > 0) {
                    return 'Hello world!';
                } else {
                    return 'Goodbye cruel world!';
                }
            }
            
            int main(int argc, char* argv[]) {
                std::cout<<MyFunction(42);
                return 0;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "function_def.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "function_def.cpp");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(fileUnit);

            var main = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "main");
            Assert.AreSame(main.ChildStatements[0], globalScope.GetStatementForLocation(new SourceLocation("function_def.cpp", 11, 17)));
        }

        [Test]
        public void TestLocationInMethodDefinition_Cpp() {
            var hXml = @"class Foo {
            public:
                int bar(int);
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(hXml, "Foo.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var hUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Foo.h");
            var globalScope = parser[Language.CPlusPlus].ParseFileUnit(hUnit);

            var cppXml = @"#include 'Foo.h'
            int Foo::bar(int baz) {
                return baz + 1;
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(cppXml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cppUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "Foo.cpp");
            globalScope = globalScope.Merge(parser[Language.CPlusPlus].ParseFileUnit(cppUnit));

            var bar = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            Assert.AreEqual(bar.ChildStatements[0], globalScope.GetStatementForLocation(new SourceLocation("Foo.cpp", 3, 17)));
        }

        [Test]
        public void TestLocationInForLoop() {
            var xml = @"for(int i = 0; i < foo.Count; i++) {
                Bar(i);
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcML, "Foo.cs");
            var globalScope = parser[Language.CSharp].ParseFileUnit(xmlElement);

            var forLoop = globalScope.GetDescendants<ForStatement>().First();
            Assert.AreSame(forLoop, globalScope.GetStatementForLocation(new SourceLocation("Foo.cs", 1, 12)));
        }
    }
}