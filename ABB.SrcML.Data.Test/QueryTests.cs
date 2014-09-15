using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading;
using ABB.SrcML.Data.Queries;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class QueryTests {
        private const string TestDir = "DataRepositoryTests";
        private const string DataDir = "DataRepositoryTestsData";
        private const string SrcMLDir = @"..\..\External\SrcML";

        [SetUp]
        public void TestSetup() {
            if(Directory.Exists(TestDir)) {
                Directory.Delete(TestDir, true);
            }
            if(Directory.Exists(DataDir)) {
                Directory.Delete(DataDir, true);
            }
            Directory.CreateDirectory(TestDir);
            Directory.CreateDirectory(DataDir);
        }

        [TestFixtureTearDown]
        public void ClassTearDown() {
            if(Directory.Exists(TestDir)) {
                Directory.Delete(TestDir, true);
            }
            if(Directory.Exists(DataDir)) {
                Directory.Delete(DataDir, true);
            }
        }

        [Test]
        public void TestFindMethodCalls_Nested() {
            File.Copy(@"..\..\TestInputs\nested_method_calls.cpp", Path.Combine(TestDir, "nested_method_calls.cpp"));

            using(var dataProj = new DataProject<CompleteWorkingSet>(DataDir, TestDir, SrcMLDir)) {
                dataProj.Update();

                NamespaceDefinition globalScope;
                MethodCall[] expected;
                Assert.That(dataProj.WorkingSet.TryObtainReadLock(Timeout.Infinite, out globalScope));
                try {
                    expected = new[] {
                        globalScope.FindExpressions<MethodCall>(true).First(mc => mc.Name == "ToString"),
                        globalScope.FindExpressions<MethodCall>(true).First(mc => mc.Name == "SomeMethodCall"),
                        globalScope.FindExpressions<MethodCall>(true).First(mc => mc.Name == "printf")
                    };
                } finally {
                    dataProj.WorkingSet.ReleaseReadLock();
                }

                var query = new FindMethodCallsAtLocationQuery(dataProj.WorkingSet, Timeout.Infinite);
                var testFile = Path.GetFullPath(Path.Combine(TestDir, "nested_method_calls.cpp"));
                var actual = query.Execute(new SourceLocation(testFile, 4, 41));
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Length, actual.Count);
                for(int i = 0; i < expected.Length; i++) {
                    Assert.AreSame(expected[i], actual[i]);
                }

            }
        }

        [Test]
        public void TestFindMethodCalls_Simple() {
            File.Copy(@"..\..\TestInputs\function_def.cpp", Path.Combine(TestDir, "function_def.cpp"));

            using(var dataProj = new DataProject<CompleteWorkingSet>(DataDir, TestDir, SrcMLDir)) {
                dataProj.Update();

                NamespaceDefinition globalScope;
                MethodCall expected;
                Assert.That(dataProj.WorkingSet.TryObtainReadLock(Timeout.Infinite, out globalScope));
                try {
                    expected = globalScope.FindExpressions<MethodCall>(true).First(mc => mc.Name == "MyFunction");
                } finally {
                    dataProj.WorkingSet.ReleaseReadLock();
                }

                var query = new FindMethodCallsAtLocationQuery(dataProj.WorkingSet, Timeout.Infinite);
                var testFile = Path.GetFullPath(Path.Combine(TestDir, "function_def.cpp"));
                var actual = query.Execute(new SourceLocation(testFile, 12, 20));
                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                Assert.AreSame(expected, actual[0]);
            }
        }

        [Test]
        public void TestFindScopeForAdjacentMethods() {
            File.Copy(@"..\..\TestInputs\adjacent_methods.cpp", Path.Combine(TestDir, "adjacent_methods.cpp"));

            using(var dataProj = new DataProject<CompleteWorkingSet>(DataDir, TestDir, SrcMLDir)) {
                dataProj.Update();

                NamespaceDefinition globalScope;
                MethodDefinition mainMethod;
                MethodDefinition fooMethod;
                Assert.That(dataProj.WorkingSet.TryObtainReadLock(Timeout.Infinite, out globalScope));
                try {
                    mainMethod = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "main");
                    fooMethod = globalScope.GetDescendants<MethodDefinition>().First(md => md.Name == "Foo");
                } finally {
                    dataProj.WorkingSet.ReleaseReadLock();
                }

                var query = new StatementForLocationQuery(dataProj.WorkingSet, Timeout.Infinite);
                var testFile = Path.GetFullPath(Path.Combine(TestDir, "adjacent_methods.cpp"));

                var startOfMain = new SourceLocation(testFile, 1, 1);
                var locationInMain = new SourceLocation(testFile, 1, 11);
                Assert.That(mainMethod.PrimaryLocation.Contains(startOfMain));
                Assert.That(mainMethod.PrimaryLocation.Contains(locationInMain));

                Assert.AreSame(mainMethod, query.Execute(startOfMain));
                Assert.AreSame(mainMethod, query.Execute(locationInMain));

                var startOfFoo = new SourceLocation(testFile, 3, 1);
                var locationInFoo = new SourceLocation(testFile, 3, 11);
                Assert.That(fooMethod.PrimaryLocation.Contains(startOfFoo));
                Assert.That(fooMethod.PrimaryLocation.Contains(locationInFoo));

                Assert.AreSame(fooMethod, query.Execute(startOfFoo));
                Assert.AreSame(fooMethod, query.Execute(locationInFoo));

                var lineBetweenMethods = new SourceLocation(testFile, 2, 1);
                Assert.That(mainMethod.PrimaryLocation.Contains(lineBetweenMethods));
                Assert.IsFalse(fooMethod.PrimaryLocation.Contains(lineBetweenMethods));
                Assert.AreSame(mainMethod, query.Execute(lineBetweenMethods));
            }
        }

        [Test]
        public void TestOverloadedMethodCallResolution() {
            File.Copy(@"..\..\TestInputs\csharp_overload_calls.cs", Path.Combine(TestDir, "csharp_overload_calls.cs"));
            using(var project = new DataProject<CompleteWorkingSet>(DataDir, TestDir, SrcMLDir)) {
                project.Update();

                var query = new StatementForLocationQuery<MethodDefinition>(project.WorkingSet, Timeout.Infinite);

                var startOffoo0 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 11, 25);
                var startOffoo1 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 13, 25);

                var startOfCallFooWithoutParameters = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 4, 25);
                var startOfCallFooWithParameter = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 8, 25);

                var foo0 = query.Execute(startOffoo0);
                Assert.AreEqual("foo", foo0.Name);
                Assert.AreEqual(0, foo0.Parameters.Count);

                var foo1 = query.Execute(startOffoo1);
                Assert.AreEqual("foo", foo1.Name);
                Assert.AreEqual(1, foo1.Parameters.Count);

                var callFooMethodWithoutParameters = query.Execute(startOfCallFooWithoutParameters);
                Assert.AreEqual("CallFooWithoutParameters", callFooMethodWithoutParameters.Name);

                var callFooMethodWithParameter = query.Execute(startOfCallFooWithParameter);
                Assert.AreEqual("CallFooWithParameter", callFooMethodWithParameter.Name);

                Assert.That(callFooMethodWithoutParameters.ContainsCallTo(foo0));
                Assert.That(!callFooMethodWithoutParameters.ContainsCallTo(foo1));

                Assert.That(callFooMethodWithParameter.ContainsCallTo(foo1));
                Assert.That(!callFooMethodWithParameter.ContainsCallTo(foo0));

                var callNoParams = callFooMethodWithoutParameters.FindExpressions<MethodCall>(true).FirstOrDefault();
                var matches = callNoParams.FindMatches().ToList();
                Assert.AreEqual(1, matches.Count);
                Assert.AreSame(foo0, matches[0]);

                var callWithParam = callFooMethodWithParameter.FindExpressions<MethodCall>(true).FirstOrDefault();
                matches = callWithParam.FindMatches().ToList();
                Assert.AreEqual(1, matches.Count);
                Assert.AreSame(foo1, matches[0]);
            }
        }

        [Test]
        public void TestOverloadedMethodCallResolutionWithCallingObject() {
            File.Copy(@"..\..\TestInputs\csharp_overload_callingobj.cs", Path.Combine(TestDir, "csharp_overload_callingobj.cs"));
            File.Copy(@"..\..\TestInputs\csharp_overload_calls.cs", Path.Combine(TestDir, "csharp_overload_calls.cs"));
            using(var project = new DataProject<CompleteWorkingSet>(DataDir, TestDir, SrcMLDir)) {
                project.Update();

                var query = new StatementForLocationQuery<MethodDefinition>(project.WorkingSet, Timeout.Infinite);

                var startOffoo0 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 11, 25);
                var startOffoo1 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_calls.cs")), 13, 25);

                var startOfRunFoo0 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_callingobj.cs")), 5, 25);
                var startOfRunFoo1 = new SourceLocation(Path.GetFullPath(Path.Combine(TestDir, "csharp_overload_callingobj.cs")), 7, 25);

                var foo0 = query.Execute(startOffoo0);
                Assert.AreEqual("foo", foo0.Name);
                Assert.AreEqual(0, foo0.Parameters.Count);

                var foo1 = query.Execute(startOffoo1);
                Assert.AreEqual("foo", foo1.Name);
                Assert.AreEqual(1, foo1.Parameters.Count);

                var runFoo0 = query.Execute(startOfRunFoo0);
                Assert.AreEqual("runFoo0", runFoo0.Name);

                var runFoo1 = query.Execute(startOfRunFoo1);
                Assert.AreEqual("runFoo1", runFoo1.Name);

                Assert.That(runFoo0.ContainsCallTo(foo0));
                Assert.That(!runFoo0.ContainsCallTo(foo1));

                Assert.That(runFoo1.ContainsCallTo(foo1));
                Assert.That(!runFoo1.ContainsCallTo(foo0));

                var callNoParams = runFoo0.FindExpressions<MethodCall>(true).FirstOrDefault();
                var matches = callNoParams.FindMatches().ToList();
                Assert.AreEqual(1, matches.Count);
                Assert.AreSame(foo0, matches[0]);

                var callWithParam = runFoo1.FindExpressions<MethodCall>(true).FirstOrDefault();
                matches = callWithParam.FindMatches().ToList();
                Assert.AreEqual(1, matches.Count);
                Assert.AreSame(foo1, matches[0]);
            }
        }

        //TODO: write tests that use the XPath overload
    }
}