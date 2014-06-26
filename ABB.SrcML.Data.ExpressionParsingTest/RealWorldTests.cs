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
        public void TestDataGeneration(RealWorldTestProject project) {
            CheckThatProjectExists(project);

            var data = SetupDataRepository(project, false, true);

            NamespaceDefinition globalNamespace;
            Assert.That(data.TryLockGlobalScope(5000, out globalNamespace));

            try {
                Console.WriteLine("Project Summary");
                Console.WriteLine("============================");
                Console.WriteLine("{0,10:N0} namespaces", globalNamespace.GetDescendants<NamespaceDefinition>().Count());
                Console.WriteLine("{0,10:N0} types", globalNamespace.GetDescendants<TypeDefinition>().Count());
                Console.WriteLine("{0,10:N0} methods", globalNamespace.GetDescendants<MethodDefinition>().Count());
            } finally {
                data.ReleaseGlobalScopeLock();
            }
        }

        [Test, TestCaseSource("TestProjects")]
        public void TestSerialization(RealWorldTestProject project) {
            string dataRepoPath = String.Format("{0}_{1}", project.ProjectName, project.Version);

            if(!Directory.Exists(dataRepoPath)) {
                Directory.CreateDirectory(dataRepoPath);
            }
            var fileLogPath = Path.Combine(dataRepoPath, "error.log");
            if(File.Exists(fileLogPath)) {
                File.Delete(fileLogPath);
            }

            using(var errorLog = new StreamWriter(fileLogPath)) {
                var archive = new SrcMLArchive(dataRepoPath, "srcML", true, new SrcMLGenerator("SrcML"));
                archive.XmlGenerator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
                archive.XmlGenerator.ExtensionMapping[".c"] = Language.CPlusPlus;
                archive.XmlGenerator.ExtensionMapping[".cc"] = Language.CPlusPlus;

                var monitor = new FileSystemFolderMonitor(project.FullPath, dataRepoPath, new LastModifiedArchive(dataRepoPath), archive);

                DateTime start = DateTime.Now, end = DateTime.MinValue;
                monitor.UpdateArchivesAsync().Wait();
                end = DateTime.Now;
                Console.WriteLine("{0} to verify srcML", end - start);

                var generator = new DataGenerator();

                var dataArchive = new DataArchive(dataRepoPath, archive, false);
                dataArchive.DataGenerator.IsLoggingErrors = true;
                dataArchive.DataGenerator.ErrorLog = errorLog;

                var srcMLMonitor = new ArchiveMonitor<SrcMLArchive>(dataRepoPath, archive, dataArchive);
                
                start = DateTime.Now;
                srcMLMonitor.UpdateArchivesAsync().Wait();
                end = DateTime.Now;

                Console.WriteLine("{0} to generate data", end - start);

                int numSrcMLFiles = archive.FileUnits.Count();
                int numDataFiles = dataArchive.GetFiles().Count();

                Console.WriteLine("Generated {0} srcML files", numSrcMLFiles);
                Console.WriteLine("Generated {0} data files", numDataFiles);
                Console.WriteLine("Parsed {0:P0} of the files in {1} {2}", numDataFiles / (double) numSrcMLFiles, project.ProjectName, project.Version);

                long count = 0, parseElapsed = 0, deserializeElapsed = 0, compareElapsed = 0;

                Console.WriteLine("{0,-12} {1,-12} {2,-12} {3,-12}", "# Files", "Parse", "Deserialize", "Comparison");
                foreach(var sourcePath in dataArchive.GetFiles().OrderBy(elem => Guid.NewGuid())) {
                    NamespaceDefinition data;
                    NamespaceDefinition serializedData;
                    try {
                        start = DateTime.Now;
                        var fileUnit = archive.GetXElementForSourceFile(sourcePath);
                        data = dataArchive.DataGenerator.Parse(fileUnit);
                        end = DateTime.Now;
                        parseElapsed += (end - start).Ticks;
                    } catch(Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                        data = null;
                    }

                    try {
                        start = DateTime.Now;
                        serializedData = dataArchive.GetData(sourcePath);
                        end = DateTime.Now;
                        deserializeElapsed += (end - start).Ticks;
                    } catch(Exception ex) {
                        Console.Error.WriteLine(ex.Message);
                        serializedData = null;
                    }

                    Assert.IsNotNull(data);
                    Assert.IsNotNull(serializedData);
                    start = DateTime.Now;
                    DataAssert.StatementsAreEqual(data, serializedData);
                    end = DateTime.Now;
                    compareElapsed += (end - start).Ticks;

                    if(++count % 25 == 0) {
                        Console.WriteLine("{0,7} {1,9:0.00} ms {2,9:0.00} ms {3,9:0.00} ms", count,
                                (double) parseElapsed / TimeSpan.TicksPerMillisecond / count,
                                (double) deserializeElapsed / TimeSpan.TicksPerMillisecond / count,
                                (double) compareElapsed / TimeSpan.TicksPerMillisecond / count);
                    }
                }

                Console.WriteLine(@"Project: {0} {1}
============================
{2,-15} {3,9}
{4,-15} {5,9:0.00} ms
{6,-15} {7,9:0.00} ms
{8,-15} {9,9:0.00} ms
============================
{10,-15} {11,9:0.00} ms
", project.ProjectName, project.Version, "# Files", count,
                        "Parsing", parseElapsed / TimeSpan.TicksPerMillisecond,
                        "Deserializing", deserializeElapsed / TimeSpan.TicksPerMillisecond,
                        "Comparing", compareElapsed / TimeSpan.TicksPerMillisecond,
                        "Total", (parseElapsed + deserializeElapsed + compareElapsed) / TimeSpan.TicksPerMillisecond);
            }
            
        }

        private static DataRepository SetupDataRepository(RealWorldTestProject project, bool shouldRegenerateSrcML, bool useAsyncMethods = false) {
            var dataRepoPath = String.Format("{0}_{1}", project.ProjectName, project.Version);
            bool regenerateSrcML = shouldRegenerateSrcML;

            var fileLogPath = Path.Combine(dataRepoPath, "parse.log");
            var unknownLogPath = Path.Combine(dataRepoPath, "unknown.log");

            if(File.Exists(fileLogPath)) {
                File.Delete(fileLogPath);
            }
            if(File.Exists(unknownLogPath)) {
                File.Delete(fileLogPath);
            }
            
            if(!Directory.Exists(dataRepoPath)) {
                regenerateSrcML = true;
            } else if(shouldRegenerateSrcML) {
                Directory.Delete(dataRepoPath, true);
            }

            if(shouldRegenerateSrcML && Directory.Exists(dataRepoPath)) {
                Directory.Delete(dataRepoPath, true);
            }

            var archive = new SrcMLArchive(dataRepoPath, "srcML", !regenerateSrcML, new SrcMLGenerator("SrcML"));
            archive.XmlGenerator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".c"] = Language.CPlusPlus;
            archive.XmlGenerator.ExtensionMapping[".cc"] = Language.CPlusPlus;

            var monitor = new FileSystemFolderMonitor(project.FullPath, dataRepoPath, new LastModifiedArchive(dataRepoPath), archive);

            DateTime start = DateTime.Now, end = DateTime.MinValue;
            if(useAsyncMethods) {
                monitor.UpdateArchivesAsync().Wait();
            } else {
                monitor.UpdateArchives();
            }
            end = DateTime.Now;
            Console.WriteLine("{0} to {1} srcML", end - start, (regenerateSrcML ? "generate" : "verify"));

            var data = new DataRepository(archive);
            var stats = new DataRepositoryStatistics(data);
            
            int numberOfFiles = 0;
            data.FileProcessed += (o, e) => {
                if(0 == ++numberOfFiles % 100) {
                    Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, stats.ErrorCount);
                }
            };

            data.ErrorRaised += (o, e) => {
                if(0 == ++numberOfFiles % 100) {
                    Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, stats.ErrorCount);
                }
            };

            using(TextWriter fileLog = new StreamWriter(fileLogPath),
                             unknownLog = new StreamWriter(unknownLogPath)) {
                stats.Out = fileLog;
                stats.Error = fileLog;

                start = DateTime.Now;
                if(useAsyncMethods) {
                    data.InitializeDataAsync().Wait();
                } else {
                    data.InitializeData();
                }
                end = DateTime.Now;
                Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, DateTime.Now - start, stats.ErrorCount);

                Console.WriteLine("{0} to generate data", end - start);
                Console.WriteLine();

                Console.WriteLine("Error Summary ({0} distinct)", stats.Errors.Count());
                Console.WriteLine("============================");
                foreach(var error in stats.Errors) {
                    Console.WriteLine("{0,5:N0}\t{1}", stats.GetLocationsForError(error).Count(), error);
                }
            }
            return data;
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
