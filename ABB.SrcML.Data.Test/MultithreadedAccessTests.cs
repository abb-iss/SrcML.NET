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

        [Test]
        public void TestQueryDuringUpdate() {
            var sourceFolder = "TestQueryDuringUpdate";
            var dataFolder = "TestQueryDuringUpdate_Data";
            Directory.CreateDirectory(sourceFolder);
            string fooSourcePath = Path.Combine(sourceFolder, "foo.cpp");
            string barSourcePath = Path.Combine(sourceFolder, "bar.cpp");

            var fooRevisions = new string[] { "void foo() { }", "void foo() { bar(); }" };

            File.WriteAllText(fooSourcePath, fooRevisions[1]);
            File.WriteAllText(barSourcePath, "void bar() { }");

            int iterations = 1000;
            using(var project = new DataProject<CompleteWorkingSet>(dataFolder, sourceFolder, "SrcML")) {
                project.Update();
                project.StartMonitoring();

                var developer = new Task(() => {
                    for(int i = 0; i < iterations; i++) {
                        File.WriteAllText(fooSourcePath, fooRevisions[i % 2]);
                    }
                });

                developer.Start();
                Assert.DoesNotThrow(() => {
                    for(int i = 0; i < iterations; i++) {
                        var foo = GetMethodWithName(project.WorkingSet, 500, "foo");
                        var bar = GetMethodWithName(project.WorkingSet, 500, "bar");
                        foo.ContainsCallTo(bar);
                        if(i % 10 == 0 && i > 0) {
                            Console.WriteLine("Finished {0} iterations", i);
                        }
                    }
                });
                developer.Wait();
            }
        }

        private static MethodDefinition GetMethodWithName(AbstractWorkingSet workingSet, int timeout, string methodName) {
            MethodDefinition result = null;
            NamespaceDefinition globalScope;
            Assert.That(workingSet.TryObtainReadLock(timeout, out globalScope));
            try {
                result = globalScope.GetNamedChildren<MethodDefinition>("foo").FirstOrDefault();
                return result;
            } finally {
                workingSet.ReleaseReadLock();
            }

        }
    }
}