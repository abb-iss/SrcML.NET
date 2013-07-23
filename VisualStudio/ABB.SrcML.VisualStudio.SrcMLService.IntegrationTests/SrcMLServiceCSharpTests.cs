using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
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
    public class SrcMLServiceCSharpTests : IInvoker {
        private const string TestSolutionName = "TestCSharpSolution";
        private static Solution TestSolution;
        private static object TestLock;
        private static string TestSolutionPath = Path.Combine(TestSolutionName, TestSolutionName + ".sln");

        [ClassInitialize]
        public static void ClassSetup(TestContext testContext) {
            // Create a local copy of the solution
            TestHelpers.CopyDirectory(Path.Combine(TestConstants.InputFolderPath, TestSolutionName), TestSolutionName);
            TestLock = new object();
        }

        [TestInitialize]
        public void TestSetup() {
            TestSolution = VsIdeTestHostContext.Dte.Solution;
            Assert.IsNotNull(TestSolution, "Could not get the solution");

            TestSolution.Open(Path.GetFullPath(TestSolutionPath));
            TestHelpers.TestScaffold.Service.StartMonitoring();
        }

        [TestCleanup]
        public void Cleanup() {
            TestHelpers.TestScaffold.Service.StopMonitoring();
            TestSolution.Close();
            //TestSolution = null;
            // TestScaffold.Cleanup();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestCsServiceStartup() {
            Assert.IsTrue(TestHelpers.WaitForServiceToFinish(TestHelpers.TestScaffold.Service, 5000));
            var archive = TestHelpers.TestScaffold.Service.GetSrcMLArchive();
            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.AreEqual(2, archive.FileUnits.Count(), "There should only be two files in the srcML archive");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestCsFileOperations() {
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
            var fileTemplate = Path.Combine(TestConstants.TemplatesFolder, "NewCSharpClass1.cs");
            expectedFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCSharpClass1.cs");
            expectedEventType = FileEventType.FileAdded;
            var item = project.ProjectItems.AddFromFileCopy(fileTemplate);
            project.Save();

            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(archive.ContainsFile(expectedFilePath));
            Assert.IsFalse(archive.IsOutdated(expectedFilePath));

            // rename a file
            string oldFilePath = expectedFilePath;
            expectedFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCSharpClass2.cs");
            expectedEventType = FileEventType.FileAdded;
            item.Open();
            item.SaveAs(expectedFilePath);
            File.Delete(oldFilePath);
            project.Save();

            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(archive.ContainsFile(expectedFilePath), "The archive should contain {0}", expectedFilePath);
            Assert.IsFalse(archive.ContainsFile(oldFilePath), "the archive should not contain {0}", oldFilePath);
            Assert.IsFalse(archive.IsOutdated(expectedFilePath), String.Format("{0} is outdated", expectedFilePath));

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
        public void TestCsProjectOperations() {
            var archive = TestHelpers.TestScaffold.Service.GetSrcMLArchive();
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            var testProjectName = "ClassLibrary1";

            var expectedProjectDirectory = Path.GetFullPath(Path.Combine(TestSolutionName, testProjectName));
            var expectedEventType = FileEventType.FileAdded;

            HashSet<string> expectedFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
                Path.Combine(expectedProjectDirectory, "Class1.cs"),
                Path.Combine(expectedProjectDirectory, "Properties", "AssemblyInfo.cs")
            };

            EventHandler<FileEventRaisedArgs> action = (o, e) => {
                lock(TestLock) {
                    if(expectedFiles.Contains(Path.GetFullPath(e.FilePath)) && e.EventType == expectedEventType) {
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffold.Service.SourceFileChanged += action;

            var projectTemplate = Path.GetFullPath(Path.Combine(TestConstants.TemplatesFolder, testProjectName, testProjectName, testProjectName + ".csproj"));

            // add a new project
            var addedProject = TestSolution.AddFromTemplate(projectTemplate, expectedProjectDirectory, testProjectName);
            addedProject.Save();
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
            // Assert.IsTrue(resetEvent.WaitOne(500));

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
