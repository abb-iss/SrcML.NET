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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("LongRunning")]
    class RealWorldTests {
        private bool shouldRegenerateSrcML = true;
        private Dictionary<Language, AbstractCodeParser> CodeParser;

        [TestFixtureSetUp]
        public void ClassSetup() {
            CodeParser = new Dictionary<Language, AbstractCodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
                { Language.CSharp, new CSharpCodeParser() }
            };
        }

        [Test]
        public void TestFileUnitParsing_NotepadPlusPlus() {
            string npp62SourcePath = @"C:\Workspace\Source\Notepad++\6.2";
            string npp62DataPath = @"C:\Workspace\SrcMLData\NPP-6.2";
            
            Console.WriteLine("\nReal world test: Notepad++ 6.2 (C++)");
            Console.WriteLine("=======================================");
            TestDataGeneration(npp62SourcePath, npp62DataPath);
        }

        [Test]
        public void TestFileUnitParsing_NotepadPlusPlus_Concurrent() {
            string npp62SourcePath = @"C:\Workspace\Source\Notepad++\6.2";
            string npp62DataPath = @"C:\Workspace\SrcMLData\concurrent\NPP-6.2";

            Console.WriteLine("\nReal world test: Notepad++ 6.2 (C++, concurrent)");
            Console.WriteLine("=======================================");
            TestDataGeneration(npp62SourcePath, npp62DataPath, true);
        }

        [Test]
        public void TestFileUnitParsing_Bullet() {
            string bullet281SourcePath = @"C:\Workspace\Source\bullet\2.81";
            string bullet281DataPath = @"C:\Workspace\SrcMLData\bullet-2.81";

            Console.WriteLine("\nReal World Test: Bullet 2.81 (C++)");
            Console.WriteLine("=======================================");
            TestDataGeneration(bullet281SourcePath, bullet281DataPath);
        }

        [Test]
        public void TestFileUnitParsing_Bullet_Concurrent() {
            string bullet281SourcePath = @"C:\Workspace\Source\bullet\2.81";
            string bullet281DataPath = @"C:\Workspace\SrcMLData\concurrent\bullet-2.81";

            Console.WriteLine("\nReal World Test: Bullet 2.81 (C++, concurrent)");
            Console.WriteLine("=======================================");
            TestDataGeneration(bullet281SourcePath, bullet281DataPath, true);
        }

        [Test]
        public void TestFileUnitParsing_Subversion() {
            string svn178SourcePath = @"C:\Workspace\Source\Subversion\1.7.8";
            string svn178DataPath = @"C:\Workspace\SrcMLData\subversion-1.7.8";

            Console.WriteLine("\nReal World Test: Subversion 1.7.8 (C)");
            Console.WriteLine("=======================================");
            TestDataGeneration(svn178SourcePath, svn178DataPath);
        }

        [Test]
        public void TestFileUnitParsing_Subversion_Concurrent() {
            string svn178SourcePath = @"C:\Workspace\Source\Subversion\1.7.8";
            string svn178DataPath = @"C:\Workspace\SrcMLData\concurrent\subversion-1.7.8";

            Console.WriteLine("\nReal World Test: Subversion 1.7.8 (C, concurrent)");
            Console.WriteLine("=======================================");
            TestDataGeneration(svn178SourcePath, svn178DataPath, true);
        }

        [Test]
        public void TestFileUnitParsing_Eclipse() {
            string eclipse422SourcePath = @"C:\Workspace\Source\eclipse\platform422";
            string eclipse422Datapath = @"C:\Workspace\SrcMLData\eclipse-4.2.2";

            Console.WriteLine("\nReal World Test: Eclipse Platform 4.2.2 (Java)");
            Console.WriteLine("=======================================");
            TestDataGeneration(eclipse422SourcePath, eclipse422Datapath);
        }

        [Test]
        public void TestFileUnitParsing_Eclipse_Concurrent() {
            string eclipse422SourcePath = @"C:\Workspace\Source\eclipse\platform422";
            string eclipse422Datapath = @"C:\Workspace\SrcMLData\concurrent\eclipse-4.2.2";

            Console.WriteLine("\nReal World Test: Eclipse Platform 4.2.2 (Java, concurrent)");
            Console.WriteLine("=======================================");
            TestDataGeneration(eclipse422SourcePath, eclipse422Datapath, true);
        }

        [Test]
        public void TestFileUnitParsing_NDatabase() {
            string ndatabase45SourcePath = @"C:\Workspace\Source\NDatabase\master45";
            string ndatabase45DataPath = @"C:\Workspace\SrcMLData\ndatabase-4.5";

            Console.WriteLine("\nReal World Test: NDatabase 4.5 (C#)");
            Console.WriteLine("=======================================");
            TestDataGeneration(ndatabase45SourcePath, ndatabase45DataPath);
        }

        [Test]
        public void TestFileUnitParsing_NDatabase_Concurrent() {
            string ndatabase45SourcePath = @"C:\Workspace\Source\NDatabase\master45";
            string ndatabase45DataPath = @"C:\Workspace\SrcMLData\concurrent\ndatabase-4.5";

            Console.WriteLine("\nReal World Test: NDatabase 4.5 (C#, concurrent)");
            Console.WriteLine("=======================================");
            TestDataGeneration(ndatabase45SourcePath, ndatabase45DataPath, true);
        }


        private void TestDataGeneration(string sourcePath, string dataPath, bool useAsyncMethods=false) {
            string fileLogPath = Path.Combine(dataPath, "parse.log");
            string callLogPath = Path.Combine(dataPath, "methodcalls.log");
            bool regenerateSrcML = shouldRegenerateSrcML;

            if(!Directory.Exists(sourcePath)) {
                Assert.Ignore("Source code for is missing");
            }
            if(File.Exists(callLogPath)) {
                File.Delete(callLogPath);
            }
            if(File.Exists(fileLogPath)) {
                File.Delete(fileLogPath);
            }
            if(!Directory.Exists(dataPath)) {
                regenerateSrcML = true;
            } else if(shouldRegenerateSrcML) {
                Directory.Delete(dataPath, true);
            }

            var archive = new SrcMLArchive(dataPath, regenerateSrcML);
            archive.XmlGenerator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".c"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".cc"] = Language.CPlusPlus;

            AbstractFileMonitor monitor = new FileSystemFolderMonitor(sourcePath, dataPath, new LastModifiedArchive(dataPath), archive);
            monitor.UseAsyncMethods = useAsyncMethods;

            ManualResetEvent mre = new ManualResetEvent(false);
            DateTime start, end = DateTime.MinValue;
            bool startupCompleted = false;
            
            //monitor.IsReadyChanged += (o, e) => {
            archive.IsReadyChanged += (o, e) => {
                if(e.UpdatedReadyState) {
                    end = DateTime.Now;
                    startupCompleted = true;
                    mre.Set();
                }
            };

            start = DateTime.Now;
            monitor.Startup();

            startupCompleted = mre.WaitOne(120000);
            if(!startupCompleted) {
                end = DateTime.Now;
            }

            Console.WriteLine("{0} to {1} srcML", end - start, (regenerateSrcML ? "generate" : "verify"));
            Assert.That(startupCompleted);

            Scope globalScope = null;

            int numberOfFailures = 0;
            int numberOfSuccesses = 0;
            int numberOfFiles = 0;
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

            start = DateTime.Now;
            using(var fileLog = new StreamWriter(fileLogPath)) {
                foreach(var unit in archive.FileUnits) {
                    if(++numberOfFiles % 100 == 0) {
                        Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, numberOfFailures);
                    }

                    var fileName = SrcMLElement.GetFileNameForUnit(unit);
                    var language = SrcMLElement.GetLanguageForUnit(unit);

                    fileLog.Write("Parsing {0}", fileName);
                    try {
                        var scopeForUnit = CodeParser[language].ParseFileUnit(unit);

                        if(null == globalScope) {
                            globalScope = scopeForUnit;
                        } else {
                            globalScope = globalScope.Merge(scopeForUnit);
                        }
                        fileLog.WriteLine(" PASSED");
                        numberOfSuccesses++;
                    } catch(Exception e) {
                        fileLog.WriteLine(" FAILED");
                        fileLog.WriteLine(e.StackTrace);
                        var key = e.StackTrace.Split('\n')[0].Trim();
                        if(!errors.ContainsKey(key)) {
                            errors[key] = new List<string>();
                        }
                        errors[key].Add(fileName);

                        numberOfFailures++;
                    }
                }
            }
            end = DateTime.Now;

            Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, end - start, numberOfFailures);
            Console.WriteLine("\nSummary");
            Console.WriteLine("===================");
            Console.WriteLine("{0,10:N0} failures  ({1,8:P2})", numberOfFailures, ((float)numberOfFailures) / numberOfFiles);
            Console.WriteLine("{0,10:N0} successes ({1,8:P2})", numberOfSuccesses, ((float)numberOfSuccesses) / numberOfFiles);
            Console.WriteLine("{0} to generate data", end - start);
            Console.WriteLine(fileLogPath);

            PrintScopeReport(globalScope);
            PrintMethodCallReport(globalScope, callLogPath);
            PrintErrorReport(errors);

            monitor.Dispose(); 
            //Assert.AreEqual(numberOfFailures, (from e in errors.Values select e.Count).Sum());
            //Assert.AreEqual(0, numberOfFailures);
        }

        
        private void TestDataGeneration_Concurrent(string sourcePath, string dataPath) {
            string fileLogPath = Path.Combine(dataPath, "parse.log");
            string callLogPath = Path.Combine(dataPath, "methodcalls.log");
            bool regenerateSrcML = shouldRegenerateSrcML;

            if(!Directory.Exists(sourcePath)) {
                Assert.Ignore("Source code for is missing");
            }
            if(File.Exists(callLogPath)) {
                File.Delete(callLogPath);
            }
            if(File.Exists(fileLogPath)) {
                File.Delete(fileLogPath);
            }
            if(!Directory.Exists(dataPath)) {
                regenerateSrcML = true;
            } else if(shouldRegenerateSrcML) {
                Directory.Delete(dataPath, true);
            }

            var archive = new SrcMLArchive(dataPath, regenerateSrcML);
            archive.XmlGenerator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".c"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".cc"] = Language.CPlusPlus;

            AbstractFileMonitor monitor = new FileSystemFolderMonitor(sourcePath, dataPath, new LastModifiedArchive(dataPath), archive);

            ManualResetEvent mre = new ManualResetEvent(false);
            Stopwatch sw = new Stopwatch();
            bool startupCompleted = false;

            monitor.IsReadyChanged += (o, e) => {
                if(e.UpdatedReadyState) {
                    sw.Stop();
                    startupCompleted = true;
                    mre.Set();
                }
            };

            sw.Start();

            //add the concurrency test: ZL
            monitor.Startup_Concurrent();

            startupCompleted = mre.WaitOne(60000);
            if(sw.IsRunning) {
                sw.Stop();
            }

            Console.WriteLine("{0} to {1} srcML", sw.Elapsed, (regenerateSrcML ? "generate" : "verify"));
            Assert.That(startupCompleted);

            Scope globalScope = null;
            sw.Reset();

            int numberOfFailures = 0;
            int numberOfSuccesses = 0;
            int numberOfFiles = 0;
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

            List<string> xmlFiles = archive.ArchivedXmlFiles();
            sw.Start();

            using(var fileLog = new StreamWriter(fileLogPath)) {
                using(BlockingCollection<NamespaceDefinition> bc = new BlockingCollection<NamespaceDefinition>()) {

                    Task.Factory.StartNew(() => {

                        Parallel.ForEach(xmlFiles, currentFile => {

                            var unit = SrcMLElement.Load(currentFile);
                            var language = SrcMLElement.GetLanguageForUnit(unit);
                            try {

                                var scopeForUnit = CodeParser[language].ParseFileUnit(unit);
                                bc.Add(scopeForUnit);

                            } catch(Exception e) {
                                var key = e.StackTrace.Split('\n')[0].Trim();
                                if(!errors.ContainsKey(key)) {
                                    errors[key] = new List<string>();
                                }
                                errors[key].Add(currentFile);
                            }
                        });

                        bc.CompleteAdding();
                    });

                    foreach(var item in bc.GetConsumingEnumerable()) {
                        if(null == globalScope) {
                            globalScope = item;
                        } else {
                            try {
                                globalScope = globalScope.Merge(item);
                            } catch(Exception e) {
                                //So far, don't know how to log the error (ZL 04/2013)
                                //Console.WriteLine("error");
                            }
                        }
                    }
                }
            }

            sw.Stop();

            Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, sw.Elapsed, numberOfFailures);
            Console.WriteLine("\nSummary");
            Console.WriteLine("===================");
            Console.WriteLine("{0,10:N0} failures  ({1,8:P2})", numberOfFailures, ((float)numberOfFailures) / numberOfFiles);
            Console.WriteLine("{0,10:N0} successes ({1,8:P2})", numberOfSuccesses, ((float)numberOfSuccesses) / numberOfFiles);
            Console.WriteLine("{0} to generate data", sw.Elapsed);
            Console.WriteLine(fileLogPath);

            PrintScopeReport(globalScope);
            PrintMethodCallReport(globalScope, callLogPath);
            PrintErrorReport(errors);

            monitor.Dispose();
            //Assert.AreEqual(numberOfFailures, (from e in errors.Values select e.Count).Sum());
            //Assert.AreEqual(0, numberOfFailures);
        }

        private void PrintScopeReport(Scope globalScope) {
            Console.WriteLine("\nScope Report");
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
        }

        private void PrintMethodCallReport(Scope globalScope, string callLogPath) {
            Console.WriteLine("\nMethod Call Report");
            Console.WriteLine("===============");
            var methodCalls = from scope in VariableScopeIterator.Visit(globalScope)
                              from call in scope.MethodCalls
                              select call;

            int numMethodCalls = 0;
            int numMatchedMethodCalls = 0;
            Stopwatch sw = new Stopwatch();

            using(var callLog = new StreamWriter(callLogPath)) {
                foreach(var call in methodCalls) {
                    sw.Start();
                    var match = call.FindMatches().FirstOrDefault();
                    sw.Stop();
                    numMethodCalls++;
                    if(null != match) {
                        numMatchedMethodCalls++;
                        callLog.WriteLine("{0} ({1}:{2}) -> {3} ({4}:{5})", call.Name, call.Location.SourceFileName, call.Location.StartingLineNumber, match.Name, match.PrimaryLocation.SourceFileName, match.PrimaryLocation.StartingLineNumber);
                    }
                }
            }
            
            Console.WriteLine("{0,10:N0} method calls", numMethodCalls);
            Console.WriteLine("{0,10:N0} matched method calls ({1,8:P2})", numMatchedMethodCalls, ((float)numMatchedMethodCalls) / numMethodCalls);
            Console.WriteLine("{0,10:N0} matches / millisecond ({1,7:N0} ms elapsed)", ((float) numMethodCalls) / sw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
            Console.WriteLine(callLogPath);
        }

        private void PrintErrorReport(Dictionary<string, List<string>> errors) {
            Console.WriteLine("\nError Report");
            Console.WriteLine("===============");
            var sortedErrors = from kvp in errors
                               orderby kvp.Value.Count descending
                               select kvp;

            if(sortedErrors.Any()) {
                foreach(var kvp in sortedErrors) {
                    var indexOfIn = kvp.Key.IndexOf(" in ");
                    var indexOfColon = kvp.Key.LastIndexOf(":");
                    var fileName = kvp.Key.Substring(indexOfIn + 4, indexOfColon - indexOfIn - 4);
                    var lineNumber = kvp.Key.Substring(indexOfColon + 6);
                    string method = kvp.Key.Substring(0, indexOfIn);

                    Console.WriteLine("{0}({1}) : {2} exception{3} {4}", fileName, lineNumber, kvp.Value.Count, (kvp.Value.Count > 1 ? "s" : String.Empty), method);
                    foreach(var sourceFile in kvp.Value) {
                        Console.WriteLine("\t{0}", sourceFile);
                    }
                }
            } else {
                Console.WriteLine("No parsing errors!");
            }
            
        }
    }
}
