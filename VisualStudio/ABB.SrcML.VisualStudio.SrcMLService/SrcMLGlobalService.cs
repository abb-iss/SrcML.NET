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
        private AbstractFileMonitor CurrentMonitor;
        
        /// <summary>
        /// SrcML.NET's SrcMLArchive.
        /// </summary>
        private AbstractArchive CurrentSrcMLArchive;

        /// <summary>
        /// The folder name of storing srcML files.
        /// </summary>
        private const string srcML = "\\SrcMLArchives";
        
        /// <summary>
        /// The path of SrcML.NET Service VS extension.
        /// </summary>
        private string SrcMLServiceDirectory;

        /// <summary>
        /// The path of storing srcML files.  (obsolete)
        /// </summary>
        //////private string SrcMLArchiveDirectory;

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
            SrcMLFileLogger.DefaultLogger.Info("Constructing a new instance of SrcMLGlobalService");

            serviceProvider = sp;
            SrcMLServiceDirectory = extensionDirectory;
        }

        // Implement the methods of ISrcMLLocalService here.
        #region ISrcMLGlobalService Members

        public event EventHandler<FileEventRaisedArgs> FileEventRaised;
        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;
        public event EventHandler<EventArgs> StartupCompleted;
        public event EventHandler<EventArgs> MonitoringStopped;

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
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitering()");
            writeLog("C:\\Data\\srcMLNETlog.txt", "======= SrcMLGlobalService.StartMonitoring() =======");

            try {

                // TODO: this SrcMLArchiveDirectory may be changed to a configurable user input, instead of under the plugin folder.
                ////SrcMLArchiveDirectory = GetSrcMLArchiveFolder(SolutionMonitorFactory.GetOpenSolution());
                ////SrcMLFileLogger.DefaultLogger.Info("SrcMLArchive Directory: [" + SrcMLArchiveDirectory + "]");
                // TODO: how would this bool useExistingSrcML come from?

                SrcMLFileLogger.DefaultLogger.Info("SrcMLService Directory: [" + SrcMLServiceDirectory + "]");
                writeLog("C:\\Data\\srcMLNETlog.txt", "SrcMLService Directory: [" + SrcMLServiceDirectory + "]");
                // Create a new instance of SrcML.NET's LastModifiedArchive
                LastModifiedArchive lastModifiedArchive = new LastModifiedArchive(SrcMLServiceDirectory);
                // Create a new instance of SrcML.NET's SrcMLArchive
                bool useExistingSrcML = true;
                CurrentSrcMLArchive = new SrcMLArchive(SrcMLServiceDirectory, useExistingSrcML);

                // Subscribe events from SrcMLArchive ==> NO events from SrcMLArchive/DefaultArchive any more
                //CurrentSrcMLArchive.FileChanged += RespondToSourceFileChangedEvent;

                // Create a new instance of SrcML.NET's solution monitor
                CurrentMonitor = SolutionMonitorFactory.CreateMonitor(SrcMLServiceDirectory, lastModifiedArchive, CurrentSrcMLArchive);

                // Subscribe events from Solution Monitor
                CurrentMonitor.FileChanged += RespondToFileChangedEvent;
                CurrentMonitor.StartupCompleted += RespondToStartupCompletedEvent;
                //CurrentSrcMLArchive.MonitoringStopped += RespondToMonitoringStoppedEvent;

                // Start monitoring
                CurrentMonitor.StartMonitoring();

            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitering()"));
            }

        }

        /// <summary>
        /// SrcML service stops monitoring the opened solution.
        /// </summary>
        public void StopMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StopMonitoring()");
            writeLog("C:\\Data\\srcMLNETlog.txt", "======= SrcMLGlobalService.StopMonitoring() =======");

            try {
                if(CurrentMonitor != null && CurrentSrcMLArchive != null) {
                    CurrentMonitor.StopMonitoring();
                    CurrentSrcMLArchive = null;
                    CurrentMonitor = null;
                }
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StopMonitoring()"));
            }
        }

        /*
        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        public void StartMonitering() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitering()");

            try {   // ==> reverse the order of CurrentMonitor and CurrentSrcMLArchive
                // Create a new instance of SrcML.NET's solution monitor
                CurrentMonitor = SolutionMonitorFactory.CreateMonitor();
                CurrentMonitor.FileEventRaised += RespondToSolutionMonitorEvent;

                // Create a new instance of SrcML.NET's SrcMLArchive
                SrcMLArchiveDirectory = GetSrcMLArchiveFolder(SolutionMonitorFactory.GetOpenSolution());
                SrcMLFileLogger.DefaultLogger.Info("SrcMLArchive Directory: [" + SrcMLArchiveDirectory + "]");

                CurrentSrcMLArchive = new SrcMLArchive(CurrentMonitor, SrcMLArchiveDirectory);
                
                // Subscribe events from SrcMLArchive
                CurrentSrcMLArchive.SourceFileChanged += RespondToSourceFileChangedEvent;
                CurrentSrcMLArchive.StartupCompleted += RespondToStartupCompletedEvent;
                CurrentSrcMLArchive.MonitoringStopped += RespondToMonitoringStoppedEvent;

                CurrentSrcMLArchive.StartWatching();
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitering()"));
            }

        }

        /// <summary>
        /// SrcML service stops monitoring the opened solution.
        /// </summary>
        public void StopMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StopMonitoring()");

            if(CurrentMonitor != null) {
                try {
                    if(CurrentSrcMLArchive != null) {
                        CurrentSrcMLArchive.StopWatching(); // ==> CurrentMonitor.StopMonitoring()
                        CurrentSrcMLArchive = null;
                    }
                    CurrentMonitor = null;
                } catch(Exception e) {
                    SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StopMonitoring()"));
                }
            }
        }
        */
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

        /* // obsolete
        /// <summary>
        /// Respond to the FileEventRaised event from Solution Monitor. (obsolete)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToSolutionMonitorEvent(object sender, FileEventRaisedArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToSolutionMonitorEvent(), File = " + eventArgs.SourceFilePath + ", EventType = " + eventArgs.EventType);
            // Current design decision: 
            // Only raise the event for non-source files.
            if(!CurrentSrcMLArchive.IsValidFileExtension(eventArgs.SourceFilePath)) {
                OnFileEventRaised(eventArgs);
            }
        }
        */

        /// <summary>
        /// Respond to the FileChanged event from Solution Monitor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToFileChangedEvent(object sender, FileEventRaisedArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToFileChangedEvent(), File = " + eventArgs.FilePath + ", EventType = " + eventArgs.EventType);
            writeLog("C:\\Data\\srcMLNETlog.txt", "SrcMLService: RespondToFileChangedEvent(), File = " + eventArgs.FilePath + ", EventType = " + eventArgs.EventType);

            OnFileChanged(eventArgs);
        }

        /// <summary>
        /// Respond to the StartupCompleted event from Solution Monitor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToStartupCompletedEvent(object sender, EventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToStartupCompletedEvent()");
            writeLog("C:\\Data\\srcMLNETlog.txt", "SrcMLService: RespondToStartupCompletedEvent()");

            OnStartupCompleted(eventArgs);
        }

        /* // obsolete
        /// <summary>
        /// Respond to the MonitorStopped event from SrcMLArchive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToMonitoringStoppedEvent(object sender, EventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToMonitoringStoppedEvent()");
            OnMonitoringStopped(eventArgs);
        }
        */

        /* // obsolete
        /// <summary>
        /// Handle SolutionMonitorEvents.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnFileEventRaised(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileEventRaised;
            if(handler != null) {
                handler(this, e);
            }
        }
        */

        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = SourceFileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnStartupCompleted(EventArgs e) {
            EventHandler<EventArgs> handler = StartupCompleted;
            if(handler != null) {
                handler(this, e);
            }
        }

        /* // obsolete
        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler<EventArgs> handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }
        */

        /// <summary>
        /// For debugging.
        /// writeLog("C:\\Data\\srcMLNETlog.txt", "======= SolutionMonitor: START MONITORING =======");
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="str"></param>
        private void writeLog(string logFile, string str) {
            StreamWriter sw = new StreamWriter(logFile, true, System.Text.Encoding.ASCII);
            sw.WriteLine(str);
            sw.Close();
        }

    }
}
