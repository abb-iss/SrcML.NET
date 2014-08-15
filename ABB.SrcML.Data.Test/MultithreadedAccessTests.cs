using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    public class MultithreadedAccessTests {

        //[Test]
        //public void TestQueryDuringUpdate() {
        //    var sourceFolder = "TestQueryDuringUpdate";
        //    var dataFolder = "TestQueryDuringUpdate_Data";
        //    Directory.CreateDirectory(sourceFolder);
        //    string fooSourcePath = Path.Combine(sourceFolder, "foo.cpp");
        //    string barSourcePath = Path.Combine(sourceFolder, "bar.cpp");

        //    var fooRevisions = new string[] { "void foo() { }", "void foo() { bar(); }" };

        //    File.WriteAllText(fooSourcePath, fooRevisions[1]);
        //    File.WriteAllText(barSourcePath, "void bar() { }");

        //    int iterations = 1000;
        //    using(var archive = new SrcMLArchive(dataFolder)) {
        //        using(var data = new DataRepository(archive)) {
        //            data.InitializeData();
        //            archive.AddOrUpdateFile(Path.Combine(sourceFolder, "foo.cpp"));
        //            archive.AddOrUpdateFile(Path.Combine(sourceFolder, "bar.cpp"));

        //            var developer = new Task(() => {
        //                for(int i = 0; i < iterations; i++) {
        //                    File.WriteAllText(fooSourcePath, fooRevisions[i % 2]);
        //                    archive.AddOrUpdateFile(fooSourcePath);
        //                }
        //            });

        //            developer.Start();
        //            Assert.DoesNotThrow(() => {
        //                for(int i = 0; i < iterations; i++) {
        //                    var foo = GetMethodWithName(data, 500, "foo");
        //                    Thread.Sleep(1);
        //                    var bar = GetMethodWithName(data, 500, "bar");

        //                    foo.ContainsCallTo(bar);
        //                    if(i % 10 == 0 && i > 0) {
        //                        Console.WriteLine("Finished {0} iterations", i);
        //                    }
        //                }
        //            });

        //            developer.Wait();
        //        }
        //    }
        //}

        //private static IMethodDefinition GetMethodWithName(IDataRepository dataRepository, int timeout, string methodName) {
        //    IMethodDefinition result = null;
        //    IScope globalScope;
        //    Assert.That(dataRepository.TryLockGlobalScope(timeout, out globalScope));
        //    try {
        //        result = globalScope.GetChildScopesWithId<IMethodDefinition>("foo").FirstOrDefault();
        //        return result;
        //    } finally {
        //        dataRepository.ReleaseGlobalScopeLock();
        //    }
            
        //}
    }
}