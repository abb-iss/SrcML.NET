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
    class RealWorldTests {
        private bool shouldRegenerateSrcML = false;
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
        public void TestFileUnitParsing_Bullet() {
            string bullet281SourcePath = @"C:\Workspace\Source\bullet\2.81";
            string bullet281DataPath = @"C:\Workspace\SrcMLData\bullet-2.81";

            Console.WriteLine("\nReal World Test: Bullet 2.81 (C++)");
            Console.WriteLine("=======================================");
            TestDataGeneration(bullet281SourcePath, bullet281DataPath);
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
        public void TestFileUnitParsing_Eclipse() {
            string eclipse422SourcePath = @"C:\Workspace\Source\eclipse\platform422";
            string eclipse422Datapath = @"C:\Workspace\SrcMLData\eclipse-4.2.2";

            Console.WriteLine("\nReal World Test: Eclipse Platform 4.2.2 (Java)");
            Console.WriteLine("=======================================");
            TestDataGeneration(eclipse422SourcePath, eclipse422Datapath);
        }

        [Test]
        public void TestFileUnitParsing_NDatabase() {
            string ndatabase45SourcePath = @"C:\Workspace\Source\NDatabase\master45";
            string ndatabase45DataPath = @"C:\Workspace\SrcMLData\ndatabase-4.5";

            Console.WriteLine("\nReal World Test: NDatabase 4.5 (C#)");
            Console.WriteLine("=======================================");
            TestDataGeneration(ndatabase45SourcePath, ndatabase45DataPath);
        }

        private void TestDataGeneration(string sourcePath, string dataPath) {
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

            monitor.StartupCompleted += (o, e) => {
                sw.Stop();
                startupCompleted = true;
                mre.Set();
            };

            sw.Start();

            //add the concurrency test: ZL
            bool isParallel = true;
            if(!isParallel) {
                monitor.Startup();
            } else {
                monitor.Startup_Concurrent();
            }

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
            Scope[] globalScope_Results = new Scope[12];

            sw.Start();

            //12 is the number of cores
            Task[] tasks = new Task[12];
            List<string> xmlFiles_temp = null;
            //for(int i = 0; i < 11; i++) {
            //    xmlFiles_temp = xmlFiles.GetRange(i * 100, 100);
            //    globalScope_Results[i] = MergeSeg(xmlFiles_temp, errors);
            //}

            //xmlFiles_temp = xmlFiles.GetRange(1100, (xmlFiles.Count - 1100));
            //globalScope_Results[11] = MergeSeg(xmlFiles_temp, errors);

            //for(int i = 0; i < 11; i++) {
            //    xmlFiles_temp = xmlFiles.GetRange(i * 100, 100);
            //    tasks[i] = new Task(() => {
            //        globalScope_Results[i] = MergeSeg(xmlFiles_temp, errors);
            //    });
            //}

            tasks[0] = new Task(() => {
                globalScope_Results[0] = MergeSeg(xmlFiles.GetRange(0,100), errors);
            });
            tasks[1] = new Task(() => {
                globalScope_Results[1] = MergeSeg(xmlFiles.GetRange(100,100), errors);
            });
            tasks[2] = new Task(() => {
                globalScope_Results[2] = MergeSeg(xmlFiles.GetRange(200,100), errors);
            });
            tasks[3] = new Task(() => {
                globalScope_Results[3] = MergeSeg(xmlFiles.GetRange(300,100), errors);
            });
            tasks[4] = new Task(() => {
                globalScope_Results[4] = MergeSeg(xmlFiles.GetRange(400,100), errors);
            });
            tasks[5] = new Task(() => {
                globalScope_Results[5] = MergeSeg(xmlFiles.GetRange(500,100), errors);
            });
            tasks[6] = new Task(() => {
                globalScope_Results[6] = MergeSeg(xmlFiles.GetRange(600,100), errors);
            });
            tasks[7] = new Task(() => {
                globalScope_Results[7] = MergeSeg(xmlFiles.GetRange(700,100), errors);
            });
            tasks[8] = new Task(() => {
                globalScope_Results[8] = MergeSeg(xmlFiles.GetRange(800,100), errors);
            });
            tasks[9] = new Task(() => {
                globalScope_Results[9] = MergeSeg(xmlFiles.GetRange(900,100), errors);
            });
            tasks[10] = new Task(() => {
                globalScope_Results[10] = MergeSeg(xmlFiles.GetRange(1000,100), errors);
            });

            xmlFiles_temp = xmlFiles.GetRange(1100, (xmlFiles.Count - 1100));
            tasks[11] = new Task(() => {
                globalScope_Results[11] = MergeSeg(xmlFiles_temp, errors);
            });

            tasks[0].Start();
            tasks[1].Start();
            tasks[2].Start();
            tasks[3].Start();
            tasks[4].Start();
            tasks[5].Start();
            tasks[6].Start();
            tasks[7].Start();
            tasks[8].Start();
            tasks[9].Start();
            tasks[10].Start();
            tasks[11].Start();

            Task.WaitAll(tasks);
            Console.WriteLine("Finishing parsing");

            ////Test Concurrently merging and parsing
            //Task task0 = new Task(() => {
            //    globalScope_Results[0] = MergeSeg(0, 100, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task0.Start();

            //Task task1 = new Task(() => {
            //    globalScope_Results[1] = MergeSeg(100, 200, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task1.Start();

            //Task task2 = new Task(() => {
            //    globalScope_Results[2] = MergeSeg(200, 300, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task2.Start();

            //Task task3 = new Task(() => {
            //    globalScope_Results[3] = MergeSeg(300, 400, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task3.Start();

            //Task task4 = new Task(() => {
            //    globalScope_Results[4] = MergeSeg(400, 500, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task4.Start();

            //Task task5 = new Task(() => {
            //    globalScope_Results[5] = MergeSeg(500, 600, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task5.Start();

            //Task task6 = new Task(() => {
            //    globalScope_Results[6] = MergeSeg(600, 700, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task6.Start();

            //Task task7 = new Task(() => {
            //    globalScope_Results[7] = MergeSeg(700, 800, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task7.Start();

            //Task task8 = new Task(() => {
            //    globalScope_Results[8] = MergeSeg(800, 900, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task8.Start();

            //Task task9 = new Task(() => {
            //    globalScope_Results[9] = MergeSeg(900, 1000, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task9.Start();

            //Task task10 = new Task(() => {
            //    globalScope_Results[10] = MergeSeg(1000, 1100, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task10.Start();

            //Task task11 = new Task(() => {
            //    globalScope_Results[11] = MergeSeg(1100, xmlFiles.Count, xmlFiles, errors);
            //}, TaskCreationOptions.LongRunning);
            //task11.Start();

            //Task.WaitAll(task0, task1, task2, task3, task4, task5, task6, task7, task8, task9, task10, task11);

            sw.Stop();

            Console.WriteLine("Finishing Parsing " + sw.Elapsed);
            sw.Start();

            globalScope = globalScope_Results[0];
            for(int i = 1; i < 12; i++) {
                globalScope = globalScope.Merge(globalScope_Results[i]);
            }
            Console.WriteLine("Finishing Merging");

            //using(var fileLog = new StreamWriter(fileLogPath)) {
            //    foreach(var unit in archive.FileUnits) {
            //        if(++numberOfFiles % 100 == 0) {
            //            Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, sw.Elapsed, numberOfFailures);
            //        }

            //        var fileName = SrcMLElement.GetFileNameForUnit(unit);
            //        var language = SrcMLElement.GetLanguageForUnit(unit);

            //        fileLog.Write("Parsing {0}", fileName);
            //        try {
            //            sw.Start();
            //            var scopeForUnit = CodeParser[language].ParseFileUnit(unit);

            //            if(null == globalScope) {
            //                globalScope = scopeForUnit;
            //            } else {
            //                globalScope = globalScope.Merge(scopeForUnit);
            //            }
            //            sw.Stop();
            //            fileLog.WriteLine(" PASSED");
            //            numberOfSuccesses++;
            //        } catch(Exception e) {
            //            sw.Stop();
            //            fileLog.WriteLine(" FAILED");
            //            fileLog.WriteLine(e.StackTrace);
            //            var key = e.StackTrace.Split('\n')[0].Trim();
            //            if(!errors.ContainsKey(key)) {
            //                errors[key] = new List<string>();
            //            }
            //            errors[key].Add(fileName);

            //            numberOfFailures++;
            //        }
            //    }
            //}

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

        private Scope MergeSeg(List<string> xmlFiles, Dictionary<string, List<string>> errors) {
            Scope globalScope = null;

            for(int i = 0; i < xmlFiles.Count; i++) {
                string fileName = xmlFiles[i];
                var unit = XElement.Load(fileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                var language = SrcMLElement.GetLanguageForUnit(unit);
                try {

                    var scopeForUnit = CodeParser[language].ParseFileUnit(unit);

                    if(null == globalScope) {
                        globalScope = scopeForUnit;
                    } else {
                        globalScope = globalScope.Merge(scopeForUnit);
                    }

                } catch(Exception e) {
                    var key = e.StackTrace.Split('\n')[0].Trim();
                    if(!errors.ContainsKey(key)) {
                        errors[key] = new List<string>();
                    }
                    errors[key].Add(fileName);
                }

            }

            return globalScope;
        }

        private Scope MergeSeg_current(List<string> xmlFiles, Dictionary<string, List<string>> errors) {
            Scope globalScope = null;

            using(BlockingCollection<NamespaceDefinition> bc = new BlockingCollection<NamespaceDefinition>()) {
                for(int i = 0; i < xmlFiles.Count; i++) {
                    string fileName = xmlFiles[i];
                    var unit = XElement.Load(fileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                    var language = SrcMLElement.GetLanguageForUnit(unit);
                    try {

                        var scopeForUnit = CodeParser[language].ParseFileUnit(unit);
                        bc.Add(scopeForUnit);

                    } catch(Exception e) {
                        var key = e.StackTrace.Split('\n')[0].Trim();
                        if(!errors.ContainsKey(key)) {
                            errors[key] = new List<string>();
                        }
                        errors[key].Add(fileName);
                    }
                }


                foreach(var item in bc.GetConsumingEnumerable()) {
                    if(null == globalScope) {
                        globalScope = item;
                    } else {
                        globalScope = globalScope.Merge(item);
                    }
                }
            }
            return globalScope;
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
