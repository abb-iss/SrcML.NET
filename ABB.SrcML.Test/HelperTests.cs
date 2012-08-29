/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.IO;
using System.Xml.XPath;
namespace ABB.SrcML.Test
{
    [TestFixture]
    [Category("Build")]
    public class HelperTests
    {
        [TearDown]
        public void HelperTestsCleanup()
        {
            if (File.Exists("test.xml"))
                File.Delete("test.xml");
        }

        [Test]
        public void GetXPathExtensionTest()
        {
            File.WriteAllText("test.xml", @"<?xml version=""1.0"" encoding=""utf-8""?>
<unit  xmlns=""http://www.sdml.info/srcML/src"" xmlns:cpp=""http://www.sdml.info/srcML/cpp"">
<unit languageFilter=""C"" filename=""c:\Test\myapp.c"">
<cpp:include>#<cpp:directive>include</cpp:directive> <cpp:file>&lt;stdio.h&gt;</cpp:file></cpp:include>

<function_decl><type><name>void</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>, <param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list>;</function_decl>

<method><type><name>int</name></type> <name>main</name><parameter_list>(<param><decl><type><name>int</name></type> <name>argc</name></decl></param>, <param><decl><type><name>char</name> **</type><name>argv</name></decl></param>)</parameter_list>
<block>{
        <expr_stmt><expr><call><name>foo</name><argument_list>(<argument><expr><call><name>atoi</name><argument_list>(<argument><expr><name><name>argv</name><index>[<expr>1</expr>]</index></name></expr></argument>)</argument_list></call></expr></argument>, <argument><expr><call><name>atoi</name><argument_list>(<argument><expr><name><name>argv</name><index>[<expr>2</expr>]</index></name></expr></argument>)</argument_list></call></expr></argument>)</argument_list></call></expr>;</expr_stmt>
        <expr_stmt><expr><call><name>printf</name><argument_list>(<argument><expr>""Finished with %s and %s""</expr></argument>, <argument><expr><name><name>argv</name><index>[<expr>1</expr>]</index></name></expr></argument>, <argument><expr><name><name>argv</name><index>[<expr>2</expr>]</index></name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
        <return>return <expr>0</expr>;</return>
}</block></method>

<method><type><name>void</name></type> <name>foo</name><parameter_list>(<param><decl><type><name>int</name></type> <name>a</name></decl></param>, <param><decl><type><name>int</name></type> <name>b</name></decl></param>)</parameter_list>
<block>{
        <for>for(<init><decl><type><name>int</name></type> <name>i</name> =<init> <expr><name>a</name></expr></init></decl>;</init> <condition><expr><name>i</name> &lt; <name>b</name></expr>;</condition> <incr><expr><name>i</name>++</expr></incr>)
        <block>{
                <expr_stmt><expr><call><name>printf</name><argument_list>(<argument><expr><name>i</name></expr></argument>)</argument_list></call></expr>;</expr_stmt>
        }</block></for>
}</block></method>
</unit>
<unit languageFilter=""C"" filename=""c:\Test\bar.c""><comment type=""line"">//line1</comment>
<comment type=""line"">//line2</comment>
</unit>
</unit>");
            SrcMLFile doc = new SrcMLFile("test.xml");

            foreach (var unit in doc.FileUnits)
            {
                foreach (var element in unit.Descendants())
                {
                    var xpath = element.GetXPath();
                    
                    // Console.WriteLine("{0}: {1}\n", element.ToSource(), xpath);
                    var elementsFromXPath = unit.XPathSelectElements(xpath, SrcML.NamespaceManager);
                    Assert.AreEqual(1, elementsFromXPath.Count());
                    Assert.AreEqual(element, elementsFromXPath.First());
                }
            }
        }
    }
}
