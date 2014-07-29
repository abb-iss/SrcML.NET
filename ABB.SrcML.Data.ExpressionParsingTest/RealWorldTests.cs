/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Utilities;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture, Category("LongRunning")]
    public class RealWorldTests {
        public const string MappingFile = @"..\..\TestInputs\project_mapping.txt";
        static List<RealWorldTestProject> TestProjects = ReadProjectMap(MappingFile).ToList();

        [Test, TestCaseSource("TestProjects")]
        public void TestCompleteWorkingSet(RealWorldTestProject testData) {
            CheckThatProjectExists(testData);
            Console.WriteLine("{0} {1} Project Summary", testData.ProjectName, testData.Version);
            Console.WriteLine("============================");
            using(var project = new DataProject<CompleteWorkingSet>(testData.DataDirectory, testData.FullPath, "SrcML")) {
                string unknownLogPath = Path.Combine(project.StoragePath, "unknown.log");
                DateTime start = DateTime.Now, end;

                using(var unknownLog = new StreamWriter(unknownLogPath)) {
                    project.UnknownLog = unknownLog;
                    project.UpdateAsync().Wait();
                }
                end = DateTime.Now;

                Console.WriteLine("{0} to initialize complete working set", end - start);

                NamespaceDefinition globalNamespace;
                Assert.That(project.WorkingSet.TryObtainReadLock(5000, out globalNamespace));

                try {
                    Console.WriteLine("{0,10:N0} files", project.Data.GetFiles().Count());
                    Console.WriteLine("{0,10:N0} namespaces", globalNamespace.GetDescendants<NamespaceDefinition>().Count());
                    Console.WriteLine("{0,10:N0} types", globalNamespace.GetDescendants<TypeDefinition>().Count());
                    Console.WriteLine("{0,10:N0} methods", globalNamespace.GetDescendants<MethodDefinition>().Count());

                    var methodCalls = from statement in globalNamespace.GetDescendantsAndSelf()
                                      from expression in statement.GetExpressions()
                                      from call in expression.GetDescendantsAndSelf<MethodCall>()
                                      select call;
                    int numMethodCalls = 0, numMatchedMethodCalls = 0;
                    Stopwatch sw = new Stopwatch();
                    foreach(var call in methodCalls) {
                        sw.Start();
                        INamedEntity match = null;
                        try {
                            match = call.FindMatches().FirstOrDefault();
                        } catch(Exception e) {
                            project.ErrorLog.WriteLine("{0}:{1}:{2}: Call Exception {3}", call.Location.SourceFileName, call.Location.StartingLineNumber, call.Location.StartingColumnNumber, e);
                        }
                        sw.Stop();
                        numMethodCalls++;
                        if(null != match) {
                            numMatchedMethodCalls++;
                        }
                    }
                    Console.WriteLine("{0,10:N0} method calls", numMethodCalls);
                    Console.WriteLine("{0,10:P2} of method calls matched", (float) numMatchedMethodCalls / numMethodCalls);
                    Console.WriteLine("{0,10:N2} matches / millisecond ({1,7:N0} ms elapsed)", ((float) numMethodCalls) / sw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
                } finally {
                    project.WorkingSet.ReleaseReadLock();
                }
            }
        }

        [Test, TestCaseSource("TestProjects")]
        public void TestSerialization(RealWorldTestProject testData) {
            using(var project = new DataProject<NullWorkingSet>(testData.DataDirectory, testData.FullPath, "SrcML")) {
                string unknownLogPath = Path.Combine(project.StoragePath, "unknown.log");

                using(var unknownLog = new StreamWriter(unknownLogPath)) {
                    project.UnknownLog = unknownLog;
                    project.UpdateAsync().Wait();

                    long count = 0;
                    TimeSpan parseElapsed = new TimeSpan(0), deserializeElapsed = new TimeSpan(0), compareElapsed = new TimeSpan(0);
                    DateTime start, end;
                    Console.WriteLine("{0,-12} {1,-12} {2,-12} {3,-12}", "# Files", "Parse", "Deserialize", "Comparison");
                    foreach(var sourcePath in project.Data.GetFiles().OrderBy(elem => Guid.NewGuid())) {
                        NamespaceDefinition data;
                        NamespaceDefinition serializedData;
                        try {
                            start = DateTime.Now;
                            var fileUnit = project.SourceArchive.GetXElementForSourceFile(sourcePath);
                            data = project.Data.Generator.Parse(fileUnit);
                            end = DateTime.Now;
                            parseElapsed += (end - start);
                        } catch(Exception ex) {
                            Console.Error.WriteLine(ex);
                            data = null;
                        }

                        try {
                            start = DateTime.Now;
                            serializedData = project.Data.GetData(sourcePath);
                            end = DateTime.Now;
                            deserializeElapsed += (end - start);
                        } catch(Exception ex) {
                            Console.Error.WriteLine(ex);
                            serializedData = null;
                        }

                        Assert.IsNotNull(data);
                        Assert.IsNotNull(serializedData);
                        start = DateTime.Now;
                        DataAssert.StatementsAreEqual(data, serializedData);
                        end = DateTime.Now;
                        compareElapsed += (end - start);

                        if(++count % 25 == 0) {
                            Console.WriteLine("{0,12:N0} {1,12:ss\\.fff} {2,12:ss\\.fff} {3,12:ss\\.fff}", count,
                                    parseElapsed,
                                    deserializeElapsed,
                                    compareElapsed);
                        }
                    }
                    Console.WriteLine("Project: {0} {1}", testData.ProjectName, testData.Version);
                    Console.WriteLine("{0,-15} {1,11:N0}", "# Files", count);
                    Console.WriteLine("{0,-15} {1:g}", "Parsing", parseElapsed);
                    Console.WriteLine("{0,-15} {1:g}", "Deserializing", deserializeElapsed);
                    Console.WriteLine("{0,-15} {1:g}", "Comparing", compareElapsed);
                    Console.WriteLine("{0,-15} {1:g}", "Total", parseElapsed + deserializeElapsed + compareElapsed);
                }
            }
        }

        private static IEnumerable<RealWorldTestProject> ReadProjectMap(string fileName) {
            if(File.Exists(fileName)) {
                var projects = from line in File.ReadAllLines(fileName)
                               where !line.StartsWith("#")
                               let parts = line.Split(',')
                               where 4 == parts.Length
                               let projectName = parts[0]
                               let projectVersion = parts[1]
                               let projectLanguage
                               = SrcMLElement.GetLanguageFromString(parts[2])
                               let rootDirectory = parts[3]
                               select new RealWorldTestProject(projectName, projectVersion, projectLanguage, rootDirectory);
                return projects;
            }
            return Enumerable.Empty<RealWorldTestProject>();
        }

        private static void CheckThatProjectExists(RealWorldTestProject project) {
            if(!Directory.Exists(project.FullPath)) {
                Assert.Ignore("Project directory for {0} {1} does not exist ({2})", project.ProjectName, project.Version, project.FullPath);
            }
        }

        public class RealWorldTestProject {
            
            public string FullPath { get; set; }

            public Language PrimaryLanguage { get; set; }

            public string ProjectName { get; set; }

            public string DataDirectory { get { return String.Format("{0}_{1}", ProjectName, Version); } }

            public string Version { get; set; }

            public RealWorldTestProject(string projectName, string projectVersion, Language language, string rootDirectory) {
                ProjectName = projectName;
                Version = projectVersion;
                FullPath = Path.GetFullPath(rootDirectory);
                PrimaryLanguage = language;
            }

            public override string ToString() {
                return String.Format("{0} {1}", ProjectName, Version);
            }
        }
    }
}
