/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 "http://www.eclipse.org/legal/epl-v10.html"+
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - implementation and documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class CPlusPlusCodeParserTests {
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new CPlusPlusCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CPlusPlus);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {
            string xml = "class A { };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassDeclaration() {
            string xml = "class A;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Class, actual.Kind);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestClassWithDeclaredVariable() {
            string xml = "class A {" +
             "    int a;" +
             "};";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.IsTrue(globalScope.IsGlobal);

            var classA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(classA);
            Assert.AreEqual("A", classA.Name);
            Assert.AreEqual(1, classA.ChildStatements.Count);

            var fieldStmt = classA.ChildStatements.First();
            Assert.IsNotNull(fieldStmt);
            var field = fieldStmt.Content as VariableDeclaration;
            Assert.IsNotNull(field);
            Assert.AreEqual("a", field.Name);
            Assert.AreEqual("int", field.VariableType.Name);
        }



        [Test]
        public void TestFreeStandingBlock() {
            string xml = @"{
             int foo = 42;
             MethodCall(foo);
             }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var firstChild = globalScope.ChildStatements.First();

            Assert.IsInstanceOf<BlockStatement>(firstChild);

            var actual = firstChild as BlockStatement;
            Assert.IsNull(actual.Content);
            Assert.AreEqual(2, actual.ChildStatements.Count);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);
        }

        [Test]
        public void TestExternStatement_Single() {
            string xml = "extern \"C\" int MyGlobalVar;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as ExternStatement;

            Assert.IsNotNull(actual);
            Assert.AreEqual("\"C\"", actual.LinkageType);
            Assert.AreEqual(1, actual.ChildStatements.Count);

        }

        [Test]
        public void TestExternStatement_Block() {
            string xml = "extern \"C\" {\n int globalVar1; \nint globalVar2;}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as ExternStatement;

            Assert.IsNotNull(actual);
            Assert.AreEqual("\"C\"", actual.LinkageType);
            Assert.AreEqual(2, actual.ChildStatements.Count);

        }

        [Test]
        public void TestConstructor_CallToSelf() {
            string xml = @"class MyClass {
            public:
               MyClass() : MyClass(0) { } 
               MyClass(int foo) { } 
            };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.h");
            var globalScope = codeParser.ParseFileUnit(unit);

            var constructors = globalScope.GetDescendants<MethodDefinition>().ToList();
            var defaultConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 0);
            var calledConstructor = constructors.FirstOrDefault(method => method.Parameters.Count == 1);

            Assert.IsNotNull(defaultConstructor);
            Assert.IsNotNull(calledConstructor);
            Assert.AreEqual(1, defaultConstructor.ConstructorInitializers.Count);

            var constructorCall = defaultConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(constructorCall);
            Assert.That(constructorCall.IsConstructor);
            Assert.That(constructorCall.IsConstructorInitializer);
            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructor_CallToSuperClass() {
            string xml = "class SuperClass {" +
            "public:" +
            "SuperClass(int foo) { } }; " +
            "class SubClass : public SuperClass {" +
            "public:" +
            "SubClass(int foo) : SuperClass(foo) { } };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.h");
            var globalScope = codeParser.ParseFileUnit(unit);

            var calledConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "SuperClass" && m.IsConstructor);
            var subClassConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "SubClass" && m.IsConstructor);
            Assert.IsNotNull(subClassConstructor);
            Assert.IsNotNull(calledConstructor);
            Assert.AreEqual(1, subClassConstructor.ConstructorInitializers.Count);

            var constructorCall = subClassConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(constructorCall);
            Assert.That(constructorCall.IsConstructor);
            Assert.That(constructorCall.IsConstructorInitializer);
            Assert.AreSame(calledConstructor, constructorCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructor_InitializeBuiltinTypeField() {
            //"test.h"
            string xml = "class Quux" +
            "{" +
            "    int _my_int;" +
            "public:" +
            "    Quux() : _my_int(5) {  }" +
            "};";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var quux = globalScope.GetNamedChildren<TypeDefinition>("Quux").First();
            var field = quux.GetNamedChildren<VariableDeclaration>("_my_int").First();
            var fieldType = field.VariableType.ResolveType().FirstOrDefault();
            Assert.IsNotNull(fieldType);

            var constructor = quux.GetNamedChildren<MethodDefinition>("Quux").First();
            Assert.AreEqual(1, constructor.ConstructorInitializers.Count);
            var fieldCall = constructor.ConstructorInitializers[0];
            Assert.IsNotNull(fieldCall);
            Assert.That(fieldCall.IsConstructor);
            Assert.That(fieldCall.IsConstructorInitializer);
            Assert.IsEmpty(fieldCall.FindMatches());
        }

        [Test]
        public void TestConstructor_InitializeField() {
            //"test.h"
            string xml = "class Foo" +
             "{" +
             "public:" +
             "    Foo(int a) { }" +
             "};" +
             "class Bar" +
             "{" +
             "    Foo baz;" +
             "public:" +
             "    Bar() : baz(42) { }" +
             "};";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var fooConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Foo" && m.IsConstructor);
            var barConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar" && m.IsConstructor);
            Assert.AreEqual(1, barConstructor.ConstructorInitializers.Count);
            var fieldCall = barConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(fieldCall);
            Assert.That(fieldCall.IsConstructor);
            Assert.That(fieldCall.IsConstructorInitializer);
            Assert.AreSame(fooConstructor, fieldCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            string xml = "using A::Foo;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("Foo", actual.AliasName);
            Assert.AreEqual("A::Foo", actual.Target.ToString());

        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            string xml = "using namespace x::y::z;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x :: y :: z", actual.ImportedNamespace.ToString());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestCreateAliasesForFiles_TypeAlias() {
            string xml = "using x = foo::bar::baz;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x", actual.AliasName, "TODO fix once srcml is updated");
            Assert.AreEqual("foo::bar::baz", actual.Target.ToString());
        }

        [Test]
        public void TestGetImports() {
            //A.cpp
            string xmlA = "namespace x {" +
             "  namespace y {" +
             "    namespace z {}" +
             "  }" +
             "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            //B.cpp
            string xmlB = "using namespace x::y::z;" +
            "foo = 17;";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);
            var foo = globalScope.ChildStatements[2].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());

            var zDef = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(ns => ns.Name == "z");
            Assert.IsNotNull(zDef);
            var zUse = imports[0].ImportedNamespace.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "z");
            Assert.AreSame(zDef, zUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestGetImports_NestedImportNamespace() {
            string xml = "using namespace x::y::z;" +
            "if(bar) {" +
            "  using namespace std;" +
            "  foo = 17;" +
            "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var foo = globalScope.ChildStatements[1].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(2, imports.Count);
            Assert.AreEqual("std", imports[0].ImportedNamespace.ToString());
            Assert.AreEqual("x :: y :: z", imports[1].ImportedNamespace.ToString());
        }

        [Test]
        public void TestGetAliases_NestedImportClass() {
            //A.cpp
            string xmlA = "namespace B {" +
             "  class Bar {}" +
             "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            //B.cpp
            string xmlB = "using namespace x::y::z;" +
             "if(bar) {" +
             "  using B::Bar;" +
             "  foo = 17;" +
             "}";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            var foo = globalScope.ChildStatements[2].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetAliases().ToList();
            Assert.AreEqual(1, aliases.Count);
            Assert.AreEqual("B::Bar", aliases[0].Target.ToString());
            Assert.AreEqual("Bar", aliases[0].AliasName);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());

            var barDef = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(ns => ns.Name == "Bar");
            Assert.IsNotNull(barDef);
            var barUse = aliases[0].Target.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "Bar");
            Assert.AreSame(barDef, barUse.FindMatches().FirstOrDefault());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestGetAliases_NestedTypeAlias() {
            string xml = @"using namespace x::y::z;
             if(bar) {
               using x = foo::bar::baz;
               foo = 17;
             }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var foo = globalScope.ChildStatements[1].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetAliases().ToList();
            Assert.AreEqual(1, aliases.Count);
            string b = aliases[0].Target.ToString();
            Assert.AreEqual("foo :: bar :: baz", aliases[0].Target.ToString());
            Assert.AreEqual("x", aliases[0].AliasName);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual("x :: y :: z", imports[0].ImportedNamespace.ToString());
        }

        [Test]
        public void TestImport_NameResolution() {
            //A.cpp
            string xmlA = "using namespace Foo::Bar;" +
                //
             "namespace A {" +
             "  class Robot {" +
             "  public: " +
             "    Baz GetThingy() { " +
             "      Baz* b = new Baz();" +
             "      return *b;" +
             "    }" +
             "  }" +
             "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            //B.cpp
            string xmlB = "namespace Foo {" +
             "  namespace Bar {" +
             "    class Baz {" +
             "    public:" +
             "      Baz() { }" +
             "  }" +
             "}";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var baz = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "Baz");
            Assert.IsNotNull(baz);

            var thingy = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "GetThingy");
            Assert.IsNotNull(thingy);
            var thingyTypes = thingy.ReturnType.FindMatches().ToList();
            Assert.AreEqual(1, thingyTypes.Count);
            Assert.AreSame(baz, thingyTypes[0]);

            var bazDef = baz.GetNamedChildren<MethodDefinition>("Baz").First();
            var bazCall = thingy.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Baz");
            Assert.IsNotNull(bazCall);
            Assert.AreSame(bazDef, bazCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestAlias_NameResolution_ImportType() {
            //A.cpp
            string xmlA = @"namespace Foo {
              namespace Bar {
                class Baz {
                public:
                  static void DoTheThing() { }
              }
            }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            //B.cpp
            string xmlB = @"using Foo::Bar::Baz;
             namespace A {
               class B {
               public:
                 B() {
                   Baz::DoTheThing();
                 }
               }
             }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var thingDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "DoTheThing");
            Assert.IsNotNull(thingDef);
            Assert.AreEqual("Baz", ((TypeDefinition)thingDef.ParentStatement).Name);

            var bDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "B");
            Assert.IsNotNull(bDef);
            Assert.AreEqual(1, bDef.ChildStatements.Count);
            var thingCall = bDef.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(thingCall);
            Assert.AreSame(thingDef, thingCall.FindMatches().First());
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestAlias_NameResolution_TypeAlias() {
            //A.cpp
            string xmlA = @"namespace Foo {
              namespace Bar {
                class Baz {
                public:
                  static void DoTheThing() { }
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            //B.cpp
            string xmlB = @"using X = Foo::Bar::Baz;
            namespace A {
              class B {
              public:
                B() {
                  X::DoTheThing();
                }
              }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var thingDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "DoTheThing");
            Assert.IsNotNull(thingDef);
            Assert.AreEqual("Baz", ((TypeDefinition)thingDef.ParentStatement).Name);

            var bDef = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "B");
            Assert.IsNotNull(bDef);
            Assert.AreEqual(1, bDef.ChildStatements.Count);
            var thingCall = bDef.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(thingCall);
            Assert.AreSame(thingDef, thingCall.FindMatches().First());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassInNamespace() {
            string xml = "namespace A { class B { }; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(xml, "B.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", namespaceA.Name);
            Assert.IsFalse(namespaceA.IsGlobal);

            Assert.AreEqual("B", typeB.Name);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithMethodDeclaration() {
            string xml = " class A {" +
             " public:" +
             " int foo(int a); };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual(1, typeA.ChildStatements.Count);
            var methodFoo = typeA.ChildStatements.First() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("foo", methodFoo.Name);

            Assert.AreEqual(1, methodFoo.Parameters.Count);
        }

        [Test]
        public void TestCreateTypeDefinition_StaticMethod() {
            string xml = "class Example {" +
            "public:" +
            "    static int Example::Foo(int bar) { return bar+1; }" +
            "};";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLstatic_method = runA.GenerateSrcMLFromString(xml, "static_method.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLstatic_method, "static_method.h");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var example = globalScope.ChildStatements.OfType<TypeDefinition>().FirstOrDefault();
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildStatements.Count());
            var foo = example.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
        }


        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {
            string xml = "int main() { class A { }; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLmain = runA.GenerateSrcMLFromString(xml, "main.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLmain, "main.cpp");
            var mainMethod = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as MethodDefinition;

            Assert.AreEqual("main", mainMethod.Name);

            var typeA = mainMethod.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main.A", typeA.GetFullName());
            Assert.AreEqual(string.Empty, typeA.GetAncestors<NamespaceDefinition>().First().GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            string xml = "class A { class B { }; };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var typeB = typeA.ChildStatements.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentStatement);
            Assert.AreEqual("A", typeA.GetFullName());

            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            string xml = "class A : B,C,D { };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var actual = globalScope.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypeNames.Count);
            Assert.That(globalScope.IsGlobal);
            Assert.AreSame(globalScope, actual.ParentStatement);

            var parentNames = from parent in actual.ParentTypeNames
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a
                );
            foreach (var parentMatchesExpected in tests) {
                Assert.That(parentMatchesExpected);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            string xml = "class D : A::B::C { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLD = runA.GenerateSrcMLFromString(xml, "D.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLD, "D.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("D", actual.Name);
            Assert.AreEqual(1, actual.ParentTypeNames.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parent = actual.ParentTypeNames.First();

            Assert.AreEqual("C", parent.Name);

            var prefixNames = parent.Prefix.Names.ToList();
            Assert.AreEqual(2, prefixNames.Count);
            Assert.AreEqual("A", prefixNames[0].Name);
            Assert.AreEqual("B", prefixNames[1].Name);

        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            string xml = "namespace A { class B { class C { }; }; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeDefinitions = globalScope.GetDescendants<TypeDefinition>().ToList();
            Assert.AreEqual(2, typeDefinitions.Count);

            var outer = typeDefinitions.First();
            var inner = typeDefinitions.Last();

            Assert.AreEqual("B", outer.Name);
            Assert.AreEqual("A", outer.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("A.B", outer.GetFullName());

            Assert.AreEqual("C", inner.Name);
            Assert.AreEqual("A", inner.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("A.B.C", inner.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_Struct() {
            string xml = "struct A { };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;

            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(TypeKind.Struct, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_Union() {
            string xml = @" union A { int a; char b; };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.AreEqual(TypeKind.Union, actual.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestGenericVariableDeclaration() {
            string xml = "vector<int> a;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("a", testDeclaration.Name);
            Assert.AreEqual("vector", testDeclaration.VariableType.Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(1, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.First().Name);
        }

        [Test]
        public void TestGenericVariableDeclarationWithPrefix() {
            string xml = "std::vector<int> a;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("a", testDeclaration.Name);
            Assert.AreEqual("vector", testDeclaration.VariableType.Name);
            Assert.AreEqual(1, testDeclaration.VariableType.Prefix.Names.Count());
            Assert.AreEqual("std", testDeclaration.VariableType.Prefix.Names.First().Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(1, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.First().Name);
        }

        [Test]
        public void TestMethodCallCreation_LengthyCallingExpression() {
            string xml = "a->b.Foo();";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(5, exp.Components.Count);
            var a = exp.Components[0] as NameUse;
            Assert.IsNotNull(a);
            Assert.AreEqual("a", a.Name);
            var arrow = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(arrow);
            Assert.AreEqual("->", arrow.Text);
            var b = exp.Components[2] as NameUse;
            Assert.IsNotNull(b);
            Assert.AreEqual("b", b.Name);
            var dot = exp.Components[3] as OperatorUse;
            Assert.IsNotNull(dot);
            Assert.AreEqual(".", dot.Text);
            var foo = exp.Components[4] as MethodCall;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.Arguments.Count);
            Assert.AreEqual(0, foo.TypeArguments.Count);
        }

        [Test]
        public void TestMergeWithUsing() {
            string headerXml = " namespace A { class B { void Foo(); }; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(headerXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            string implementationXml = @"using namespace A; void B::Foo() { }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLtestc = runB.GenerateSrcMLFromString(implementationXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var headerScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.h"));
            var implementationScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "A.cpp"));

            var globalScope = headerScope.Merge(implementationScope);
            Assert.AreEqual(1, globalScope.ChildStatements.OfType<NamedScope>().Count());

            var namespaceA = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(n => n.Name == "A");
            Assert.IsNotNull(namespaceA);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count);

            var typeB = namespaceA.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            Assert.AreEqual(1, typeB.ChildStatements.Count);

            var methodFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(methodFoo);
            Assert.AreEqual(0, methodFoo.ChildStatements.Count);
            Assert.AreEqual(2, methodFoo.Locations.Count);

            headerScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.h"));
            implementationScope = codeParser.ParseFileUnit(fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "A.cpp"));

            var globalScope_implementationFirst = implementationScope.Merge(headerScope);

            namespaceA = globalScope_implementationFirst.GetDescendants<NamespaceDefinition>().FirstOrDefault(n => n.Name == "A");
            Assert.IsNotNull(namespaceA);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count);

            typeB = namespaceA.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            Assert.AreEqual(1, typeB.ChildStatements.Count);

            methodFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(methodFoo);
            Assert.AreEqual(0, methodFoo.ChildStatements.Count);
            Assert.AreEqual(2, methodFoo.Locations.Count);
        }

        [Test]
        public void TestMethodCallCreation_WithConflictingMethodNames() {
            //# A.h
            string a_xml = "class A {" +
            "    B b;" +
            "public:" +
            "    bool Contains() { b.Contains(); }" +
            "};";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(a_xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            //# B.h
            string b_xml = "class B {" +
            "public:" +
            "    bool Contains() { return true; }" +
            "};";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLtestb = runB.GenerateSrcMLFromString(b_xml, "B.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);


            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(srcMLtestb, "A.h");
            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "B.h");

            var scopeForA = codeParser.ParseFileUnit(fileUnitA);
            var scopeForB = codeParser.ParseFileUnit(fileUnitB);
            var globalScope = scopeForA.Merge(scopeForB);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var classA = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "A");
            Assert.IsNotNull(classA);
            var classB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(classB);

            var aDotContains = classA.GetNamedChildren<MethodDefinition>("Contains").FirstOrDefault();
            Assert.IsNotNull(aDotContains);
            var bDotContains = classB.GetNamedChildren<MethodDefinition>("Contains").FirstOrDefault();
            Assert.IsNotNull(bDotContains);

            Assert.AreEqual(1, aDotContains.ChildStatements.Count);
            var methodCall = aDotContains.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(methodCall);

            Assert.AreSame(bDotContains, methodCall.FindMatches().FirstOrDefault());
            Assert.AreNotSame(aDotContains, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_WithThisKeyword() {
            string a_xml = @"class A {
                void Bar() { }
                class B {
                    int a;
                    void Foo() { this->Bar(); }
                    void Bar() { return this->a; }
                };
            };";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(a_xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            
            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.cpp");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var aDotBar = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(aDotBar);
            var classB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(classB);
            var aDotBDotFoo = classB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(aDotBDotFoo);
            var aDotBDotBar = classB.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(aDotBDotBar);

            Assert.AreEqual(1, aDotBDotFoo.ChildStatements.Count);
            var barCall = aDotBDotFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(barCall);
            Assert.AreSame(aDotBDotBar, barCall.FindMatches().FirstOrDefault());
            Assert.AreNotSame(aDotBar, barCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_GlobalFunction() {
            string xml = "void foo(int a) { printf(a); }" +
            "int main() {" +
            "    foo(5);" +
            "    return 0;" +
            "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").FirstOrDefault();
            var mainMethod = globalScope.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(mainMethod);
            //Assert.AreEqual(2, mainMethod.MethodCalls.Count());

            Assert.AreEqual(2, mainMethod.ChildStatements.Count);

            var fiveCall = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(fiveCall);
            var matches = fiveCall.FindMatches();
            Assert.AreSame(fooMethod, matches.FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_CallGlobalNamespace() {
            string xml = "void Foo() {" +
            "    std::cout<<\"global::Foo\"<<std::endl;" +
            "}" +
            "namespace A" +
            "{" +
            "    void Foo() {" +
            "        std::cout<<\"A::Foo\"<<std::endl;" +
            "    }" +
            "    void print()" +
            "    {" +
            "         Foo();" +
            "         ::Foo();" +
            "    }" +
            "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var globalFoo = globalScope.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            var aFoo = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            var print = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<MethodDefinition>("print").FirstOrDefault();

            Assert.IsNotNull(globalFoo);
            Assert.IsNotNull(aFoo);
            Assert.IsNotNull(print);

            Assert.AreEqual(2, print.ChildStatements.Count);

            var aCall = print.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(aCall);
            var aCallMatches = aCall.FindMatches().ToList();
            //Assert.AreEqual(1, matches.Count);
            Assert.AreSame(aFoo, aCallMatches.FirstOrDefault());

            var globalCall = print.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(globalCall);
            var globalCallMatches = globalCall.FindMatches().ToList();
            Assert.AreEqual(1, globalCallMatches.Count);
            Assert.AreSame(globalFoo, globalCallMatches.FirstOrDefault());
        }

        [Test]
        public void TestMethodCallFindMatches() {
            string headerXml = @"class A { int context;
             public:
             A(); };";

            string implementationXml = "#include \"A.h\" A: :A() {}";

            string mainXml = "#include \"A.h\" int main() { A a = A(); return 0; }";


            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(headerXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLtestb = runB.GenerateSrcMLFromString(implementationXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLtestc = runC.GenerateSrcMLFromString(mainXml, "main.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var headerElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.h");
            var implementationElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtestb, "A.cpp");
            var mainElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "main.cpp");

            var header = codeParser.ParseFileUnit(headerElement);
            var implementation = codeParser.ParseFileUnit(implementationElement);
            var main = codeParser.ParseFileUnit(mainElement);

            var unmergedMainMethod = main.ChildStatements.First() as MethodDefinition;
            Assert.That(unmergedMainMethod.FindExpressions<MethodCall>(true).First().FindMatches(), Is.Empty);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from namedChild in globalScope.ChildStatements.OfType<INamedEntity>()
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.FindExpressions<MethodCall>(true).First();
            var constructor = typeA.ChildStatements.OfType<MethodDefinition>().FirstOrDefault() as MethodDefinition;

            Assert.IsTrue(callInMain.IsConstructor);
            Assert.IsTrue(constructor.IsConstructor);
            Assert.AreSame(constructor, callInMain.FindMatches().First());
        }

        [Test]
        public void TestMethodCallFindMatches_WithArguments() {
            string headerXml = @"class A { int context;
            public:
            A(int value); };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(headerXml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            string implementationXml = "#include \"A.h\" A: :A(int value) { context = value;}";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLtestb = runB.GenerateSrcMLFromString(implementationXml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            string mainXml = "#include \"A.h\" int main() { int startingState = 0; A *a = new A(startingState); return startingState; }";

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLtestc = runC.GenerateSrcMLFromString(mainXml, "main.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var headerElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.h");
            var implementationElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtestb, "A.cpp");
            var mainElement = fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "main.cpp");

            var header = codeParser.ParseFileUnit(headerElement);
            var implementation = codeParser.ParseFileUnit(implementationElement);
            var main = codeParser.ParseFileUnit(mainElement);

            var globalScope = main.Merge(implementation);
            globalScope = globalScope.Merge(header);

            var namedChildren = from namedChild in globalScope.ChildStatements.OfType<INamedEntity>()
                                orderby namedChild.Name
                                select namedChild;

            Assert.AreEqual(2, namedChildren.Count());

            var typeA = namedChildren.First() as TypeDefinition;
            var mainMethod = namedChildren.Last() as MethodDefinition;

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("main", mainMethod.Name);

            var callInMain = mainMethod.FindExpressions<MethodCall>(true).First();
            var constructor = typeA.ChildStatements.OfType<MethodDefinition>().FirstOrDefault() as MethodDefinition;

            Assert.IsTrue(callInMain.IsConstructor);
            Assert.IsTrue(constructor.IsConstructor);
            Assert.AreSame(constructor, callInMain.FindMatches().First());
        }

        [Test]
        public void TestMethodCallMatchToParameter() {
            string xml = "void CallFoo(B b) { b.Foo(); }" +
             "class B { void Foo() { } }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var methodCallFoo = testScope.GetNamedChildren<MethodDefinition>("CallFoo").FirstOrDefault();
            var classB = testScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();

            Assert.IsNotNull(methodCallFoo, "can't find CallFoo");
            Assert.IsNotNull(classB, "can't find class B");

            var bDotFoo = classB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "can't find B.Foo()");

            var callToFoo = methodCallFoo.FindExpressions<MethodCall>(true).FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find a call to Foo()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodDefinition_ReturnType() {
            string xml = "int Foo() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);
            Assert.AreEqual("int", method.ReturnType.Name);
            Assert.AreEqual(0, method.Parameters.Count);
            Assert.IsFalse(method.IsConstructor);
            Assert.IsFalse(method.IsDestructor);
            Assert.IsFalse(method.IsPartial);
        }

        [Test]
        public void TestMethodDefinition_ReturnTypeAndSpecifier() {
            string xml = "static int Foo() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinition_Parameters() {
            string xml = "int Foo(int bar, char baz) { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var foo = testScope.GetNamedChildren<MethodDefinition>("Foo").First();
            Assert.AreEqual("int", foo.ReturnType.Name);
            Assert.AreEqual(2, foo.Parameters.Count);
            Assert.AreEqual("int", foo.Parameters[0].VariableType.Name);
            Assert.AreEqual("bar", foo.Parameters[0].Name);
            Assert.AreEqual("char", foo.Parameters[1].VariableType.Name);
            Assert.AreEqual("baz", foo.Parameters[1].Name);
        }

        [Test]
        public void TestMethodDefinition_VoidParameter() {
            string xml = "void Foo(void) { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual(0, method.Parameters.Count);
        }

        [Test]
        public void TestMethodDefinition_FunctionPointerParameter() {
            string xml = "int Foo(char bar, int (*pInit)(Quux *theQuux)) {}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var foo = testScope.GetNamedChildren<MethodDefinition>("Foo").First();
            Assert.AreEqual("int", foo.ReturnType.Name);
            Assert.AreEqual(2, foo.Parameters.Count);
            Assert.AreEqual("char", foo.Parameters[0].VariableType.Name);
            Assert.AreEqual("bar", foo.Parameters[0].Name);
            Assert.AreEqual("int", foo.Parameters[1].VariableType.Name);
            Assert.AreEqual("pInit", foo.Parameters[1].Name);
        }

        [Test]
        public void TestMethodDefinition_VoidReturn() {
            string xml = "void Foo() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");
            Assert.AreEqual("Foo", method.Name);
            Assert.IsNull(method.ReturnType, "return type should be null");
        }

        [Test]
        public void TestMethodDefinition_DefaultArguments() {
            string xml = "void foo(int a = 0);" +
             "int main() {" +
             "    foo();" +
             "    foo(5);" +
             "    return 0;" +
             "}" +
             "void foo(int a) { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");
            var globalScope = codeParser.ParseFileUnit(unit).Merge(new NamespaceDefinition());
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooMethod = globalScope.GetNamedChildren<MethodDefinition>("foo").FirstOrDefault();
            var mainMethod = globalScope.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(mainMethod);

            Assert.AreEqual(3, mainMethod.ChildStatements.Count);
            var defaultCall = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(defaultCall);
            Assert.AreSame(fooMethod, defaultCall.FindMatches().FirstOrDefault());

            var fiveCall = mainMethod.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(fiveCall);
            Assert.AreSame(fooMethod, fiveCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTwoVariableDeclarations() {
            string testXml = "int a,b;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(testXml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            var declStmt = globalScope.ChildStatements.First();
            var varDecls = declStmt.Content.Components.OfType<VariableDeclaration>().ToList();

            Assert.AreEqual(2, varDecls.Count);
            Assert.AreEqual("a", varDecls[0].Name);
            Assert.AreEqual("int", varDecls[0].VariableType.Name);
            Assert.AreEqual("b", varDecls[1].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[1].VariableType);
        }

        [Test]
        public void TestThreeVariableDeclarations() {
            string testXml = "int a,b,c;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(testXml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);

            var declStmt = globalScope.ChildStatements.First();
            var varDecls = declStmt.Content.Components.OfType<VariableDeclaration>().ToList();

            Assert.AreEqual(3, varDecls.Count);
            Assert.AreEqual("a", varDecls[0].Name);
            Assert.AreEqual("int", varDecls[0].VariableType.Name);
            Assert.AreEqual("b", varDecls[1].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[1].VariableType);
            Assert.AreEqual("c", varDecls[2].Name);
            Assert.AreSame(varDecls[0].VariableType, varDecls[2].VariableType);
        }

        [Test]
        public void TestVariablesWithSpecifiers() {
            string testXml = "const int A;" +
            "static int B;" +
            "static const Foo C;" +
            "extern Foo D;";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtest = runA.GenerateSrcMLFromString(testXml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtest, "test.cpp");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(4, globalScope.ChildStatements.Count);

            var declA = globalScope.ChildStatements[0].Content as VariableDeclaration;
            Assert.IsNotNull(declA);
            Assert.AreEqual("A", declA.Name);
            Assert.AreEqual("int", declA.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declA.Accessibility);

            var declB = globalScope.ChildStatements[1].Content as VariableDeclaration;
            Assert.IsNotNull(declB);
            Assert.AreEqual("B", declB.Name);
            Assert.AreEqual("int", declB.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declB.Accessibility);

            var declC = globalScope.ChildStatements[2].Content as VariableDeclaration;
            Assert.IsNotNull(declC);
            Assert.AreEqual("C", declC.Name);
            Assert.AreEqual("Foo", declC.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declC.Accessibility);

            var declD = globalScope.ChildStatements[3].Content as VariableDeclaration;
            Assert.IsNotNull(declD);
            Assert.AreEqual("D", declD.Name);
            Assert.AreEqual("Foo", declD.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declD.Accessibility);
        }

        [Test]
        public void TestLiteralUse() {
            string xml = "a = 17; foo = \"watermelon\"; if(true) { c = 'h';}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var numLit = globalScope.ChildStatements[0].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(numLit);
            Assert.AreEqual("17", numLit.Text);
            Assert.AreEqual(LiteralKind.Number, numLit.Kind);

            var stringLit = globalScope.ChildStatements[1].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(stringLit);
            Assert.AreEqual("\"watermelon\"", stringLit.Text);
            Assert.AreEqual(LiteralKind.String, stringLit.Kind);

            var ifStmt = globalScope.ChildStatements[2] as IfStatement;
            Assert.IsNotNull(ifStmt);

            var boolLit = ifStmt.Condition as LiteralUse;
            Assert.IsNotNull(boolLit);
            Assert.AreEqual("true", boolLit.Text);
            Assert.AreEqual(LiteralKind.Boolean, boolLit.Kind);

            var charLit = ifStmt.ChildStatements[0].Content.GetDescendantsAndSelf<LiteralUse>().FirstOrDefault();
            Assert.IsNotNull(charLit);
            Assert.AreEqual("\'h\'", charLit.Text);
            Assert.AreEqual(LiteralKind.Character, charLit.Kind);
        }

        [Test]
        public void TestIfElse() {
            string xml = "if(a==b) {" +
             "  i = 17;" +
             "} else {" +
             "  i = 42;" +
             "  ReportError();" +
             "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var ifStmt = globalScope.ChildStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt);
            Assert.IsNull(ifStmt.Content);
            Assert.IsNotNull(ifStmt.Condition);
            Assert.AreEqual(1, ifStmt.ChildStatements.Count);
            Assert.AreEqual(2, ifStmt.ElseStatements.Count);
        }
        /// <summary>
        /// Test is testing for proper nesting of else and if in srcML archive.
        /// </summary>
        [Test]
        public void TestIfElseIf() {
            string xml = @"if(a==b) {
              i = 17;
            } else if(a==c) {
              i = 42;
              foo();
            } else {
              ReportError();
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var ifStmt = globalScope.ChildStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt);
            Assert.IsNull(ifStmt.Content);
            Assert.IsNotNull(ifStmt.Condition);
            Assert.AreEqual(1, ifStmt.ChildStatements.Count);
            Assert.AreEqual(2, ifStmt.ElseStatements.Count);

            var ifStmt2 = ifStmt.ElseStatements.First() as IfStatement;
            Assert.IsNotNull(ifStmt2);
            Assert.IsNull(ifStmt2.Content);
            Assert.IsNotNull(ifStmt2.Condition);
            Assert.AreEqual(2, ifStmt2.ChildStatements.Count);
            Assert.AreEqual(0, ifStmt2.ElseStatements.Count);
        }

        [Test]
        public void TestEmptyStatement() {
            string xml = ";";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.h");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var actual = globalScope.ChildStatements[0];
            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.ChildStatements.Count);
            Assert.IsNull(actual.Content);
        }

        [Test]
        public void TestVariableUse_Index() {
            string xml = "foo.bar[17];";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLa = runA.GenerateSrcMLFromString(xml, "a.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLa, "a.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);
            var exp = globalScope.ChildStatements[0].Content;
            Assert.IsNotNull(exp);
            Assert.AreEqual(3, exp.Components.Count);
            var foo = exp.Components[0] as NameUse;
            Assert.IsNotNull(foo);
            Assert.AreEqual("foo", foo.Name);
            var op = exp.Components[1] as OperatorUse;
            Assert.IsNotNull(op);
            Assert.AreEqual(".", op.Text);
            var bar = exp.Components[2] as VariableUse;
            Assert.IsNotNull(bar);
            Assert.AreEqual("bar", bar.Name);
            var index = bar.Index as LiteralUse;
            Assert.IsNotNull(index);
            Assert.AreEqual("17", index.Text);
            Assert.AreEqual(LiteralKind.Number, index.Kind);
        }

        [Test]
        public void TestResolveVariable_Field() {
            string xml = "class A {" +
            "public:" +
            "  int Foo;" +
            "  A() { Foo = 42; }" +
            "};";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(1, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_FieldInParent() {
            string xml = "class B {" +
            "public:" +
            "  int Foo;" +
            "};" +
            "class A : public B {" +
            "public:" +
            "  A() { Foo = 42; }" +
            "};";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("B").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(1, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_Global() {
            string xml = "int Foo;" +
             "int Bar() {" +
             "  Foo = 17;" +
             "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<VariableDeclaration>("Foo").First();
            var bar = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            var fooUse = bar.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_VarInNamespace() {
            string xml = "namespace A {" +
            " int Foo;" +
            " int Bar() {" +
            "   Foo = 17;" +
            " }" +
            "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<NamespaceDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var bar = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "Bar");
            Assert.AreEqual(1, bar.ChildStatements.Count);
            var fooUse = bar.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_Masking() {
            string xml = "int foo = 17;" +
            "int main(int argc, char** argv)" +
            "{" +
            "    std::cout<<foo<<std::endl;" +
            "    float foo = 42.0;" +
            "    std::cout<<foo<<std::endl;" +
            "    return 0;" +
            "}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);
            var globalFoo = globalScope.GetNamedChildren<VariableDeclaration>("foo").First();
            var main = globalScope.GetNamedChildren<MethodDefinition>("main").First();
            Assert.AreEqual(4, main.ChildStatements.Count);

            var globalFooUse = main.ChildStatements[0].Content.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "foo");
            var globalFooUseMatches = globalFooUse.FindMatches().ToList();
            Assert.AreEqual(1, globalFooUseMatches.Count);
            Assert.AreSame(globalFoo, globalFooUseMatches[0]);

            var localFoo = main.GetNamedChildren<VariableDeclaration>("foo").First();
            var localFooUse = main.ChildStatements[2].Content.GetDescendantsAndSelf<NameUse>().First(n => n.Name == "foo");
            var localFooUseMatches = localFooUse.FindMatches().ToList();
            Assert.AreEqual(1, localFooUseMatches.Count);
            Assert.AreSame(localFoo, localFooUseMatches[0]);
        }

        [Test]
        public void TestVariableDeclaredInCallingObjectWithParentClass() {
            string a_xml = "class A { B b; };";

            string b_xml = "class B { void Foo() { } };";

            string c_xml = "class C : A { };";

            string d_xml = @"class D {
              C c;
              void Bar() { c.b.Foo(); }
            };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLtesta = runA.GenerateSrcMLFromString(a_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLtestb = runB.GenerateSrcMLFromString(b_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLtestc = runC.GenerateSrcMLFromString(c_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLtestd = runD.GenerateSrcMLFromString(d_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var aUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtesta, "A.h");
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtestb, "B.h");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "C.h");
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtestd, "D.h");

            var globalScope = codeParser.ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(bUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(cUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(dUnit));

            var typeA = globalScope.GetNamedChildren<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetNamedChildren<TypeDefinition>("C").FirstOrDefault();
            var typeD = globalScope.GetNamedChildren<TypeDefinition>("D").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");
            Assert.IsNotNull(typeD, "could not find class D");

            var adotB = typeA.GetNamedChildren<VariableDeclaration>("b").FirstOrDefault();
            Assert.IsNotNull(adotB, "could not find variable A.b");
            Assert.AreEqual("b", adotB.Name);

            var bDotFoo = typeB.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(bDotFoo, "could not method B.Foo()");

            var dDotBar = typeD.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(dDotBar, "could not find method D.Bar()");

            var callToFoo = dDotBar.FindExpressions<MethodCall>(true).FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in D.Bar()");

            Assert.AreEqual(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveArrayVariable_Local() {
            string xml = "int Foo() {" +
            "  if(MethodCall()) {" +
            "    int* bar = malloc(SIZE);" +
            "    bar[0] = 42;" +
            "  }" +
            "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLa = runA.GenerateSrcMLFromString(xml, "a.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLa, "a.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var ifStmt = globalScope.GetDescendants<IfStatement>().First();
            Assert.AreEqual(2, ifStmt.ChildStatements.Count());

            var barDecl = ifStmt.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault(v => v.Name == "bar");
            Assert.IsNotNull(barDecl);
            var barUse = ifStmt.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "bar");
            Assert.IsNotNull(barUse);
            Assert.AreSame(barDecl, barUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveArrayVariable_Field() {
            string xml = "class A {" +
            "public:" +
            "  char* Foo;" +
            "  A() { " +
            "    Foo = malloc(SIZE);" +
            "    Foo[17] = 'x';" +
            "  }" +
            "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("A").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(2, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[1].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveCallOnArrayVariable() {
            //#include <iostream>
            string xml = "const int SIZE = 5;" +
            "class Foo {" +
            "public:" +
            "    int GetNum() { return 42; }" +
            "};" +
            "class Bar {" +
            "public:" +
            "    Foo FooArray[SIZE];" +
            "};" +
            "int main(int argc, char** argv) {" +
            "    Bar myBar;" +
            "    std::cout<< myBar.FooArray[0].GetNum() << std::endl;" +
            "    return 0;" +
            "}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cpp");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var getNum = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "GetNum");
            Assert.IsNotNull(getNum);
            var main = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(main);
            Assert.AreEqual(3, main.ChildStatements.Count);

            var getNumCall = main.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().First(mc => mc.Name == "GetNum");
            var matches = getNumCall.FindMatches().ToList();
            Assert.AreEqual(1, matches.Count);
            Assert.AreSame(getNum, matches.First());
        }

        [Test]
        public void TestTypeUseForOtherNamespace() {
            string c_xml = @"namespace A {
                namespace B {
                    class C {
                        int Foo() { }
                    };
                }
            }";

            string e_xml = @"using namespace A::B;
            namespace D {
                class E {
                    void main() {
                        C c = new C();
                        c.Foo();
                    }
                };
            }";

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLtestc= runC.GenerateSrcMLFromString(c_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLtestc, "C.cpp");

            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLteste = runE.GenerateSrcMLFromString(e_xml, "test.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var eUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLteste, "E.cpp");

            NamespaceDefinition globalScope = codeParser.ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(eUnit));

            var typeC = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.FindExpressions<VariableDeclaration>(true).FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.ResolveType().FirstOrDefault());

            var callToCConstructor = mainMethod.FindExpressions<MethodCall>(true).FirstOrDefault();
            var callToFoo = mainMethod.FindExpressions<MethodCall>(true).LastOrDefault();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }
    }
}