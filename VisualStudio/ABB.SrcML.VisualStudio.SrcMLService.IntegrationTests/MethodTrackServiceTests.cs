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
        public void TestMethodTrackings()
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