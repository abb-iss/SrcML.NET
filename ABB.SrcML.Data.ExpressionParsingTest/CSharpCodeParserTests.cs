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
            //	public class foo { }
            //}
            var xml = @"<namespace>namespace <name>A</name> <block>{ 
	<class><specifier>public</specifier> class <name>foo</name> <block>{ }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            var xml = @"<namespace>namespace <name>A</name> <block>{
    <class><specifier>public</specifier> class <name>B</name> <block>{
        <function><type><name>void</name></type> <name><name>Foo</name><argument_list>&lt;<argument><name>T</name></argument>&gt;</argument_list></name><parameter_list>(<param><decl><type><name>T</name></type> <name>t</name></decl></param>)</parameter_list> <block>{ }</block></function>
        <function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name>Foo</name><argument_list>(<argument><expr><name>this</name></expr></argument>)</argument_list></call></expr>;</expr_stmt> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
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
            var xml = @"<namespace>namespace <name>A</name> <block>{
    <class><specifier>public</specifier> class <name>B</name> <block>{ <function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class>
    <class><specifier>public</specifier> class <name>C</name> <super>: <name>B</name></super> <block>{ }</block></class>
    <class><specifier>public</specifier> class <name>D</name> <super>: <name>C</name></super> <block>{ <function><type><specifier>public</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name>Foo</name><argument_list>()</argument_list></call></expr></expr_stmt> }</block></function> }</block></class>
}</block></namespace>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
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
        [Category("Todo")]
        public void TestCallWithTypeParameters() {
            //namespace A {
            //	public interface IQuery { }
            //	public interface IOdb { IQuery Query<T>(); }
            //	public class Test {
            //		public IOdb Open() { }
            //		void Test1() {
            //			var odb = Open();
            //			var query = odb.Query<Foo>();
            //		}
            //	}
            //}
            var xml = @"<namespace>namespace <name>A</name> <block>{
	<class type=""interface""><specifier>public</specifier> interface <name>IQuery</name> <block>{ }</block></class>
	<class type=""interface""><specifier>public</specifier> interface <name>IOdb</name> <block>{ <function_decl><type><name>IQuery</name></type> <name><name>Query</name><argument_list>&lt;<argument><name>T</name></argument>&gt;</argument_list></name><parameter_list>()</parameter_list>;</function_decl> }</block></class>

	<class><specifier>public</specifier> class <name>Test</name> <block>{
		<function><type><specifier>public</specifier> <name>IOdb</name></type> <name>Open</name><parameter_list>()</parameter_list> <block>{ }</block></function>
		<function><type><name>void</name></type> <name>Test1</name><parameter_list>()</parameter_list> <block>{
			<decl_stmt><decl><type><name>var</name></type> <name>odb</name> =<init> <expr><call><name>Open</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
			<decl_stmt><decl><type><name>var</name></type> <name>query</name> =<init> <expr><call><name><name>odb</name><op:operator>.</op:operator><name><name>Query</name><argument_list>&lt;<argument><name>Foo</name></argument>&gt;</argument_list></name></name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
		}</block></function>
	}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
            var scope = codeParser.ParseFileUnit(unit);

            var queryMethod = scope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Query");
            Assert.IsNotNull(queryMethod);
            var test1Method = scope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Test1");
            Assert.IsNotNull(test1Method);

            Assert.AreEqual(2, test1Method.ChildStatements.Count);
            var callToQuery = test1Method.ChildStatements[1].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault();
            Assert.IsNotNull(callToQuery);

            Assert.AreSame(queryMethod, callToQuery.FindMatches().FirstOrDefault());
        }

        [Test]
        public void TestCallConstructor() {
            //class Foo {
            //  public Foo() { }
            //}
            //class Bar {
            //  Foo myFoo = new Foo();
            //}
            string xml = @"<class>class <name>Foo</name> <block>{
  <constructor><specifier>public</specifier> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></constructor>
}</block></class>
<class>class <name>Bar</name> <block>{
  <decl_stmt><decl><type><name>Foo</name></type> <name>myFoo</name> <init>= <expr><op:operator>new</op:operator> <call><name>Foo</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
}</block></class>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(2, globalScope.ChildStatements.Count);

            var fooConstructor = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(fooConstructor);
            var fooCall = globalScope.ChildStatements[1].ChildStatements[0].Content.GetDescendantsAndSelf<MethodCall>().FirstOrDefault(mc => mc.Name == "Foo");
            Assert.IsNotNull(fooCall);
            Assert.AreSame(fooConstructor, fooCall.FindMatches().First());
        }

