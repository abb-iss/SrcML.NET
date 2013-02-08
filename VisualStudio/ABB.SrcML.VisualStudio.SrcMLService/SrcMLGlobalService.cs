/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

using EnvDTE;
using ABB.SrcML;
using ABB.SrcML.VisualStudio.SolutionMonitor;

namespace ABB.SrcML.VisualStudio.SrcMLService {
    /// <summary>
    /// Step 4: Implement the global service class.
    /// This is the class that implements the global service. All it needs to do is to implement 
    /// the interfaces exposed by this service (in this case ISrcMLGlobalService).
    /// This class also needs to implement the SSrcMLGlobalService interface in order to notify the 
    /// package that it is actually implementing this service.
    /// </summary>
    public class SrcMLGlobalService : ISrcMLGlobalService, SSrcMLGlobalService {

        /// <summary>
        /// SrcML.NET's Solution Monitor.
        /// </summary>
        private ABB.SrcML.VisualStudio.SolutionMonitor.SolutionMonitor CurrentMonitor;
        
        /// <summary>
        /// SrcML.NET's SrcMLArchive.
        /// </summary>
        private SrcMLArchive CurrentSrcMLArchive;

        /// <summary>
        /// The folder name of storing srcML files.
        /// </summary>
        private const string srcML = "\\SrcMLArchives";
        
        /// <summary>
        /// The path of SrcML.NET Service VS extension.
        /// </summary>
        private string SrcMLServiceDirectory;

        /// <summary>
        /// The path of storing srcML files.
        /// </summary>
        private string SrcMLArchiveDirectory;

        /// <summary>
        /// Store in this variable the service provider that will be used to query for other services.
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="extensionDirectory"></param>
        public SrcMLGlobalService(IServiceProvider sp, string extensionDirectory) {
            FileLogger.DefaultLogger.Info("Constructing a new instance of SrcMLGlobalService");

            serviceProvider = sp;
            SrcMLServiceDirectory = extensionDirectory;
        }

        // Implement the methods of ISrcMLLocalService here.
        #region ISrcMLGlobalService Members
        /*
        /// <summary>
        /// Implementation of the function that does not access the local service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
        public void GlobalServiceFunction() {
            string outputText = "Global SrcML Service Function called.\n";
            HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
        }

        /// <summary>
        /// Implementation of the function that will call a method of the local service.
        /// Notice that this class will access the local service using as service provider the one
        /// implemented by ServicesPackage.
        /// </summary>
        public int CallLocalService() {
            // Query the service provider for the local service.
            // This object is supposed to be build by ServicesPackage and it pass its service provider
            // to the constructor, so the local service should be found.
            ISrcMLLocalService localService = serviceProvider.GetService(typeof(SSrcMLLocalService)) as ISrcMLLocalService;
            if(null == localService) {
                // The local service was not found; write a message on the debug output and exit.
                Trace.WriteLine("Can not get the local service from the global one.");
                return -1;
            }

            // Now call the method of the local service. This will write a message on the output window.
            return localService.LocalServiceFunction();
        }
        */

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        public void StartMonitering() {
            FileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitering()");

            try {
                // Create a new instance of SrcML.NET's solution monitor
                CurrentMonitor = SolutionMonitorFactory.CreateMonitor();

                // Create a new instance of SrcML.NET's SrcMLArchive
                SrcMLArchiveDirectory = GetSrcMLArchiveFolder(SolutionMonitorFactory.GetOpenSolution());
                FileLogger.DefaultLogger.Info("SrcMLArchive Directory: [" + SrcMLArchiveDirectory + "]");

                CurrentSrcMLArchive = new SrcMLArchive(CurrentMonitor, SrcMLArchiveDirectory);
                
                // Subscribe events from SrcMLArchive (these statements should be in client applications)
                //CurrentSrcMLArchive.SourceFileChanged += RespondToSourceFileChangedEvent;
                //CurrentSrcMLArchive.StartupCompleted += RespondToStartupCompletedEvent;
                //CurrentSrcMLArchive.MonitoringStopped += RespondToMonitoringStoppedEvent;

                CurrentSrcMLArchive.StartWatching();
            } catch(Exception e) {
                FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitering()"));
            }

        }

        /// <summary>
        /// SrcML service stops monitoring the opened solution.
        /// </summary>
        public void StopMonitoring() {
            FileLogger.DefaultLogger.Info("SrcMLGlobalService.StopMonitoring()");

            if(CurrentMonitor != null) {
                try {
                    if(CurrentSrcMLArchive != null) {
                        CurrentSrcMLArchive.StopWatching();
                        CurrentSrcMLArchive = null;
                    }
                    CurrentMonitor = null;
                } catch(Exception e) {
                    FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StopMonitoring()"));
                }
            }
        }

        #endregion

        /// <summary>
        /// Generate the folder path for storing srcML files.
        /// (For all the following four methods.)
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        public string GetSrcMLArchiveFolder(Solution openSolution) {
            return CreateNamedFolder(openSolution, srcML);
        }

        private string CreateNamedFolder(Solution openSolution, string str) {
            var srcMLFolder = CreateFolder(str, SrcMLServiceDirectory);
            CreateFolder(GetName(openSolution), srcMLFolder + "\\");
            return srcMLFolder + "\\" + GetName(openSolution);
        }

        private string CreateFolder(string folderName, string parentDirectory) {
            if(!File.Exists(parentDirectory + folderName)) {
                var directoryInfo = Directory.CreateDirectory(parentDirectory + folderName);
                return directoryInfo.FullName;
            } else {
                return parentDirectory + folderName;
            }
        }

        private string GetName(Solution openSolution) {
            var fullName = openSolution.FullName;
            var split = fullName.Split('\\');
            return split[split.Length - 1] + fullName.GetHashCode();
        }
    }
}
