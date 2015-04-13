using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using ABB.SrcML.Utilities;

namespace ABB.SrcML.Test
{
    [TestFixture]
    [Category("Build")]
    public class SrcDiffFilterTests
    {
        private Dictionary<Language, SrcMLFileUnitSetup> fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            fileSetup = new Dictionary<Language, SrcMLFileUnitSetup> {
                {Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp)},
                {Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus)},
                {Language.Java, new SrcMLFileUnitSetup(Language.Java)}
            };
        }

        [Test]
        public void TestRenameFunction() {
            string beforeXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>a</name></decl></parameter>, <parameter><decl><type><name>BOOL</name></type> <name>b</name></decl></parameter>)</parameter_list>
<block>{
    <return>return <expr><ternary><condition><expr><name>b</name></expr> ?</condition><then> <expr><name>a</name></expr> </then><else>: <expr><name>a</name><operator>-</operator><literal type=""number"">1</literal></expr></else></ternary></expr>;</return>
}</block></function>";
            var beforeUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(beforeXml, "before.cpp");
            var beforeFunc = beforeUnit.Element(SRC.Function);
            
            string afterXml = @"<function><type><name>int</name></type> <name>YourFunction</name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>a</name></decl></parameter>, <parameter><decl><type><name>BOOL</name></type> <name>b</name></decl></parameter>)</parameter_list>
<block>{
    <return>return <expr><ternary><condition><expr><name>b</name></expr> ?</condition><then> <expr><name>a</name></expr> </then><else>: <expr><name>a</name><operator>-</operator><literal type=""number"">1</literal></expr></else></ternary></expr>;</return>
}</block></function>";
            var afterUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(afterXml, "after.cpp");
            var afterFunc = afterUnit.Element(SRC.Function);
            
            string diffXml = @"<function><type><name>int</name></type> <name><diff:delete type=""change"">MyFunction</diff:delete><diff:insert type=""change"">YourFunction</diff:insert></name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>a</name></decl></parameter>, <parameter><decl><type><name>BOOL</name></type> <name>b</name></decl></parameter>)</parameter_list>
