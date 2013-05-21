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
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    public class BuiltInTypeFactoryTests {
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
        public void TestJavaBuiltIns() {
            // #a.java
            // TYPE a;
            // TYPE b;
            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtIn in new string[] { "byte", "short", "int", "long", "float", "double", "boolean", "char" }) {
                var aXml = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtIn), "a.java");

                var variableA = CodeParser[Language.Java].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).First(), new ParserContext(aXml)).First();
                var variableB = CodeParser[Language.Java].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).Last(), new ParserContext(aXml)).First();
                
                Assert.AreEqual("a", variableA.Name);
                Assert.AreEqual("b", variableB.Name);
                Assert.AreEqual(builtIn, variableA.VariableType.Name);

                var typeOfA = variableA.VariableType.FindMatches().First();
                var typeOfB = variableB.VariableType.FindMatches().First();
                Assert.AreSame(typeOfA, typeOfB);
            }
        }

        [Test]
        public void TestCppBuiltIns_WithSingleWord() {
            // #a.cpp
            // TYPE a;
            // TYPE b;

            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtIn in new string[] { "char", "short", "int", "long", "bool", "float", "double", "wchar_t" }) {
                var aXml = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtIn), "a.cpp");

                var variableA = CodeParser[Language.CPlusPlus].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).First(), new ParserContext(aXml)).First();
                var variableB = CodeParser[Language.CPlusPlus].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).Last(), new ParserContext(aXml)).First();

                Assert.AreEqual("a", variableA.Name);
                Assert.AreEqual("b", variableB.Name);
                Assert.AreEqual(builtIn, variableA.VariableType.Name);

                var typeOfA = variableA.VariableType.FindMatches().First();
                var typeOfB = variableB.VariableType.FindMatches().First();
                Assert.AreSame(typeOfA, typeOfB);
            }
        }

        [Test]
        [Category("Todo")]
        public void TestCppBuiltIns_WithDoubleWord() {
            // #a.cpp
            // #example: "unsigned int a;";
            // MODIFIER TYPE a;
            // MODIFIER TYPE b;
            string xmlFormat = @"<decl_stmt><decl><type><name>{0}</name> <name>{1}</name></type> <name>a</name></decl>;</decl_stmt>
<decl_stmt><decl><type><name>{0}</name> <name>{1}</name></type> <name>b</name></decl>;</decl_stmt>";

            foreach(var builtInModifier in new string[] { "unsigned", "signed", "long" }) {
                foreach(var builtIn in new string[] { "int", "double" }) {
                    var aXml = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(String.Format(xmlFormat, builtInModifier, builtIn), "a.cpp");

                    var variableA = CodeParser[Language.CPlusPlus].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).First(), new ParserContext(aXml)).First();
                    var variableB = CodeParser[Language.CPlusPlus].ParseDeclarationElement(aXml.Descendants(SRC.Declaration).Last(), new ParserContext(aXml)).First();

                    Assert.AreEqual("a", variableA.Name);
                    Assert.AreEqual("b", variableB.Name);
                    Assert.AreEqual(String.Format("{0} {1}", builtInModifier, builtIn), variableA.VariableType.Name, "TODO: Fix compound types");
                    var typeOfA = variableA.VariableType.FindMatches().First();
                    var typeOfB = variableB.VariableType.FindMatches().First();
                    Assert.AreSame(typeOfA, typeOfB);
                }
            }
        }
    }
}
