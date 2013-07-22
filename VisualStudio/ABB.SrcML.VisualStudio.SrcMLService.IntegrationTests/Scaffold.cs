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
    class Scaffold<ISERVICE> where ISERVICE : class {
        public IVsPackage Package { get; set; }
        public Solution Solution { get; set; }
        public ISERVICE Service { get; set; }

        public static Scaffold<ISERVICE> Setup(IVsPackage package, Type serviceType) {
            Assert.IsNotNull(package, "Package is null");
            var scaffold = new Scaffold<ISERVICE>();
            scaffold.Package = package;

            var serviceProvider = VsIdeTestHostContext.Dte as IOleServiceProvider;
            Assert.IsNotNull(serviceProvider, "Could not get the service provider");

            // site the srcML service package
            Assert.AreEqual(VSConstants.S_OK, scaffold.Package.SetSite(serviceProvider), "Could not site the srcML service package");

            object serviceObject = VsIdeTestHostContext.ServiceProvider.GetService(serviceType);
            Assert.IsNotNull(serviceObject, "Could not get the SrcML Service");
            scaffold.Service = serviceObject as ISERVICE;
            Assert.IsNotNull(scaffold.Service, "Service object does not implement " + typeof(ISERVICE));

            return scaffold;
        }

        public void Cleanup() {
            Service = null;
            Package.SetSite(null);
            Package = null;
        }
    }
}