<block>{
    <return>return <expr><ternary><condition><expr><name>b</name></expr> ?</condition><then> <expr><name>a</name></expr> </then><else>: <expr><name>a</name><operator>-</operator><literal type=""number"">1</literal></expr></else></ternary></expr>;</return>
}</block></function>";
            var diffUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(diffXml, "before.cpp|after.cpp");
            var diffFunc = diffUnit.Element(SRC.Function);

            var diffBeforeUnit = SrcDiffFilter.GetBeforeVersion(diffUnit);
            var diffBeforeFunc = diffBeforeUnit.Element(SRC.Function);

            Assert.AreNotSame(diffBeforeFunc, beforeFunc);
            Assert.IsTrue(XNode.DeepEquals(beforeFunc, diffBeforeFunc));

            var diffAfterUnit = SrcDiffFilter.GetAfterVersion(diffUnit);
            var diffAfterFunc = diffAfterUnit.Element(SRC.Function);

            Assert.AreNotSame(diffAfterFunc, afterFunc);
            Assert.IsTrue(XNode.DeepEquals(afterFunc, diffAfterFunc));
        }

        [Test]
        public void TestAddConditionBlock() {
            string beforeXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
        <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var beforeUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(beforeXml, "before.cpp");
            var beforeFunc = beforeUnit.Element(SRC.Function);

            string afterXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
        <if>if <condition>(<expr><name>mode</name> <operator>!=</operator> <name>UNDO</name></expr>)</condition><then>
        <block>{
            <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        }</block></then></if>
        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var afterUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(afterXml, "after.cpp");
            var afterFunc = afterUnit.Element(SRC.Function);

            string diffXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
<diff:insert>        <if>if <condition>(<expr><name>mode</name> <operator>!=</operator> <name>UNDO</name></expr>)</condition><then>
        <block>{
    <diff:common>        <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
</diff:common>        }</block></then></if>
</diff:insert>        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var diffUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(diffXml, "before.cpp|after.cpp");
            var diffFunc = diffUnit.Element(SRC.Function);

            var diffBeforeUnit = SrcDiffFilter.GetBeforeVersion(diffUnit);
            var diffBeforeFunc = diffBeforeUnit.Element(SRC.Function);

            Assert.AreNotSame(diffBeforeFunc, beforeFunc);
            Assert.IsTrue(XNode.DeepEquals(beforeFunc, diffBeforeFunc));

            var diffAfterUnit = SrcDiffFilter.GetAfterVersion(diffUnit);
            var diffAfterFunc = diffAfterUnit.Element(SRC.Function);

            Assert.AreNotSame(diffAfterFunc, afterFunc);
            Assert.IsTrue(XNode.DeepEquals(afterFunc, diffAfterFunc));
        }

        [Test]
        public void TestRemoveConditionBlock() {
            string beforeXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
        <if>if <condition>(<expr><name>mode</name> <operator>!=</operator> <name>UNDO</name></expr>)</condition><then>
        <block>{
            <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        }</block></then></if>
        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var beforeUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(beforeXml, "before.cpp");
            var beforeFunc = beforeUnit.Element(SRC.Function);

            string afterXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
        <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var afterUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(afterXml, "after.cpp");
            var afterFunc = afterUnit.Element(SRC.Function);

            string diffXml = @"<function><type><name>int</name></type> <name>MyFunction</name><parameter_list>(<parameter><decl><type><name>char</name><modifier>*</modifier></type> <name>data</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>mode</name></decl></parameter>)</parameter_list>
<block>{
    <if>if <condition>(<expr><name>data</name> <operator>==</operator> <literal type=""string"">""Hello, World""</literal></expr>)</condition><then>
    <block>{
<diff:delete>        <if>if <condition>(<expr><name>mode</name> <operator>!=</operator> <name>UNDO</name></expr>)</condition><then>
        <block>{
    <diff:common>        <expr_stmt><expr><call><name>CancelEvent</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
</diff:common>        }</block></then></if>
</diff:delete>        <expr_stmt><expr><call><name>DoOtherWork</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></then></if>
    <return>return <expr><literal type=""number"">0</literal></expr>;</return>
}</block></function>
";
            var diffUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(diffXml, "before.cpp|after.cpp");
            var diffFunc = diffUnit.Element(SRC.Function);

            var diffBeforeUnit = SrcDiffFilter.GetBeforeVersion(diffUnit);
            var diffBeforeFunc = diffBeforeUnit.Element(SRC.Function);

            Assert.AreNotSame(diffBeforeFunc, beforeFunc);
            Assert.IsTrue(XNode.DeepEquals(beforeFunc, diffBeforeFunc));

            var diffAfterUnit = SrcDiffFilter.GetAfterVersion(diffUnit);
            var diffAfterFunc = diffAfterUnit.Element(SRC.Function);

            Assert.AreNotSame(diffAfterFunc, afterFunc);
            Assert.IsTrue(XNode.DeepEquals(afterFunc, diffAfterFunc));
        }

        [Test]
        public void TestSplitFunction() {
            string beforeXml = @"<constructor><name><name>Foo</name><operator>::</operator><name>Foo</name></name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>bar</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>baz</name></decl></parameter>)</parameter_list> <member_init_list>: <call><name>_bar</name><argument_list>(<argument><expr><name>bar</name></expr></argument>)</argument_list></call>, <call><name>_baz</name><argument_list>(<argument><expr><name>baz</name></expr></argument>)</argument_list></call>
</member_init_list><block>{
    <expr_stmt><expr><call><name>DoWork1</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><call><name>DoWork2</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></constructor>
";
            var beforeUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(beforeXml, "test.cpp");
            var beforeFunc = beforeUnit.Element(SRC.Function);

            string afterXml = @"<constructor><name><name>Foo</name><operator>::</operator><name>Foo</name></name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>bar</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>baz</name></decl></parameter>)</parameter_list> <member_init_list>: <call><name>_bar</name><argument_list>(<argument><expr><name>bar</name></expr></argument>)</argument_list></call>, <call><name>_baz</name><argument_list>(<argument><expr><name>baz</name></expr></argument>)</argument_list></call> </member_init_list><block>{}</block></constructor>

<function><type><name>void</name></type> <name><name>Foo</name><operator>::</operator><name>Initialize</name></name><parameter_list>()</parameter_list>
<block>{
    <expr_stmt><expr><call><name>DoWork1</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><call><name>DoWork2</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></function>
";
            var afterUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(afterXml, "test.cpp");
            var afterFunc = afterUnit.Element(SRC.Function);

            string diffXml = @"<constructor><name><name>Foo</name><operator>::</operator><name>Foo</name></name><parameter_list>(<parameter><decl><type><name>int</name></type> <name>bar</name></decl></parameter>, <parameter><decl><type><name>int</name></type> <name>baz</name></decl></parameter>)</parameter_list> <member_init_list>: <call><name>_bar</name><argument_list>(<argument><expr><name>bar</name></expr></argument>)</argument_list></call>, <call><name>_baz</name><argument_list>(<argument><expr><name>baz</name></expr></argument>)</argument_list></call><diff:delete type=""whitespace"">
</diff:delete><diff:insert type=""whitespace""> </diff:insert></member_init_list><block><diff:delete type=""change"">{
    <expr_stmt><expr><call><name>DoWork1</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><call><name>DoWork2</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</diff:delete><diff:insert type=""change"">{}</diff:insert></block></constructor>
<diff:insert>
<function><type><name>void</name></type> <name><name>Foo</name><operator>::</operator><name>Initialize</name></name><parameter_list>()</parameter_list>
<block>{
    <expr_stmt><expr><call><name>DoWork1</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    <expr_stmt><expr><call><name>DoWork2</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
}</block></function>
</diff:insert>";
            var diffUnit = fileSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(diffXml, "test.cpp");
            var diffFunc = diffUnit.Element(SRC.Function);

            var diffBeforeUnit = SrcDiffFilter.GetBeforeVersion(diffUnit);
            var diffBeforeFunc = diffBeforeUnit.Element(SRC.Function);

            Assert.AreNotSame(diffBeforeUnit, beforeUnit);
            Assert.IsTrue(XNode.DeepEquals(beforeUnit, diffBeforeUnit));

            var diffAfterUnit = SrcDiffFilter.GetAfterVersion(diffUnit);
            var diffAfterFunc = diffAfterUnit.Element(SRC.Function);

            Assert.AreNotSame(diffAfterUnit, afterUnit);
            Assert.IsTrue(XNode.DeepEquals(afterUnit, diffAfterUnit));
        }
    }
}
