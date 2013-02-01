using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ABB.SrcML.VisualStudio.SrcMLService
{
	/// <summary>
    /// //// 5.Implement the local service class.
	/// This is the class that implements the local service. It implements ISrcMLLocalService
	/// because this is the interface that we want to use, but it also implements the empty
	/// interface SSrcMLLocalService in order to notify the service creator that it actually
	/// implements this service.
	/// </summary>
	public class SrcMLLocalService : ISrcMLLocalService, SSrcMLLocalService
	{
		// Store a reference to the service provider that will be used to access the shell's services
		private IServiceProvider provider;
		/// <summary>
		/// Public constructor of this service. This will use a reference to a service provider to
		/// access the services provided by the shell.
		/// </summary>
        public SrcMLLocalService(IServiceProvider sp)
		{
            Trace.WriteLine("Constructing a new instance of SrcMLLocalService");
			provider = sp;
		}

        //// Implement the methods of ISrcMLLocalService here.
        #region ISrcMLLocalService Members
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
		public int LocalServiceFunction()
		{
			string outputText = " ======================================\n" +
								"\tLocal SrcML Service Function called.\n" +
								" ======================================\n";
			HelperFunctions.WriteOnOutputWindow(provider, outputText);
			return 0;
		}
		#endregion
	}
}
