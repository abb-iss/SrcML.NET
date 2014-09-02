/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using ABB.SrcML;

namespace ABB.SrcML.Test {
    /// <summary>
    /// Tests for ABB.SrcML.SrcMLElement
    /// </summary>
    [TestFixture]
    [Category("Build")]
    public class SrcMLElementTests {
        private string srcMLFormat;

        [TestFixtureSetUp]
        public void ClassSetup() {
            //construct the necessary srcML wrapper unit tags
            XmlNamespaceManager xnm = SrcML.NamespaceManager;
            StringBuilder namespaceDecls = new StringBuilder();
            foreach(string prefix in xnm) {
                if(prefix != string.Empty && !prefix.StartsWith("xml", StringComparison.OrdinalIgnoreCase)) {
                    if(prefix.Equals("src", StringComparison.OrdinalIgnoreCase)) {
                        namespaceDecls.AppendFormat("xmlns=\"{0}\" ", xnm.LookupNamespace(prefix));
                    } else {
                        namespaceDecls.AppendFormat("xmlns:{0}=\"{1}\" ", prefix, xnm.LookupNamespace(prefix));
                    }
                }
            }
            srcMLFormat = string.Format("<unit {0}>{{0}}</unit>", namespaceDecls.ToString());
        }

        [Test]
        public void TestGetMethodSignature_Normal() {
            string testSrcML = @"<function><type><name>char</name><type:modifier>*</type:modifier></type> <name><name>MyClass</name><op:operator>::</op:operator><name>foo</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>bar</name></decl></param>)</parameter_list> <block>{
    <if>if<condition>(<expr><name>bar</name> <op:operator>&gt;</op:operator> <call><name>GetNumber</name><argument_list>()</argument_list></call></expr>)</condition><then> <block>{
        <return>return <expr><lit:literal type=""string"">""Hello, world!""</lit:literal></expr>;</return>
    }</block></then> <else>else <block>{
        <return>return <expr><lit:literal type=""string"">""Goodbye cruel world!""</lit:literal></expr>;</return>
    }</block></else></if>
}</block></function>";
            XElement xml = XElement.Parse(string.Format(srcMLFormat, testSrcML), LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Element(SRC.Function));
            string expected = "char* MyClass::foo(int bar)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetMethodSignature_Whitespace() {
            string testSrcML = @"<function><type><name>char</name><type:modifier>*</type:modifier></type> <name><name>MyClass</name><op:operator>::</op:operator><name>foo</name></name><parameter_list>(
	<param><decl><type><name>int</name></type> <name>bar</name></decl></param>,
	<param><decl><type><name>int</name></type> <name>baz</name></decl></param>,
	<param><decl><type><name>float</name></type> <name>xyzzy</name></decl></param>)</parameter_list> 
<block>{
    <if>if<condition>(<expr><name>bar</name> <op:operator>&gt;</op:operator> <call><name>GetNumber</name><argument_list>()</argument_list></call></expr>)</condition><then> <block>{
        <return>return <expr><lit:literal type=""string"">""Hello, world!""</lit:literal></expr>;</return>
    }</block></then> <else>else <block>{
        <return>return <expr><lit:literal type=""string"">""Goodbye cruel world!""</lit:literal></expr>;</return>
    }</block></else></if>
}</block></function>";
            XElement xml = XElement.Parse(string.Format(srcMLFormat, testSrcML), LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Element(SRC.Function));
            string expected = "char* MyClass::foo( int bar, int baz, float xyzzy)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetMethodSignature_InitializerList() {
            string testSrcML = @"<constructor><name><name>MyClass</name><op:operator>::</op:operator><name>MyClass</name></name><parameter_list>(<param><decl><type><name>int</name></type> <name>bar</name></decl></param>)</parameter_list> <member_list>: <call><name>_capacity</name><argument_list>(<argument><expr><lit:literal type=""number"">15</lit:literal></expr></argument>)</argument_list></call>, <call><name>_len</name><argument_list>(<argument><expr><lit:literal type=""number"">0</lit:literal></expr></argument>)</argument_list></call> </member_list><block>{
    <if>if<condition>(<expr><name>bar</name> <op:operator>&gt;</op:operator> <call><name>GetNumber</name><argument_list>()</argument_list></call></expr>)</condition><then> <block>{
        <return>return <expr><lit:literal type=""string"">""Hello, world!""</lit:literal></expr>;</return>
    }</block></then> <else>else <block>{
        <return>return <expr><lit:literal type=""string"">""Goodbye cruel world!""</lit:literal></expr>;</return>
    }</block></else></if>
}</block></constructor>";
            XElement xml = XElement.Parse(string.Format(srcMLFormat, testSrcML), LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Element(SRC.Constructor));
            string expected = "MyClass::MyClass(int bar)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetLanguageForUnit_ValidLanguage() {
            string testXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""http://www.sdml.info/srcML/src"" language=""C++"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>";

            XElement fileUnit = XElement.Parse(testXml);

            Assert.AreEqual(Language.CPlusPlus, SrcMLElement.GetLanguageForUnit(fileUnit));
        }

        [Test]
        public void TestGetLanguageForUnit_NoLanguage() {
            string testXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""http://www.sdml.info/srcML/src"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>";

            XElement fileUnit = XElement.Parse(testXml);

            Assert.Throws<SrcMLException>(() => SrcMLElement.GetLanguageForUnit(fileUnit));
        }

        [Test]
        public void TestGetLanguageForUnit_InvalidLanguage() {
            string testXml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""http://www.sdml.info/srcML/src"" language=""C+"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>";

            XElement fileUnit = XElement.Parse(testXml);

            Assert.Throws<SrcMLException>(() => SrcMLElement.GetLanguageForUnit(fileUnit));
        }

        [Test]
        public void TestGetLanguageForUnit_InvalidArgument() {
            string testXml = @"<function><type><name>int</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            XElement fileUnit = XElement.Parse(testXml);

            Assert.Throws<ArgumentException>(() => SrcMLElement.GetLanguageForUnit(fileUnit));

            fileUnit = null;
            Assert.Throws<ArgumentNullException>(() => SrcMLElement.GetLanguageForUnit(fileUnit));
        }
    }
}
