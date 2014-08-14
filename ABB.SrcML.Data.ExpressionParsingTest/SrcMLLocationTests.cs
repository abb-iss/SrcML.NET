using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class SrcMLLocationTests {
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
            var classLoc = new SrcMLLocation(classElement, "Foo.cs");
            var methodLoc = new SrcMLLocation(methodElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(methodLoc));
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
            var classLoc = new SrcMLLocation(classElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(classLoc));
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
            var methodLoc = new SrcMLLocation(methodElement, "Foo.cs");
            var declLoc = new SrcMLLocation(declElement, "Foo.cs");
            Assert.IsTrue(methodLoc.Contains(declLoc));
        }

        [Test]
        public void TestContains_TwoLevel() {
            ////Example.cs
            //namespace Example {
            //    class Foo {
            //        int Bar(){return 0;}
            //    }
            //}
            var xml = @"<namespace pos:line=""1"" pos:column=""1"">namespace <name pos:line=""1"" pos:column=""11"">Example</name> <block pos:line=""1"" pos:column=""19"">{
    <class pos:line=""2"" pos:column=""5"">class <name pos:line=""2"" pos:column=""11"">Foo</name> <block pos:line=""2"" pos:column=""15"">{
        <function><type><name pos:line=""3"" pos:column=""9"">int</name></type> <name pos:line=""3"" pos:column=""13"">Bar</name><parameter_list pos:line=""3"" pos:column=""16"">()</parameter_list><block pos:line=""3"" pos:column=""18"">{<return pos:line=""3"" pos:column=""19"">return <expr><lit:literal type=""number"" pos:line=""3"" pos:column=""26"">0</lit:literal></expr>;</return>}</block></function>
    }</block></class>
}</block></namespace>";
            var namespaceElement = fileUnitSetup.GetFileUnitForXmlSnippet(xml, "Example.cs").Element(SRC.Namespace);
            var methodElement = namespaceElement.Descendants(SRC.Function).First();
            var namespaceLoc = new SrcMLLocation(namespaceElement, "Example.cs");
            var methodLoc = new SrcMLLocation(methodElement, "Example.cs");
            Assert.IsTrue(namespaceLoc.Contains(methodLoc));
        }

        [Test]
        public void TestGetXElement() {
            var archive = new SrcMLArchive("SrcMLLocationTest", false, new SrcMLGenerator("SrcML"));
            var sourcePath = Path.GetFullPath(@"..\..\TestInputs\class_test.h");
            archive.AddOrUpdateFile(sourcePath);

            var unit = archive.GetXElementForSourceFile(sourcePath);
            Assert.IsNotNull(unit);
            var classElement = unit.Descendants(SRC.Class).FirstOrDefault();
            Assert.IsNotNull(classElement);
            
            var parser = new CPlusPlusCodeParser();
            var globalScope = parser.ParseFileUnit(unit);
            var typeDefinition = globalScope.ChildStatements.OfType<TypeDefinition>().FirstOrDefault();
            Assert.IsNotNull(typeDefinition);

            var element = typeDefinition.PrimaryLocation.GetXElement(archive);
            Assert.IsNotNull(element);
            Assert.AreEqual(classElement.GetSrcLineNumber(), element.GetSrcLineNumber());
            Assert.AreEqual(classElement.GetSrcLinePosition(), element.GetSrcLinePosition());
            Assert.AreEqual(classElement.GetXPath(), element.GetXPath());
        }
    }
}