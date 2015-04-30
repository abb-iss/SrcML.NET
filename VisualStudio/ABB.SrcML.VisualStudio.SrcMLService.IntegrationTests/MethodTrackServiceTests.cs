using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ABB.SrcML.Test.Utilities;
using ABB.VisualStudio;
using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using Thread = System.Threading.Thread;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {

    [TestClass]
    public class MethodTrackServiceTests : IInvoker {
        private const string TestSolutionName = "TestMethodTrackingSolution";
        private static object TestLock;
        private static Solution TestSolution;
        private static string TestSolutionPath = Path.Combine(TestSolutionName, TestSolutionName + ".sln");
        private static DTE testDTE;

        [ClassInitialize]
        public static void ClassSetup(TestContext testContext) {
            // Create a local copy of the solution
            FileUtils.CopyDirectory(Path.Combine(TestConstants.InputFolderPath, TestSolutionName), TestSolutionName);
            TestLock = new object();
        }

        public void Invoke(MethodInvoker globalSystemWindowsFormsMethodInvoker) {
            UIThreadInvoker.Invoke(globalSystemWindowsFormsMethodInvoker);
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestCursorMonitoring()
        {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            
            var CMservice = TestHelpers.TestScaffoldCM.Service;

            AutoResetEvent resetEvent = new AutoResetEvent(false);

            PropertyChangedEventHandler action = (o, e) =>
            {
                lock (TestLock)
                {
                    resetEvent.Set();
                }
            };
            TestHelpers.TestScaffoldCM.Service.PropertyChanged += action;

            // open a file (Class1.cs) 
            string filePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            var window = testDTE.ItemOperations.OpenFile(filePath);
            Assert.AreEqual(window.Kind, "Document");
            var document = window.Document;
            Assert.AreEqual(document.Name, "Class1.cs");
            window.Activate();

            // move cursor to line 15
            testDTE.ExecuteCommand("Edit.Goto", "15");
            Assert.AreEqual(CMservice.CurrentLineNumber, 15);
            
            TestHelpers.TestScaffoldCM.Service.PropertyChanged -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestMethodTrackingsOnCursorMoving()
        {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var archive = TestHelpers.TestScaffold.Service.CurrentSrcMLArchive;
            var dataarchive = TestHelpers.TestScaffoldData.Service.CurrentDataArchive;
            var MTservice = TestHelpers.TestScaffoldMT.Service;

            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.IsNotNull(dataarchive, "Could not get the Data Archive");

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MethodEventType expectedEventType = MethodEventType.PositionChanged;

            EventHandler<MethodEventRaisedArgs> action = (o, e) =>
            {
                lock(TestLock) {
                    if(e.EventType == expectedEventType) 
                    {
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent += action; 

            // open a file (Class1.cs) 
            string FilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            var window = testDTE.ItemOperations.OpenFile(FilePath);
            Assert.AreEqual(window.Kind, "Document");
            var document = window.Document;
            Assert.AreEqual(document.Name, "Class1.cs");
            window.Activate();

            // move cursor to (15,x) which is in method "member1"
            testDTE.ExecuteCommand("Edit.Goto", "15");
            Assert.AreEqual(MTservice.CurrentLineNumber, 15);
            Assert.AreEqual(MTservice.CurrentMethod.Name, "member1");
            Assert.AreEqual(MTservice.CurrentMethod.StartLineNumber, 13);
            Console.WriteLine(MTservice.CurrentMethod.NameSpace);
            Console.WriteLine(MTservice.CurrentMethod.Type);
            List<string> paranames = new List<string> {"x", "y"};
            Assert.AreEqual(MTservice.CurrentMethod.ParameterNames.Count, paranames.Count);
            for (int i = 0; i < paranames.Count; i++)
                Assert.AreEqual(MTservice.CurrentMethod.ParameterNames[i], paranames[i]);
            List<string> paratypes = new List<string> {"int", "string"};
            Assert.AreEqual(MTservice.CurrentMethod.ParameterTypes.Count, paratypes.Count);
            for (int i = 0; i < paratypes.Count; i++)
                Assert.AreEqual(MTservice.CurrentMethod.ParameterTypes[i], paratypes[i]);

                TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestNavHistoryListInMethodTracking()
        {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var archive = TestHelpers.TestScaffold.Service.CurrentSrcMLArchive;
            var dataarchive = TestHelpers.TestScaffoldData.Service.CurrentDataArchive;
            var MTservice = TestHelpers.TestScaffoldMT.Service;

            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.IsNotNull(dataarchive, "Could not get the Data Archive");

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MethodEventType expectedEventType = MethodEventType.PositionChanged;

            EventHandler<MethodEventRaisedArgs> action = (o, e) =>
            {
                lock (TestLock)
                {
                    if (e.EventType == expectedEventType)
                    {
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent += action;

            // open a file (Class1.cs) 
            string FilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            var window = testDTE.ItemOperations.OpenFile(FilePath);
            window.Activate();

            // move cursor to (15,x) which is in method "member1"
            testDTE.ExecuteCommand("Edit.Goto", "15");
            Assert.AreEqual(MTservice.NavigatedMethods.Count, 1);
            Assert.AreEqual(MTservice.NavigatedMethods[0], MTservice.CurrentMethod);

            // move cursor to (16,x) which is still in method "member1"
            testDTE.ExecuteCommand("Edit.Goto", "16");
            Assert.AreEqual(MTservice.NavigatedMethods.Count, 1);
            Assert.AreEqual(MTservice.NavigatedMethods[0], MTservice.CurrentMethod);
            var oldMethod = MTservice.CurrentMethod;

            // move cursor to (22,x) which is in method "member2"
            testDTE.ExecuteCommand("Edit.Goto", "22");
            Assert.AreEqual(MTservice.CurrentMethod.Name, "member2");
            Assert.AreEqual(MTservice.CurrentMethod.StartLineNumber, 20);
            Assert.AreEqual(MTservice.CurrentMethod.ParameterNames.Count, 0);
            Assert.AreEqual(MTservice.NavigatedMethods.Count, 2);
            Assert.AreEqual(MTservice.NavigatedMethods[0], oldMethod);
            Assert.AreEqual(MTservice.NavigatedMethods[1], MTservice.CurrentMethod);

            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestMethodTrackingsOnFileDelete()
        {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var service = TestHelpers.TestScaffold.Service;
            var archive = TestHelpers.TestScaffold.Service.CurrentSrcMLArchive;
            var dataarchive = TestHelpers.TestScaffoldData.Service.CurrentDataArchive;
            var MTservice = TestHelpers.TestScaffoldMT.Service;

            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.IsNotNull(dataarchive, "Could not get the Data Archive");

            int scanInterval = 5;
            int scanIntervalMs = scanInterval * 1000;
            service.ScanInterval = scanInterval; //this is important for file change operation
            
            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MethodEventType expectedEventType = MethodEventType.MethodDeleted;
            EventHandler<MethodEventRaisedArgs> action = (o, e) =>
            {
                //Console.WriteLine(e.EventType.ToString());
                
                lock (TestLock)
                {
                    if (e.EventType == expectedEventType)
                    {   
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent += action;

            // open a file (Class1.cs) 
            string FilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            var window = testDTE.ItemOperations.OpenFile(FilePath);
            window.Activate();

            // move cursor to (15,x) which is in method "member1"
            testDTE.ExecuteCommand("Edit.Goto", "15");
            var navigatedMethod1 = MTservice.CurrentMethod;
            window.Close();

            // add a file
            var fileTemplate = Path.Combine(TestConstants.TemplatesFolder, "NewCSharpClass2.cs");
            var newFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "NewCSharpClass2.cs");
            var item = project.ProjectItems.AddFromFileCopy(fileTemplate);
            project.Save();

            //SrcMLService scans every 5 seconds, archive is not yet updated unless wait for 5 seconds 
            Thread.Sleep(scanIntervalMs);

            Assert.IsTrue(archive.ContainsFile(newFilePath));
            Assert.IsTrue(dataarchive.ContainsFile(newFilePath));
            
            //open added file
            window = testDTE.ItemOperations.OpenFile(newFilePath);
            window.Activate();

            // move cursor to (13,x) which is in method "foo"
            expectedEventType = MethodEventType.PositionChanged;
            testDTE.ExecuteCommand("Edit.Goto", "13");
            var navigatedMethod2 = MTservice.CurrentMethod;

            Assert.IsTrue(resetEvent.WaitOne(scanIntervalMs));
            Assert.AreEqual(navigatedMethod2.Name, "foo");
            Assert.AreEqual(navigatedMethod2.StartLineNumber, 11);
            Assert.AreEqual(navigatedMethod2.ParameterNames.Count, 0);

            Assert.AreEqual(MTservice.NavigatedMethods.Count, 2);
            Assert.AreEqual(MTservice.NavigatedMethods[0], navigatedMethod1);
            Assert.AreEqual(MTservice.NavigatedMethods[1], navigatedMethod2);

            // delete the file
            item = TestSolution.FindProjectItem(newFilePath);
            expectedEventType = MethodEventType.MethodDeleted;
            item.Delete();
            project.Save();

            Assert.IsTrue(resetEvent.WaitOne(scanIntervalMs));
            Assert.AreEqual(MTservice.NavigatedMethods.Count, 1);
            Assert.AreEqual(MTservice.NavigatedMethods[0], navigatedMethod1);
            
            //since the file (containing navigatedMethoed2) was delected, this field should be reset
            Assert.AreEqual(MTservice.CurrentMethod.Name, ""); 
             
            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent -= action;
        }


        [TestMethod]
        [HostType("VS IDE")]
        public void TestMethodTrackingsOnFileChange()
        {
            // setup
            Project project = TestHelpers.GetProjects(TestSolution).FirstOrDefault();
            Assert.IsNotNull(project, "Couldn't get the project");
            var service = TestHelpers.TestScaffold.Service;
            var archive = TestHelpers.TestScaffold.Service.CurrentSrcMLArchive;
            var dataarchive = TestHelpers.TestScaffoldData.Service.CurrentDataArchive;
            var MTservice = TestHelpers.TestScaffoldMT.Service;

            Assert.IsNotNull(archive, "Could not get the SrcML Archive");
            Assert.IsNotNull(dataarchive, "Could not get the Data Archive");

            int scanInterval = 5;
            int scanIntervalMs = scanInterval * 1000;
            service.ScanInterval = scanInterval; //this is important for file change operation

            AutoResetEvent resetEvent = new AutoResetEvent(false);
            MethodEventType expectedEventType = MethodEventType.MethodDeleted;
            EventHandler<MethodEventRaisedArgs> action = (o, e) =>
            {
                lock (TestLock)
                {
                    if (e.EventType == expectedEventType)
                    {   
                        resetEvent.Set();
                    }
                }
            };
            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent += action;

            // open a file (Class1.cs) 
            string FilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            var window = testDTE.ItemOperations.OpenFile(FilePath);
            window.Activate();

            // move cursor to (15,x) which is in method "member1"
            expectedEventType = MethodEventType.PositionChanged;
            testDTE.ExecuteCommand("Edit.Goto", "15");
            var navigatedMethod1 = MTservice.CurrentMethod;

            //move cursor to (22,x) which is in method "member2"
            expectedEventType = MethodEventType.PositionChanged;
            testDTE.ExecuteCommand("Edit.Goto", "22");
            var navigatedMethod2 = MTservice.CurrentMethod;
            window.Close();
           
            // replace the current file with another one --- simulate the "file change"
            // Startline of "member1" is changed from 13 to 15
            // method "member2" is commented out (deleted)
            var fileTemplate = Path.Combine(TestConstants.TemplatesFolder, "Class1Changed.cs");
            var newFilePath = Path.Combine(Path.GetDirectoryName(project.FullName), "Class1.cs");
            File.Copy(fileTemplate, newFilePath, true);
            window = testDTE.ItemOperations.OpenFile(newFilePath);
            window.Activate();
            testDTE.ExecuteCommand("EDIT.SelectAll");
            testDTE.ExecuteCommand("EDIT.Cut");
            testDTE.ExecuteCommand("Edit.Paste");
            project.Save();

            //method change (of member1) is raised first because it was navigated first
            expectedEventType = MethodEventType.MethodChanged; 
            Assert.IsTrue(resetEvent.WaitOne(scanIntervalMs));

            //method (of member2) is deleted
            expectedEventType = MethodEventType.MethodDeleted;
            Assert.IsTrue(resetEvent.WaitOne(scanIntervalMs));
            
            Assert.AreEqual(MTservice.NavigatedMethods.Count, 1);
            Assert.IsTrue(MTservice.NavigatedMethods[0].SignatureEquals(navigatedMethod1));
            Assert.AreEqual(MTservice.NavigatedMethods[0].StartLineNumber, 15);

            TestHelpers.TestScaffoldMT.Service.MethodUpdatedEvent -= action;
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestDataServiceStartup()
        {
            var archive = TestHelpers.TestScaffold.Service.CurrentSrcMLArchive;
            Assert.IsNotNull(archive, "Could not get the Srcml Archive");
            var storagepath = TestHelpers.TestScaffold.Service.CurrentMonitor.MonitorStoragePath;
            Assert.AreNotEqual(storagepath, "");
            Console.WriteLine(storagepath);

            
            var dataarchive = TestHelpers.TestScaffoldData.Service.CurrentDataArchive;
            Assert.IsNotNull(dataarchive, "Could not get the Data Archive"); 
            
            var workingset = TestHelpers.TestScaffoldData.Service.CurrentWorkingSet;
            Assert.IsNotNull(workingset, "Could not get the Workingset");                       
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void TestMtServiceStartup() {
            var method = TestHelpers.TestScaffoldMT.Service.CurrentMethod;
            int line = TestHelpers.TestScaffoldMT.Service.CurrentLineNumber;
            int column = TestHelpers.TestScaffoldMT.Service.CurrentColumnNumber;
            Assert.IsNotNull(method, "Could not get the current file");
            Assert.AreEqual(line, 0);
            Assert.AreEqual(column, 0);
        }

        [TestInitialize]
        public void TestSetup() {
            testDTE = VsIdeTestHostContext.Dte;
            TestSolution = testDTE.Solution;
            Assert.IsNotNull(TestSolution, "Could not get the solution");

            TestSolution.Open(Path.GetFullPath(TestSolutionPath));
            Assert.IsTrue(TestHelpers.WaitForServiceToFinish(TestHelpers.TestScaffold.Service, 5000));
            //System.Diagnostics.Debugger.Launch(); // workaround for Wait DataService to finish - click "No"
            Assert.IsTrue(TestHelpers.WaitForServiceToFinish(TestHelpers.TestScaffoldData.Service, 5000));
        }

        [TestCleanup]
        public void TestCleanup() {
            TestHelpers.TestScaffold.Service.StopMonitoring();
            TestSolution.Close();
            //TestSolution = null;
            // TestScaffold.Cleanup();
        }
    }
}