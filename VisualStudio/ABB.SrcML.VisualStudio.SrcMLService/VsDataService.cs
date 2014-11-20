/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using ABB.VisualStudio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// The VS Data service responds to file change events from <see cref="VsMonitoringService"/> and keeps <see cref="CurrentWorkingSet"/> up to date.
    /// </summary>
    public class VsDataService : AbstractMonitoringService, ISrcMLDataService, SSrcMLDataService {
        private ArchiveMonitor<SrcMLArchive> _srcMonitor;
        private ISrcMLGlobalService _srcMLService;
        private IWorkingSetRegistrarService _workingSetFactories;

        /// <summary>
        /// Creates a new data service
        /// </summary>
        /// <param name="serviceProvider">The container where this service is sited</param>
        /// <param name="taskManagerService">The task manager</param>
        /// <param name="srcMLService">The srcML service</param>
        /// <param name="workingSetService">The working set factory service</param>
        public VsDataService(SrcMLServicePackage serviceProvider, ITaskManagerService taskManagerService, ISrcMLGlobalService srcMLService, IWorkingSetRegistrarService workingSetService)
            : base(serviceProvider, taskManagerService) {
            _srcMLService = srcMLService;
            _workingSetFactories = workingSetService;

            if(_srcMLService.IsMonitoring) {
                _srcMLService_MonitoringStarted(this, new EventArgs());
            }
            Subscribe();
        }

        /// <summary>
        /// Raised whenever a file is processed
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileProcessed;

        /// <summary>
        /// The data archive for the current solution
        /// </summary>
        public DataArchive CurrentDataArchive { get; private set; }

        /// <summary>
        /// The working set for the current solution
        /// </summary>
        public AbstractWorkingSet CurrentWorkingSet { get; private set; }

        /// <summary>
        /// Saves the state for this service
        /// </summary>
        protected override void Save() {
            _srcMonitor.Save();
        }

        /// <summary>
        /// Sets up the <see cref="CurrentDataArchive"/> and <see cref="CurrentWorkingSet"/> to respond to events from the srcML service
        /// </summary>
        protected override void Setup() {
            string storagePath = _srcMLService.CurrentMonitor.MonitorStoragePath;
            SrcMLArchive sourceArchive = _srcMLService.CurrentSrcMLArchive;

            CurrentDataArchive = new DataArchive(storagePath, sourceArchive, GlobalScheduler);
            CurrentDataArchive.Generator.ErrorLog = sourceArchive.Generator.ErrorLog;
            CurrentDataArchive.Generator.IsLoggingErrors = true;

            _srcMonitor = new ArchiveMonitor<SrcMLArchive>(GlobalScheduler, storagePath, sourceArchive, CurrentDataArchive);
            CurrentWorkingSet = _workingSetFactories.Default.CreateWorkingSet(storagePath, CurrentDataArchive, GlobalTaskFactory);
            CurrentWorkingSet.UseAsynchronousMethods = true;
        }

        const string WS_FAILOVER_FILENAME = "FAILOVER_COMPLETE_WORKINGSET";

        /// <summary>
        /// Implementation method for <see cref="AbstractMonitoringService.Update"/>
        /// </summary>
        protected override void UpdateImpl() {
            _srcMonitor.UpdateArchivesAsync().Wait();

            bool workingSetFailed = false;
            try {
                CurrentWorkingSet.InitializeAsync().Wait();
            } catch(AggregateException e) {
                workingSetFailed = true;
                var logFileName = Path.Combine(ServiceProvider.ExtensionDirectory, "update_error.log");
                bool logFileIsNew = !File.Exists(logFileName);
                using(var error = new StreamWriter(logFileName, true)) {
                    if(logFileIsNew) {
                        error.WriteLine("Please e-mail the contents of this file to US-prodet@abb.com");
                        error.WriteLine();
                        error.WriteLine("Calling Assembly: {0}", this.GetType().AssemblyQualifiedName);
                        error.WriteLine("Working Set: {0}", CurrentWorkingSet.GetType().AssemblyQualifiedName);
                    }
                    
                    foreach(var exception in e.InnerExceptions) {
                        error.WriteLine();
                        error.WriteLine("=========================================================");
                        error.WriteLine("Message: {0}", exception.Message);
                        error.WriteLine("Source: {0}", exception.Source);
                        error.WriteLine(exception.StackTrace);
                    }
                }

                if(workingSetFailed) {
                    var failoverFileName = Path.Combine(_srcMonitor.MonitorStoragePath, WS_FAILOVER_FILENAME);
                    bool useCompleteWorkingSet;
                    bool askUser = true;

                    if(File.Exists(failoverFileName) && Boolean.TryParse(File.ReadAllText(failoverFileName), out useCompleteWorkingSet)) {
                        askUser = false;
                    } else {
                        useCompleteWorkingSet = false;
                    }

                    if(askUser) {
                        string message = String.Format("Prodet selective analysis has encountered an error. Do you want to load analysis for all files? Your solution has {0} files. Solutions with >1000 files may consume too much memory.", CurrentDataArchive.GetFiles().Count());
                        int fileCount = CurrentDataArchive.GetFiles().Count();
                        MessageBoxResult defaultResult = (fileCount > 1000 ? MessageBoxResult.No : MessageBoxResult.Yes);
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            var userInput = MessageBox.Show(Application.Current.MainWindow, message, "Working Set Error", MessageBoxButton.YesNo, MessageBoxImage.Error, defaultResult);

                            switch(userInput) {
                                case MessageBoxResult.Yes:
                                    useCompleteWorkingSet = true;
                                    break;
                                case MessageBoxResult.No:
                                    useCompleteWorkingSet = false;
                                    break;
                                default:
                                    useCompleteWorkingSet = false;
                                    break;
                            }
                        }));

                        File.WriteAllText(failoverFileName, useCompleteWorkingSet.ToString());
                    }

                    if(logFileIsNew) {
                        System.Diagnostics.Process.Start("notepad.exe", logFileName);
                    }

                    CurrentWorkingSet.Dispose();
                    if(useCompleteWorkingSet) {
                        CurrentWorkingSet = new CompleteWorkingSet(CurrentDataArchive, GlobalTaskFactory);
                        CurrentWorkingSet.InitializeAsync().Wait();
                    } else {
                        CurrentWorkingSet = null;
                    }
                }
            }
        }

        /// <summary>
        /// Implementation method for <see cref="AbstractMonitoringService.StartMonitoring"/>
        /// </summary>
        protected override void StartMonitoringImpl() {
            _srcMonitor.FileChanged += _srcMonitor_FileChanged;
            _srcMonitor.StartMonitoring();
            CurrentWorkingSet.StartMonitoring();
        }

        /// <summary>
        /// Implementation method for <see cref="AbstractMonitoringService.StopMonitoring"/>
        /// </summary>
        protected override void StopMonitoringImpl() {
            if(null != CurrentWorkingSet) {
                _srcMonitor.FileChanged -= _srcMonitor_FileChanged;
                CurrentWorkingSet.StopMonitoring();
                _srcMonitor.StopMonitoring();
                CurrentWorkingSet.Dispose();
                _srcMonitor.Dispose();
                CurrentWorkingSet = null;
                CurrentDataArchive = null;
                _srcMonitor = null;
            }
        }

        /// <summary>
        /// Raises the <see cref="FileProcessed"/> event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnFileProcessed(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileProcessed;
            if(null != handler) {
                handler(this, e);
            }
        }

        private void Subscribe() {
            if(null != _srcMLService) {
                _srcMLService.MonitoringStarted += _srcMLService_MonitoringStarted;
                _srcMLService.MonitoringStopped += _srcMLService_MonitoringStopped;
            }
        }

        void _srcMonitor_FileChanged(object sender, FileEventRaisedArgs e) {
            OnFileProcessed(e);
        }

        private void _srcMLService_MonitoringStopped(object sender, EventArgs e) {
            StopMonitoring();
        }

        private void _srcMLService_MonitoringStarted(object sender, EventArgs e) {
            StartMonitoring();
        }
    }
}
