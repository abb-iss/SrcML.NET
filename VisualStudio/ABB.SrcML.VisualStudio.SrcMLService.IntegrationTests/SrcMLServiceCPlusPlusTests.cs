using ABB.SrcML.Test.Utilities;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    [TestClass]
    public class SrcMLServiceCPlusPlusTests : IInvoker {
        private const string TestSolutionName = "TestCPPSolution";
        private static Solution TestSolution;
        private static object TestLock;

        private static string TestSolutionPath = Path.Combine(TestSolutionName, TestSolutionName + ".sln");

        [ClassInitialize]
        public static void ClassSetup(TestContext testContext) {
            // Create a local copy of the solution
            FileUtils.CopyDirectory(Path.Combine(TestConstants.InputFolderPath, TestSolutionName), TestSolutionName);
            TestLock = new object();
        }

        [TestInitialize]
        public void Setup() {
            TestSolution = VsIdeTestHostContext.Dte.Solution;
            Assert.IsNotNull(TestSolution, "Could not get the solution");

            TestSolution.Open(Path.GetFullPath(TestSolutionPath));
            TestHelpers.TestScaffold.Service.StartMonitoring();
        }

        [TestCleanup]
        public void Cleanup() {
            TestHelpers.TestScaffold.Service.StopMonitoring();
            TestSolution.Close();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestCppServiceStartup() {
            Assert.IsTrue(TestHelpers.WaitForServiceToFinish(TestHelpers.TestScaffold.Service, 5000));
            var archive = TestHelpers.TestScaffold.Service.GetSrcMLArchive();
            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.AreEqual(4, archive.FileUnits.Count(), "There should only be four files in the srcML archive");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestCppFileOperations() {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var archive = TestHelpers.TestScaffold.Service.GetSrcMLArchive();
            Assert.IsNotNull(archive, "Could not get the SrcML Archive");

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            string expectedFilePath = null;
            FileEventType expectedEventType = FileEventType.FileDeleted;

            EventHandler<FileEventRaisedArgs> action = (o, e) => {
                lock(TestLock) {
                    if(e.FilePath.Equals(expectedFilePath, StringComparison.InvariantCultureIgnoreCase) && e.EventType == expectedEventType) {
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffold.Service.SourceFileChanged += action;

            // add a file
            var fileTemplate = Path.Combine(TestConstants.TemplatesFolder, "NewCPPClass1.cpp");
            expectedFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCPPClass1.cpp");
            expectedEventType = FileEventType.FileAdded;
            File.Copy(fileTemplate, expectedFilePath);
            var item = project.ProjectItems.AddFromFile(expectedFilePath);
            // project.Save();

            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(archive.ContainsFile(expectedFilePath));
            Assert.IsFalse(archive.IsOutdated(expectedFilePath));

            //// rename a file
            //string oldFilePath = expectedFilePath;
            //expectedFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCPPClass2.cpp");
            //expectedEventType = FileEventType.FileAdded;
            //item = TestSolution.FindProjectItem(oldFilePath);
            //item.SaveAs(expectedFilePath);
            //File.Delete(oldFilePath);
            //project.Save();

            //Assert.IsTrue(resetEvent.WaitOne(500));
            //Assert.IsTrue(archive.ContainsFile(expectedFilePath), "The archive should contain {0}", expectedFilePath);
            //Assert.IsFalse(archive.ContainsFile(oldFilePath), "the archive should not contain {0}", oldFilePath);
            //Assert.IsFalse(archive.IsOutdated(expectedFilePath), String.Format("{0} is outdated", expectedFilePath));

            // delete the file
            expectedEventType = FileEventType.FileDeleted;
            item = TestSolution.FindProjectItem(expectedFilePath);
            item.Delete();
            project.Save();
            Assert.IsTrue(resetEvent.WaitOne(500));

            Assert.IsFalse(archive.IsOutdated(expectedFilePath));
            //Assert.AreEqual(2, archive.FileUnits.Count());
            TestHelpers.TestScaffold.Service.SourceFileChanged -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]

        public void TestCppProjectOperations() {
            var archive = TestHelpers.TestScaffold.Service.GetSrcMLArchive();
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            var testProjectName = "ConsoleApplication1";

            var expectedProjectDirectory = Path.GetFullPath(Path.Combine(TestSolutionName, testProjectName));
            var expectedEventType = FileEventType.FileAdded;

            HashSet<string> expectedFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
                Path.Combine(expectedProjectDirectory, "ConsoleApplication1.cpp"),
                Path.Combine(expectedProjectDirectory, "stdafx.cpp"),
                Path.Combine(expectedProjectDirectory, "stdafx.h"),
                Path.Combine(expectedProjectDirectory, "targetver.h"),
            };

            EventHandler<FileEventRaisedArgs> action = (o, e) => {
                lock(TestLock) {
                    if(expectedFiles.Contains(Path.GetFullPath(e.FilePath)) && e.EventType == expectedEventType) {
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffold.Service.SourceFileChanged += action;

            var projectTemplate = Path.GetFullPath(Path.Combine(TestConstants.TemplatesFolder, testProjectName, testProjectName, testProjectName + ".vcxproj"));

            // add a new project
            var addedProject = TestSolution.AddFromTemplate(projectTemplate, expectedProjectDirectory, testProjectName);
            addedProject.Save();
            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(resetEvent.WaitOne(500));

            foreach(var expectedFile in expectedFiles) {
                Assert.IsTrue(File.Exists(expectedFile));
                Assert.IsTrue(archive.ContainsFile(expectedFile));
                Assert.IsFalse(archive.IsOutdated(expectedFile));
            }

            // remove the project
            expectedEventType = FileEventType.FileDeleted;
            TestSolution.Remove(addedProject);

            Assert.IsTrue(resetEvent.WaitOne(500));
            //Assert.IsTrue(resetEvent.WaitOne(1000));

            foreach(var expectedFile in expectedFiles) {
                Assert.IsFalse(archive.ContainsFile(expectedFile));
            }
            TestHelpers.TestScaffold.Service.SourceFileChanged -= action;
        }
        public void Invoke(MethodInvoker globalSystemWindowsFormsMethodInvoker) {
            UIThreadInvoker.Invoke(globalSystemWindowsFormsMethodInvoker);
        }
    }
}
