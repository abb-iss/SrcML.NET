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
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data.Test
{
    [TestFixture]
    [Category("Build")]
    public class CodeParserTests
    {
        private Dictionary<Language, AbstractCodeParser> codeParsers;
        private Dictionary<Language, SrcMLFileUnitSetup> fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup()
        {
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
        public void TestTwoVariableDeclarations(Language lang)
        {
            int a, b;
            string xml = @"int a,b;";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.cpp", lang, new Collection<UInt32>() { }, false);
            var testUnit = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.cpp");

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
        public void TestField(Language lang)
        {

            string xml = @"class A {
              int Foo;
              Bar baz;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestField_Cpp(Language lang)
        {

            string xml = @"class A {
              int Foo;
              Bar baz;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestMethodCallCreation(Language lang)
        {
            // A.cs

            string xml = @"class A {
                public int Execute() {
                    B b = new B();
                    for(int i = 0; i < b.max(); i++) {
                        try {
                            PrintOutput(b.analyze(i));
                        } catch(Exception e) {
                            PrintError(e.ToString());
                        }
                    }
                }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var fileUnit = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");
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
        public void TestSimpleExpression(Language lang)
        {
            string xml = @"foo = 2+3;";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestSubExpression(Language lang)
        {
            string xml = @"foo = (2+3)*5;";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestGetChildren_Statements(Language lang)
        {
            string xml = @"if(foo == 0) {
              return;
              try {
                return;
              } catch(Exception e) {
                return;
              } 
            } else {
              return;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            Assert.AreEqual(4, globalScope.GetDescendantsAndSelf<ReturnStatement>().Count());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestGetChildren_Expressions(Language lang)
        {
            string xml = @"Foo f = (bar + baz(qux(17))).Xyzzy();";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

            var globalScope = codeParsers[lang].ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            Assert.AreEqual(3, globalScope.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().Count());
        }

        [TestCase(Language.CPlusPlus)]
        [TestCase(Language.Java)]
        [TestCase(Language.CSharp)]
        public void TestResolveLocalVariable(Language lang)
        {
            string xml = @"int Foo() {
              if(MethodCall()) {
                int bar = 17;
                bar = 42;
              }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestResolveLocalVariable_ParentExpression(Language lang)
        {
            string xml = @"int Foo() {
              for(int i = 0; i < bar; i++) {
                printf(i);
              }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.code", lang, new Collection<UInt32>() { }, false);
            var xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "test.code");

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
        public void TestResolveLocalVariable_Parameter(Language lang)
        {
            string xml = @"int Foo(int num, bool option) {
              if(option) {
                printf(num);
              }
              return 0;
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "A.cpp", lang, new Collection<UInt32>() { }, false);
            XElement xmlElement = fileSetup[lang].GetFileUnitForXmlSnippet(srcML, "A.cpp");

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
