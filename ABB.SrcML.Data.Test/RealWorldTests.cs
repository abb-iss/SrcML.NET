/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    class RealWorldTests {
        private bool shouldRegenerateSrcML = false;

        [Test]
        public void TestFileUnitParsing_NotepadPlusPlus() {
            string npp62SourcePath = @"C:\Workspace\Source\Notepad++\6.2";
            string npp62DataPath = @"C:\Workspace\Source\Notepad++\6.2-data";
            string callLogPath = Path.Combine(npp62DataPath, "methodcalls.log");
            bool regenerateSrcML = shouldRegenerateSrcML;

            if(!Directory.Exists(npp62SourcePath)) {
                Assert.Ignore("Source code for Notepad++ 6.2 is missing");
            }
            if(File.Exists(callLogPath)) {
                File.Delete(callLogPath);
            }

            if(!Directory.Exists(npp62DataPath)) {
                regenerateSrcML = true;
            } else if(shouldRegenerateSrcML) {
                Directory.Delete(npp62DataPath, true);
            }
            
            var archive = new SrcMLArchive(npp62DataPath, regenerateSrcML);
            AbstractFileMonitor monitor = new FileSystemFolderMonitor(npp62SourcePath, npp62DataPath, new LastModifiedArchive(npp62DataPath), archive);

            ManualResetEvent mre = new ManualResetEvent(false);
            Stopwatch sw = new Stopwatch();
            bool startupCompleted = false;

            monitor.StartupCompleted += (o,e) => {
                sw.Stop();
                startupCompleted = true;
                mre.Set();
            };

            sw.Start();
            monitor.Startup();
            startupCompleted = mre.WaitOne(60000);
            if(sw.IsRunning) {
                sw.Stop();
            }
            var srcMLGenerationElapsed = sw.ElapsedMilliseconds;
            Assert.That(startupCompleted);

            AbstractCodeParser parser = new CPlusPlusCodeParser();
            Scope globalScope = null;
            sw.Reset();
            
            int numberOfFailures = 0;
            int numberOfSuccesses = 0;
            int numberOfFiles = 0;
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
            
            foreach(var unit in archive.FileUnits) {
                numberOfFiles++;
                var fileName = parser.GetFileNameForUnit(unit);
                Console.Write("Parsing {0}", fileName);
                try {
                    sw.Start();
                    var scopeForUnit = parser.ParseFileUnit(unit);

                    if(null == globalScope) {
                        globalScope = scopeForUnit;
                    } else {
                        globalScope = globalScope.Merge(scopeForUnit);
                    }
                    sw.Stop();
                    Console.WriteLine(" PASSED");
                    numberOfSuccesses++;
                } catch(Exception e) {
                    sw.Stop();
                    Console.WriteLine(" FAILED");
                    var key = e.StackTrace.Split('\n')[0].Trim();
                    if(!errors.ContainsKey(key)) {
                        errors[key] = new List<string>();
                    }
                    errors[key].Add(fileName);

                    numberOfFailures++;
                }
            }

            Console.WriteLine("\nSummary");
            Console.WriteLine("=======");

            Console.WriteLine("{0} to {1} srcML", TimeSpan.FromMilliseconds(srcMLGenerationElapsed), (regenerateSrcML ? "generate" : "verify"));
            Console.WriteLine("{0,10:N0} failures  ({1,7:P2})", numberOfFailures, ((float)numberOfFailures) / numberOfFiles);
            Console.WriteLine("{0,10:N0} successes ({1,7:P2})", numberOfSuccesses, ((float)numberOfSuccesses) / numberOfFiles);
            Console.WriteLine("{0} to generate data", sw.Elapsed);

            Console.WriteLine("\nScope Breakdown");
            Console.WriteLine("===============");

            Console.WriteLine("{0,10:N0} scopes", VariableScopeIterator.Visit(globalScope).Count());
            var namedScopes = from scope in VariableScopeIterator.Visit(globalScope)
                              where (scope as NamedScope) != null
                              select scope;
            Console.WriteLine("{0,10:N0} named scopes", namedScopes.Count());
            var namespaceScopes = from scope in namedScopes
                                  where (scope as NamespaceDefinition) != null
                                  select scope;
            var typeScopes = from scope in namedScopes
                             where (scope as TypeDefinition) != null
                             select scope;
            var methodScopes = from scope in namedScopes
                                  where (scope as MethodDefinition) != null
                                  select scope;
            Console.WriteLine("{0,10:N0} namespaces", namespaceScopes.Count());
            Console.WriteLine("{0,10:N0} types", typeScopes.Count());
            Console.WriteLine("{0,10:N0} methods", methodScopes.Count());

            Console.WriteLine("\nMethod Calls");
            Console.WriteLine("============");
            var methodCalls = from scope in VariableScopeIterator.Visit(globalScope)
                              from call in scope.MethodCalls
                              select call;

            int numMethodCalls = 0;
            int numMatchedMethodCalls = 0;
            sw.Reset();
            using(var callLog = new StreamWriter(callLogPath)) {
                foreach(var call in methodCalls) {
                    sw.Start();
                    var match = call.FindMatches().FirstOrDefault();
                    sw.Stop();
                    numMethodCalls++;
                    if(null != match) {
                        numMatchedMethodCalls++;
                        callLog.WriteLine("{0} ({1}:{2}) -> {3} ({4}:{5})", call.Name, call.Location.SourceFileName, call.Location.SourceLineNumber, match.Name, match.PrimaryLocation.SourceFileName, match.PrimaryLocation.SourceLineNumber);
                    }
                }
            }

            Console.WriteLine("{0,10:N0} method calls", numMethodCalls);
            Console.WriteLine("{0,10:N0} matched method calls ({1,7:P2})", numMatchedMethodCalls, ((float)numMatchedMethodCalls) / numMethodCalls);
            Console.WriteLine("{0} to match the method calls", sw.Elapsed);
            Console.WriteLine("{0,10:N0} ms / match", ((float) sw.ElapsedMilliseconds) / numMethodCalls);
            Console.WriteLine("See matched method calls in {0}", callLogPath);

            Console.WriteLine("\nErrors");
            Console.WriteLine("======");
            var sortedErrors = from kvp in errors
                               orderby kvp.Value.Count descending
                               select kvp;

            foreach(var kvp in sortedErrors) {
                Console.WriteLine("{0} exceptions {1}", kvp.Value.Count, kvp.Key);
                foreach(var fileName in kvp.Value) {
                    Console.WriteLine("\t{0}", fileName);
                }
            }

            monitor.Dispose();
            Assert.AreEqual(numberOfFailures, (from e in errors.Values select e.Count).Sum());
            Assert.AreEqual(0, numberOfFailures);
        }
    }
}
