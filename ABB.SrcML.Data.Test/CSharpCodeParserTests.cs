/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using NUnit.Framework;
using System.Linq;
using System.Xml.Linq;
using ABB.SrcML;
using System.Collections.ObjectModel;
using System;
namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class CSharpCodeParserTests {
        private CSharpCodeParser codeParser;
        private SrcMLFileUnitSetup fileSetup;

        [TestFixtureSetUp]
        public void ClassSetup() {
            codeParser = new CSharpCodeParser();
            fileSetup = new SrcMLFileUnitSetup(Language.CSharp);
        }

        [Test]
        public void TestNamespace() {
            //namespace A { 
            //  public class foo { }
            //}
            var xml = @"namespace A { 
                public class foo { }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcML, "A.cs");
            var globalScope = codeParser.ParseFileUnit(unit);
            Assert.IsTrue(globalScope.IsGlobal);

            var actual = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(actual);
            Assert.AreEqual("A", actual.Name);
            Assert.AreEqual(1, actual.ChildStatements.Count);
        }

        [Test]
        public void TestCallToGenericMethod() {
            //namespace A {
            //    public class B {
            //        void Foo<T>(T t) { }
            //        void Bar() { Foo(this); }
            //    }
            //}
            var code = @"namespace A {
    public class B {
        void Foo<T>(T t) { }
        void Bar() { Foo(this); }
    }
}";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(code, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            var unit = fileSetup.GetFileUnitForXmlSnippet(srcML, "A.cs");
            var globalScope = codeParser.ParseFileUnit(unit);

            var foo = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "Foo");
            var bar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(md => md.Name == "Bar");
            Assert.IsNotNull(foo);
            Assert.IsNotNull(bar);

            Assert.AreEqual(1, bar.ChildStatements.Count);
            var callToFoo = bar.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToFoo);

            Assert.AreSame(foo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCallToGrandparent() {
            //namespace A {
            //    public class B { public void Foo() { } }
            //    public class C : B { }
            //    public class D : C { public void Bar() { Foo() } }
            //}
            var xml = @"namespace A {
    public class B { public void Foo() { } }
    public class C : B { }
    public class D : C { public void Bar() { Foo() } }
}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var scope = codeParser.ParseFileUnit(unit);

            var bDotFoo = scope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            var dDotBar = scope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(bDotFoo);
            Assert.IsNotNull(dDotBar);

            Assert.AreEqual(1, dDotBar.ChildStatements.Count);
            var callToFoo = dDotBar.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToFoo);

            Assert.AreSame(bDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodCallToParentOfCallingObject() {
            //class A { void Foo() { } }
            string axml = @"class A { void Foo() { } }";

            //class B : A { void Bar() { } }
            string bxml = @"class B : A { void Bar() { } }";

            //class C {
            //  private B b;
            //  void main() {
            //      b.Foo();
            //  }
            //}
            string cxml = @"class C {
    private B b;
    void main() {
        b.Foo();
    }
}";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(axml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var aUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            NamespaceDefinition globalScope = codeParser.ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(bUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(cUnit));

            var typeA = globalScope.GetNamedChildren<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetNamedChildren<TypeDefinition>("C").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");

            var aDotFoo = typeA.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            var cDotMain = typeC.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();

            Assert.IsNotNull(aDotFoo, "could not find method A.Foo()");
            Assert.IsNotNull(cDotMain, "could not find method C.main()");

            var callToFooFromC = cDotMain.FindExpressions<MethodCall>(true).FirstOrDefault();

            Assert.IsNotNull(callToFooFromC, "could not find any calls in C.main()");
            Assert.AreEqual("Foo", callToFooFromC.Name);
            var callingObject = callToFooFromC.GetSiblingsBeforeSelf<NameUse>().Last();
            Assert.AreEqual("b", callingObject.Name);

            Assert.AreEqual(typeB, callingObject.ResolveType().FirstOrDefault());
            Assert.AreEqual(aDotFoo, callToFooFromC.FindMatches().FirstOrDefault());
        }

        [Test]
        [Category("Todo")]
        public void TestCallWithTypeParameters() {
            //TODO: get answer about how generics are suppsoed to be handled for the parser. Don't see how they're being parsed.
            //namespace A {
            //    public interface IOdb { 
            //        int Query();
            //        int Query<T>();
            //    }
            //    public class Test {
            //        public IOdb Open() { }
            //        void Test1() {
            //            IOdb odb = Open();
            //            var query = odb.Query<Foo>();
            //        }
            //    }
            //}
            var xml = @"namespace A {
    public interface IOdb {
        int Query();
        int Query<T>();
    }
    public class Test {
        public IOdb Open() { }
        void Test1() {
            IOdb odb = Open();
            var query = odb.Query<Foo>();
        }
    }
}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var scope = codeParser.ParseFileUnit(unit);

            //TODO: update to search for method with type params, not just LastOrDefault
            var queryTMethod = scope.GetDescendants<MethodDefinition>().LastOrDefault(m => m.Name == "Query");
            Assert.IsNotNull(queryTMethod);
            var test1Method = scope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Test1");
            Assert.IsNotNull(test1Method);

            Assert.AreEqual(2, test1Method.ChildStatements.Count);
            var callToQuery = test1Method.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToQuery);

            var matches = callToQuery.FindMatches().ToList();
            Assert.AreEqual(1, matches.Count);
            Assert.AreSame(queryTMethod, matches[0]);
        }

        [Test]
        public void TestCallConstructor() {
            //class Foo {
            //  public Foo() { }
            //}
            //class Bar {
            //  Foo myFoo = new Foo();
            //}
            string xml = @"class Foo {
                           public Foo() { }
                         }
                         class Bar {
                           Foo myFoo = new Foo();
                         }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooConstructor = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(fooConstructor);
            var fooCall = globalScope.ChildStatements[1].ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Foo");
            Assert.IsNotNull(fooCall);
            Assert.AreSame(fooConstructor, fooCall.FindMatches().First());
        }

        [Test]
        public void TestConstructorWithBaseKeyword() {
            // B.cs namespace A { class B { public B() { } } }
            string bxml = @"namespace A { class B { public B() { } } }";
            // C.cs namespace A { class C : B { public C() : base() { } } }
            string cxml = @"namespace A { class C : B { public C() : base() { } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var bConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "B" && m.IsConstructor);
            var cConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "C" && m.IsConstructor);
            Assert.AreEqual(1, cConstructor.ConstructorInitializers.Count);

            var methodCall = cConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(methodCall);
            Assert.That(methodCall.IsConstructor);
            Assert.That(methodCall.IsConstructorInitializer);
            Assert.AreEqual("base", methodCall.Name);
            Assert.AreSame(bConstructor, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestConstructorWithThisKeyword() {
            // B.cs
            //namespace A {
            //    class B {
            //        public B() : this(0) { }
            //        public B(int i) { }
            //    }
            //}

            string bxml = @"namespace A {
    class B {
        public B() : this(0) { }
        public B(int i) { }
    }
}";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");

            var globalScope = codeParser.ParseFileUnit(bUnit);

            var oneArgumentConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "B" && m.Parameters.Count == 1);
            var defaultConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "B" && m.Parameters.Count == 0);
            Assert.AreEqual(1, defaultConstructor.ConstructorInitializers.Count);

            var methodCall = defaultConstructor.ConstructorInitializers[0];
            Assert.IsNotNull(methodCall);
            Assert.That(methodCall.IsConstructor);
            Assert.That(methodCall.IsConstructorInitializer);
            Assert.AreEqual("this", methodCall.Name);
            Assert.AreSame(oneArgumentConstructor, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCreateAliasesForFiles_UsingNamespace() {
            // using x.y.z;
            string xml = @"using x.y.z;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x . y . z", actual.ImportedNamespace.ToString());
        }

        [Test]
        public void TestCreateAliasesForFiles_UsingAlias() {
            // using x = Foo.Bar.Baz;
            string xml = @"using x = Foo.Bar.Baz;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as AliasStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x", actual.AliasName);
            Assert.AreEqual("Foo . Bar . Baz", actual.Target.ToString());
        }

        [Test]
        public void TestGetImports() {
            //B.cs
            //namespace x.y.z {}
            string xmlB = @"namespace x.y.z {}";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            //A.cs
            //using x.y.z;
            //foo = 17;
            string xmlA = @"using x.y.z;
foo = 17;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);
            var foo = globalScope.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x . y . z", imports[0].ImportedNamespace.ToString());

            var nsd = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(ns => ns.Name == "z");
            Assert.IsNotNull(nsd);
            var zUse = imports[0].ImportedNamespace.GetDescendantsAndSelf<NameUse>().LastOrDefault();
            Assert.IsNotNull(zUse);
            Assert.AreEqual("z", zUse.Name);
            Assert.AreSame(nsd, zUse.FindMatches().First());
        }

        [Test]
        public void TestGetImports_NestedImportNamespace() {
            //A.cs
            //namespace bar.baz {}
            string xmlA = @"namespace bar.baz {}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            //B.cs
            //using x.y.z;
            //if(bar) {
            //  using bar.baz;
            //  foo = 17;
            //}
            string xmlB = @"using x.y.z;
            if(bar) {
              using bar.baz;
              foo = 17;
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            var foo = globalScope.ChildStatements[2].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(2, imports.Count);
            Assert.AreEqual("bar . baz", imports[0].ImportedNamespace.ToString());
            Assert.AreEqual("x . y . z", imports[1].ImportedNamespace.ToString());

            var baz = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(ns => ns.Name == "baz");
            Assert.IsNotNull(baz);
            var bazUse = imports[0].ImportedNamespace.GetDescendantsAndSelf<NameUse>().LastOrDefault();
            Assert.IsNotNull(bazUse);
            Assert.AreEqual("baz", bazUse.Name);
            Assert.AreSame(baz, bazUse.FindMatches().First());
        }

        [Test]
        public void TestGetImports_SeparateFiles() {
            //A.cs
            //using x.y.z;
            //Foo = 17;
            string xmlA = @"using x.y.z;
            Foo = 17;";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            //B.cs
            //using a.b.howdy;
            //Bar();
            string xmlB = @"using a.b.howdy;
            Bar();";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(4, globalScope.ChildStatements.Count);

            var foo = globalScope.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(nu => nu.Name == "Foo");
            Assert.IsNotNull(foo);
            var fooImports = foo.GetImports().ToList();
            Assert.AreEqual(1, fooImports.Count);
            Assert.AreEqual("x . y . z", fooImports[0].ImportedNamespace.ToString());

            var bar = globalScope.ChildStatements[3].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(nu => nu.Name == "Bar");
            Assert.IsNotNull(bar);
            var barImports = bar.GetImports().ToList();
            Assert.AreEqual(1, barImports.Count);
            Assert.AreEqual("a . b . howdy", barImports[0].ImportedNamespace.ToString());
        }

        [Test]
        public void TestGetAliases_NestedUsingAlias() {
            //A.cs
            //namespace bar {
            //  class baz {}
            //}
            string xmlA = @"namespace bar {
              class baz {}
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            //B.cs
            //using x.y.z;
            //if(bar) {
            //  using x = bar.baz;
            //  foo = 17;
            //}
            string xmlB = @"using x.y.z;
            if(bar) {
              using x = bar.baz;
              foo = 17;
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runA.GenerateSrcMLFromString(xmlB, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "A.cs");

            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            var foo = globalScope.ChildStatements[2].ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetAliases().ToList();
            Assert.AreEqual(1, aliases.Count);
            Assert.AreEqual("bar . baz", aliases[0].Target.ToString());
            Assert.AreEqual("x", aliases[0].AliasName);
            var imports = foo.GetImports().ToList();
            Assert.AreEqual(1, imports.Count);
            Assert.AreEqual("x . y . z", imports[0].ImportedNamespace.ToString());

            var baz = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(ns => ns.Name == "baz");
            Assert.IsNotNull(baz);
            var bazUse = aliases[0].Target.GetDescendantsAndSelf<NameUse>().LastOrDefault(nu => nu.Name == "baz");
            Assert.IsNotNull(bazUse);
            Assert.AreSame(baz, bazUse.FindMatches().First());
        }

        [Test]
        public void TestImport_NameResolution() {
            //A.cs
            //using Foo.Bar;
            //
            //namespace A {
            //  public class Robot {
            //    public Baz GetThingy() { return new Baz(); }
            //  }
            //}
            string xmlA = @"using Foo.Bar;
            namespace A {
              public class Robot {
                public Baz GetThingy() { return new Baz(); }
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            //B.cs
            //namespace Foo.Bar {
            //  public class Baz {
            //    public Baz() { }
            //  }
            //}
            string xmlB = @"namespace Foo.Bar {
              public class Baz {
                public Baz() { }
              }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");

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
        public void TestAlias_NameResolution() {
            string xmlA = @"namespace Foo.Bar {
              public class Baz {
                public static void DoTheThing() { };
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xmlA, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);

            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            string xmlB = @"using Baz = Foo.Bar.Baz;
            namespace A {
              public class B {
                public B() {
                  Baz.DoTheThing();
                }
              }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xmlB, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");

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
            var data = thingCall.FindMatches().First();
            Assert.AreSame(thingDef, thingCall);
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestUsingBlock_SingleDecl() {
            //using(var f = File.Open("out.txt")) {
            //  ;
            //}
            string xml = "using(var f = File.Open(\"out.txt\")) {;}";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as UsingBlockStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.ChildStatements.Count);
            Assert.IsNotNull(actual.Initializer);
            var decls = actual.Initializer.GetDescendantsAndSelf<VariableDeclaration>().ToList();
            Assert.AreEqual(1, decls.Count);
            Assert.AreEqual("f", decls[0].Name);
            Assert.AreEqual("var", decls[0].VariableType.Name);
            Assert.IsNotNull(decls[0].Initializer);
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestUsingBlock_MultipleDecl() {
            // using(Foo a = new Foo(1), b = new Foo(2)) { ; }
            string xml = @"using(Foo a = new Foo(1), b = new Foo(2)) { ; }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as UsingBlockStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.ChildStatements.Count);
            Assert.IsNotNull(actual.Initializer);
            var decls = actual.Initializer.GetDescendantsAndSelf<VariableDeclaration>().ToList();
            Assert.AreEqual(2, decls.Count);
            Assert.AreEqual("a", decls[0].Name);
            Assert.AreEqual("Foo", decls[0].VariableType.Name);
            Assert.IsNotNull(decls[0].Initializer);
            Assert.AreEqual("b", decls[1].Name);
            Assert.AreEqual("Foo", decls[1].VariableType.Name);
            Assert.IsNotNull(decls[1].Initializer);
        }

        [Test]
        [Category("SrcMLUpdate")]
        public void TestUsingBlock_Expression() {
            //using(bar = new Foo()) { ; }
            string xml = @"using(bar = new Foo()) { ; }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as UsingBlockStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.ChildStatements.Count);
            var init = actual.Initializer;
            Assert.IsNotNull(actual.Initializer);
            Assert.AreEqual(4, init.Components.Count);
            var bar = init.Components[0] as NameUse;
            Assert.IsNotNull(bar);
            Assert.AreEqual("bar", bar.Name);
            var equals = init.Components[1] as OperatorUse;
            Assert.IsNotNull(equals);
            Assert.AreEqual("=", equals.Text);
            var newOp = init.Components[2] as OperatorUse;
            Assert.IsNotNull(newOp);
            Assert.AreEqual("new", newOp.Text);
            var foo = init.Components[3] as MethodCall;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(0, foo.Arguments.Count);
        }

        [Test]
        public void TestCreateTypeDefinition_Class() {
            ////Foo.cs
            //public class Foo {
            //    public int bar;
            //}
            string fooXml = @"public class Foo {
                public int bar;
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(1, foo.ChildStatements.Count());

            var bar = foo.ChildStatements[0].Content as VariableDeclaration;
            Assert.IsNotNull(bar);
            Assert.AreEqual("bar", bar.Name);
            Assert.AreEqual("int", bar.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, bar.Accessibility);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithParent() {
            ////Foo.cs
            //public class Foo : Baz {
            //    public int bar;
            //}
            string fooXml = @"public class Foo : Baz {
                public int bar;
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(1, foo.ChildStatements.Count());
            Assert.AreEqual(1, foo.ParentTypeNames.Count);
            Assert.AreEqual("Baz", foo.ParentTypeNames.First().Name);
        }

        [Test]
        public void TestCreateTypeDefinition_ClassWithQualifiedParent() {
            ////Foo.cs
            //public class Foo : Baz, System.IDisposable {
            //    public int bar;
            //}
            string fooXml = @"public class Foo : Baz, System.IDisposable {
                public int bar;
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Class, foo.Kind);
            Assert.AreEqual(1, foo.ChildStatements.Count());
            Assert.AreEqual(2, foo.ParentTypeNames.Count);
            Assert.AreEqual("Baz", foo.ParentTypeNames[0].Name);
            Assert.AreEqual("IDisposable", foo.ParentTypeNames[1].Name);
            Assert.AreEqual("System", foo.ParentTypeNames[1].Prefix.Names.First().Name);
        }

        [Test]
        public void TestCreateTypeDefinition_CompoundNamespace() {
            ////Foo.cs
            //namespace Example.Level2.Level3 {
            //    public class Foo {
            //        public int bar;
            //    }
            //}
            string fooXml = @"namespace Example.Level2.Level3 {
                public class Foo {
                    public int bar;
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var example = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildStatements.Count());
            var level2 = example.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(level2);
            Assert.AreEqual("Level2", level2.Name);
            Assert.AreEqual(1, level2.ChildStatements.Count());
            var level3 = level2.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(level3);
            Assert.AreEqual("Level3", level3.Name);
            Assert.AreEqual(1, level3.ChildStatements.Count());
            var foo = level3.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(1, foo.ChildStatements.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Interface() {
            ////Foo.cs
            //public interface Foo {
            //    public int GetBar();
            //}
            string fooXml = @"public interface Foo {
               public int GetBar();
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as  TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Interface, foo.Kind);
            Assert.AreEqual(1, foo.ChildStatements.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Namespace() {
            ////Foo.cs
            //namespace Example {
            //    public class Foo {
            //        public int bar;
            //    }
            //}
            string fooXml = @"namespace Example {
                public class Foo {
                    public int bar;
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var example = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildStatements.Count());
            var foo = example.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(1, foo.ChildStatements.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_NestedCompoundNamespace() {
            ////Foo.cs
            //namespace Watermelon {
            //    namespace Example.Level2.Level3 {
            //        public class Foo {
            //            public int bar;
            //        }
            //    }
            //}
            string fooXml = @"namespace Watermelon {
                namespace Example.Level2.Level3 {
                    public class Foo {
                        public int bar;
                    }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var watermelon = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(watermelon);
            Assert.AreEqual("Watermelon", watermelon.Name);
            Assert.AreEqual(1, watermelon.ChildStatements.Count());
            var example = watermelon.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(example);
            Assert.AreEqual("Example", example.Name);
            Assert.AreEqual(1, example.ChildStatements.Count());
            var level2 = example.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(level2);
            Assert.AreEqual("Level2", level2.Name);
            Assert.AreEqual(1, level2.ChildStatements.Count());
            var level3 = level2.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(level3);
            Assert.AreEqual("Level3", level3.Name);
            Assert.AreEqual(1, level3.ChildStatements.Count());
            var foo = level3.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(1, foo.ChildStatements.Count());
        }

        [Test]
        public void TestCreateTypeDefinition_Struct() {
            ////Foo.cs
            //public struct Foo {
            //    public int bar;
            //}
            string fooXml = @"public struct Foo {
                public int bar;
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(fooXml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual(TypeKind.Struct, foo.Kind);
            Assert.AreEqual(1, foo.ChildStatements.Count());
        }

        [Test]
        public void TestCreateTypeDefinitions_ClassWithInnerClass() {
            ////A.cs
            //class A {
            //    class B {}
            //}
            string xml = @"class A {
                class B {}
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            var typeB = typeA.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeB);

            Assert.AreSame(typeA, typeB.ParentStatement);
            Assert.AreEqual("A", typeA.GetFullName());
            Assert.AreEqual("A.B", typeB.GetFullName());
        }

        [Test]
        public void TestCreateTypeDefinitions_InnerClassWithNamespace() {
            ////A.cs
            //namespace Foo {
            //    class A {
            //        class B {}
            //    }
            //}
            string xml = @"namespace Foo {
                class A {
                    class B {}
                }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as NamespaceDefinition;
            Assert.IsNotNull(foo);
            Assert.AreEqual(1, foo.ChildStatements.Count());
            var typeA = foo.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            var typeB = typeA.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeB);

            Assert.AreEqual("A", typeA.Name);
            Assert.AreEqual("Foo", typeA.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("Foo.A", typeA.GetFullName());

            Assert.AreEqual("B", typeB.Name);
            Assert.AreEqual("Foo", typeB.GetAncestors<NamespaceDefinition>().First().GetFullName());
            Assert.AreEqual("Foo.A.B", typeB.GetFullName());
        }

        [Test]
        public void TestDeclarationWithTypeVarFromConstructor() {
            // B.cs namespace A { class B { public B() { }; } }
            string bxml = @"namespace A { class B { public B() { }; } }";
            // C.cs namespace A { class C { void main() { var b = new B(); } } }
            string cxml = @"namespace A { class C { void main() { var b = new B(); } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");
            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var main = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(main);

            Assert.AreEqual(1, main.ChildStatements.Count);
            var varDecl = main.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(varDecl);

            Assert.AreSame(typeB, varDecl.ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestDeclarationWithTypeVarFromImplicitConstructor() {
            // B.cs namespace A { class B { } }
            string bxml = @"namespace A { class B { } }";
            // C.cs namespace A { class C { void main() { var b = new B(); } } }
            string cxml = @"namespace A { class C { void main() { var b = new B(); } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");
            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var main = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(main);

            Assert.AreEqual(1, main.ChildStatements.Count);
            var varDecl = main.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(varDecl);

            Assert.AreSame(typeB, varDecl.ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestDeclarationWithTypeVarFromMethod() {
            //namespace A {
            //    class B {
            //        public static void main() { var b = getB(); }
            //        public static B getB() { return new B(); }
            //    }
            //}
            string xml = @"namespace A {
                class B {
                    public static void main() { var b = getB(); }
                    public static B getB() { return new B(); }
                }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            var globalScope = codeParser.ParseFileUnit(unit);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var mainMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(mainMethod);

            Assert.AreEqual(1, mainMethod.ChildStatements.Count);
            var varDecl = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(varDecl);

            Assert.AreSame(typeB, varDecl.ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestDeclarationWithTypeVarInForeach() {
            //class Foo {
            //    int[] GetInts() {
            //        return new[] {1, 2, 3, 4};
            //    }
            //    int main() {
            //        foreach(var num in GetInts()) {
            //            print(num);
            //        }
            //    }
            //}
            string xml = @"class Foo {
                int[] GetInts() {
                    return new[] {1, 2, 3, 4};
                }
                int main() {
                    foreach(var num in GetInts()) {
                        print(num);
                    }
                }
            }";
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            var globalScope = codeParser.ParseFileUnit(unit);

            var loop = globalScope.GetDescendants<ForeachStatement>().First();
            var varDecl = loop.Condition.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(varDecl);

            Assert.AreSame(BuiltInTypeFactory.GetBuiltIn(new TypeUse() { Name = "int" }), varDecl.ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestFieldCreation() {
            //// A.cs
            //class A {
            //    public int Foo;
            //}
            string xml = @"class A {
                public int Foo;
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var typeA = globalScope.ChildStatements.First() as TypeDefinition;
            Assert.IsNotNull(typeA);
            Assert.AreEqual(1, typeA.ChildStatements.Count());
            var foo = typeA.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(foo);
            Assert.AreEqual("Foo", foo.Name);
            Assert.AreEqual("int", foo.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, foo.Accessibility);
        }

        [Test]
        public void TestFindParentType() {
            // namespace A { class B : C { } }
            string bxml = @"namespace A { class B : C { } }";

            // namespace A { class C { } }
            string cxml = @"namespace A { class C { } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLD = runD.GenerateSrcMLFromString(cxml, "D.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLD, "D.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);

            var globalScope = bScope.Merge(cScope);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var typeC = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "C");
            Assert.IsNotNull(typeC);

            Assert.AreEqual(1, typeB.ParentTypeNames.Count);
            Assert.AreSame(typeC, typeB.ParentTypeNames[0].ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestFindQualifiedParentType() {
            // namespace A { class B : C.D { } }
            string bxml = @"A { class B : C.D { } }";

            // namespace C { class D { } }
            string dxml = @"namespace C { class D { } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLD = runD.GenerateSrcMLFromString(dxml, "D.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLD, "D.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var dScope = codeParser.ParseFileUnit(dUnit);

            var globalScope = bScope.Merge(dScope);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var typeD = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "D");
            Assert.IsNotNull(typeD);

            Assert.AreEqual(1, typeB.ParentTypeNames.Count);
            Assert.AreSame(typeD, typeB.ParentTypeNames[0].ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestGenericType() {
            //public class B<T> { }
            var xml = @"public class B<T> { }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            var scope = codeParser.ParseFileUnit(unit);

            var typeB = scope.GetDescendants<TypeDefinition>().FirstOrDefault();
            Assert.IsNotNull(typeB);
            Assert.AreEqual("B", typeB.Name);
        }

        [Test]
        public void TestGenericVariableDeclaration() {
            //Dictionary<string,int> map;
            string xml = @"Dictionary<string,int> map;";

            LibSrcMLRunner runt = new LibSrcMLRunner();
            string srcMLt = runt.GenerateSrcMLFromString(xml, "test.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLt, "test.cs");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("map", testDeclaration.Name);
            Assert.AreEqual("Dictionary", testDeclaration.VariableType.Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(2, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("string", testDeclaration.VariableType.TypeParameters.First().Name);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.Last().Name);
        }

        [Test]
        public void TestGenericVariableDeclarationWithPrefix() {
            string xml = @"System.Collection.Dictionary<string,int> map;";

            LibSrcMLRunner runt = new LibSrcMLRunner();
            string srcMLt = runt.GenerateSrcMLFromString(xml, "test.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLt, "test.cs");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var testDeclaration = testScope.ChildStatements.First().Content as VariableDeclaration;
            Assert.IsNotNull(testDeclaration, "could not find the test declaration");
            Assert.AreEqual("map", testDeclaration.Name);
            Assert.AreEqual("Dictionary", testDeclaration.VariableType.Name);
            var prefixNames = testDeclaration.VariableType.Prefix.Names.ToList();
            Assert.AreEqual(2, prefixNames.Count);
            Assert.AreEqual("System", prefixNames[0].Name);
            Assert.AreEqual("Collection", prefixNames[1].Name);
            Assert.That(testDeclaration.VariableType.IsGeneric);
            Assert.AreEqual(2, testDeclaration.VariableType.TypeParameters.Count);
            Assert.AreEqual("string", testDeclaration.VariableType.TypeParameters.First().Name);
            Assert.AreEqual("int", testDeclaration.VariableType.TypeParameters.Last().Name);
        }

        [Test]
        public void TestGetAccessModifierForMethod_InternalProtected() {
            string xml = @"namespace Example {
                public class Foo {
                    internal protected bool Bar() { return true; }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();
            //The specifier isn't in type anymore so it doesn't parse correctly.
            Assert.AreEqual(AccessModifier.ProtectedInternal, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_None() {
            //namespace Example {
            //    public class Foo {
            //        bool Bar() { return true; }
            //    }
            //}
            string xml = @"namespace Example {
                public class Foo {
                    bool Bar() { return true; }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.None, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_Normal() {
            //namespace Example {
            //    public class Foo {
            //        public bool Bar() { return true; }
            //    }
            //}
            string xml = @"namespace Example {
                public class Foo {
                    public bool Bar() { return true; }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_ProtectedInternal() {
            //namespace Example {
            //    public class Foo {
            //        protected internal bool Bar() { return true; }
            //    }
            //}
            string xml = @"namespace Example {
                public class Foo {
                    protected internal bool Bar() { return true; }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_ProtectedInternalStatic() {
            //namespace Example {
            //    public class Foo {
            //        protected static internal bool Bar() { return true; }
            //    }
            //}
            string xml = @"namespace Example {
                public class Foo {
                    protected static internal bool Bar() { return true; }
                }
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_InternalProtected() {
            //namespace Example {
            //    internal protected class Foo {}
            //}
            string xml = @"namespace Example {
                internal protected class Foo {}
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_None() {
            //namespace Example {
            //    class Foo {}
            //}
            string xml = @"namespace Example {
                class Foo {}
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.None, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Normal() {
            //namespace Example {
            //    public class Foo {}
            //}
            string xml = @"namespace Example {
                public class Foo {}
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternal() {
            //namespace Example {
            //    protected internal class Foo {}
            //}
            string xml = @"namespace Example {
                protected internal class Foo {}
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternalStatic() {
            //namespace Example {
            //    protected static internal class Foo {}
            //}
            string xml = @"namespace Example {
                protected static internal class Foo {}
            }";
            LibSrcMLRunner runo = new LibSrcMLRunner();
            string srcMLo = runo.GenerateSrcMLFromString(xml, "Foo.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLo, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestMethodCallWithBaseKeyword() {
            // B.cs namespace A { class B { public virtual void Foo() { } } }
            string bxml = @"namespace A { class B { public virtual void Foo() { } } }";
            // C.cs namespace A { class C : B { public override void Foo() { base.Foo(); } } }
            string cxml = @"namespace A { class C : B { public override void Foo() { base.Foo(); } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = bScope.Merge(cScope);

            var fooMethods = globalScope.GetDescendants<MethodDefinition>().ToList();

            var bDotFoo = fooMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "B");
            Assert.IsNotNull(bDotFoo);
            var cDotFoo = fooMethods.FirstOrDefault(m => m.GetAncestors<TypeDefinition>().FirstOrDefault().Name == "C");
            Assert.IsNotNull(cDotFoo);

            Assert.AreEqual(1, cDotFoo.ChildStatements.Count);
            var methodCall = cDotFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(methodCall);
            Assert.AreSame(bDotFoo, methodCall.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestMethodDefinitionWithReturnType() {
            //int Foo() { }
            string xml = @"int Foo() { }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcML, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithReturnTypeAndWithSpecifier() {
            //static int Foo() { }
            string xml = @"static int Foo() { }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcML, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithVoidReturn() {
            //void Foo() { }
            string xml = @"void Foo() { }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "test.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcML, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.IsNull(method.ReturnType, "return type should be null");
        }


        [Test]
        public void TestProperty() {
            // namespace A { class B { int Foo { get; set; } } }
            string xml = @"namespace A { class B { int Foo { get; set; } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(xml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            var testScope = codeParser.ParseFileUnit(testUnit);

            var classB = testScope.GetDescendants<TypeDefinition>().FirstOrDefault();

            Assert.IsNotNull(classB);
            Assert.AreEqual(1, classB.ChildStatements.Count());

            var fooProperty = classB.ChildStatements.First() as PropertyDefinition;
            Assert.IsNotNull(fooProperty);
            Assert.AreEqual("Foo", fooProperty.Name);
            Assert.AreEqual("int", fooProperty.ReturnType.Name);
            Assert.AreEqual(AccessModifier.None, fooProperty.Accessibility);
            Assert.IsNotNull(fooProperty.Getter);
            Assert.IsNotNull(fooProperty.Setter);
        }

        [Test]
        public void TestPropertyAsCallingObject() {
            string bxml = @"namespace A {
              class B {
                C Foo { get; set; }
              }
            }";

            string cxml = @"namespace A {
                class C {
                    static void main() {
                        B b = new B();
                        b.Foo.Bar();
                    }
                    void Bar() { }
                }
            }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");
            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);

            var globalScope = bScope.Merge(cScope);

            var classB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(classB);
            var classC = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "C");
            Assert.IsNotNull(classC);

            var mainMethod = classC.GetNamedChildren<MethodDefinition>("main").FirstOrDefault();
            var barMethod = classC.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(mainMethod);
            Assert.IsNotNull(barMethod);

            Assert.AreEqual(2, mainMethod.ChildStatements.Count);
            var callToBar = mainMethod.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToBar);
            Assert.AreSame(barMethod, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestStaticMethodCall() {
            //namespace A { public class B { public static void Bar() { } } }
            var bxml = @"namespace A { public class B { public static void Bar() { } } }";
            //namespace A { public class C { public void Foo() { B.Bar(); } } }
            var cxml = @"namespace A { public class C { public void Foo() { B.Bar(); } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);

            var globalScope = bScope.Merge(cScope);

            var fooMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            var barMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(barMethod);

            Assert.AreEqual(1, fooMethod.ChildStatements.Count);
            var callToBar = fooMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToBar);
            Assert.AreSame(barMethod, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestStaticMethodCallInDifferentNamespace() {
            //namespace A { public class B { public static void Bar() { } } }
            var bxml = @"namespace A { public class B { public static void Bar() { } } }";
            //namespace C { public class D { public void Foo() { A.B.Bar(); } } }
            var dxml = @"namespace C { public class D { public void Foo() { A.B.Bar(); } } }";

            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(dxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            var bScope = codeParser.ParseFileUnit(bUnit);
            var dScope = codeParser.ParseFileUnit(dUnit);

            var globalScope = bScope.Merge(dScope);

            var fooMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            var barMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(barMethod);

            Assert.AreEqual(1, fooMethod.ChildStatements.Count);
            var callToBar = fooMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToBar);
            Assert.AreSame(barMethod, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariablesWithSpecifiers() {
            //static int A;
            //public const int B;
            //public static readonly Foo C;
            //volatile  int D;
            string testXml = @"static int A;
            public const int B;
            public static readonly Foo C;
            volatile int D;";
            LibSrcMLRunner runt = new LibSrcMLRunner();
            string srcMLt = runt.GenerateSrcMLFromString(testXml, "test.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLt, "test.cs");

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
            Assert.AreEqual(AccessModifier.Public, declB.Accessibility);

            var declC = globalScope.ChildStatements[2].Content as VariableDeclaration;
            Assert.IsNotNull(declC);
            Assert.AreEqual("C", declC.Name);
            Assert.AreEqual("Foo", declC.VariableType.Name);
            Assert.AreEqual(AccessModifier.Public, declC.Accessibility);

            var declD = globalScope.ChildStatements[3].Content as VariableDeclaration;
            Assert.IsNotNull(declD);
            Assert.AreEqual("D", declD.Name);
            Assert.AreEqual("int", declD.VariableType.Name);
            Assert.AreEqual(AccessModifier.None, declD.Accessibility);
        }

        [Test]
        public void TestStaticInstanceVariable() {
            //namespace A {
            //  class B {
            //      public static B Instance { get; set; }
            //      public void Bar() { }
            //  }
            //  
            //  class C { public void Foo() { B.Instance.Bar(); } }
            //}
            var xml = @"namespace A {
                class B {
                    public static B Instance { get; set; }
                    public void Bar() { }
                }
                
                class C { public void Foo() { B.Instance.Bar(); } }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(unit);

            var methodBar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(methodBar);
            var methodFoo = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(methodFoo);

            Assert.AreEqual(1, methodFoo.ChildStatements.Count);
            var callToBar = methodFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToBar);
            Assert.AreSame(methodBar, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestStaticInstanceVariableInDifferentNamespace() {
            //namespace A {
            //  class B {
            //      public static B Instance { get; set; }
            //      public void Bar() { }
            //  }
            //}
            var axml = @"namespace A {
                class B {
                    public static B Instance { get; set; }
                    public void Bar() { }
                }
            }";
            //using A;
            //
            //namespace C {
            //  class D {
            //      public void Foo() { B.Instance.Bar(); }
            //  }
            //}
            var cxml = @"using A;
            namespace C {
                class D {
                    public void Foo() { B.Instance.Bar(); }
                }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(axml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var aUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");
            var aScope = codeParser.ParseFileUnit(aUnit);
            var cScope = codeParser.ParseFileUnit(cUnit);
            var globalScope = aScope.Merge(cScope);

            var methodBar = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(methodBar);
            var methodFoo = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(methodFoo);

            Assert.AreEqual(1, methodFoo.ChildStatements.Count);
            var callToBar = methodFoo.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToBar);
            Assert.AreSame(methodBar, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCallAsCallingObject() {
            //namespace A {
            //  public class B {
            //      void main() {
            //          Foo().Bar();
            //      }
            //
            //      C Foo() { return new C(); }
            //  }
            //
            //  public class C {
            //      void Bar() { }
            //  }
            //}
            var xml = @"namespace A {
                public class B {
                    void main() {
                        Foo().Bar();
                    }
            
                    C Foo() { return new C(); }
                }
            
                public class C {
                    void Bar() { }
                }
            }";
            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcML = run.GenerateSrcMLFromString(xml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var unit = fileSetup.GetFileUnitForXmlSnippet(srcML, "B.cs");
            var globalScope = codeParser.ParseFileUnit(unit);

            var mainMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            var fooMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            var barMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Bar");
            Assert.IsNotNull(mainMethod);
            Assert.IsNotNull(fooMethod);
            Assert.IsNotNull(barMethod);

            Assert.AreEqual(1, mainMethod.ChildStatements.Count);
            var callToFoo = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Foo");
            var callToBar = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Bar");
            Assert.IsNotNull(callToFoo);
            Assert.IsNotNull(callToBar);

            Assert.AreSame(fooMethod, callToFoo.FindMatches().FirstOrDefault());
            Assert.AreSame(barMethod, callToBar.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestResolveVariable_Field() {
            //class A {
            //  public int Foo;
            //  public A() {
            //    Foo = 42;
            //  }
            //}
            string xml = @"class A {
              public int Foo;
              public A() {
                Foo = 42;
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

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
            //class B {
            //  public int Foo;
            //}
            //class A : B {
            //  public A() {
            //    Foo = 42;
            //  }
            //}
            var xml = @"class B {
            public int Foo;
            }
            class A : B {
              public A() {
                Foo = 42;
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var fooDecl = globalScope.GetNamedChildren<TypeDefinition>("B").First().GetNamedChildren<VariableDeclaration>("Foo").First();
            var aConstructor = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "A");
            Assert.AreEqual(1, aConstructor.ChildStatements.Count);
            var fooUse = aConstructor.ChildStatements[0].Content.GetDescendants<NameUse>().FirstOrDefault(n => n.Name == "Foo");
            Assert.IsNotNull(fooUse);
            Assert.AreSame(fooDecl, fooUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCallingVariableDeclaredInParentClass() {
            //class A { void Foo() { } }
            string axml = @"class A { void Foo() { } }";

            //class B { protected A a; }
            string bxml = @"class B { protected A a; }";

            //class C : B { void Bar() { a.Foo(); } }
            string cxml = @"class C : B { void Bar() { a.Foo(); } }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(axml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var aUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");

            var globalScope = codeParser.ParseFileUnit(aUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(bUnit));
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(cUnit));

            var typeA = globalScope.GetNamedChildren<TypeDefinition>("A").FirstOrDefault();
            var typeB = globalScope.GetNamedChildren<TypeDefinition>("B").FirstOrDefault();
            var typeC = globalScope.GetNamedChildren<TypeDefinition>("C").FirstOrDefault();

            Assert.IsNotNull(typeA, "could not find class A");
            Assert.IsNotNull(typeB, "could not find class B");
            Assert.IsNotNull(typeC, "could not find class C");

            var aDotFoo = typeA.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
            Assert.IsNotNull(aDotFoo, "could not find method A.Foo()");

            var cDotBar = typeC.GetNamedChildren<MethodDefinition>("Bar").FirstOrDefault();
            Assert.IsNotNull(cDotBar, "could not find method C.Bar()");

            var callToFoo = cDotBar.FindExpressions<MethodCall>(true).FirstOrDefault();
            Assert.IsNotNull(callToFoo, "could not find any method calls in C.Bar()");
            Assert.AreEqual("Foo", callToFoo.Name);

            Assert.AreEqual(aDotFoo, callToFoo.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestVariableDeclaredInCallingObjectWithParentClass() {
            //class A { B b; }
            string axml = @"class A { B b; }";

            //class B { void Foo() { } }
            string bxml = @"class B { void Foo() { } }";

            //class C : A { }
            string cxml = @"class C : A { }";

            //class D {
            //  C c;
            //  void Bar() { c.b.Foo(); }
            //}
            string dxml = @"class D {
                C c;
                void Bar() { c.b.Foo(); }
            }";

            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(axml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var aUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            LibSrcMLRunner runB = new LibSrcMLRunner();
            string srcMLB = runB.GenerateSrcMLFromString(bxml, "B.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var bUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLB, "B.cs");
            LibSrcMLRunner runC = new LibSrcMLRunner();
            string srcMLC = runC.GenerateSrcMLFromString(cxml, "C.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cs");
            LibSrcMLRunner runD = new LibSrcMLRunner();
            string srcMLD = runD.GenerateSrcMLFromString(dxml, "D.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLD, "D.cs");

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
        public void TestResolveArrayVariable_Property() {
            //class Foo {
            //  Collection<int> Parameters { get; set; }
            //  void DoWork() {
            //    printf(Parameters[0]);
            //  }
            //}
            string xml = @"class Foo {
              Collection<int> Parameters { get; set; }
              void DoWork() {
                printf(Parameters[0]);
              }
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            var paramDecl = globalScope.GetDescendants<PropertyDefinition>().First(p => p.Name == "Parameters");
            Assert.IsNotNull(paramDecl);
            var doWork = globalScope.GetDescendants<MethodDefinition>().First(m => m.Name == "DoWork");
            Assert.AreEqual(1, doWork.ChildStatements.Count);
            var paramUse = doWork.ChildStatements[0].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "Parameters");
            Assert.IsNotNull(paramUse);
            Assert.AreSame(paramDecl, paramUse.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestTypeUseForOtherNamespace() {
            //namespace A.B {
            //    class C {
            //        int Foo() { }
            //    }
            //}
            string cxml = @"namespace A.B {
                class C {
                    int Foo() { }
                }
            }";

            //using A.B;
            //namespace D {
            //    class E {
            //        void main() {
            //            C c = new C();
            //            c.Foo();
            //        }
            //    }
            //}
            string exml = @"using A.B;
            namespace D {
                class E {
                    void main() {
                        C c = new C();
                        c.Foo();
                    }
                }
            }";

            LibSrcMLRunner run = new LibSrcMLRunner();
            string srcMLC = run.GenerateSrcMLFromString(cxml, "C.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLC, "C.cpp");
            LibSrcMLRunner runE = new LibSrcMLRunner();
            string srcMLE = runE.GenerateSrcMLFromString(exml, "E.cpp", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var eUnit = fileSetup.GetFileUnitForXmlSnippet(srcMLE, "E.cpp");

            NamespaceDefinition globalScope = codeParser.ParseFileUnit(cUnit);
            globalScope = globalScope.Merge(codeParser.ParseFileUnit(eUnit)) as NamespaceDefinition;

            var typeC = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "C").FirstOrDefault();
            var typeE = globalScope.GetDescendants<TypeDefinition>().Where(t => t.Name == "E").FirstOrDefault();

            var mainMethod = typeE.ChildStatements.First() as MethodDefinition;
            Assert.IsNotNull(mainMethod, "is not a method definition");
            Assert.AreEqual("main", mainMethod.Name);

            var fooMethod = typeC.GetNamedChildren<MethodDefinition>("Foo").FirstOrDefault();
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

        [Test]
        public void TestLockStatement() {
            //lock(myVar) {
            //    myVar.DoFoo();
            //}
            string xml = @"lock(myVar) {
                myVar.DoFoo();
            }";
            LibSrcMLRunner runA = new LibSrcMLRunner();
            string srcMLA = runA.GenerateSrcMLFromString(xml, "A.cs", Language.CSharp, new Collection<UInt32>() { LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_POSITION }, false);
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(srcMLA, "A.cs");
            var globalScope = codeParser.ParseFileUnit(xmlElement);

            var lockStmt = globalScope.ChildStatements.First() as LockStatement;
            Assert.IsNotNull(lockStmt);
            Assert.AreEqual(1, lockStmt.ChildStatements.Count);

            var lockVar = lockStmt.LockExpression as NameUse;
            Assert.IsNotNull(lockVar);
            Assert.AreEqual("myVar", lockVar.Name);
        }
    }
}