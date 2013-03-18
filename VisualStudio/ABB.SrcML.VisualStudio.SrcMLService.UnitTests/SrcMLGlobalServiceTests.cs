using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Reflection;
using Microsoft.VsSDK.UnitTestLibrary;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ABB.SrcML.VisualStudio.SrcMLService;

namespace ABB.SrcML.VisualStudio.SrcMLService.UnitTests {
    [TestClass()]
    public class SrcMLGlobalServiceTests {

        private string extensionDirectory;
        private void SetUpSrcMLServiceExtensionDirectory() {
            //pluginDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            extensionDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }

        // Called in TestOutputNoPane()
        private GenericMockFactory mockOutputWindowFactory;

        [TestMethod()]
        public void TestOutputNoPane() {
            // Create an instance of the package and initialize it so that the GetService
            // will succeed, but the GetPane will fail.

            // As first create a service provider.
            using(OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices()) {
                // Now create the mock object for the output window.
                if(null == mockOutputWindowFactory) {
                    mockOutputWindowFactory = new GenericMockFactory("MockOutputWindow", new Type[] { typeof(IVsOutputWindow) });
                }
                BaseMock mockBase = mockOutputWindowFactory.GetInstance() as BaseMock;
                mockBase.AddMethodReturnValues(string.Format("{0}.{1}", typeof(IVsOutputWindow).FullName, "GetPane"),
                                               new object[] { -1, Guid.Empty, null });
                // Add the output window to the services provided by the service provider.
                serviceProvider.AddService(typeof(SVsOutputWindow), mockBase, false);

                // Create an instance of the package and initialize it calling SetSite.
                SrcMLServicePackage package = new SrcMLServicePackage();
                int result = ((IVsPackage)package).SetSite(serviceProvider);
                Assert.IsTrue(Microsoft.VisualStudio.ErrorHandler.Succeeded(result), "SetSite failed.");

                // Now we can create an instance of the service
                SetUpSrcMLServiceExtensionDirectory();
                SrcMLGlobalService service = new SrcMLGlobalService(package, extensionDirectory);
                
                service.GlobalServiceFunction();
                                
                ((IVsPackage)package).SetSite(null);
                ((IVsPackage)package).Close();
            }
        }

        // Called in TestOutput()
        private bool callbackExecuted;
        private void OutputWindowPaneCallback(object sender, CallbackArgs args) {
            callbackExecuted = true;
            string expectedText = "Global SrcML Service Function called.\n";
            string inputText = (string)args.GetParameter(0);
            Assert.AreEqual(expectedText, inputText, "OutputString called with wrong text.");
            args.ReturnValue = 0;
        }

        [TestMethod()]
        public void TestOutput() {
            callbackExecuted = false;
            // As first create a service provider.
            using(OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices()) {
                // Create a mock object for the output window pane.
                GenericMockFactory mockWindowPaneFactory = new GenericMockFactory("MockOutputWindowPane", new Type[] { typeof(IVsOutputWindowPane) });
                BaseMock mockWindowPane = mockWindowPaneFactory.GetInstance();
                mockWindowPane.AddMethodCallback(string.Format("{0}.{1}", typeof(IVsOutputWindowPane).FullName, "OutputString"),
                                                 new EventHandler<CallbackArgs>(OutputWindowPaneCallback));

                // Now create the mock object for the output window.
                if(null == mockOutputWindowFactory) {
                    mockOutputWindowFactory = new GenericMockFactory("MockOutputWindow1", new Type[] { typeof(IVsOutputWindow) });
                }
                BaseMock mockOutputWindow = mockOutputWindowFactory.GetInstance();
                mockOutputWindow.AddMethodReturnValues(
                        string.Format("{0}.{1}", typeof(IVsOutputWindow).FullName, "GetPane"),
                        new object[] { 0, Guid.Empty, (IVsOutputWindowPane)mockWindowPane });

                // Add the output window to the services provided by the service provider.
                serviceProvider.AddService(typeof(SVsOutputWindow), mockOutputWindow, false);

                // Create an instance of the package and initialize it calling SetSite.
                SrcMLServicePackage package = new SrcMLServicePackage();
                int result = ((IVsPackage)package).SetSite(serviceProvider);
                Assert.IsTrue(Microsoft.VisualStudio.ErrorHandler.Succeeded(result), "SetSite failed.");

                // Now we can create an instance of the service
                SrcMLGlobalService service = new SrcMLGlobalService(package, extensionDirectory);

                service.GlobalServiceFunction();
                
                Assert.IsTrue(callbackExecuted, "OutputText not called.");
                ((IVsPackage)package).SetSite(null);
                ((IVsPackage)package).Close();
            }
        }


    }
}
