using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
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
            var xml = @"class Foo {
                int Bar(){return 0;}
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { }, false);
            var classElement = fileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.cs").Descendants(SRC.Class).First();
            var methodElement = classElement.Descendants(SRC.Function).First();
            var classLoc = new SrcMLLocation(classElement, "Foo.cs");
            var methodLoc = new SrcMLLocation(methodElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(methodLoc));
        }

        [Test]
        public void TestContains_Reflexive() {
            var xml = @"class Foo {
                int Bar(){return 0;}
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { }, false);
            var classElement = fileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.cs").Descendants(SRC.Class).First();
            var classLoc = new SrcMLLocation(classElement, "Foo.cs");
            Assert.IsTrue(classLoc.Contains(classLoc));
        }

        [Test]
        public void TestContains_Sibling() {
            var xml = @"class Foo {
                string Bar(){
                    string a = 'Hello, world!';
                    return a;
                }
                int Baz(){ return 0; }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { }, false);
            var methodElement = fileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Foo.cs").Descendants(SRC.Function).First();
            var declElement = methodElement.Descendants(SRC.DeclarationStatement).First();
            var methodLoc = new SrcMLLocation(methodElement, "Foo.cs");
            var declLoc = new SrcMLLocation(declElement, "Foo.cs");
            Assert.IsTrue(methodLoc.Contains(declLoc));
        }

        [Test]
        public void TestContains_TwoLevel() {
            var xml = @"namespace Example {
                class Foo {
                    int Bar(){return 0;}
                }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "Example.cs", Language.CSharp, new Collection<UInt32>() { }, false);
            var namespaceElement = fileUnitSetup.GetFileUnitForXmlSnippet(srcML, "Example.cs").Element(SRC.Namespace);
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