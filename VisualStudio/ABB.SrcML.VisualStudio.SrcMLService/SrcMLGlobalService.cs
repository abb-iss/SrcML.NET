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
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using ABB.SrcML;
using ABB.SrcML.Utilities;
using ABB.SrcML.VisualStudio.SolutionMonitor;
using log4net;

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
        private SrcMLArchive CurrentSrcMLArchive;

        /// <summary>
        /// The folder name of storing srcML archives.
        /// </summary>
        private const string srcMLArchivesFolderString = "\\SrcMLArchives";
        
        /// <summary>
        /// The path of SrcML.NET Service VS extension.
        /// </summary>
        private string SrcMLServiceDirectory;

        /// <summary>
        /// Store in this variable the service provider that will be used to query for other services.
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Status bar service.
        /// </summary>
        private IVsStatusbar statusBar;
        private int frozen;
        private uint cookie = 0;
        private uint amountCompleted = 0;
        private bool duringStartup;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="extensionDirectory"></param>
        public SrcMLGlobalService(IServiceProvider sp, string extensionDirectory) {
            SrcMLFileLogger.DefaultLogger.Info("Constructing a new instance of SrcMLGlobalService");
            serviceProvider = sp;
            SrcMLServiceDirectory = extensionDirectory;
            statusBar = (IVsStatusbar)Package.GetGlobalService(typeof(SVsStatusbar));
        }

        // Implement the methods of ISrcMLLocalService here.
        #region ISrcMLGlobalService Members

        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;
        public event EventHandler<EventArgs> MonitoringStopped;

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        /// <param name="srcMLArchiveDirectory"></param>
        /// <param name="useExistingSrcML"></param>
        public void StartMonitoring(bool useExistingSrcML, string srcMLBinaryDirectory) {
            // Get the path of the folder that storing the srcML archives
            string srcMLArchiveDirectory = GetSrcMLArchiveFolder(SolutionMonitorFactory.GetOpenSolution());
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitoring( " + srcMLArchiveDirectory + " )");
            try {
                // Create a new instance of SrcML.NET's LastModifiedArchive
                LastModifiedArchive lastModifiedArchive = new LastModifiedArchive(srcMLArchiveDirectory);

                // Create a new instance of SrcML.NET's SrcMLArchive
                CurrentSrcMLArchive = new SrcMLArchive(srcMLArchiveDirectory, useExistingSrcML, new SrcMLGenerator(srcMLBinaryDirectory));

                // Create a new instance of SrcML.NET's solution monitor
                CurrentMonitor = SolutionMonitorFactory.CreateMonitor(srcMLArchiveDirectory, lastModifiedArchive, CurrentSrcMLArchive);

                // Subscribe events from Solution Monitor
                CurrentMonitor.FileChanged += RespondToFileChangedEvent;
                CurrentMonitor.IsReadyChanged += RespondToIsReadyChangedEvent;

                CurrentMonitor.MonitoringStopped += RespondToMonitoringStoppedEvent;

                // Initialize the progress bar.
                if(statusBar != null) {
                    statusBar.Progress(ref cookie, 1, "", 0, 0);
                }

                // Start monitoring
                duringStartup = true;
                CurrentMonitor.StartMonitoring();
            } catch(Exception e) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(e, "Exception in SrcMLGlobalService.StartMonitoring()"));
            }
        }

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        public void StartMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StartMonitoring() - default");
            StartMonitoring(true, SrcMLHelper.GetSrcMLDefaultDirectory(SrcMLServiceDirectory));
        }

        /// <summary>
        /// SrcML service stops monitoring the opened solution.
        /// </summary>
        public void StopMonitoring() {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLGlobalService.StopMonitoring()");
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

        /// <summary>
        /// Get current SrcMLArchive instance.
        /// </summary>
        /// <returns></returns>
        public SrcMLArchive GetSrcMLArchive() {
            return CurrentSrcMLArchive;
        }

        /// <summary>
        /// Gets the XElement for the specified source file.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        public XElement GetXElementForSourceFile(string sourceFilePath) {
            return CurrentSrcMLArchive.GetXElementForSourceFile(sourceFilePath);
        }

        /// <summary>
        /// Implementation of the function that does not access the local service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
        public void GlobalServiceFunction() {
            string outputText = "Global SrcML Service Function called.\n";
            HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
        }

        /*
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

        #endregion

        /// <summary>
        /// Generate the folder path for storing srcML files.
        /// (For all the following four methods.)
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        public string GetSrcMLArchiveFolder(Solution openSolution) {
            return CreateNamedFolder(openSolution, srcMLArchivesFolderString);
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
            return split[split.Length - 1].Substring(0, split[split.Length - 1].Length - 4) + fullName.GetHashCode();
        }

        /// <summary>
        /// Respond to the FileChanged event from Solution Monitor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToFileChangedEvent(object sender, FileEventRaisedArgs eventArgs) {
            //SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToFileChangedEvent(), File = " + eventArgs.FilePath + ", EventType = " + eventArgs.EventType + ", HasSrcML = " + eventArgs.HasSrcML);
            // Show progress on the status bar.
            if(duringStartup) {
                amountCompleted++;
                ShowProgressOnStatusBar("SrcML Service is processing " + eventArgs.FilePath);
            }
            OnFileChanged(eventArgs);
        }

        /// <summary>
        /// Respond to the IsReady
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToIsReadyChangedEvent(object sender, IsReadyChangedEventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToStartupCompletedEvent()");
            if(eventArgs.UpdatedReadyState) {
                // Clear the progress bar.
                amountCompleted = 0;
                if(statusBar != null) {
                    statusBar.Progress(ref cookie, 0, "", 0, 0);
                }
                DisplayTextOnStatusBar("SrcML Service has finished processing files");
                duringStartup = false;
            }
            OnIsReadyChanged(eventArgs);
        }

        /// <summary>
        /// Respond to the MonitorStopped event from SrcMLArchive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToMonitoringStoppedEvent(object sender, EventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("SrcMLService: RespondToMonitoringStoppedEvent()");
            OnMonitoringStopped(eventArgs);
        }

        protected virtual void OnFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = SourceFileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnIsReadyChanged(IsReadyChangedEventArgs e) {
            EventHandler<IsReadyChangedEventArgs> handler = IsReadyChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler<EventArgs> handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Display text on the Visual Studio status bar.
        /// </summary>
        /// <param name="text"></param>
        void DisplayTextOnStatusBar(string text) {
            if(statusBar != null) {
                statusBar.IsFrozen(out frozen);
                if(frozen == 0) {
                    // Set the status bar text and make its display static.
                    statusBar.SetText(text);
                    statusBar.FreezeOutput(1);
                    // Clear the status bar text.
                    statusBar.FreezeOutput(0);
                    statusBar.Clear();
                }
            }
        }

        /// <summary>
        /// Display incremental progress on the Visual Studio status bar.
        /// </summary>
        /// <param name="label"></param>
        void ShowProgressOnStatusBar(string label) {
            if(statusBar != null) {
                statusBar.Progress(ref cookie, 1, label, amountCompleted, (uint)CurrentMonitor.NumberOfAllMonitoredFiles);
            }
        }

    }
}
