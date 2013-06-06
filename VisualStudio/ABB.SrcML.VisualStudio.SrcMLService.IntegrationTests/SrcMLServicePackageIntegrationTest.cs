using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ABB.SrcML.Utilities;
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

        private static bool receivedFileAdded = false, receivedFileUpdated = false, receivedFileDeleted = false;
        private static FileEventRaisedArgs fera;

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
            srcMLService.IsReadyChanged += IsReadyChanged;
            srcMLService.MonitoringStopped += MonitoringStopped;
        }

        [TestInitialize]
        public void TestInitialize() {
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void FileLevelIncrementalUpdateTest() {
            // CSharp
            OpenSolution(testCSharpSolutionFilePath);
            CheckCSharpSolutionStartup();
            string newFileName = "NewCSharpClass1.cs";
            string saveAsFileName = "NewCSharpClass111111.cs";
            string templateFilePath = Path.Combine(testFileTemplateFolder, newFileName);
            string newFilePath = Path.Combine(testCSharpProjectFolder, newFileName);
            string saveAsFilePath = Path.Combine(testCSharpProjectFolder, saveAsFileName);
            AddCSharpProjectItem(testCSharpProjectFilePath, templateFilePath);
            CheckSrcMLFiles();
            System.Threading.Thread.Sleep(1000);
            SaveCSharpProjectItem(newFilePath);
            //CheckSrcMLFiles();    // EnvDTE.ProjectItem.Save() does not trigger IVsRunningDocTableEvents.OnAfterSave()
            System.Threading.Thread.Sleep(1000);
            RenameCSharpProjectItem(newFilePath, saveAsFilePath);
            CheckSrcMLFiles();
            System.Threading.Thread.Sleep(1000);
            DeleteCSharpProjectItem(saveAsFilePath);
            CheckSrcMLFiles();
            System.Threading.Thread.Sleep(1000);
            CloseSolution();

            // CPP
            OpenSolution(testCPPSolutionFilePath);
            CheckCPPSolutionStartup();
            newFileName = "NewCPPClass1.cpp";
            saveAsFileName = "NewCPPClass111111.cpp";
            templateFilePath = Path.Combine(testFileTemplateFolder, newFileName);
            newFilePath = Path.Combine(testCPPProjectFolder, newFileName);
            saveAsFilePath = Path.Combine(testCPPProjectFolder, saveAsFileName);
            AddCPPProjectItem(testCPPProjectFilePath, templateFilePath, newFilePath);
            CheckSrcMLFiles();
            System.Threading.Thread.Sleep(1000);
            //SaveCPPProjectItem(newFilePath);  // EnvDTE.ProjectItem.Save() is not implemented in VS for CPP project item. (NotImplementedException)
            //System.Threading.Thread.Sleep(1000);
            //RenameCPPProjectItem(newFilePath, saveAsFilePath);  // EnvDTE.ProjectItem.SaveAs() is not implemented in VS for CPP project item. (NotImplementedException)
            //System.Threading.Thread.Sleep(1000);
            //DeleteCPPProjectItem(saveAsFilePath);
            DeleteCPPProjectItem(newFilePath);
            CheckSrcMLFiles();
            System.Threading.Thread.Sleep(1000);
            CloseSolution();
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void ProjectLevelIncrementalUpdateTest() {
            // CPP
            OpenSolution(testCPPSolutionFilePath);
            CheckCPPSolutionStartup();
            string templateProjectFilePath = Path.GetFullPath(Path.Combine(testFileTemplateFolder, @"ConsoleApplication1\ConsoleApplication1\ConsoleApplication1.vcxproj"));
            //WriteLog(logFilePath, "templateProjectFilePath: [" + templateProjectFilePath + "]");
            AddCPPProject(templateProjectFilePath);
            CheckSrcMLFilesForNewCPPProject(true);
            System.Threading.Thread.Sleep(1000);
            RemoveCPPProject(templateProjectFilePath);
            CheckSrcMLFilesForNewCPPProject(false);
            //System.Threading.Thread.Sleep(1000);
            CloseSolution();

            /*
            // CSharp
            OpenSolution(testCSharpSolutionFilePath);
            CheckCSharpSolutionStartup();
            templateProjectFilePath = Path.Combine(testFileTemplateFolder, @"ClassLibrary1\ClassLibrary1\ClassLibrary1.csproj");
            //WriteLog(logFilePath, "C# templateProjectFilePath: [" + templateProjectFilePath + "]");
            AddCSharpProject(templateProjectFilePath);
            CheckSrcMLFilesForNewCSharpProject(true);
            System.Threading.Thread.Sleep(1000);
            RemoveCSharpProject(templateProjectFilePath);
            CheckSrcMLFilesForNewCSharpProject(false);
            System.Threading.Thread.Sleep(1000);
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
            System.Threading.Thread.Sleep(3000);
        }

        public void CheckCSharpSolutionStartup() {
            SrcMLArchive archive = srcMLService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
            string sourcePath = Path.Combine(testCSharpProjectFolder, "Class1.cs");
            string srcMLPath = archive.GetXmlPathForSourcePath(sourcePath);
            Assert.IsTrue(File.Exists(sourcePath), "The source file [" + sourcePath + "] does not exist.");
            Assert.IsTrue(File.Exists(srcMLPath), "The srcML file [" + srcMLPath + "] does not exist.");
            Assert.AreEqual(new FileInfo(sourcePath).LastWriteTime, new FileInfo(srcMLPath).LastWriteTime);
            XElement xelement = srcMLService.GetXElementForSourceFile(sourcePath);
            Assert.IsNotNull(xelement, "GetXElementForSourceFile returned null.");
            string sourcePathX = Path.Combine(testCSharpProjectFolder, "AlreadyDeletedClass1.cs");
            XElement xelementX = srcMLService.GetXElementForSourceFile(sourcePathX);
            Assert.IsNull(xelementX, "GetXElementForSourceFile returned not null.");
        }

        public void CheckCPPSolutionStartup() {
            SrcMLArchive archive = srcMLService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
            string sourcePath1 = Path.Combine(testCPPProjectFolder, "stdafx.cpp");
            string sourcePath2 = Path.Combine(testCPPProjectFolder, "stdafx.h");
            string sourcePath3 = Path.Combine(testCPPProjectFolder, "targetver.h");
            string sourcePath4 = Path.Combine(testCPPProjectFolder, "TestCPPSolution.cpp");
            string srcMLPath1 = archive.GetXmlPathForSourcePath(sourcePath1);
            string srcMLPath2 = archive.GetXmlPathForSourcePath(sourcePath2);
            string srcMLPath3 = archive.GetXmlPathForSourcePath(sourcePath3);
            string srcMLPath4 = archive.GetXmlPathForSourcePath(sourcePath4);
            Assert.IsTrue(File.Exists(sourcePath1), "The source file [" + sourcePath1 + "] does not exist.");
            Assert.IsTrue(File.Exists(sourcePath2), "The source file [" + sourcePath2 + "] does not exist.");
            Assert.IsTrue(File.Exists(sourcePath3), "The source file [" + sourcePath3 + "] does not exist.");
            Assert.IsTrue(File.Exists(sourcePath4), "The source file [" + sourcePath4 + "] does not exist.");
            Assert.IsTrue(File.Exists(srcMLPath1), "The srcML file [" + srcMLPath1 + "] does not exist.");
            Assert.IsTrue(File.Exists(srcMLPath2), "The srcML file [" + srcMLPath2 + "] does not exist.");
            Assert.IsTrue(File.Exists(srcMLPath3), "The srcML file [" + srcMLPath3 + "] does not exist.");
            Assert.IsTrue(File.Exists(srcMLPath4), "The srcML file [" + srcMLPath4 + "] does not exist.");
            Assert.AreEqual(new FileInfo(sourcePath1).LastWriteTime, new FileInfo(srcMLPath1).LastWriteTime);
            Assert.AreEqual(new FileInfo(sourcePath2).LastWriteTime, new FileInfo(srcMLPath2).LastWriteTime);
            Assert.AreEqual(new FileInfo(sourcePath3).LastWriteTime, new FileInfo(srcMLPath3).LastWriteTime);
            Assert.AreEqual(new FileInfo(sourcePath4).LastWriteTime, new FileInfo(srcMLPath4).LastWriteTime);
            XElement xelement1 = srcMLService.GetXElementForSourceFile(sourcePath1);
            XElement xelement2 = srcMLService.GetXElementForSourceFile(sourcePath2);
            XElement xelement3 = srcMLService.GetXElementForSourceFile(sourcePath3);
            XElement xelement4 = srcMLService.GetXElementForSourceFile(sourcePath4);
            Assert.IsNotNull(xelement1, "GetXElementForSourceFile returned null.");
            Assert.IsNotNull(xelement2, "GetXElementForSourceFile returned null.");
            Assert.IsNotNull(xelement3, "GetXElementForSourceFile returned null.");
            Assert.IsNotNull(xelement4, "GetXElementForSourceFile returned null.");
            string sourcePathX = Path.Combine(testCSharpProjectFolder, "AlreadyDeletedClass1.cpp");
            XElement xelementX = srcMLService.GetXElementForSourceFile(sourcePathX);
            Assert.IsNull(xelementX, "GetXElementForSourceFile returned not null.");
        }

        public static void SourceFileChanged(object sender, FileEventRaisedArgs args) {
            fera = args;
            switch(args.EventType) {
                case FileEventType.FileAdded:
                    receivedFileAdded = true;
                    break;
                case FileEventType.FileChanged:
                    receivedFileUpdated = true;
                    break;
                case FileEventType.FileDeleted:
                    receivedFileDeleted = true;
                    break;
            }
        }

        public void CheckSrcMLFiles() {
            string sourcePath = fera.FilePath;
            string oldSourcePath = fera.OldFilePath;
            FileEventType type = fera.EventType;
            bool hasSrcML = fera.HasSrcML;
            if(type == FileEventType.FileAdded || type == FileEventType.FileChanged) {
                Assert.IsTrue((receivedFileAdded || receivedFileUpdated));
                if(hasSrcML) {
                    SrcMLArchive archive = srcMLService.GetSrcMLArchive();
                    Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
                    string srcMLPath = archive.GetXmlPathForSourcePath(sourcePath);
                    ////WriteLog(logFilePath, "Adding/Updating srcMLPath = " + srcMLPath);
                    Assert.IsTrue(File.Exists(srcMLPath), "The srcML file [" + srcMLPath + "] does not exist.");
                    Assert.AreEqual(new FileInfo(sourcePath).LastWriteTime, new FileInfo(srcMLPath).LastWriteTime);
                    XElement xelement = srcMLService.GetXElementForSourceFile(sourcePath);
                    Assert.IsNotNull(xelement, "GetXElementForSourceFile returned null.");
                }
            } else if(type == FileEventType.FileDeleted) {
                Assert.IsTrue(receivedFileDeleted);
                SrcMLArchive archive = srcMLService.GetSrcMLArchive();
                Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
                string srcMLPath = archive.GetXmlPathForSourcePath(sourcePath);
                ////WriteLog(logFilePath, "Deleting srcMLPath = " + srcMLPath);
                Assert.IsFalse(File.Exists(srcMLPath), "The srcML file [" + srcMLPath + "] still exists.");
                XElement xelementX = srcMLService.GetXElementForSourceFile(sourcePath);
                Assert.IsNull(xelementX, "GetXElementForSourceFile returned not null.");
            }
            receivedFileAdded = receivedFileUpdated = receivedFileDeleted = false;
            fera = null;
        }

        public void CheckSrcMLFilesForNewCSharpProject(bool flag) {
            SrcMLArchive archive = srcMLService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
            string addedCSharpProjectFolder = Path.Combine(testFileTemplateFolder, @"ClassLibrary1\ClassLibrary1");
            string sourcePath1 = Path.Combine(addedCSharpProjectFolder, "Class1.cs");
            string srcMLPath1 = archive.GetXmlPathForSourcePath(sourcePath1);
            if(flag) {  //add
                Assert.IsTrue(File.Exists(sourcePath1), "The source file [" + sourcePath1 + "] does not exist.");
                Assert.IsTrue(File.Exists(srcMLPath1), "The srcML file [" + srcMLPath1 + "] does not exist.");
                Assert.AreEqual(new FileInfo(sourcePath1).LastWriteTime, new FileInfo(srcMLPath1).LastWriteTime);
                XElement xelement1 = srcMLService.GetXElementForSourceFile(sourcePath1);
                Assert.IsNotNull(xelement1, "GetXElementForSourceFile returned null.");
            } else {    //remove
                Assert.IsFalse(File.Exists(srcMLPath1), "The srcML file [" + srcMLPath1 + "] still exists.");
            }
        }

        public void CheckSrcMLFilesForNewCPPProject(bool flag) {
            SrcMLArchive archive = srcMLService.GetSrcMLArchive();
            Assert.IsNotNull(archive, "GetSrcMLArchive returned null.");
            string addedCPPProjectFolder = Path.Combine(testFileTemplateFolder, @"ConsoleApplication1\ConsoleApplication1");
            string sourcePath1 = Path.Combine(addedCPPProjectFolder, "stdafx.cpp");
            string sourcePath2 = Path.Combine(addedCPPProjectFolder, "stdafx.h");
            string sourcePath3 = Path.Combine(addedCPPProjectFolder, "targetver.h");
            string sourcePath4 = Path.Combine(addedCPPProjectFolder, "ConsoleApplication1.cpp");
            string srcMLPath1 = archive.GetXmlPathForSourcePath(sourcePath1);
            string srcMLPath2 = archive.GetXmlPathForSourcePath(sourcePath2);
            string srcMLPath3 = archive.GetXmlPathForSourcePath(sourcePath3);
            string srcMLPath4 = archive.GetXmlPathForSourcePath(sourcePath4);
            if(flag) {  //add
                Assert.IsTrue(File.Exists(sourcePath1), "The source file [" + sourcePath1 + "] does not exist.");
                Assert.IsTrue(File.Exists(sourcePath2), "The source file [" + sourcePath2 + "] does not exist.");
                Assert.IsTrue(File.Exists(sourcePath3), "The source file [" + sourcePath3 + "] does not exist.");
                Assert.IsTrue(File.Exists(sourcePath4), "The source file [" + sourcePath4 + "] does not exist.");
                Assert.IsTrue(File.Exists(srcMLPath1), "The srcML file [" + srcMLPath1 + "] does not exist.");
                Assert.IsTrue(File.Exists(srcMLPath2), "The srcML file [" + srcMLPath2 + "] does not exist.");
                Assert.IsTrue(File.Exists(srcMLPath3), "The srcML file [" + srcMLPath3 + "] does not exist.");
                Assert.IsTrue(File.Exists(srcMLPath4), "The srcML file [" + srcMLPath4 + "] does not exist.");
                Assert.AreEqual(new FileInfo(sourcePath1).LastWriteTime, new FileInfo(srcMLPath1).LastWriteTime);
                Assert.AreEqual(new FileInfo(sourcePath2).LastWriteTime, new FileInfo(srcMLPath2).LastWriteTime);
                Assert.AreEqual(new FileInfo(sourcePath3).LastWriteTime, new FileInfo(srcMLPath3).LastWriteTime);
                Assert.AreEqual(new FileInfo(sourcePath4).LastWriteTime, new FileInfo(srcMLPath4).LastWriteTime);
                XElement xelement1 = srcMLService.GetXElementForSourceFile(sourcePath1);
                XElement xelement2 = srcMLService.GetXElementForSourceFile(sourcePath2);
                XElement xelement3 = srcMLService.GetXElementForSourceFile(sourcePath3);
                XElement xelement4 = srcMLService.GetXElementForSourceFile(sourcePath4);
                Assert.IsNotNull(xelement1, "GetXElementForSourceFile returned null.");
                Assert.IsNotNull(xelement2, "GetXElementForSourceFile returned null.");
                Assert.IsNotNull(xelement3, "GetXElementForSourceFile returned null.");
                Assert.IsNotNull(xelement4, "GetXElementForSourceFile returned null.");
            } else {    //remove
                Assert.IsFalse(File.Exists(srcMLPath1), "The srcML file [" + srcMLPath1 + "] still exists.");
                Assert.IsFalse(File.Exists(srcMLPath2), "The srcML file [" + srcMLPath2 + "] still exists.");
                Assert.IsFalse(File.Exists(srcMLPath3), "The srcML file [" + srcMLPath3 + "] still exists.");
                Assert.IsFalse(File.Exists(srcMLPath4), "The srcML file [" + srcMLPath4 + "] still exists.");
            }
        }

        public static void IsReadyChanged(object sender, IsReadyChangedEventArgs args) {
        }

        public static void MonitoringStopped(object sender, EventArgs args) {
        }

        public void AddCSharpProjectItem(string testProjectFilePath, string fromFilePath) {
            var allProjects = ModelSolution.Projects;
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                var project = (Project)enumerator.Current;
                if(project != null && project.ProjectItems != null) {
                    ////WriteLog(logFilePath, ">> Project: [" + project.FullName + "]");
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
                    ////WriteLog(logFilePath, ">> Project: [" + project.FullName + "]");
                    if(testProjectFilePath.Equals(project.FullName)) {
                        File.Copy(fromFilePath, newFilePath);
                        ProjectItem item = project.ProjectItems.AddFromFile(newFilePath);
                    }
                }
            }
        }

        public void SaveCSharpProjectItem(string filePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be saved: [" + projectItem.Name + "]");
                projectItem.Open();
                projectItem.Save();
            }
        }

        public void SaveCPPProjectItem(string filePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be saved: [" + projectItem.Name + "]");
                projectItem.Open();
                projectItem.Save(); // EnvDTE.ProjectItem.Save() is not implemented for CPP project item. (NotImplementedException)
            }
        }

        public void RenameCSharpProjectItem(string filePath, string saveAsFilePath) {
            SaveAsCSharpProjectItem(filePath, saveAsFilePath);
            File.Delete(filePath);
        }

        public void SaveAsCSharpProjectItem(string filePath, string saveAsFilePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be save-as-ed: [" + projectItem.Name + "]");
                projectItem.SaveAs(saveAsFilePath);
            }
        }

        public void RenameCPPProjectItem(string filePath, string saveAsFilePath) {
            SaveAsCPPProjectItem(filePath, saveAsFilePath);
            File.Delete(filePath);
        }

        public void SaveAsCPPProjectItem(string filePath, string saveAsFilePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be save-as-ed: [" + projectItem.Name + "]");
                projectItem.SaveAs(saveAsFilePath); // EnvDTE.ProjectItem.SaveAs() is not implemented for CPP project item. (NotImplementedException)
            }
        }

        public void DeleteCSharpProjectItem(string filePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be deleted: [" + projectItem.Name + "]");
                projectItem.Delete();   // File being deleted from the file system
                //projectItem.Remove();   // File not being deleted from the file system, just removed from VS Solution Explorer
            }
        }

        public void DeleteCPPProjectItem(string filePath) {
            var projectItem = ModelSolution.FindProjectItem(filePath);
            if(projectItem != null) {
                ////WriteLog(logFilePath, "ProjectItem to be deleted: [" + projectItem.Name + "]");
                projectItem.Delete();   // File being deleted from the file system
                //projectItem.Remove();   // File not being deleted from the file system, just removed from VS Solution Explorer
            }
        }

        public void AddCPPProject(string templateProjectFilePath) {
            ModelSolution.AddFromFile(templateProjectFilePath);
        }

        public void AddCSharpProject(string templateProjectFilePath) {
            ModelSolution.AddFromFile(templateProjectFilePath);
        }

        public void RemoveCPPProject(string projectFilePath) {
            var allProjects = ModelSolution.Projects;
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                Project project = enumerator.Current as Project;
                if(project != null && projectFilePath.Equals(project.FullName)) {
                    //WriteLog(logFilePath, "Project to be removed: [" + project.FullName + "]");
                    ModelSolution.Remove(project);
                    break;
                }
            }
        }

        public void RemoveCSharpProject(string projectFilePath) {
            var allProjects = ModelSolution.Projects;
            var enumerator = allProjects.GetEnumerator();
            while(enumerator.MoveNext()) {
                Project project = enumerator.Current as Project;
                if(project != null && projectFilePath.Equals(project.FullName)) {
                    //WriteLog(logFilePath, "C# Project to be removed: [" + project.FullName + "]");
                    ModelSolution.Remove(project);
                    break;
                }
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

        /*
        private static void WriteLog(string logFile, string str) {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }
        */
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
}

