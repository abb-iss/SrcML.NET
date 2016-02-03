/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ABB.SrcML.Test.Utilities;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    public class MergeTests {
        private Dictionary<Language, AbstractCodeParser> CodeParser;
        private Dictionary<Language, SrcMLFileUnitSetup> FileUnitSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            FileUnitSetup = new Dictionary<Language, SrcMLFileUnitSetup>() {
                { Language.CPlusPlus, new SrcMLFileUnitSetup(Language.CPlusPlus) },
                { Language.Java, new SrcMLFileUnitSetup(Language.Java) },
                { Language.CSharp, new SrcMLFileUnitSetup(Language.CSharp) }
            };
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
                { Language.CSharp, new CSharpCodeParser() }
            };
        }

        [Test]
        public void TestConstructorMerge_Cpp() {
            string header_xml = @"class A { A(); };";

            string impl_xml = @"A::A() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(header_xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.h");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(impl_xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLE, "A.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(header);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(implementation);

            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);

            var constructor = typeA.ChildStatements.First() as MethodDefinition;
            Assert.That(constructor.IsConstructor);
            Assert.IsFalse(constructor.IsDestructor);
            Assert.IsFalse(constructor.IsPartial);
            Assert.AreEqual(AccessModifier.Private, constructor.Accessibility);
            Assert.AreEqual("A.cpp", constructor.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestNestedConstructorMerge_Cpp() {
            string headerXml = @"class Foo 
            {
            public:
                Foo(int, int, char);
                virtual ~Foo();
                struct Bar
                {
                    Bar(float, float);
                    virtual ~Bar();
                }
            };";

            string implXml = @"Foo::Bar::Bar(float a, float b) { }
            Foo::Bar::~Bar() { }
            
            Foo::Foo(int a, int b, char c) { }
            Foo::~Foo() { }";
            LibSrcMLRunner runF = new LibSrcMLRunner();
            string srcMLF = runF.GenerateSrcMLFromString(headerXml, "Foo.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLF, "Foo.h");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(implXml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLE, "Foo.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(header);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(implementation);

            var globalScope = headerScope.Merge(implementationScope);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var foo = globalScope.GetDescendants<TypeDefinition>().First(t => t.Name == "Foo");
            Assert.AreEqual(3, foo.ChildStatements.Count);
            Assert.AreEqual(2, foo.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.AreEqual(1, foo.ChildStatements.OfType<TypeDefinition>().Count());
            Assert.AreEqual("Foo.h", foo.PrimaryLocation.SourceFileName);

            var bar = globalScope.GetDescendants<TypeDefinition>().First(t => t.Name == "Bar");
            Assert.AreEqual(2, bar.ChildStatements.Count);
            Assert.AreEqual(2, bar.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.AreEqual("Foo.h", bar.PrimaryLocation.SourceFileName);

            var barConstructor = bar.GetNamedChildren<MethodDefinition>("Bar").First(m => m.IsConstructor);
            Assert.AreEqual(2, barConstructor.Locations.Count);
            Assert.AreEqual("Foo.cpp", barConstructor.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestDestructorMerge_Cpp() {
            string header_xml = @"class A { ~A(); };";

            string impl_xml = @"A::~A() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(header_xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.h");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(impl_xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "A.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(header);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(implementation);

            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);

            var destructor = typeA.ChildStatements.First() as MethodDefinition;
            Assert.That(destructor.IsDestructor);
            Assert.IsFalse(destructor.IsConstructor);
            Assert.IsFalse(destructor.IsPartial);
            Assert.AreEqual(AccessModifier.Private, destructor.Accessibility);
            Assert.AreEqual("A.cpp", destructor.PrimaryLocation.SourceFileName);
        }
        [Test]
        public void TestMethodDefinitionMerge_Cpp() {
            string header_xml = @"class A { int Foo(); };";

            string impl_xml = @"int A::Foo() { int bar = 1; return bar; }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(header_xml, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var header = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.h");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(impl_xml, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var implementation = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "A.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(header);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(implementation);

            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);

            var methodFoo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual(2, methodFoo.ChildStatements.Count());
            //TODO Assert.AreEqual(1, methodFoo.DeclaredVariables.Count());
            Assert.AreEqual("A.cpp", methodFoo.PrimaryLocation.SourceFileName);
            Assert.AreEqual(AccessModifier.Private, methodFoo.Accessibility);
        }

        [Test]
        public void TestMethodDefinitionMerge_NoParameterName() {
            string declXml = "int Foo(char);";
            LibSrcMLRunner runF = new LibSrcMLRunner();
            string srcMLF = runF.GenerateSrcMLFromString(declXml, "Foo.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileunitDecl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLF, "Foo.h");
            var declarationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileunitDecl);

            string defXml = "int Foo(char bar) { return 0; }";
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(defXml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitDef = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLE, "Foo.cpp");

            var globalScope = new NamespaceDefinition();
            var definitionScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitDef);

            globalScope = globalScope.Merge(declarationScope) as NamespaceDefinition;
            globalScope = globalScope.Merge(definitionScope) as NamespaceDefinition;

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var methodFoo = globalScope.ChildStatements[0] as MethodDefinition;
            Assert.IsNotNull(methodFoo);

            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual(1, methodFoo.Parameters.Count);

            var parameter = methodFoo.Parameters[0];
            Assert.AreEqual("char", parameter.VariableType.Name);
            Assert.AreEqual("bar", parameter.Name);
        }

        [Test]
        public void TestCreateMethodDefinition_TwoUnresolvedParents() {
            string xmlh = @"namespace A { class B { }; }";

            string xmlcpp = @"int A::B::Foo() { }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlh, "B.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "B.h");
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlcpp, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "B.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl);
            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(2, namespaceA.Locations.Count);

            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A.B", typeB.GetFullName());
            Assert.AreEqual(1, typeB.ChildStatements.Count());
            Assert.AreEqual(2, typeB.Locations.Count);

            var methodFoo = typeB.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.GetFullName());
            Assert.AreEqual(0, methodFoo.ChildStatements.Count());
            Assert.AreEqual(1, methodFoo.Locations.Count);

            Assert.AreSame(globalScope, namespaceA.ParentStatement);
            Assert.AreSame(namespaceA, typeB.ParentStatement);
            Assert.AreSame(typeB, methodFoo.ParentStatement);
        }

        [Test]
        public void TestCreateMethodDefinition_TwoUnresolvedParentsWithPrototype() {
            string xmlh = @"namespace A { class B { int Foo(); }; }";

            string xmlcpp = @"int A::B::Foo() { }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlh, "B.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "B.h");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlcpp, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "B.cpp");

            var headerScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader);
            var implementationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl);
            var globalScope = headerScope.Merge(implementationScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("A", namespaceA.Name);
            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(2, namespaceA.Locations.Count);

            var typeB = namespaceA.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A.B", typeB.GetFullName());
            Assert.AreEqual(1, typeB.ChildStatements.Count());
            Assert.AreEqual(2, typeB.Locations.Count);

            var methodFoo = typeB.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("A.B.Foo", methodFoo.GetFullName());
            Assert.AreEqual(0, methodFoo.ChildStatements.Count());
            Assert.AreEqual(2, methodFoo.Locations.Count);

            Assert.AreSame(globalScope, namespaceA.ParentStatement);
            Assert.AreSame(namespaceA, typeB.ParentStatement);
            Assert.AreSame(typeB, methodFoo.ParentStatement);
        }
        [Test]
        public void TestMethodDefinitionMerge_NoParameters() {
            string declXml = "int Foo();";
            LibSrcMLRunner runF = new LibSrcMLRunner();
            string srcMLF = runF.GenerateSrcMLFromString(declXml, "Foo.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileunitDecl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLF, "Foo.h");
            var declarationScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileunitDecl);

            string defXml = @"int Foo() { return 0; }";
            LibSrcMLRunner runG = new LibSrcMLRunner();
            string srcMLG = runG.GenerateSrcMLFromString(defXml, "Foo.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitDef = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLG, "Foo.cpp");
            var definitionScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitDef);

            var globalScope = declarationScope.Merge(definitionScope);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            Assert.AreEqual("Foo", ((MethodDefinition)globalScope.ChildStatements.First()).Name);
        }


        [Test]
        public void TestNamespaceMerge_Cpp() {
            string d_xml = @"namespace A { namespace B { namespace C { class D { }; } } }";

            string e_xml = @"namespace A { namespace B { namespace C { class E { }; } } }";

            string f_xml = @"namespace D { class F { }; }";

            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLD = runD.GenerateSrcMLFromString(d_xml, "D.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitD = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLD, "D.h");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(e_xml, "E.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitE = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLE, "E.h");
            LibSrcMLRunner runF = new LibSrcMLRunner();
            string srcMLF = runF.GenerateSrcMLFromString(f_xml, "F.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitF = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLF, "F.h");

            var globalScope = new NamespaceDefinition();
            var scopeD = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitD);
            var scopeE = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitE);
            var scopeF = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitF);

            globalScope = globalScope.Merge(scopeD).Merge(scopeE).Merge(scopeF) as NamespaceDefinition;

            Assert.AreEqual(2, globalScope.ChildStatements.Count());

            var namespaceA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var namespaceD = globalScope.ChildStatements.Last() as NamespaceDefinition;

            Assert.AreEqual(1, namespaceA.ChildStatements.Count());
            Assert.AreEqual(1, namespaceD.ChildStatements.Count());
            Assert.AreEqual("A", namespaceA.GetFullName());
            Assert.AreEqual("D", namespaceD.GetFullName());

            var namespaceB = namespaceA.ChildStatements.First() as NamespaceDefinition;
            var typeF = namespaceD.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("B", namespaceB.Name);
            Assert.AreEqual("F", typeF.Name);
            Assert.AreEqual(1, namespaceB.ChildStatements.Count());

            var namespaceC = namespaceB.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("C", namespaceC.Name);
            Assert.AreEqual("A.B", namespaceC.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual(2, namespaceC.ChildStatements.Count());
            var typeD = namespaceC.ChildStatements.First() as TypeDefinition;
            var typeE = namespaceC.ChildStatements.Last() as TypeDefinition;

            Assert.That(typeD.ParentStatement == typeE.ParentStatement);
            Assert.That(typeD.ParentStatement == namespaceC);

            Assert.AreEqual("A.B.C.D", typeD.GetFullName());
            Assert.AreEqual("A.B.C.E", typeE.GetFullName());
            Assert.AreEqual("D.F", typeF.GetFullName());
        }

        [Test]
        public void TestNamespaceMerge_Java() {
            string d_xml = @"package A.B.C; class D { public void Foo() { } }";

            string e_xml = @"package A.B.C; class E { public void Bar() { } }";

            string f_xml = @"package D; class F { public void Oof() { } }";

            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLD = runD.GenerateSrcMLFromString(d_xml, "D.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitD = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(srcMLD, "D.java");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(e_xml, "E.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitE = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(srcMLE, "E.java");
            LibSrcMLRunner runF = new LibSrcMLRunner();
            string srcMLF = runF.GenerateSrcMLFromString(f_xml, "F.java", Language.Java, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitF = FileUnitSetup[Language.Java].GetFileUnitForXmlSnippet(srcMLF, "F.java");

            var globalScopeD = CodeParser[Language.Java].ParseFileUnit(fileUnitD);
            var globalScopeE = CodeParser[Language.Java].ParseFileUnit(fileUnitE);
            var globalScopeF = CodeParser[Language.Java].ParseFileUnit(fileUnitF);
            var globalScope = globalScopeD.Merge(globalScopeE).Merge(globalScopeF);

            Assert.AreEqual(2, globalScope.ChildStatements.Count());

            var packageA = globalScope.ChildStatements.First() as NamespaceDefinition;
            var packageD = globalScope.ChildStatements.Last() as NamespaceDefinition;

            Assert.AreEqual("A", packageA.Name);
            Assert.AreEqual("D", packageD.Name);

            var packageAB = packageA.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("B", packageAB.Name);
            Assert.AreEqual("A.B", packageAB.GetFullName());

            var packageABC = packageAB.ChildStatements.First() as NamespaceDefinition;
            Assert.AreEqual("C", packageABC.Name);

            Assert.AreEqual("C", packageABC.Name);
            Assert.AreEqual("A.B.C", packageABC.GetFullName());

            var typeD = packageABC.ChildStatements.First() as TypeDefinition;
            var typeE = packageABC.ChildStatements.Last() as TypeDefinition;
            var typeF = packageD.ChildStatements.First() as TypeDefinition;

            Assert.AreEqual("D", typeD.Name);
            Assert.AreEqual("E", typeE.Name);
            Assert.That(typeD.ParentStatement == typeE.ParentStatement);

            Assert.That(typeD.ParentStatement != typeF.ParentStatement);
        }

        [Test]
        public void TestMethodDefinitionMerge_DifferentPrefixes() {
            string aCpp = @"int A::Foo() { return 0; }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(aCpp, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitA = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            var aScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitA);

            string bCpp = @"int B::Foo() { return 1; }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bCpp, "B.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fileUnitB = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "B.cpp");
            var bScope = CodeParser[Language.CPlusPlus].ParseFileUnit(fileUnitB);

            var globalScope = aScope.Merge(bScope);

            Assert.AreEqual(2, globalScope.ChildStatements.Count);
        }
        [Test]
        public void TestPartialClassMerge_CSharp() {
            string a1Xml = @"public partial class A {
                public int Execute() {
                    return 0;
                }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcMLA, "A1.cs");
            var globalScope = CodeParser[Language.CSharp].ParseFileUnit(a1FileUnit);

            string a2Xml = @"public partial class A {
                private bool Foo() {
                    return true;
                }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcMLB, "A2.cs");
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(2, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Execute"));
            Assert.IsTrue(typeA.ChildStatements.OfType<MethodDefinition>().Any(m => m.Name == "Foo"));
        }

        [Test]
        public void TestPartialMethodMerge_CSharp() {
            string a1Xml = @"public partial class A {
                public partial int Foo();
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(a1Xml, "A1.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a1FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcMLA, "A1.cs");
            var globalScope = CodeParser[Language.CSharp].ParseFileUnit(a1FileUnit);

            string a2Xml = @"public partial class A {
                public partial int Foo() { return 42; }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(a2Xml, "A2.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var a2FileUnit = FileUnitSetup[Language.CSharp].GetFileUnitForXmlSnippet(srcMLB, "A2.cs");
            globalScope = globalScope.Merge(CodeParser[Language.CSharp].ParseFileUnit(a2FileUnit));

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.OfType<MethodDefinition>().Count());
            var foo = typeA.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
        }

        [Test]
        public void TestUnresolvedParentMerge_ClassEncounteredFirst_Cpp() {
            string xmlcpp = @"int A::Foo() { return 0; }
             int A::Bar() { return 0; }";


            string xmlh = @"class A { };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlcpp, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlh, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "A.h");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader);
            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual(0, typeA.ChildStatements.Count());

            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl));
            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual(2, typeA.ChildStatements.Count());

            var aDotFoo = typeA.ChildStatements.First() as MethodDefinition;
            var aDotBar = typeA.ChildStatements.Last() as MethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.GetFullName());
            Assert.AreEqual("A.Bar", aDotBar.GetFullName());

            Assert.AreSame(typeA, aDotFoo.ParentStatement);
            Assert.AreSame(typeA, aDotFoo.ParentStatement);
            Assert.AreSame(globalScope, typeA.ParentStatement);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }

        [Test]
        public void TestUnresolvedParentMerge_MethodsEncounteredFirst_Cpp() {
            string xmlcpp = @"int A::Foo() { return 0; }
             int A::Bar() { return 0; }";

            string xmlh = @"class A { };";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlcpp, "A.cpp", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlImpl = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLA, "A.cpp");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlh, "A.h", Language.CPlusPlus, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlHeader = FileUnitSetup[Language.CPlusPlus].GetFileUnitForXmlSnippet(srcMLB, "A.h");

            var globalScope = CodeParser[Language.CPlusPlus].ParseFileUnit(xmlImpl);
            Assert.AreEqual(2, globalScope.ChildStatements.Count());

            var methodFoo = globalScope.ChildStatements.First() as MethodDefinition;
            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual(1, methodFoo.ChildStatements.Count());

            var methodBar = globalScope.ChildStatements.Last() as MethodDefinition;
            Assert.AreEqual("Bar", methodBar.Name);
            Assert.AreEqual(1, methodBar.ChildStatements.Count());

            Assert.AreEqual("Foo", methodFoo.Name);
            Assert.AreEqual("A.Foo", methodFoo.GetFullName());
            Assert.AreEqual("Bar", methodBar.Name);
            Assert.AreEqual("A.Bar", methodBar.GetFullName());

            globalScope = globalScope.Merge(CodeParser[Language.CPlusPlus].ParseFileUnit(xmlHeader));

            Assert.AreEqual(1, globalScope.ChildStatements.Count());

            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.AreEqual(2, typeA.ChildStatements.Count());

            var aDotFoo = typeA.ChildStatements.First() as MethodDefinition;
            var aDotBar = typeA.ChildStatements.Last() as MethodDefinition;

            Assert.AreEqual("A.Foo", aDotFoo.GetFullName());
            Assert.AreEqual("A.Bar", aDotBar.GetFullName());

            Assert.AreSame(methodFoo, aDotFoo);
            Assert.AreSame(methodBar, aDotBar);

            Assert.AreSame(typeA, methodFoo.ParentStatement);
            Assert.AreSame(typeA, methodBar.ParentStatement);
            Assert.AreSame(globalScope, typeA.ParentStatement);

            Assert.AreEqual("A.h", typeA.PrimaryLocation.SourceFileName);
        }
    }
}
