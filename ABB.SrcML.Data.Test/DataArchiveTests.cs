using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    class DataArchiveTests {
        [TestFixtureSetUp]
        public void ClassSetup() {
            if(Directory.Exists("DataArchiveTests")) {
                Directory.Delete("DataArchiveTests", true);
            }
        }
        
        [Test]
        public void TestFindMethodCalls_Simple() {
            using(var sa = new SrcMLArchive("DataArchiveTests")) {
                sa.AddOrUpdateFile(@"..\..\TestInputs\function_def.cpp");
                var da = new DataArchive(sa);
                var expected = da.GlobalScope.ChildScopes.OfType<MethodDefinition>().First(md => md.Name == "main").MethodCalls.First();
                var actual = da.FindMethodCalls(new SourceLocation(@"TestInputs\function_def.cpp", 12, 20));
                Assert.IsNotNull(actual);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(expected, actual[0]);
            }
        }

        [Test]
        public void TestFindMethodCalls_Nested() {
            using(var sa = new SrcMLArchive("DataArchiveTests")) {
                sa.AddOrUpdateFile(@"..\..\TestInputs\nested_method_calls.cpp");
                var da = new DataArchive(sa);
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

        //TODO: write tests that use the XPath overload
    }
}