//        [Test]
//        public void TestConstructorWithBaseKeyword() {
//            // B.cs namespace A { class B { public B() { } } }
//            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <block>{ }</block></constructor> }</block></class> }</block></namespace>";
//            // C.cs namespace A { class C : B { public C() : base() { } } }
//            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <super>: <name>B</name></super> <block>{ <constructor><specifier>public</specifier> <name>C</name><parameter_list>()</parameter_list> <member_list>: <call><name>base</name><argument_list>()</argument_list></call> </member_list><block>{ }</block></constructor> }</block></class> }</block></namespace>";

//            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
//            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");

//            var bScope = codeParser.ParseFileUnit(bUnit);
//            var cScope = codeParser.ParseFileUnit(cUnit);
//            var globalScope = bScope.Merge(cScope);

//            var constructors = from methodDefinition in globalScope.GetDescendantScopes<IMethodDefinition>()
//                               where methodDefinition.IsConstructor
//                               select methodDefinition;

//            var bConstructor = (from method in constructors
//                                where method.GetParentScopes<ITypeDefinition>().FirstOrDefault().Name == "B"
//                                select method).FirstOrDefault();

//            var cConstructor = (from method in constructors
//                                where method.GetParentScopes<ITypeDefinition>().FirstOrDefault().Name == "C"
//                                select method).FirstOrDefault();

//            var methodCall = (from scope in globalScope.GetDescendantScopes()
//                              from call in scope.MethodCalls
//                              select call).FirstOrDefault();

//            Assert.IsNotNull(methodCall);
//            Assert.That(methodCall.IsConstructor);
//            Assert.AreSame(bConstructor, methodCall.FindMatches().FirstOrDefault());
//        }

//        [Test]
//        public void TestConstructorWithThisKeyword() {
//            // B.cs
//            //namespace A {
//            //    class B {
//            //        public B() : this(0) { }
//            //        public B(int i) { }
//            //    }
//            //}

//            string bXml = @"<namespace>namespace <name>A</name> <block>{
//    <class>class <name>B</name> <block>{
//        <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <member_list>: <call><name>this</name><argument_list>(<argument><expr><lit:literal type=""number"">0</lit:literal></expr></argument>)</argument_list></call> </member_list><block>{ }</block></constructor>
//        <constructor><specifier>public</specifier> <name>B</name><parameter_list>(<param><decl><type><name>int</name></type> <name>i</name></decl></param>)</parameter_list> <block>{ }</block></constructor>
//    }</block></class>
//}</block></namespace>";

//            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");

//            var globalScope = codeParser.ParseFileUnit(bUnit);

//            var constructors = from methodDefinition in globalScope.GetDescendantScopes<IMethodDefinition>()
//                               where methodDefinition.IsConstructor
//                               select methodDefinition;

//            var defaultConstructor = (from method in constructors
//                                      where method.Parameters.Count == 0
//                                      select method).FirstOrDefault();

//            var oneArgumentConstructor = (from method in constructors
//                                          where method.Parameters.Count == 1
//                                          select method).FirstOrDefault();

//            var methodCall = (from scope in globalScope.GetDescendantScopes()
//                              from call in scope.MethodCalls
//                              select call).FirstOrDefault();

