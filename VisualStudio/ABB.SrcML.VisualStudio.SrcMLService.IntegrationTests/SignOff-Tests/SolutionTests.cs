using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using EnvDTE;
using System.IO;
using Microsoft.VsSDK.IntegrationTestLibrary;

using ABB.SrcML.VisualStudio.SrcMLService;
using Microsoft.VisualStudio.Shell;
using Microsoft.VsSDK.UnitTestLibrary;


namespace SrcMLService_IntegrationTests.IntegrationTests {
    [TestClass]
    public class SolutionTests {
        private delegate void ThreadInvoker();
        private TestContext _testContext;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext {
            get { return _testContext; }
            set { _testContext = value; }
        }

        public SolutionTests() {
        }

        [TestMethod]
        [HostType("VS IDE")]
        public void CreateEmptySolution() {
            UIThreadInvoker.Invoke((ThreadInvoker)delegate() {
                TestUtils testUtils = new TestUtils();
                testUtils.CloseCurrentSolution(__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave);
                testUtils.CreateEmptySolution(TestContext.TestDir, "EmptySolution");
                Assert.IsNotNull(testUtils, "Can not get the TestUtils.");
            });
        }
    }
}
