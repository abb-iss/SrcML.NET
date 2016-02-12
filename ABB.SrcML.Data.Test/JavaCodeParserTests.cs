/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http:www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
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
    internal class JavaCodeParserTests {
        private AbstractCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new JavaCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.Java);
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportClass() {
            string xml = @"import x.y.z;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("z", actual.AliasName);
            Assert.AreEqual("x . y . z", actual.Target.ToString());
        }

        [Test]
        public void TestCreateAliasesForFiles_ImportNamespace() {
            string xml = @"import x . /*test */ y  /*test */ . z .* /*test*/;";
            //<import>import <name><name>x</name> <op:operator>.</op:operator> <comment type=""block"">/*test */</comment> <name>y</name>  <comment type=""block"">/*test */</comment> <op:operator>.</op:operator> <name>z</name></name> .* <comment type=""block"">/*test*/</comment>;</import>";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x . y . z", actual.ImportedNamespace.ToString());
        }

        [Test]
        public void TestCreateTypeDefinition_ClassInPackage() {
            string xml = @"package A.B.C;
            public class D { }";
            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLA = runD.GenerateSrcMLFromString(xml, "D.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "D.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.IsTrue(globalScope.IsGlobal);
            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var packageA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageA);
            Assert.AreEqual("A", packageA.Name);
            Assert.IsFalse(packageA.IsGlobal);
            Assert.AreEqual(1, packageA.ChildStatements.Count());

            var packageAB = packageA.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageAB);
            Assert.AreEqual("B", packageAB.Name);
            Assert.IsFalse(packageAB.IsGlobal);
            Assert.AreEqual(1, packageAB.ChildStatements.Count());

            var packageABC = packageAB.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(packageABC);
            Assert.AreEqual("C", packageABC.Name);
            Assert.IsFalse(packageABC.IsGlobal);
            Assert.AreEqual(1, packageABC.ChildStatements.Count());

            var typeD = packageABC.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeD, "Type D is not a type definition");
            Assert.AreEqual("D", typeD.Name);
        }

        [Test]
        public void TestCreateTypeDefinitions_Class() {

            string xml = @"class A { }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.IsTrue(globalScope.IsGlobal);

            var actual = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(0, actual.ChildStatements.Count);
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassInFunction() {

            string xml = @"class A { int foo() { class B { } } }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var fooMethod = typeA.ChildStatements.First() as MethodDefinition;
            var typeB = fooMethod.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreSame(typeA, fooMethod.ParentStatement);
            Assert.AreEqual("A.foo", fooMethod.GetFullName());
            Assert.AreSame(fooMethod, typeB.ParentStatement);
            Assert.AreEqual("A.foo.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithExtendsAndImplements() {
            string xml = @"public class Foo extends xyzzy implements A, B, C {
                public int bar;
            }";

            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.IsNotNull(globalNamespace);
            Assert.AreEqual("Foo", actual.Name);
            Assert.AreEqual(4, actual.ParentTypeNames.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parentNames = from parent in actual.ParentTypeNames
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "xyzzy", "A", "B", "C" }, parentNames, (e, a) => e == a);
            foreach (var test in tests) {
                Assert.That(test);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            string xml = @"class A { class B { } }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            var typeB = typeA.ChildStatements.First() as TypeDefinition;

            Assert.AreSame(typeA, typeB.ParentStatement);
            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithParents() {
            string xml = @"class A implements B,C,D { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(3, actual.ParentTypeNames.Count);
            Assert.That(globalNamespace.IsGlobal);

            var parentNames = from parent in actual.ParentTypeNames
                              select parent.Name;

            var tests = Enumerable.Zip<string, string, bool>(new[] { "B", "C", "D" }, parentNames, (e, a) => e == a);
            foreach (var test in tests) {
                Assert.That(test);
            }
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithQualifiedParent() {
            string xml = @"class D implements A.B.C { }";

            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLA = runD.GenerateSrcMLFromString(xml, "D.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "D.java");

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
        public void TestCreateTypeDefinitions_ClassWithSuperClass() {

            string xml = @"public class Foo extends xyzzy {
                public int bar;
            }";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var actual = codeParser.ParseFileUnit(xmlElement).ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(actual);
            Assert.AreEqual("Foo", actual.Name);
            Assert.AreEqual(1, actual.ParentTypeNames.Count);
            Assert.AreEqual("xyzzy", actual.ParentTypeNames.First().Name);
            var globalNamespace = actual.ParentStatement as NamespaceDefinition;
            Assert.IsNotNull(globalNamespace);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {

            string xml = @"package A; class B { class C { } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLA = runB.GenerateSrcMLFromString(xml, "B.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "B.java");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeDefinitions = globalScope.GetDescendants<TypeDefinition>().ToList();
            Assert.AreEqual(2, typeDefinitions.Count());

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
        public void TestCreateTypeDefinitions_Interface() {
            string xml = @"interface A { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var actual = codeParser.ParseFileUnit(xmlElement);
            var actual2 = actual.ChildStatements.First() as TypeDefinition;
            var globalNamespace = actual as NamespaceDefinition;

            Assert.AreEqual("A", actual2.Name);
            Assert.AreEqual(TypeKind.Interface, actual2.Kind);
            Assert.That(globalNamespace.IsGlobal);
        }

        [Test]
        public void TestFieldCreation() {
            string xml = @"class A { int B; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var typeA = globalScope.ChildStatements.First();
            Assert.AreEqual(1, typeA.ChildStatements.Count);

            var fieldB = typeA.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(fieldB);
            Assert.AreEqual("B", fieldB.Name);
            Assert.AreEqual("int", fieldB.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, fieldB.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_None() {

            string xml = @"public class Foo {
                bool Bar() { return true; }
            }";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.None, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_Normal() {

            string xml = @"public class Foo {
                public bool Bar() { return true; }
            }";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_Static() {

            string xml = @"public class Foo {
                static public bool Bar() { return true; }
            }";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_None() {
            string xml = @"class Foo {}";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.None, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Normal() {

            string xml = @"public class Foo {}";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Static() {
            string xml = @"static public class Foo {}";
            LibSrcMLRunner runFoo = new LibSrcMLRunner();
            string srcMLA = runFoo.GenerateSrcMLFromString(xml, "Foo.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "Foo.java");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }



        [Test]
        public void TestMethodCallCreation_WithConflictingMethodNames() {
            string a_xml = @"class A {
                B b;
                boolean Contains() { b.Contains(); }
            }";


            string b_xml = @"class B {
                boolean Contains() { return true; }
            }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a_xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(b_xml, "B.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.java");

            var scopeForA = codeParser.ParseFileUnit(fileUnitA);
            var scopeForB = codeParser.ParseFileUnit(fileUnitB);
            var globalScope = scopeForA.Merge(scopeForB);

            var containsMethods = globalScope.GetDescendants<MethodDefinition>().Where(m => m.Name == "Contains").ToList();

            var aDotContains = containsMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "A");
            Assert.IsNotNull(aDotContains);
            var bDotContains = containsMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "B");
            Assert.IsNotNull(bDotContains);
            Assert.AreEqual("A.Contains", aDotContains.GetFullName());
            Assert.AreEqual("B.Contains", bDotContains.GetFullName());

            Assert.AreEqual(1, aDotContains.ChildStatements.Count);
            var containsCall = aDotContains.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(containsCall);
            Assert.AreSame(bDotContains, containsCall.FindMatches().First());
            Assert.AreNotSame(aDotContains, containsCall.FindMatches().First());
        }

        [Test]
        public void TestMethodCallCreation_WithThisKeyword() {
            string a_xml = @"class A {
                void Bar() { }
                class B {
                    int a;
                    void Foo() { this.Bar(); }
                    void Bar() { return this.a; }
                }
            }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a_xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");
            var globalScope = codeParser.ParseFileUnit(fileUnit);

            var aDotBar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar" && ((TypeDefinition)m.ParentStatement).Name == "A");
            Assert.IsNotNull(aDotBar);
            var aDotBDotFoo = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(aDotBDotFoo);
            var aDotBDotBar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar" && ((TypeDefinition)m.ParentStatement).Name == "B");
            Assert.IsNotNull(aDotBDotBar);

            Assert.AreEqual("A.Bar", aDotBar.GetFullName());
            Assert.AreEqual("A.B.Foo", aDotBDotFoo.GetFullName());
            Assert.AreEqual("A.B.Bar", aDotBDotBar.GetFullName());

            Assert.AreEqual(1, aDotBDotFoo.ChildStatements.Count);
            var barCall = aDotBDotFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(barCall);
            var matches = barCall.FindMatches().ToList();
            Assert.AreEqual(1, matches.Count);
            Assert.AreSame(aDotBDotBar, matches.First());
        }

        [Test]
        public void TestMethodCallCreation_WithSuperKeyword() {
            string xml = @"class B {
              public void Foo() { } 
            }
            class C extends B { 
              public void Foo() { }
              public void Bar() { 
                super.Foo(); 
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooMethods = globalScope.GetDescendants<MethodDefinition>().ToList();

            var bDotFoo = fooMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "B");
            Assert.IsNotNull(bDotFoo);
            var cDotFoo = fooMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "C");
            Assert.IsNotNull(cDotFoo);

            var bar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "Bar");
            Assert.IsNotNull(bar);
            Assert.AreEqual(1, bar.ChildStatements.Count);
            var methodCall = bar.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(methodCall);
            Assert.AreSame(bDotFoo, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_SuperConstructor() {
            string xml = @"class B {
              public B(int num) { }
            }
            class C extends B { 
              public C() {
                super(17);
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var bConstructor = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "B");
            Assert.IsNotNull(bConstructor);
            var cConstructor = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "C");
            Assert.IsNotNull(cConstructor);

            Assert.AreEqual(1, cConstructor.ChildStatements.Count);
            var superCall = cConstructor.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(superCall);
            Assert.IsTrue(superCall.IsConstructor);
            Assert.AreSame(bConstructor, superCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallCreation_ConstructorFromOtherNamespace() {
            string c_xml = @"package A.B;
            class C {
              public C() { }
            }";

            string e_xml = @"package A.D;
            import A.B.*;
            class E {
              public void main() {
                  C c = new C();
              }
            }";

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLA = runC.GenerateSrcMLFromString(c_xml, "C.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "C.java");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(e_xml, "E.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var eUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLE, "E.java");

            var cScope = codeParser.ParseFileUnit(cUnit);
            var eScope = codeParser.ParseFileUnit(eUnit);

            var globalScope = cScope.Merge(eScope);

            var typeC = (from typeDefinition in globalScope.GetDescendantsAndSelf<TypeDefinition>()
                         where typeDefinition.Name == "C"
                         select typeDefinition).First();

            var typeE = (from typeDefinition in globalScope.GetDescendantsAndSelf<TypeDefinition>()
                         where typeDefinition.Name == "E"
                         select typeDefinition).First();

            Assert.IsNotNull(typeC, "Could not find class C");
            Assert.IsNotNull(typeE, "Could not find class E");

            var constructorForC = typeC.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();

            Assert.IsNotNull(constructorForC, "C has no methods");
            Assert.AreEqual("C", constructorForC.Name);
            Assert.That(constructorForC.IsConstructor);

            var eDotMain = typeE.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(eDotMain, "could not find main method in E");
            Assert.AreEqual("main", eDotMain.Name);

            var callToC = (from stmt in eDotMain.GetDescendants()
                           from expr in stmt.GetExpressions()
                           from call in expr.GetDescendantsAndSelf<MethodCall>()
                           select call).FirstOrDefault();
            Assert.IsNotNull(callToC, "main contains no calls");
            Assert.AreEqual("C", callToC.Name);
            Assert.That(callToC.IsConstructor);

            Assert.AreEqual(constructorForC, callToC.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariablesWithSpecifiers() {

            string testXml = @"public static int A;
            public final int B;
            private static final Foo C;";
            LibSrcMLRunner runtest = new LibSrcMLRunner();
            string srcMLA = runtest.GenerateSrcMLFromString(testXml, "test.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "test.java");

            var globalScope = codeParser.ParseFileUnit(testUnit);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);

            var declA = globalScope.ChildStatements[0].Content as VariableDeclaration;
            Assert.IsNotNull(declA);
            Assert.AreEqual("A", declA.Name);
            Assert.AreEqual("int", declA.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, declA.Accessibility);

            var declB = globalScope.ChildStatements[1].Content as VariableDeclaration;
            Assert.IsNotNull(declB);
            Assert.AreEqual("B", declB.Name);
            Assert.AreEqual("int", declB.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, declB.Accessibility);

            var declC = globalScope.ChildStatements[2].Content as VariableDeclaration;
            Assert.IsNotNull(declC);
            Assert.AreEqual("C", declC.Name);
            Assert.AreEqual("Foo", declC.VariableType.Name);
            Assert.AreEqual(AccessModifier.Private, declC.Accessibility);
        }

        [Test]
        public void TestImport_NameResolution() {
            string xmlA = @"import Foo.Bar.*;
            package A;
            public class Robot {
              public Baz GetThingy() { return new Baz(); }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");

            string xmlB = @"package Foo.Bar;
            public class Baz {
              public Baz() { }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.java");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

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
        public void TestAlias_NameResolution() {
            string xmlA = @"package Foo.Bar;
            public class Baz {
              public static void DoTheThing() { };
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");
            string xmlB = @"import Foo.Bar.Baz;
            package A;
            public class B {
              public B() {
                Baz.DoTheThing();
              }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.java");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

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
        public void BasicParentTest_Java() {
            string a_xml = @"class A implements B { }";
            string b_xml = @"class B { }";
            string c_xml = @"class C { A a; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a_xml, "A.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.java");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(b_xml, "B.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.java");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(c_xml, "C.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitC = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.java");

            var globalScope = codeParser.ParseFileUnit(fileUnitA);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(fileUnitB));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(fileUnitC));

            Assert.AreEqual(3, globalScope.ChildStatements.Count());

            var typeDefinitions = globalScope.GetDescendants<TypeDefinition>().OrderBy(t => t.Name).ToList();

            var typeA = typeDefinitions[0];
            var typeB = typeDefinitions[1];
            var typeC = typeDefinitions[2];

            Assert.AreEqual("B", typeB.Name);

            var cDotA = (from stmt in typeC.GetDescendants()
                         from decl in stmt.GetExpressions().OfType<VariableDeclaration>()
                         select decl).FirstOrDefault();
            var parentOfA = typeA.ParentTypeNames.FirstOrDefault();
            Assert.That(cDotA.VariableType.FindMatches().FirstOrDefault(), Is.SameAs(typeA));
            Assert.That(parentOfA.FindMatches().FirstOrDefault(), Is.SameAs(typeB));
        }

        [Test]
        public void TestTypeUseForOtherNamespace() {
            string c_xml = @"package A.B;
            class C {
                int Foo();
            }";

            string e_xml = @"package D;
            import A.B.*;
            class E {
                public static void main() {
                    C c = new C();
                    c.Foo();
                }
            }";

            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLA = runC.GenerateSrcMLFromString(c_xml, "C.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "C.java");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(e_xml, "E.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var eUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLE, "E.java");

            var globalScope = codeParser.ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(eUnit));

            var typeC = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.ChildStatements.OfType<MethodDefinition>().FirstOrDefault();
            Assert.IsNotNull(fooMethod, "no method foo found");
            Assert.AreEqual("Foo", fooMethod.Name);

            var cDeclaration = mainMethod.FindExpressions<VariableDeclaration>(true).FirstOrDefault();
            Assert.IsNotNull(cDeclaration, "No declaration found");
            Assert.AreSame(typeC, cDeclaration.VariableType.ResolveType().FirstOrDefault());

            var callToCConstructor = mainMethod.FindExpressions<MethodCall>(true).First();
            var callToFoo = mainMethod.FindExpressions<MethodCall>(true).Last();

            Assert.AreEqual("C", callToCConstructor.Name);
            Assert.That(callToCConstructor.IsConstructor);
            Assert.IsNull(callToCConstructor.FindMatches().FirstOrDefault());

            Assert.AreEqual("Foo", callToFoo.Name);
            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
        }
    }
}