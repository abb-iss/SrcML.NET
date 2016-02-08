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
using System.Collections.ObjectModel;

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
            foreach (string prefix in xnm) {
                if (prefix != string.Empty && !prefix.StartsWith("xml", StringComparison.OrdinalIgnoreCase)) {
                    if (prefix.Equals("src", StringComparison.OrdinalIgnoreCase)) {
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
            string testSrcML = @"char* MyClass::foo(int bar) {
    if(bar > GetNumber()) {
        return 'Hello, world!';
    } else {
        return 'Goodbye cruel world!';
    }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(testSrcML, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xml = XElement.Parse(srcMLA, LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Descendants(SRC.Function).First());
            string expected = "char* MyClass::foo(int bar)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetMethodSignature_Whitespace() {
            string testSrcML = @"char* MyClass::foo(
        int bar,
        int baz,
        float xyzzy)
{
    if(bar > GetNumber()) {
        return 'Hello, world!';
    } else {
        return 'Goodbye cruel world!';
    }
}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(testSrcML, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xml = XElement.Parse(srcMLA, LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Descendants(SRC.Function).First());
            string expected = "char* MyClass::foo( int bar, int baz, float xyzzy)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetMethodSignature_InitializerList() {
            string testSrcML = @"MyClass::MyClass(int bar) : _capacity(15), _len(0) {
    if(bar > GetNumber()) {
        return 'Hello, world!';
    } else {
        return 'Goodbye cruel world!';
    }
}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(testSrcML, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xml = XElement.Parse(srcMLA, LoadOptions.PreserveWhitespace);

            string actual = SrcMLElement.GetMethodSignature(xml.Descendants(SRC.Constructor).First());
            string expected = "MyClass::MyClass(int bar)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGetLanguageForUnit_ValidLanguage() {
            string testXml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""{0}"" language=""C++"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>", SRC.NS);

            XElement fileUnit = XElement.Parse(testXml);

            Assert.AreEqual(Language.CPlusPlus, SrcMLElement.GetLanguageForUnit(fileUnit));
        }

        [Test]
        public void TestGetLanguageForUnit_NoLanguage() {
            string testXml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""{0}"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>", SRC.NS);

            XElement fileUnit = XElement.Parse(testXml);

            Assert.Throws<SrcMLException>(() => SrcMLElement.GetLanguageForUnit(fileUnit));
        }

        [Test]
        public void TestGetLanguageForUnit_InvalidLanguage() {
            string testXml = String.Format(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<unit xmlns=""{0}"" language=""C+"" filename=""test.cpp""><expr_stmt><expr></expr></expr_stmt>
</unit>", SRC.NS);

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
