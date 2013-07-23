using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace ABB.SrcML.VisualStudio.SrcMLService.IntegrationTests {
    /// <summary>
    /// The scaffold class provides a quick setup for Visual Studio integration tests.
    /// </summary>
    /// <typeparam name="ISERVICE">The service interface being tested</typeparam>
    class Scaffold<ISERVICE> where ISERVICE : class {
        /// <summary>
        /// The package that contains <typeparam name="ISERVICE" />
        /// </summary>
        public IVsPackage Package { get; set; }

        /// <summary>
        /// An instance of the service to be tested
        /// </summary>
        public ISERVICE Service { get; set; }

        /// <summary>
        /// Sets up the visual studio host instance. This method sites the package in the <see cref="Microsoft.VSSDK.Tools.VsIdeTesting.VsIdeTestHostContext"/>
        /// and gets the service instance.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public static Scaffold<ISERVICE> Setup(IVsPackage package, Type serviceType) {
            Assert.IsNotNull(package, "Package is null");
            var scaffold = new Scaffold<ISERVICE>();
            scaffold.Package = package;

            var serviceProvider = VsIdeTestHostContext.Dte as IOleServiceProvider;
            Assert.IsNotNull(serviceProvider, "Could not get the service provider");

            // site the package
            Assert.AreEqual(VSConstants.S_OK, scaffold.Package.SetSite(serviceProvider), "Could not site the package");

            object serviceObject = VsIdeTestHostContext.ServiceProvider.GetService(serviceType);
            Assert.IsNotNull(serviceObject, String.Format("Could not get the service {0}", serviceType));
            Assert.IsInstanceOfType(serviceObject, typeof(ISERVICE), String.Format("Service object does not implement {0}", typeof(ISERVICE)));
            scaffold.Service = serviceObject as ISERVICE;

            return scaffold;
        }

        /// <summary>
        /// Cleans up the scaffold (unsites the package and sets <see cref="Service"/> and <see cref="Package"/> to null.
        /// </summary>
        public void Cleanup() {
            Service = null;
            Package.SetSite(null);
            Package = null;
        }
    }
}
