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
        public void TestGetCallsTo() {
            //void foo() {
            //    bar();
            //    if(0) bar();
            //}
            //
            //void bar() { star(); }
            //
            //void star() { }
            string xml = @"<function><type><name>void</name></type> <name>foo</name><parameter_list>()</parameter_list> <block>{
    <expr_stmt><expr><call><name>bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <if>if<condition>(<expr><lit:literal type=""number"">0</lit:literal></expr>)</condition><then> <expr_stmt><expr><call><name>bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt></then></if>
}</block></function>

<function><type><name>void</name></type> <name>bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name>star</name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>

<function><type><name>void</name></type> <name>star</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var unit = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(xml, "test.cpp");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(unit);

            var methodFoo = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault();
            var methodBar = globalScope.GetDescendants<MethodDefinition>().Skip(1).FirstOrDefault();
            var methodStar = globalScope.GetDescendants<MethodDefinition>().LastOrDefault();

            Assert.IsNotNull(methodFoo, "could not find method foo");
            Assert.IsNotNull(methodBar, "could not find method bar");
            Assert.IsNotNull(methodStar, "could not find method star");

            Assert.AreEqual("foo", methodFoo.Name);
            Assert.AreEqual("bar", methodBar.Name);
            Assert.AreEqual("star", methodStar.Name);

            Assert.That(methodFoo.ContainsCallTo(methodBar));
            Assert.AreEqual(2, methodFoo.GetCallsTo(methodBar, true).Count());

            Assert.That(methodBar.ContainsCallTo(methodStar));
            Assert.AreEqual(1, methodBar.GetCallsTo(methodStar, true).Count());

            Assert.IsFalse(methodFoo.ContainsCallTo(methodStar));
            Assert.IsFalse(methodBar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodFoo));
            Assert.IsFalse(methodStar.ContainsCallTo(methodBar));
        }
    }
}