//            Assert.IsNotNull(methodCall);
//            Assert.That(methodCall.IsConstructor);
//            Assert.AreSame(oneArgumentConstructor, methodCall.FindMatches().FirstOrDefault());
//        }

        [Test]
        public void TestCreateAliasesForFiles_UsingNamespace() {
            // using x.y.z;
            string xml = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

            var globalScope = codeParser.ParseFileUnit(xmlElement);
            Assert.AreEqual(1, globalScope.ChildStatements.Count);

            var actual = globalScope.ChildStatements[0] as ImportStatement;
            Assert.IsNotNull(actual);
            Assert.AreEqual("x . y . z", actual.ImportedNamespace.ToString());
        }

        [Test]
        public void TestCreateAliasesForFiles_UsingAlias() {
            // using x = Foo.Bar.Baz;
            string xml = @"<using>using <name>x</name> <init>= <expr><name><name>Foo</name><op:operator>.</op:operator><name>Bar</name><op:operator>.</op:operator><name>Baz</name></name></expr></init>;</using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            string xmlB = @"<namespace>namespace <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name> <block>{}</block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cs");
            //A.cs
            //using x.y.z;
            //foo = 17;
            string xmlA = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>
<expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            
            var scopeA = codeParser.ParseFileUnit(xmlElementA);
            var scopeB = codeParser.ParseFileUnit(xmlElementB);
            var globalScope = scopeA.Merge(scopeB);
            Assert.AreEqual(3, globalScope.ChildStatements.Count);
            var foo = globalScope.ChildStatements[1].Content.GetDescendantsAndSelf<NameUse>().FirstOrDefault(n => n.Name == "foo");
            Assert.IsNotNull(foo);
            var aliases = foo.GetImports().ToList();
            Assert.AreEqual(1, aliases.Count);
            Assert.AreEqual("x . y . z", aliases[0].ImportedNamespace.ToString());

            var nsd = globalScope.GetDescendants<NamespaceDefinition>().FirstOrDefault(ns => ns.Name == "z");
            Assert.IsNotNull(nsd);
            var zUse = aliases[0].ImportedNamespace.GetDescendantsAndSelf<NameUse>().LastOrDefault();
            Assert.IsNotNull(zUse);
            Assert.AreEqual("z", zUse.Name);
            Assert.AreSame(nsd, zUse.FindMatches().First());
        }

        [Test]
        public void TestGetImports_NestedImportNamespace() {
            //A.cs
            //namespace bar.baz {}
            string xmlA = @"<namespace>namespace <name><name>bar</name><op:operator>.</op:operator><name>baz</name></name> <block>{}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            //B.cs
            //using x.y.z;
            //if(bar) {
            //  using bar.baz;
            //  foo = 17;
            //}
            string xmlB = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>
<if>if<condition>(<expr><name>bar</name></expr>)</condition><then> <block>{
  <using>using <name><name>bar</name><op:operator>.</op:operator><name>baz</name></name>;</using>
  <expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cs");

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
        [Category("Todo")]
        public void TestGetImports_SeparateFiles() {
            //A.cs
            //using x.y.z;
            //Foo = 17;
            string xmlA = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>
<expr_stmt><expr><name>Foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            //B.cs
            //using a.b.howdy;
            //Bar();
            string xmlB = @"<using>using <name><name>a</name><op:operator>.</op:operator><name>b</name><op:operator>.</op:operator><name>howdy</name></name>;</using>
<expr_stmt><expr><call><name>Bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cs");

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
            string xmlA = @"<namespace>namespace <name>bar</name> <block>{
  <class>class <name>baz</name> <block>{}</block></class>
}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            //B.cs
            //using x.y.z;
            //if(bar) {
            //  using x = bar.baz;
            //  foo = 17;
            //}
            string xmlB = @"<using>using <name><name>x</name><op:operator>.</op:operator><name>y</name><op:operator>.</op:operator><name>z</name></name>;</using>
<if>if<condition>(<expr><name>bar</name></expr>)</condition><then> <block>{
  <using>using <name>x</name> <init>= <expr><name><name>bar</name><op:operator>.</op:operator><name>baz</name></name></expr></init>;</using>
  <expr_stmt><expr><name>foo</name> <op:operator>=</op:operator> <lit:literal type=""number"">17</lit:literal></expr>;</expr_stmt>
}</block></then></if>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "A.cs");
            
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
            string xmlA = @"<using>using <name><name>Foo</name><op:operator>.</op:operator><name>Bar</name></name>;</using>

<namespace>namespace <name>A</name> <block>{
  <class><specifier>public</specifier> class <name>Robot</name> <block>{
    <function><type><specifier>public</specifier> <name>Baz</name></type> <name>GetThingy</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><op:operator>new</op:operator> <call><name>Baz</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
  }</block></class>
}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            //B.cs
            //namespace Foo.Bar {
            //  public class Baz {
            //    public Baz() { }
            //  }
            //}
            string xmlB = @"<namespace>namespace <name><name>Foo</name><op:operator>.</op:operator><name>Bar</name></name> <block>{
  <class><specifier>public</specifier> class <name>Baz</name> <block>{
    <constructor><specifier>public</specifier> <name>Baz</name><parameter_list>()</parameter_list> <block>{ }</block></constructor>
  }</block></class>
}</block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cs");
            
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
            //A.cs
            //namespace Foo.Bar {
            //  public class Baz {
            //    public static void DoTheThing() { };
            //  }
            //}
            string xmlA = @"<namespace>namespace <name><name>Foo</name><op:operator>.</op:operator><name>Bar</name></name> <block>{
  <class><specifier>public</specifier> class <name>Baz</name> <block>{
    <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>DoTheThing</name><parameter_list>()</parameter_list> <block>{ }</block></function><empty_stmt>;</empty_stmt>
  }</block></class>
}</block></namespace>";
            XElement xmlElementA = fileSetup.GetFileUnitForXmlSnippet(xmlA, "A.cs");
            //B.cs
            //using Baz = Foo.Bar.Baz;
            //namespace A {
            //  public class B {
            //    public B() {
            //      Baz.DoTheThing();
            //    }
            //  }
            //}
            string xmlB = @"<using>using <name>Baz</name> <init>= <expr><name><name>Foo</name><op:operator>.</op:operator><name>Bar</name><op:operator>.</op:operator><name>Baz</name></name></expr></init>;</using>
