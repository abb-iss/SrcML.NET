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
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using SysThread = System.Threading.Thread;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    [TestClass]
    public class SrcMLServiceCSharpTests : IInvoker {
        private const string TestSolutionName = "TestCSharpSolution";
        private static ISrcMLGlobalService TestService;
        private static IVsPackage TestPackage;
        private static Solution TestSolution;

        private static string baseTestInputsFolder = @"..\..\..\TestInputs\SrcMLService\";
        private static string TestSolutionPath = Path.Combine(TestSolutionName, TestSolutionName + ".sln");

        [ClassInitialize]
        public static void Setup(TestContext testContext) {
            // Create a local copy of the solution
            TestHelpers.CopyDirectory(Path.Combine(baseTestInputsFolder, TestSolutionName), TestSolutionName);

            // get the DTE service provider
            var serviceProvider = VsIdeTestHostContext.Dte as IOleServiceProvider;
            Assert.IsNotNull(serviceProvider, "Could not get the service provider");

            // Create SrcMLServicePackage
            SrcMLServicePackage servicePackage = new SrcMLServicePackage();
            Assert.IsNotNull(servicePackage, "could not create a SrcML Service Package");
            TestPackage = servicePackage as IVsPackage;
            Assert.IsNotNull(TestPackage, "service package is not an IVsPackage");

            // site the srcML service package
            Assert.AreEqual(VSConstants.S_OK, TestPackage.SetSite(serviceProvider), "Could not site the srcML service package");

            object serviceObject = VsIdeTestHostContext.ServiceProvider.GetService(typeof(SSrcMLGlobalService));
            Assert.IsNotNull(serviceObject, "Could not get the SrcML Service");
            TestService = serviceObject as ISrcMLGlobalService;
            Assert.IsNotNull(TestService, "Service object does not implement ISrcMLGlobalService");

            TestSolution = VsIdeTestHostContext.Dte.Solution;
            Assert.IsNotNull(TestSolution, "Could not get the solution");
            TestSolution.Open(Path.GetFullPath(TestSolutionPath));
            TestService.StartMonitoring();
        }

        [ClassCleanup]
        public static void Cleanup() {
            TestService.StopMonitoring();
            TestSolution.Close();
            TestService = null;
            TestPackage.SetSite(null);
            TestPackage.Close();
            TestSolution = null;
            TestPackage = null;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestServiceStartup() {
            Assert.IsTrue(TestHelpers.WaitForServiceToFinish(TestService, 5000));
            var archive = TestService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.AreEqual(2, archive.FileUnits.Count(), "There should only be one file in the srcML archive");
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestFileOperations() {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var archive = TestService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            string expectedFilePath = null;
            FileEventType expectedEventType = FileEventType.FileDeleted;

            EventHandler<FileEventRaisedArgs> action = (o, e) => {
                if(e.FilePath == expectedFilePath && e.EventType == expectedEventType) {
                    resetEvent.Set();
                }
            };
            TestService.SourceFileChanged += action;

            // add a file
            var fileTemplate = Path.Combine(TestConstants.TemplatesFolder, "NewCSharpClass1.cs");
            expectedFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCSharpClass1.cs");
            expectedEventType = FileEventType.FileAdded;
            var item = project.ProjectItems.AddFromFileCopy(fileTemplate);
            project.Save();

            Assert.IsTrue(resetEvent.WaitOne(500));
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
            Assert.IsFalse(archive.IsOutdated(expectedFilePath), String.Format("{0} is outdated", expectedFilePath));

            // delete the file
            expectedEventType = FileEventType.FileDeleted;
            item = TestSolution.FindProjectItem(expectedFilePath);
            item.Delete();
            project.Save();
            Assert.IsTrue(resetEvent.WaitOne(500));

            Assert.IsFalse(archive.IsOutdated(expectedFilePath));
            //Assert.AreEqual(2, archive.FileUnits.Count());
            TestService.SourceFileChanged -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestProjectOperations() {
            var archive = TestService.GetSrcMLArchive();
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            var testProjectName = "ClassLibrary1";

            var expectedProjectDirectory = Path.GetFullPath(Path.Combine(TestSolutionName, testProjectName));
            var expectedEventType = FileEventType.FileAdded;

            HashSet<string> expectedFiles = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) {
                Path.Combine(expectedProjectDirectory, "Class1.cs"),
                Path.Combine(expectedProjectDirectory, "Properties", "AssemblyInfo.cs")
            };

            EventHandler<FileEventRaisedArgs> action = (o, e) => {
                if(expectedFiles.Contains(Path.GetFullPath(e.FilePath)) && e.EventType == expectedEventType) {
                    resetEvent.Set();
                }
            };
            TestService.SourceFileChanged += action;

            var projectTemplate = Path.GetFullPath(Path.Combine(TestConstants.TemplatesFolder, testProjectName, testProjectName, testProjectName + ".csproj"));

            // add a new project
            var addedProject = TestSolution.AddFromTemplate(projectTemplate, expectedProjectDirectory, testProjectName);
            addedProject.Save();
            Assert.IsTrue(resetEvent.WaitOne(500));
            Assert.IsTrue(resetEvent.WaitOne(500));
            foreach(var expectedFile in expectedFiles) {
                Assert.IsTrue(File.Exists(expectedFile));
                Assert.IsFalse(archive.IsOutdated(expectedFile));
            }

            //// remove the project
            //expectedEventType = FileEventType.FileDeleted;
            //TestSolution.Remove(addedProject);
            
            //Assert.IsTrue(resetEvent.WaitOne(5000));
            // Assert.IsTrue(resetEvent.WaitOne(5000));

            //foreach(var expectedFile in expectedFiles) {
            //     Assert.IsFalse(File.Exists(expectedFile));
            //    Assert.IsFalse(archive.IsOutdated(expectedFile));
            //}
            TestService.SourceFileChanged -= action;
        }
        public void Invoke(MethodInvoker globalSystemWindowsFormsMethodInvoker) {
            UIThreadInvoker.Invoke(globalSystemWindowsFormsMethodInvoker);
        }
    }
}
