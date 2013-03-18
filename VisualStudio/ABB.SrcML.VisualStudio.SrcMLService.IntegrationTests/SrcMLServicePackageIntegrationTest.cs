using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using ABB.SrcML.VisualStudio.SrcMLService;

// New added references
using System.Windows.Forms;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VsSDK.UnitTestLibrary;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    /// <summary>
    /// Integration test for package validation
    /// </summary>
    [TestClass]
    public class SrcMLServicePackageIntegrationTests : IInvoker {

        private static IVsPackage package;
        private static ISrcMLGlobalService srcMLService;
        private static IComponentModel context;
        private static Solution ModelSolution;
        private static string baseTestInputsFolder = @"..\..\..\TestInputs\SrcMLService\";
        private static string testFileTemplateFolder = Path.GetFullPath(Path.Combine(baseTestInputsFolder, "Template"));
        private static string logFilePath = Path.GetFullPath(Path.Combine(baseTestInputsFolder, "testlog.txt"));
        private static string testCSharpSolutionFolder = Path.GetFullPath(Path.Combine(baseTestInputsFolder, "TestCSharpSolution"));
        private static string testCSharpSolutionFilePath = Path.Combine(testCSharpSolutionFolder, "TestCSharpSolution.sln");
        private static string testCSharpProjectFolder = Path.GetFullPath(Path.Combine(baseTestInputsFolder, @"TestCSharpSolution\TestCSharpSolution"));
        private static string testCSharpProjectFilePath = Path.Combine(testCSharpProjectFolder, "TestCSharpSolution.csproj");
        private static string testCPPSolutionFolder = Path.GetFullPath(Path.Combine(baseTestInputsFolder, "TestCPPSolution"));
        private static string testCPPSolutionFilePath = Path.Combine(testCPPSolutionFolder, "TestCPPSolution.sln");
        private static string testCPPProjectFolder = Path.GetFullPath(Path.Combine(baseTestInputsFolder, @"TestCPPSolution\TestCPPSolution"));
        private static string testCPPProjectFilePath = Path.Combine(testCPPProjectFolder, "TestCPPSolution.vcxproj");

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext) {
            // Create SrcMLServicePackage
            SrcMLServicePackage packageObject = new SrcMLServicePackage();
            package = (IVsPackage)packageObject;
            Assert.IsNotNull(package, "Get a null SrcMLServicePackage instance.");
            IServiceProvider serviceProvider = package as IServiceProvider;
            // Get SrcML Service
            object o = serviceProvider.GetService(typeof(SSrcMLGlobalService));
            Assert.IsNotNull(o, "GetService returned null for the global service.");
            srcMLService = o as ISrcMLGlobalService;
            Assert.IsNotNull(srcMLService, "The service SSrcMLGlobalService does not implements ISrcMLGlobalService.");

            // Register SrcML Service events
            srcMLService.SourceFileChanged += SourceFileChanged;
            srcMLService.StartupCompleted += StartupCompleted;
            srcMLService.MonitoringStopped += MonitoringStopped;
        }

        [TestInitialize]
        public void TestInitialize() {
        }

        [HostType("VS IDE")]
        [TestMethod]
        public void FileLevelIncrementalUpdateTest() {
            // CSharp
            OpenSolution(testCSharpSolutionFilePath);
            string newFileName = "NewCSharpClass1.cs";
            string saveAsFileName = "NewCSharpClass111111.cs";
            string templateFilePath = Path.Combine(testFileTemplateFolder, newFileName);
            string newFilePath = Path.Combine(testCSharpProjectFolder, newFileName);
            string saveAsFilePath = Path.Combine(testCSharpProjectFolder, saveAsFileName);
            AddCSharpProjectItem(testCSharpProjectFilePath, templateFilePath);
            System.Threading.Thread.Sleep(1000);
            SaveCSharpProjectItem(newFilePath);
            System.Threading.Thread.Sleep(1000);
            RenameCSharpProjectItem(newFilePath, saveAsFilePath);
            System.Threading.Thread.Sleep(1000);
            DeleteCSharpProjectItem(saveAsFileName);
            System.Threading.Thread.Sleep(1000);
            CloseSolution();
            // CPP
            /*
            OpenSolution(testCPPSolutionFilePath);
            string newFileName = "NewCPPClass1.cpp";
            string saveAsFileName = "NewCPPClass111111.cpp";
            string templateFilePath = Path.Combine(testFileTemplateFolder, newFileName);
            string newFilePath = Path.Combine(testCPPProjectFolder, newFileName);
            string saveAsFilePath = Path.Combine(testCPPProjectFolder, saveAsFileName);
            AddCPPProjectItem(testCPPProjectFilePath, templateFilePath, newFilePath);
            System.Threading.Thread.Sleep(1000);
            //SaveCPPProjectItem(newFilePath);
            //System.Threading.Thread.Sleep(1000);
            //RenameCPPProjectItem(newFilePath, saveAsFilePath);
            //System.Threading.Thread.Sleep(1000);
            //DeleteCPPProjectItem(saveAsFileName);
            //System.Threading.Thread.Sleep(1000);
            CloseSolution();
            */
        }
        
        [TestCleanup]
        public void TestCleanup() {
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            srcMLService = null;
            package.SetSite(null);
            package.Close();
            package = null;
        }

        public void OpenSolution(string testSolutionFilePath) {
            // Get the components service
            context = VsIdeTestHostContext.ServiceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            // Open a solution that is the initial state for your tests
            ModelSolution = VsIdeTestHostContext.Dte.Solution;
            ModelSolution.Open(Path.GetFullPath(testSolutionFilePath));
            Assert.IsNotNull(ModelSolution, "VS solution not found");
            // Start up
            srcMLService.StartMonitoring(true, SrcMLHelper.GetSrcMLDefaultDirectory());
            System.Threading.Thread.Sleep(5000);
        }

        public static void SourceFileChanged(object sender, FileEventRaisedArgs args) {
            WriteLog(logFilePath, "Respond to SrcMLService.SourceFileChanged, File = " + args.FilePath + ", OldFile = " + args.OldFilePath + ", EventType = " + args.EventType + ", HasSrcML = " + args.HasSrcML);
            string sourcePath = args.FilePath;
            string oldSourcePath = args.OldFilePath;
            bool hasSrcML = args.HasSrcML;
            if(args.EventType == FileEventType.FileAdded || args.EventType == FileEventType.FileChanged) {
                if(hasSrcML) {
                    SrcMLArchive archive = srcMLService.GetSrcMLArchive();
                    Assert.IsNotNull(archive, "GetSrcMLArchive reuturned null.");
                    string srcMLPath = archive.GetXmlPathForSourcePath(sourcePath);
                    WriteLog(logFilePath, "Adding/Updating srcMLPath = " + srcMLPath);
                    Assert.IsTrue(File.Exists(srcMLPath), "The srcML file [" + srcMLPath + "] does not exist.");
                }
            } else if(args.EventType == FileEventType.FileDeleted) {
                //if(hasSrcML) {
                    SrcMLArchive archive = srcMLService.GetSrcMLArchive();
                    Assert.IsNotNull(archive, "GetSrcMLArchive reuturned null.");
                    string srcMLPath = archive.GetXmlPathForSourcePath(sourcePath);
                    WriteLog(logFilePath, "Deleting srcMLPath = " + srcMLPath);
                    Assert.IsFalse(File.Exists(srcMLPath), "The srcML file [" + srcMLPath + "] still exists.");
                //}
            }
        }

        public static void StartupCompleted(object sender, EventArgs args) {
            WriteLog(logFilePath, "Respond to SrcMLService.StartupCompleted");
        }

        public static void MonitoringStopped(object sender, EventArgs args) {
            WriteLog(logFilePath, "Respond to SrcMLService.MonitoringStopped");
        }

        public void AddCSharpProjectItem(string testProjectFilePath, string fromFilePath) {
            var allProjects = ModelSolution.Projects;
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                var project = (Project)enumerator.Current;
                if(project != null && project.ProjectItems != null) {
                    WriteLog(logFilePath, ">> Project: [" + project.FullName + "]");
                    if(testProjectFilePath.Equals(project.FullName)) {
                        project.ProjectItems.AddFromFileCopy(fromFilePath);
                    }
                }
            }
        }

        public void AddCPPProjectItem(string testProjectFilePath, string fromFilePath, string newFilePath) {
            var allProjects = ModelSolution.Projects;
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                var project = (Project)enumerator.Current;
                if(project != null && project.ProjectItems != null) {
                    WriteLog(logFilePath, ">> Project: [" + project.FullName + "]");
                    if(testProjectFilePath.Equals(project.FullName)) {
                        ProjectItem item = project.ProjectItems.AddFromFileCopy(fromFilePath);
                    }
                }
            }
        }

        public void SaveCSharpProjectItem(string filePath) {
            WriteLog(logFilePath, "SaveProjectItem: [" + filePath + "]");
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                WriteLog(logFilePath, "ProjectItem to be saved: [" + projectItem.Name + "]");
                projectItem.Open();
                projectItem.Save();
            }
        }

        public void RenameCSharpProjectItem(string filePath, string saveAsFilePath) {
            SaveAsCSharpProjectItem(filePath, saveAsFilePath);
            File.Delete(filePath);
        }

        public void SaveAsCSharpProjectItem(string filePath, string saveAsFilePath) {
            WriteLog(logFilePath, "SaveAsProjectItem: [" + filePath + ", " + saveAsFilePath + "]");
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                WriteLog(logFilePath, "ProjectItem to be save-as-ed: [" + projectItem.Name + "]");
                projectItem.SaveAs(saveAsFilePath);
            }
        }

        public void DeleteCSharpProjectItem(string filePath) {
            WriteLog(logFilePath, "DeleteProjectItem: [" + filePath + "]");
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                WriteLog(logFilePath, "ProjectItem to be deleted: [" + projectItem.Name + "]");
                projectItem.Delete();   // File being deleted from the file system
                //projectItem.Remove();   // File not being deleted from the file system, just removed from VS Solution Explorer
            }
        }

        public void CloseSolution() {
            // Stop monitoring
            srcMLService.StopMonitoring();
            // Close the solution
            ModelSolution.Close();
            ModelSolution = null;
        }
        
        // FROM MSDN: http://msdn.microsoft.com/en-us/library/gg985355.aspx#UiThread
        // If your tests, or the methods under test, make changes to the model store, 
        // then you must execute them in the user interface thread. If you do not do this, 
        // you might see an AccessViolationException. Enclose the code of the test method in a call to Invoke:
        // System.Windows.Forms.MethodInvoker
        public void Invoke(MethodInvoker globalSystemWindowsFormsMethodInvoker) {
            UIThreadInvoker.Invoke(globalSystemWindowsFormsMethodInvoker);
        }

        private static void WriteLog(string logFile, string str) {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }
    }



    /* // The following code is generated by VS template, but does not work. 
    using(OleServiceProvider provider = OleServiceProvider.CreateOleServiceProviderWithBasicServices()) {
        int result = package.SetSite(provider);
        Assert.IsTrue(Microsoft.VisualStudio.ErrorHandler.Succeeded(result), "SetSite failed.");
        IServiceProvider serviceProvider = package as IServiceProvider;
        object o = serviceProvider.GetService(typeof(SSrcMLGlobalService));
        Assert.IsNotNull(o, "GetService returned null for the global service.");
        ISrcMLGlobalService service = o as ISrcMLGlobalService;
        Assert.IsNotNull(service, "The service SSrcMLGlobalService does not implements ISrcMLGlobalService.");
        ///////service.GlobalServiceFunction();
        ////service.StartMonitoring();
    }
    */
    
    /* // The following code is generated by VS template.  This works except for those statements commented out by "////".
    /// <summary>
    /// Integration test for package validation
    /// </summary>
    [TestClass]
    public class SrcMLServicePackageIntegrationTests {
        private delegate void ThreadInvoker();

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void PackageLoadTest() {
            UIThreadInvoker.Invoke((ThreadInvoker)delegate() {

                //Get the Shell Service
                IVsShell shellService = VsIdeTestHostContext.ServiceProvider.GetService(typeof(SVsShell)) as IVsShell;
                Assert.IsNotNull(shellService);

                //Validate package load
                IVsPackage package;
                Guid packageGuid = new Guid(GuidList.guidSrcMLServicePkgString);
                ////Assert.IsTrue(0 == shellService.LoadPackage(ref packageGuid, out package));
                ////Assert.IsNotNull(package, "Package failed to load");

            });
        }
    }
    */
    //Assert.AreEqual(@"C:\Users\USJIZHE\Documents\GitHub\SrcML.NET\TestInputs\SrcMLService\TestCSharpSolution\TestCSharpSolution.sln", Path.GetFullPath(testSolutionFilePath));
}

