using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace ABB.SrcML.VisualStudio.SrcMLService
{
	/// <summary>
    /// //// 4.Implement the global service class.
	/// This is the class that implements the global service. All it needs to do is to implement 
    /// the interfaces exposed by this service (in this case ISrcMLGlobalService).
    /// This class also needs to implement the SSrcMLGlobalService interface in order to notify the 
	/// package that it is actually implementing this service.
	/// </summary>
	public class SrcMLGlobalService : ISrcMLGlobalService, SSrcMLGlobalService
	{
		// Store in this variable the service provider that will be used to query for other services.
		private IServiceProvider serviceProvider;
        public SrcMLGlobalService(IServiceProvider sp)
		{
            writeLog("D:\\Data\\log.txt", "SrcMLGlobalService.SrcMLGlobalService()");
            Trace.WriteLine("Constructing a new instance of SrcMLGlobalService");
			serviceProvider = sp;
		}

        // Implement the methods of ISrcMLLocalService here.
        #region ISrcMLGlobalService Members
        /// <summary>
		/// Implementation of the function that does not access the local service.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
		public void GlobalServiceFunction()
		{
			string outputText = " ======================================\n" +
			                    "\tGlobal SrcML Service Function called.\n" +
			                    " ======================================\n";
			HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
		}

		/// <summary>
		/// Implementation of the function that will call a method of the local service.
		/// Notice that this class will access the local service using as service provider the one
		/// implemented by ServicesPackage.
		/// </summary>
		public int CallLocalService()
		{
			// Query the service provider for the local service.
			// This object is supposed to be build by ServicesPackage and it pass its service provider
			// to the constructor, so the local service should be found.
            ISrcMLLocalService localService = serviceProvider.GetService(typeof(SSrcMLLocalService)) as ISrcMLLocalService;
			if (null == localService)
			{
				// The local service was not found; write a message on the debug output and exit.
				Trace.WriteLine("Can not get the local service from the global one.");
				return -1;
			}

			// Now call the method of the local service. This will write a message on the output window.
			return localService.LocalServiceFunction();
		}

        public void StartMonitering()
        {
            writeLog("D:\\Data\\log.txt", "==> Triggered SrcMLGlobalService.StartMonitering()");
            string outputText = " ======================================\n" +
                                "\tStartMonitering() called.\n" +
                                " ======================================\n";
            HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
        }

        public void StopMonitoring()
        {
            writeLog("D:\\Data\\log.txt", "==> Triggered SrcMLGlobalService.StopMonitoring()");
            string outputText = " ======================================\n" +
                                "\tStopMonitering() called.\n" +
                                " ======================================\n";
            HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
        }

        #endregion

        /// <summary>
        /// For debugging.
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="str"></param>
        private void writeLog(string logFile, string str)
        {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }

	}
}
