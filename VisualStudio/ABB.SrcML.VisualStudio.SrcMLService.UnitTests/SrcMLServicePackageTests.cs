using System;
using System.Collections;
using System.Text;
using System.Reflection;
using Microsoft.VsSDK.UnitTestLibrary;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ABB.SrcML.VisualStudio.SrcMLService;

// New added references
using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace ABB.SrcML.VisualStudio.SrcMLService.UnitTests {
    [TestClass()]
    public class SrcMLServicePackageTests {
        [TestMethod()]
        // TODO: remove?
        public void CreateInstance() {
            SrcMLServicePackage package = new SrcMLServicePackage();
        }

        [TestMethod()]
        public void IsIVsPackage() {
            SrcMLServicePackage package = new SrcMLServicePackage();
            Assert.IsNotNull(package as IVsPackage, "The object does not implement IVsPackage");
        }

        [TestMethod()]
        public void SetSite() {
            // Create the package
            IVsPackage package = new SrcMLServicePackage() as IVsPackage;
            Assert.IsNotNull(package, "The object does not implement IVsPackage");
            // Create a basic service provider
            OleServiceProvider serviceProvider = OleServiceProvider.CreateOleServiceProviderWithBasicServices();
            // Site the package
            Assert.AreEqual(0, package.SetSite(serviceProvider), "SetSite did not return S_OK");
            // Unsite the package
            Assert.AreEqual(0, package.SetSite(null), "SetSite(null) did not return S_OK");
        }

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [TestMethod()]
        public void SetSiteSimple() {
            SrcMLServicePackage packageObject = new SrcMLServicePackage();
            IVsPackage package = (IVsPackage)packageObject;
            using(OleServiceProvider provider = OleServiceProvider.CreateOleServiceProviderWithBasicServices()) {
                int result = package.SetSite(provider);
                Assert.IsTrue(Microsoft.VisualStudio.ErrorHandler.Succeeded(result), "SetSite failed.");
            }
            package.SetSite(null);
            package.Close();
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        [TestMethod()]
        public void GetGlobalServiceSimple() {
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

            }
            package.SetSite(null);
            package.Close();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}
