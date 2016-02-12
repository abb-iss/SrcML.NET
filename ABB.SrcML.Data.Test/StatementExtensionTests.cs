/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http:www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            string xml = @"void foo() {
              printf('Hello');
            }
            
            int main() {
              foo();
              return 0;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Foo.cpp");

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

            string xml = @"void star() { }
            
            void bar() { star(); }
            
            void foo() {
                bar();
                if(0) bar();
            }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Foo.cpp");

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

            var xml = @"void foo() { printf('Global foo'); } 
            class Bar {
            public:
              void foo() { printf('Bar::foo'); }
              void baz() { foo(); }
            };";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Foo.cpp");

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

            string xml = @"int Qux() { return 42; }
            int Xyzzy() { return 17; }
            
            void foo() {
              if(Qux()) {
                print(Xyzzy());
              }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcML, "Foo.cpp");

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