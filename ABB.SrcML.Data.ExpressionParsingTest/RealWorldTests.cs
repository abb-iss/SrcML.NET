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
        public void TestCompleteWorkingSet(RealWorldTestProject project) {
            CheckThatProjectExists(project);

            var archive = GenerateSrcML(project, false, true);
            var data = GenerateData(project, archive, true, true);
            var workingSet = new CompleteWorkingSet(data);
            DateTime start = DateTime.Now, end;
            workingSet.Initialize();
            end = DateTime.Now;
            Console.WriteLine("{0} to initialize complete working set", end - start);

            NamespaceDefinition globalNamespace;
            Assert.That(workingSet.TryObtainReadLock(5000, out globalNamespace));

            try {
                Console.WriteLine("Project Summary");
                Console.WriteLine("============================");
                Console.WriteLine("{0,10:N0} namespaces", globalNamespace.GetDescendants<NamespaceDefinition>().Count());
                Console.WriteLine("{0,10:N0} types", globalNamespace.GetDescendants<TypeDefinition>().Count());
                Console.WriteLine("{0,10:N0} methods", globalNamespace.GetDescendants<MethodDefinition>().Count());
            } finally {
                workingSet.ReleaseReadLock();
            }
        }

        [Test, TestCaseSource("TestProjects")]
        public void TestSerialization(RealWorldTestProject project) {
            var archive = GenerateSrcML(project, false, true);
            var dataArchive = GenerateData(project, archive, true, true);

            dataArchive.Generator.UnknownLog = null;
            dataArchive.Generator.ErrorLog = Console.Error;

            long count = 0;
            TimeSpan parseElapsed = new TimeSpan(0), deserializeElapsed = new TimeSpan(0), compareElapsed = new TimeSpan(0);
            DateTime start, end;
            Console.WriteLine("{0,-12} {1,-12} {2,-12} {3,-12}", "# Files", "Parse", "Deserialize", "Comparison");
            foreach(var sourcePath in dataArchive.GetFiles().OrderBy(elem => Guid.NewGuid())) {
                NamespaceDefinition data;
                NamespaceDefinition serializedData;
                try {
                    start = DateTime.Now;
                    var fileUnit = archive.GetXElementForSourceFile(sourcePath);
                    data = dataArchive.Generator.Parse(fileUnit);
                    end = DateTime.Now;
                    parseElapsed += (end - start);
                } catch(Exception ex) {
                    Console.Error.WriteLine(ex);
                    data = null;
                }

                try {
                    start = DateTime.Now;
                    serializedData = dataArchive.GetData(sourcePath);
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
                            parseElapsed ,
                            deserializeElapsed ,
                            compareElapsed);
                }
            }

            Console.WriteLine(@"Project: {0} {1}
            ============================
            {2,-15} {3,11:N0}
            {4,-15} {5:g}
            {6,-15} {7:g}
            {8,-15} {9:g}
            ============================
            {10,-15} {11,9:g}
            ", project.ProjectName, project.Version, "# Files", count,
                    "Parsing", parseElapsed ,
                    "Deserializing", deserializeElapsed,
                    "Comparing", compareElapsed ,
                    "Total", parseElapsed + deserializeElapsed + compareElapsed);
        }

        private static SrcMLArchive GenerateSrcML(RealWorldTestProject project, bool shouldRegenerateSrcML, bool useAsyncMethods = true) {
            if(!Directory.Exists(project.DataDirectory)) {
                Directory.CreateDirectory(project.DataDirectory);
            }
            bool regenerateSrcML = shouldRegenerateSrcML;

            if(shouldRegenerateSrcML && Directory.Exists(project.DataDirectory)) {
                Directory.Delete(project.DataDirectory);
            }
            var lastModified = new LastModifiedArchive(project.DataDirectory);
            var archive = new SrcMLArchive(project.DataDirectory, "srcML", !shouldRegenerateSrcML, new SrcMLGenerator("SrcML"));

            archive.Generator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
            archive.Generator.ExtensionMapping[".c"] = Language.CPlusPlus;
            archive.Generator.ExtensionMapping[".cc"] = Language.CPlusPlus;

            var monitor = new FileSystemFolderMonitor(project.FullPath, project.DataDirectory, lastModified, archive);

            DateTime start = DateTime.Now, end;
            if(useAsyncMethods) {
                monitor.UpdateArchivesAsync().Wait();
            } else {
                monitor.UpdateArchives();
            }
            end = DateTime.Now;
            lastModified.Save();
            archive.Save();
            Console.WriteLine("{0:g} to {1} srcML", end - start, (regenerateSrcML ? "generate" : "verify"));
            return archive;
        }

        private static DataArchive GenerateData(RealWorldTestProject project, SrcMLArchive archive, bool shouldRegenerateData, bool useAsyncMethods = true) {
            if(!Directory.Exists(project.DataDirectory)) {
                Directory.CreateDirectory(project.DataDirectory);
            }
            var fileLogPath = Path.Combine(project.DataDirectory, "error.log");
            if(File.Exists(fileLogPath)) {
                File.Delete(fileLogPath);
            }
            
            var unknownLogPath = Path.Combine(project.DataDirectory, "unknown.log");
            if(File.Exists(unknownLogPath)) {
                File.Delete(unknownLogPath);
            }

            var dataArchive = new DataArchive(project.DataDirectory, archive, false);
            dataArchive.Generator.IsLoggingErrors = true;
            using(StreamWriter errorLog = new StreamWriter(fileLogPath),
                               unknownLog = new StreamWriter(unknownLogPath)) {
                dataArchive.Generator.ErrorLog = errorLog;
                dataArchive.Generator.UnknownLog = unknownLog;
                var srcMLMonitor = new ArchiveMonitor<SrcMLArchive>(project.DataDirectory, archive, dataArchive);
                DateTime start = DateTime.Now, end;
                if(useAsyncMethods) {
                    srcMLMonitor.UpdateArchivesAsync().Wait();
                } else {
                    srcMLMonitor.UpdateArchives();
                }
                end = DateTime.Now;
                dataArchive.Save();
                int numSrcMLFiles = archive.GetFiles().Count;
                int numDataFiles = dataArchive.GetFiles().Count;
                Console.WriteLine("{0:g} to generate data", end - start);
                Console.WriteLine("Parsed {0:P0} ({1:N0} / {2:N0})", numDataFiles / (double) numSrcMLFiles, numDataFiles, numSrcMLFiles);
            }
            return dataArchive;
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