<namespace>namespace <name>A</name> <block>{
  <class><specifier>public</specifier> class <name>B</name> <block>{
    <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <block>{
      <expr_stmt><expr><call><name><name>Baz</name><op:operator>.</op:operator><name>DoTheThing</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
    }</block></constructor>
  }</block></class>
}</block></namespace>";
            XElement xmlElementB = fileSetup.GetFileUnitForXmlSnippet(xmlB, "B.cs");
            
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
        public void TestUsingBlock_SingleDecl() {
            //using(var f = File.Open("out.txt")) {
            //  ;
            //}
            string xml = @"<using>using(<decl><type><name>var</name></type> <name>f</name> <init>= <expr><call><name><name>File</name><op:operator>.</op:operator><name>Open</name></name><argument_list>(<argument><expr><lit:literal type=""string"">""out.txt""</lit:literal></expr></argument>)</argument_list></call></expr></init></decl>) <block>{
  <empty_stmt>;</empty_stmt>
}</block></using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            string xml = @"<using>using(<decl><type><name>Foo</name></type> <name>a</name> <init>= <expr><op:operator>new</op:operator> <call><name>Foo</name><argument_list>(<argument><expr><lit:literal type=""number"">1</lit:literal></expr></argument>)</argument_list></call></expr></init><op:operator>,</op:operator> <name>b</name> <init>= <expr><op:operator>new</op:operator> <call><name>Foo</name><argument_list>(<argument><expr><lit:literal type=""number"">2</lit:literal></expr></argument>)</argument_list></call></expr></init></decl>) <block>{ <empty_stmt>;</empty_stmt> }</block></using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            string xml = @"<using>using(<expr><name>bar</name> <op:operator>=</op:operator> <op:operator>new</op:operator> <call><name>Foo</name><argument_list>()</argument_list></call></expr>) <block>{ <empty_stmt>;</empty_stmt> }</block></using>";
            XElement xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <super>: <name>Baz</name></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<class><specifier>public</specifier> class <name>Foo</name> <super>: <name>Baz</name>, <name><name>System</name><op:operator>.</op:operator><name>IDisposable</name></name></super> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<namespace>namespace <name><name>Example</name><op:operator>.</op:operator><name>Level2</name><op:operator>.</op:operator><name>Level3</name></name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    }</block></class>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<class type=""interface""><specifier>public</specifier> interface <name>Foo</name> <block>{
    <function_decl><type><specifier>public</specifier> <name>int</name></type> <name>GetBar</name><parameter_list>()</parameter_list>;</function_decl>
}</block></class>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
            var globalScope = codeParser.ParseFileUnit(fooFileUnit);

            Assert.AreEqual(1, globalScope.ChildStatements.Count());
            var foo = globalScope.ChildStatements.First() as TypeDefinition;
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
            string fooXml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
    }</block></class>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<namespace>namespace <name>Watermelon</name> <block>{
    <namespace>namespace <name><name>Example</name><op:operator>.</op:operator><name>Level2</name><op:operator>.</op:operator><name>Level3</name></name> <block>{
        <class><specifier>public</specifier> class <name>Foo</name> <block>{
            <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
        }</block></class>
    }</block></namespace>
}</block></namespace>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string fooXml = @"<struct><specifier>public</specifier> struct <name>Foo</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>bar</name></decl>;</decl_stmt>
}</block></struct>";
            var fooFileUnit = fileSetup.GetFileUnitForXmlSnippet(fooXml, "Foo.cs");
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
            string xml = @"<class>class <name>A</name> <block>{
    <class>class <name>B</name> <block>{}</block></class>
}</block></class>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
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
            string xml = @"<namespace>namespace <name>Foo</name> <block>{
    <class>class <name>A</name> <block>{
        <class>class <name>B</name> <block>{}</block></class>
    }</block></class>
}</block></namespace>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
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
        [Category("Todo")]
        public void TestDeclarationWithTypeVarFromConstructor() {
            // B.cs namespace A { class B { public B() { }; } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <constructor><specifier>public</specifier> <name>B</name><parameter_list>()</parameter_list> <block>{ }</block></constructor><empty_stmt>;</empty_stmt> }</block></class> }</block></namespace>";
            // C.cs namespace A { class C { void main() { var b = new B(); } } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <block>{ <function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ <decl_stmt><decl><type><name>var</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");
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

            Assert.AreSame(typeB, varDecl.VariableType.ResolveType().FirstOrDefault());
        }

        [Test]
        [Category("Todo")]
        public void TestDeclarationWithTypeVarFromMethod() {
            //namespace A {
            //    class B {
            //        public static void main() { var b = getB(); }
            //        public static B getB() { return new B(); }
            //    }
            //}
            string xml = @"<namespace>namespace <name>A</name> <block>{
    <class>class <name>B</name> <block>{
        <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{ <decl_stmt><decl><type><name>var</name></type> <name>b</name> =<init> <expr><call><name>getB</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt> }</block></function>
        <function><type><specifier>public</specifier> <specifier>static</specifier> <name>B</name></type> <name>getB</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
            var globalScope = codeParser.ParseFileUnit(unit);

            var typeB = globalScope.GetDescendants<TypeDefinition>().FirstOrDefault(t => t.Name == "B");
            Assert.IsNotNull(typeB);
            var mainMethod = globalScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "main");
            Assert.IsNotNull(mainMethod);

            Assert.AreEqual(1, mainMethod.ChildStatements.Count);
            var varDecl = mainMethod.ChildStatements[0].Content.GetDescendantsAndSelf<VariableDeclaration>().FirstOrDefault();
            Assert.IsNotNull(varDecl);

            Assert.AreSame(typeB, varDecl.VariableType.ResolveType().FirstOrDefault());
        }

        [Test]
        public void TestFieldCreation() {
            //// A.cs
            //class A {
            //    public int Foo;
            //}
            string xml = @"<class>class <name>A</name> <block>{
    <decl_stmt><decl><type><specifier>public</specifier> <name>int</name></type> <name>Foo</name></decl>;</decl_stmt>
}</block></class>";
            var xmlElement = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");
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
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <super>: <name>C</name></super> <block>{<private type=""default""> </private>}</block> <decl/></class>}</block></namespace>";

            // namespace A { class C { } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <block>{<private type=""default""> </private>}</block> <decl/></class>}</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "D.cs");

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
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <super>: <name><name>C</name><op:operator>.</op:operator><name>D</name></name></super> <block>{<private type=""default""> </private>}</block> <decl/></class>}</block></namespace>";

            // namespace C { class D { } }
            string dXml = @"<namespace>namespace <name>C</name> <block>{ <class>class <name>D</name> <block>{<private type=""default""> </private>}</block> <decl/></class>}</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(dXml, "D.cs");

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
            var xml = @"<class><specifier>public</specifier> class <name><name>B</name><argument_list>&lt;<argument><name>T</name></argument>&gt;</argument_list></name> <block>{ }</block></class>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
            var scope = codeParser.ParseFileUnit(unit);

            var typeB = scope.GetDescendants<TypeDefinition>().FirstOrDefault();
            Assert.IsNotNull(typeB);
            Assert.AreEqual("B", typeB.Name);
        }

        [Test]
        public void TestGenericVariableDeclaration() {
            //Dictionary<string,int> map;
            string xml = @"<decl_stmt><decl><type><name><name>Dictionary</name><argument_list>&lt;<argument><name>string</name></argument>,<argument><name>int</name></argument>&gt;</argument_list></name></type> <name>map</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cs");

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
            //System.Collection.Dictionary<string,int> map;
            string xml = @"<decl_stmt><decl><type><name><name>System</name><op:operator>.</op:operator><name>Collection</name><op:operator>.</op:operator><name><name>Dictionary</name><argument_list>&lt;<argument><name>string</name></argument>,<argument><name>int</name></argument>&gt;</argument_list></name></name></type> <name>map</name></decl>;</decl_stmt>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cs");

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
            //namespace Example {
            //    public class Foo {
            //        internal protected bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>internal</specifier> <specifier>protected</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");
            
            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForMethod_None() {
            //namespace Example {
            //    public class Foo {
            //        bool Bar() { return true; }
            //    }
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

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
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>public</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

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
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>protected</specifier> <specifier>internal</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

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
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{
        <function><type><specifier>protected</specifier> <specifier>static</specifier> <specifier>internal</specifier> <name>bool</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><lit:literal type=""boolean"">true</lit:literal></expr>;</return> }</block></function>
    }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var method = globalScope.GetDescendants<MethodDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, method.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_InternalProtected() {
            //namespace Example {
            //    internal protected class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>internal</specifier> <specifier>protected</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_None() {
            //namespace Example {
            //    class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class>class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.None, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_Normal() {
            //namespace Example {
            //    public class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>public</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.Public, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternal() {
            //namespace Example {
            //    protected internal class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>protected</specifier> <specifier>internal</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestGetAccessModifierForType_ProtectedInternalStatic() {
            //namespace Example {
            //    protected static internal class Foo {}
            //}
            string xml = @"<namespace>namespace <name>Example</name> <block>{
    <class><specifier>protected</specifier> <specifier>static</specifier> <specifier>internal</specifier> class <name>Foo</name> <block>{}</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "Foo.cs");

            var globalScope = codeParser.ParseFileUnit(unit);
            var type = globalScope.GetDescendants<TypeDefinition>().First();

            Assert.AreEqual(AccessModifier.ProtectedInternal, type.Accessibility);
        }

        [Test]
        public void TestMethodCallWithBaseKeyword() {
            // B.cs namespace A { class B { public virtual void Foo() { } } }
            string bXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <function><type><specifier>public</specifier> <specifier>virtual</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class> }</block></namespace>";
            // C.cs namespace A { class C : B { public override void Foo() { base.Foo(); } } }
            string cXml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>C</name> <super>: <name>B</name></super> <block>{ <function><type><specifier>public</specifier> <specifier>override</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>base</name><op:operator>.</op:operator><name>Foo</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");

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
            string xml = @"<function><type><name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithReturnTypeAndWithSpecifier() {
            //static int Foo() { }
            string xml = @"<function><type><specifier>static</specifier> <name>int</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.AreEqual("int", method.ReturnType.Name);
        }

        [Test]
        public void TestMethodDefinitionWithVoidReturn() {
            //void Foo() { }
            string xml = @"<function><type><name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ }</block></function>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "test.cpp");

            var testScope = codeParser.ParseFileUnit(testUnit);

            var method = testScope.GetDescendants<MethodDefinition>().FirstOrDefault(m => m.Name == "Foo");
            Assert.IsNotNull(method, "could not find the test method");

            Assert.IsNull(method.ReturnType, "return type should be null");
        }


        [Test]
        public void TestProperty() {
            // namespace A { class B { int Foo { get; set; } } }
            string xml = @"<namespace>namespace <name>A</name> <block>{ <class>class <name>B</name> <block>{ <decl_stmt><decl><type><name>int</name></type> <name>Foo</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt> }</block></class> }</block></namespace>";

            var testUnit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
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
            // B.cs
            //namespace A {
            //  class B {
            //    C Foo { get; set; }
            //  }
            //}
            string bXml = @"<namespace>namespace <name>A</name> <block>{
  <class>class <name>B</name> <block>{
    <decl_stmt><decl><type><name>C</name></type> <name>Foo</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt>
  }</block></class>
}</block></namespace>";
            // C.cs
            //namespace A {
            //	class C {
            //		static void main() {
            //			B b = new B();
            //			b.Foo.Bar();
            //		}
            //		void Bar() { }
            //	}
            //}
            string cXml = @"<namespace>namespace <name>A</name> <block>{
	<class>class <name>C</name> <block>{
		<function><type><specifier>static</specifier> <name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
			<decl_stmt><decl><type><name>B</name></type> <name>b</name> =<init> <expr><op:operator>new</op:operator> <call><name>B</name><argument_list>()</argument_list></call></expr></init></decl>;</decl_stmt>
			<expr_stmt><expr><call><name><name>b</name><op:operator>.</op:operator><name>Foo</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt>
		}</block></function>

		<function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
	}</block></class>
}</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");
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
            var bXml = @"<namespace>namespace <name>A</name> <block>{ <class><specifier>public</specifier> class <name>B</name> <block>{ <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class> }</block></namespace>";
            //namespace A { public class C { public void Foo() { B.Bar(); } } }
            var cXml = @"<namespace>namespace <name>A</name> <block>{ <class><specifier>public</specifier> class <name>C</name> <block>{ <function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>B</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");

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
            var bXml = @"<namespace>namespace <name>A</name> <block>{ <class><specifier>public</specifier> class <name>B</name> <block>{ <function><type><specifier>public</specifier> <specifier>static</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function> }</block></class> }</block></namespace>";
            //namespace C { public class D { public void Foo() { A.B.Bar(); } } }
            var dXml = @"<namespace>namespace <name>C</name> <block>{ <class><specifier>public</specifier> class <name>D</name> <block>{ <function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>A</name><op:operator>.</op:operator><name>B</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class> }</block></namespace>";

            var bUnit = fileSetup.GetFileUnitForXmlSnippet(bXml, "B.cs");
            var dUnit = fileSetup.GetFileUnitForXmlSnippet(dXml, "C.cs");

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
            string testXml = @"<decl_stmt><decl><type><specifier>static</specifier> <name>int</name></type> <name>A</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>public</specifier> <specifier>const</specifier> <name>int</name></type> <name>B</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>public</specifier> <specifier>static</specifier> <specifier>readonly</specifier> <name>Foo</name></type> <name>C</name></decl>;</decl_stmt>
