using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    class SourceLocationTests {
        private SrcMLFileUnitSetup fileUnitSetup;

        [TestFixtureSetUp]
        public void SetUpFixture() {
            fileUnitSetup = new SrcMLFileUnitSetup(Language.CSharp);
        }
        
        [Test]
        public void TestContains_NoSibling() {
            ////Foo.cs
            //class Foo {
            //    int Bar(){return 0;}
            //}
            var xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Foo</name> <block pos:line=""1"" pos:column=""11"">{
    <function><type><name pos:line=""2"" pos:column=""5"">int</name></type> <name pos:line=""2"" pos:column=""9"">Bar</name><parameter_list pos:line=""2"" pos:column=""12"">()</parameter_list><block pos:line=""2"" pos:column=""14"">{<return pos:line=""2"" pos:column=""15"">return <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""22"">0</lit:literal></expr>;</return>}</block></function>
}</block></class>";
            var classElement = fileUnitSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).First();
            var methodElement = classElement.Descendants(SRC.Function).First();
            var classLoc = new SourceLocation(classElement, "Foo.cs");
            var methodLoc = new SourceLocation(methodElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(methodLoc));
        }

        [Test]
        public void TestContains_Sibling() {
            ////Foo.cs
            //class Foo {
            //    string Bar(){
            //        string a = "Hello, world!";
            //        return a;
            //    }
            //    int Baz(){ return 0; }
            //}
            var xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Foo</name> <block pos:line=""1"" pos:column=""11"">{
    <function><type><name pos:line=""2"" pos:column=""5"">string</name></type> <name pos:line=""2"" pos:column=""12"">Bar</name><parameter_list pos:line=""2"" pos:column=""15"">()</parameter_list><block pos:line=""2"" pos:column=""17"">{
        <decl_stmt><decl><type><name pos:line=""3"" pos:column=""9"">string</name></type> <name pos:line=""3"" pos:column=""16"">a</name> =<init pos:line=""3"" pos:column=""19""> <expr><lit:literal type=""string"" pos:line=""3"" pos:column=""20"">""Hello, world!""</lit:literal></expr></init></decl>;</decl_stmt>
        <return pos:line=""4"" pos:column=""9"">return <expr><name pos:line=""4"" pos:column=""16"">a</name></expr>;</return>
    }</block></function>
    <function><type><name pos:line=""6"" pos:column=""5"">int</name></type> <name pos:line=""6"" pos:column=""9"">Baz</name><parameter_list pos:line=""6"" pos:column=""12"">()</parameter_list><block pos:line=""6"" pos:column=""14"">{ <return pos:line=""6"" pos:column=""16"">return <expr><lit:literal type=""number"" pos:line=""6"" pos:column=""23"">0</lit:literal></expr>;</return> }</block></function>
}</block></class>";
            var methodElement = fileUnitSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Function).First();
            var declElement = methodElement.Descendants(SRC.DeclarationStatement).First();
            var methodLoc = new SourceLocation(methodElement, "Foo.cs");
            var declLoc = new SourceLocation(declElement, "Foo.cs");
            Assert.IsTrue(methodLoc.Contains(declLoc));
        }

        [Test]
        public void TestContains_Reflexive() {
            ////Foo.cs
            //class Foo {
            //    int Bar(){return 0;}
            //}
            var xml = @"<class pos:line=""1"" pos:column=""1"">class <name pos:line=""1"" pos:column=""7"">Foo</name> <block pos:line=""1"" pos:column=""11"">{
    <function><type><name pos:line=""2"" pos:column=""5"">int</name></type> <name pos:line=""2"" pos:column=""9"">Bar</name><parameter_list pos:line=""2"" pos:column=""12"">()</parameter_list><block pos:line=""2"" pos:column=""14"">{<return pos:line=""2"" pos:column=""15"">return <expr><lit:literal type=""number"" pos:line=""2"" pos:column=""22"">0</lit:literal></expr>;</return>}</block></function>
}</block></class>";
            var classElement = fileUnitSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs").Descendants(SRC.Class).First();
            var classLoc = new SourceLocation(classElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(classLoc));
        }
    }
}
