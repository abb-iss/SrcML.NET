using NUnit.Framework;
using System.IO;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class DataRepositoryTests {

        [Test]
        public void TestFindMethodCalls_Nested() {
            using(var sa = new SrcMLArchive("DataRepositoryTests")) {
                sa.AddOrUpdateFile(@"..\..\TestInputs\nested_method_calls.cpp");

                using(var da = new DataRepository(sa)) {
                    da.InitializeData();
                    var foo = da.GlobalScope.ChildScopes.OfType<MethodDefinition>().First(md => md.Name == "Foo");
                    var expected = new[]
                               {
                                   foo.MethodCalls.First(mc => mc.Name == "ToString"),
                                   foo.MethodCalls.First(mc => mc.Name == "SomeMethodCall"),
                                   foo.MethodCalls.First(mc => mc.Name == "printf")
                               };

                    var actual = da.FindMethodCalls(new SourceLocation(@"TestInputs\nested_method_calls.cpp", 4, 41));
                    Assert.IsNotNull(actual);
                    Assert.AreEqual(expected.Length, actual.Count);
                    for(int i = 0; i < expected.Length; i++) {
                        Assert.AreEqual(expected[i], actual[i]);
                    }
                }
            }
        }

        [Test]
        public void TestFindMethodCalls_Simple() {
            using(var sa = new SrcMLArchive("DataRepositoryTests")) {
                sa.AddOrUpdateFile(@"..\..\TestInputs\function_def.cpp");

                using(var da = new DataRepository(sa)) {
                    da.InitializeData();
                    var expected = da.GlobalScope.ChildScopes.OfType<MethodDefinition>().First(md => md.Name == "main").MethodCalls.First();
                    var actual = da.FindMethodCalls(new SourceLocation(@"TestInputs\function_def.cpp", 12, 20));
                    Assert.IsNotNull(actual);
                    Assert.AreEqual(1, actual.Count);
                    Assert.AreEqual(expected, actual[0]);
                }
            }
        }

        [Test]
        public void TestFindScopeForAdjacentMethods() {
            using(var sa = new SrcMLArchive("DataRepositoryTests")) {
                sa.AddOrUpdateFile(@"..\..\TestInputs\adjacent_methods.cpp");

                using(var da = new DataRepository(sa)) {
                    da.InitializeData();
                    var mainMethod = da.GlobalScope.GetDescendantScopes<MethodDefinition>().FirstOrDefault();
                    var fooMethod = da.GlobalScope.GetDescendantScopes<MethodDefinition>().LastOrDefault();

                    Assert.IsNotNull(mainMethod, "could not find main()");
                    Assert.IsNotNull(fooMethod, "could not find foo()");
                    Assert.AreEqual("main", mainMethod.Name);
                    Assert.AreEqual("Foo", fooMethod.Name);

                    var fileName = @"TestInputs\adjacent_methods.cpp";

                    var startOfMain = new SourceLocation(fileName, 1, 1);
                    var locationInMain = new SourceLocation(fileName, 1, 11);

                    Assert.That(mainMethod.PrimaryLocation.Contains(startOfMain));
                    Assert.That(mainMethod.PrimaryLocation.Contains(locationInMain));

                    Assert.AreEqual(mainMethod, da.FindScope(startOfMain));
                    Assert.AreEqual(mainMethod, da.FindScope(locationInMain));

                    var startOfFoo = new SourceLocation(fileName, 3, 1);
                    var locationInFoo = new SourceLocation(fileName, 3, 11);

                    Assert.That(fooMethod.PrimaryLocation.Contains(startOfFoo));
                    Assert.That(fooMethod.PrimaryLocation.Contains(locationInFoo));

                    Assert.AreEqual(fooMethod, da.FindScope(startOfFoo));
                    Assert.AreEqual(fooMethod, da.FindScope(locationInFoo));

                    var lineBetweenMethods = new SourceLocation(fileName, 2, 1);
                    Assert.That(mainMethod.PrimaryLocation.Contains(lineBetweenMethods));
                    Assert.IsFalse(fooMethod.PrimaryLocation.Contains(lineBetweenMethods));
                    Assert.AreEqual(mainMethod, da.FindScope(lineBetweenMethods));
                }
            }
        }

        [SetUp]
        public void TestSetup() {
            if(Directory.Exists("DataRepositoryTests")) {
                Directory.Delete("DataRepositoryTests", true);
            }
        }

        //TODO: write tests that use the XPath overload
    }
}