<decl_stmt><decl><type><specifier>volatile</specifier>  <name>int</name></type> <name>D</name></decl>;</decl_stmt>";
            var testUnit = fileSetup.GetFileUnitForXmlSnippet(testXml, "test.cs");

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
            //	class B {
            //		public static B Instance { get; set; }
            //		public void Bar() { }
            //	}
            //	
            //	class C { public void Foo() { B.Instance.Bar(); } }
            //}
            var xml = @"<namespace>namespace <name>A</name> <block>{
	<class>class <name>B</name> <block>{
		<decl_stmt><decl><type><specifier>public</specifier> <specifier>static</specifier> <name>B</name></type> <name>Instance</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt>
		<function><type><specifier>public</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
	}</block></class>
	
	<class>class <name>C</name> <block>{ <function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>B</name><op:operator>.</op:operator><name>Instance</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function> }</block></class>
}</block></namespace>";
            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "A.cs");

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
            //	class B {
            //		public static B Instance { get; set; }
            //		public void Bar() { }
            //	}
            //}
            var aXml = @"<namespace>namespace <name>A</name> <block>{
	<class>class <name>B</name> <block>{
		<decl_stmt><decl><type><specifier>public</specifier> <specifier>static</specifier> <name>B</name></type> <name>Instance</name> <block>{ <function_decl><name>get</name>;</function_decl> <function_decl><name>set</name>;</function_decl> }</block></decl></decl_stmt>
		<function><type><specifier>public</specifier> <name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
	}</block></class>
}</block></namespace>";
            //using A;
            //
            //namespace C {
            //	class D {
            //		public void Foo() { B.Instance.Bar(); }
            //	}
            //}
            var cXml = @"<using>using <name>A</name>;</using>

