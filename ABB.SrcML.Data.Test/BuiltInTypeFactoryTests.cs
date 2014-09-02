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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class BuiltInTypeFactoryTests {
        private Dictionary<Language, AbstractCodeParser> CodeParser;
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;

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
        [Category("Todo")]
        public void TestCppBuiltIns_WithDoubleWord() {
            // #a.cpp #example: "unsigned int a;"; MODIFIER TYPE a; MODIFIER TYPE b;
            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name> <name>{1}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name> <name>{1}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtInModifier in new string[] { "unsigned", "signed", "long" }) {
                foreach(var builtIn in new string[] { "int", "double" }) {
                    var aXml = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtInModifier, builtIn), "a.cpp");

                    var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(aXml);
                    var variables = from stmt in globalScope.GetDescendants()
                                    from declaration in stmt.GetExpressions().OfType<VariableDeclaration>()
                                    select declaration;
                    var variableA = variables.FirstOrDefault();
                    var variableB = variables.LastOrDefault();
                    
                    Assert.AreEqual("a", variableA.Name);
                    Assert.AreEqual("b", variableB.Name);
                    Assert.AreEqual(String.Format("{0} {1}", builtInModifier, builtIn), variableA.VariableType.Name, "TODO: Fix compound types");
                    var typeOfA = variableA.VariableType.FindMatches().First();
                    var typeOfB = variableB.VariableType.FindMatches().First();
                    Assert.AreSame(typeOfA, typeOfB);
                }
            }
        }

        [Test]
        public void TestCppBuiltIns_WithSingleWord() {
            // #a.cpp TYPE a; TYPE b;

            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtIn in new string[] { "char", "short", "int", "long", "bool", "float", "double", "wchar_t" }) {
                var aXml = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtIn), "a.cpp");

                var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(aXml);
                var variables = from stmt in globalScope.GetDescendants()
                                from declaration in stmt.GetExpressions().OfType<VariableDeclaration>()
                                select declaration;
                var variableA = variables.FirstOrDefault();
                var variableB = variables.LastOrDefault();

                Assert.AreEqual("a", variableA.Name);
                Assert.AreEqual("b", variableB.Name);
                Assert.AreEqual(builtIn, variableA.VariableType.Name);

                var typeOfA = variableA.VariableType.FindMatches().First();
                var typeOfB = variableB.VariableType.FindMatches().First();
                Assert.AreSame(typeOfA, typeOfB);
            }
        }

        [Test]
        public void TestJavaBuiltIns() {
            // #a.java TYPE a; TYPE b;
            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtIn in new string[] { "byte", "short", "int", "long", "float", "double", "boolean", "char" }) {
                var aXml = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtIn), "a.java");

                var globalScope = CodeParser[Language.Java].ParseFileUnit(aXml);
                var variables = from stmt in globalScope.GetDescendants()
                                from declaration in stmt.GetExpressions().OfType<VariableDeclaration>()
                                select declaration;
                var variableA = variables.FirstOrDefault();
                var variableB = variables.LastOrDefault();

                
                Assert.AreEqual(builtIn, variableA.VariableType.Name);

                var typeOfA = variableA.VariableType.FindMatches().First();
                var typeOfB = variableB.VariableType.FindMatches().First();
                Assert.AreSame(typeOfA, typeOfB);
            }
        }
    }
}