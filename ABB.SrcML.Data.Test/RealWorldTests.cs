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

using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("LongRunning")]
    internal class RealWorldTests {
        private Dictionary<Language, ICodeParser> CodeParser;
        private bool shouldRegenerateSrcML = true;

        [TestFixtureSetUp]
        public void ClassSetup() {
            CodeParser = new Dictionary<Language, ICodeParser>() {
                { Language.CPlusPlus, new CPlusPlusCodeParser() },
                { Language.Java, new JavaCodeParser() },
                { Language.CSharp, new CSharpCodeParser() }
            };
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
        public void TestFileUnitParsing_FamilyShow() {
            string familyShow3SourcePath = @"C:\Workspace\Source\FamilyShow\3.0";
            string familyShow3Datapath = @"C:\Workspace\SrcMLData\FamilyShow-3.0";

            Console.WriteLine("\nReal World Test: Family Show 3.0 (C#)");
            Console.WriteLine("=======================================");
            TestDataGeneration(familyShow3SourcePath, familyShow3Datapath);
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
        public void TestFileUnitParsing_QuickGraph3() {
            string quickgraph3SourcePath = @"C:\Workspace\Source\QuickGraph\69709-3.0\sources";
            string quickgraph3DataPath = @"c:\Workspace\SrcMLData\QuickGraph-69709-3.0";

            Console.WriteLine("\nReal world test: QuickGraph 3.0 (C#)");
            Console.WriteLine("=======================================");
            TestDataGeneration(quickgraph3SourcePath, quickgraph3DataPath, true);
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

        private void PrintMethodCallReport(IScope globalScope, string callLogPath) {
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
                    IMethodDefinition match = null;
                    try {
                        match = call.FindMatches().FirstOrDefault();
                    } catch(Exception e) {
                        Console.WriteLine("{0}:{1}:{2}): {3}", call.Location.SourceFileName, call.Location.StartingLineNumber, call.Location.StartingColumnNumber, e.Message);
                    }

                    sw.Stop();
                    numMethodCalls++;
                    if(null != match) {
                        numMatchedMethodCalls++;
                        callLog.WriteLine("{0} ({1}:{2}) -> {3} ({4}:{5})", call.Name, call.Location.SourceFileName, call.Location.StartingLineNumber, match.Name, match.PrimaryLocation.SourceFileName, match.PrimaryLocation.StartingLineNumber);
                    }
                }
            }

            Console.WriteLine("{0,10:N0} method calls", numMethodCalls);
            Console.WriteLine("{0,10:N0} matched method calls ({1,8:P2})", numMatchedMethodCalls, ((float) numMatchedMethodCalls) / numMethodCalls);
            Console.WriteLine("{0,10:N0} matches / millisecond ({1,7:N0} ms elapsed)", ((float) numMethodCalls) / sw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
            Console.WriteLine(callLogPath);
        }

        private void PrintScopeReport(IScope globalScope) {
            Console.WriteLine("\nScope Report");
            Console.WriteLine("===============");

            Console.WriteLine("{0,10:N0} scopes", VariableScopeIterator.Visit(globalScope).Count());
            var namedScopes = from scope in VariableScopeIterator.Visit(globalScope)
                              where (scope as INamedScope) != null
                              select scope;
            Console.WriteLine("{0,10:N0} named scopes", namedScopes.Count());
            var namespaceScopes = from scope in namedScopes
                                  where (scope as INamespaceDefinition) != null
                                  select scope;
            var typeScopes = from scope in namedScopes
                             where (scope as ITypeDefinition) != null
                             select scope;
            var methodScopes = from scope in namedScopes
                               where (scope as IMethodDefinition) != null
                               select scope;
            Console.WriteLine("{0,10:N0} namespaces", namespaceScopes.Count());
            Console.WriteLine("{0,10:N0} types", typeScopes.Count());
            Console.WriteLine("{0,10:N0} methods", methodScopes.Count());
        }

        private void TestDataGeneration(string sourcePath, string dataPath, bool useAsyncMethods = false) {
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

            int numberOfFailures = 0;
            int numberOfSuccesses = 0;
            int numberOfFiles = 0;
            Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

            using(var fileLog = new StreamWriter(fileLogPath)) {
                using(var monitor = new FileSystemFolderMonitor(sourcePath, dataPath, new LastModifiedArchive(dataPath), archive)) {
                    DateTime start, end = DateTime.MinValue;
                    bool startupCompleted = false;

                    start = DateTime.Now;
                    if(useAsyncMethods) {
                        var task = monitor.UpdateArchivesAsync().ContinueWith((t) => {
                            end = DateTime.Now;
                            startupCompleted = true;
                        });
                        task.Wait();
                    } else {
                        monitor.UpdateArchives();
                        end = DateTime.Now;
                        startupCompleted = true;
                    }

                    if(!startupCompleted) {
                        end = DateTime.Now;
                    }

                    Console.WriteLine("{0} to {1} srcML", end - start, (regenerateSrcML ? "generate" : "verify"));
                    Assert.That(startupCompleted);

                    using(var data = new DataRepository(archive)) {
                        start = DateTime.Now;

                        data.FileProcessed += (o, e) => {
                            if(e.EventType == FileEventType.FileAdded) {
                                numberOfFiles++;
                                numberOfSuccesses++;
                                if(numberOfFiles % 100 == 0) {
                                    Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, numberOfFailures);
                                }
                                fileLog.WriteLine("OK {0}", e.FilePath);
                            }
                        };

                        data.ErrorRaised += (o, e) => {
                            numberOfFiles++;
                            numberOfFailures++;
                            if(numberOfFiles % 100 == 0) {
                                Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, numberOfFailures);
                            }
                            ParseException pe = e.Exception as ParseException;
                            if(pe != null) {
                                fileLog.WriteLine("ERROR {0}", pe.FileName);
                                fileLog.WriteLine(e.Exception.InnerException.StackTrace);
                                var key = e.Exception.InnerException.StackTrace.Split('\n')[0].Trim();
                                if(!errors.ContainsKey(key)) {
                                    errors[key] = new List<string>();
                                }
                                int errorLineNumber = (pe.LineNumber < 1 ? 1 : pe.LineNumber);
                                int errorColumnNumber = (pe.ColumnNumber < 1 ? 1 : pe.ColumnNumber);
                                var errorLocation = String.Format("{0}({1},{2})", pe.FileName, errorLineNumber, errorColumnNumber);
                                errors[key].Add(errorLocation);
                            }
                        };

                        if(useAsyncMethods) {
                            data.InitializeDataAsync().Wait();
                        } else {
                            data.InitializeData();
                        }

                        end = DateTime.Now;

                        Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, end - start, numberOfFailures);
                        Console.WriteLine("{0} to generate data", end - start);

                        Console.WriteLine("\nSummary");
                        Console.WriteLine("===================");
                        Console.WriteLine("{0,10:N0} failures  ({1,8:P2})", numberOfFailures, ((float) numberOfFailures) / numberOfFiles);
                        Console.WriteLine("{0,10:N0} successes ({1,8:P2})", numberOfSuccesses, ((float) numberOfSuccesses) / numberOfFiles);
                        Console.WriteLine("{0} to generate data", end - start);
                        Console.WriteLine(fileLogPath);

                        PrintScopeReport(data.GetGlobalScope());
                        PrintMethodCallReport(data.GetGlobalScope(), callLogPath);
                        PrintErrorReport(errors);
                    }
                }
            }
        }
    }
}