<namespace>namespace <name>C</name> <block>{
	<class>class <name>D</name> <block>{
		<function><type><specifier>public</specifier> <name>void</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <expr_stmt><expr><call><name><name>B</name><op:operator>.</op:operator><name>Instance</name><op:operator>.</op:operator><name>Bar</name></name><argument_list>()</argument_list></call></expr>;</expr_stmt> }</block></function>
	}</block></class>
}</block></namespace>";
            var aUnit = fileSetup.GetFileUnitForXmlSnippet(aXml, "A.cs");
            var cUnit = fileSetup.GetFileUnitForXmlSnippet(cXml, "C.cs");
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
            //	public class B {
            //		void main() {
            //			Foo().Bar();
            //		}
            //
            //		C Foo() { return new C(); }
            //	}
            //
            //	public class C {
            //		void Bar() { }
            //	}
            //}
            var xml = @"<namespace>namespace <name>A</name> <block>{
	<class><specifier>public</specifier> class <name>B</name> <block>{
		<function><type><name>void</name></type> <name>main</name><parameter_list>()</parameter_list> <block>{
			<expr_stmt><expr><call><name>Foo</name><argument_list>()</argument_list></call><op:operator>.</op:operator><call><name>Bar</name><argument_list>()</argument_list></call></expr>;</expr_stmt>
		}</block></function>

		<function><type><name>C</name></type> <name>Foo</name><parameter_list>()</parameter_list> <block>{ <return>return <expr><op:operator>new</op:operator> <call><name>C</name><argument_list>()</argument_list></call></expr>;</return> }</block></function>
	}</block></class>

	<class><specifier>public</specifier> class <name>C</name> <block>{
		<function><type><name>void</name></type> <name>Bar</name><parameter_list>()</parameter_list> <block>{ }</block></function>
	}</block></class>
}</block></namespace>";

            var unit = fileSetup.GetFileUnitForXmlSnippet(xml, "B.cs");
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
    }
}