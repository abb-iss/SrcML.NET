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
    //public class SolutionTests : Package {
        private delegate void ThreadInvoker();
        private TestContext _testContext;

        private ISrcMLGlobalService srcMLService;

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

        /*
        #region Package Members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            base.Initialize();
            SetUpSrcMLService();
        }
        #endregion

        private void SetUpSrcMLService() {
            //SrcMLFileLogger.DefaultLogger.Info("> Set up SrcML Service.");
            WriteLog("C:\\Data\\testlog.txt", "SolutionTests.cs > Set up SrcML Service.");
            srcMLService = Package.GetGlobalService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
            if(null == srcMLService) {
                WriteLog("C:\\Data\\testlog.txt", "Can not get the SrcML global service.");
            }
            Assert.IsNotNull(srcMLService, "Can not get the SrcML global service.");
        }
        */
        /*
        [TestInitialize]
        public void Init() {
            SrcMLServicePackage packageObject = new SrcMLServicePackage();
            IVsPackage package = (IVsPackage)packageObject;
            using(OleServiceProvider provider = OleServiceProvider.CreateOleServiceProviderWithBasicServices()) {
                int result = package.SetSite(provider);
                Assert.IsTrue(Microsoft.VisualStudio.ErrorHandler.Succeeded(result), "SetSite failed.");
                IServiceProvider serviceProvider = package as IServiceProvider;
                object o = serviceProvider.GetService(typeof(SSrcMLGlobalService));
                Assert.IsNotNull(o, "GetService returned null for the global service.");
                ISrcMLGlobalService service = o as ISrcMLGlobalService;
                Assert.IsNotNull(service, "The service SSrcMLGlobalService does not implements ISrcMLGlobalService.");


                ///////service.GlobalServiceFunction();
                ///////service.StartMonitoring();

            }
            package.SetSite(null);
            package.Close();
        }
        */
        [TestMethod]
        [HostType("VS IDE")]
        public void CreateEmptySolution() {
            UIThreadInvoker.Invoke((ThreadInvoker)delegate() {
                TestUtils testUtils = new TestUtils();
                testUtils.CloseCurrentSolution(__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave);
                testUtils.CreateEmptySolution(TestContext.TestDir, "EmptySolution");

                //Initialize();

                /*
                WriteLog("C:\\Data\\testlog.txt", "SolutionTests.cs CreateEmptySolution > Set up SrcML Service.");
                srcMLService = Package.GetGlobalService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
                if(null == srcMLService) {
                    WriteLog("C:\\Data\\testlog.txt", "Can not get the SrcML global service.");
                }
                Assert.IsNotNull(srcMLService, "Can not get the SrcML global service.");
                */
            });
        }




        private static void WriteLog(string logFile, string str) {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }

    }
}
