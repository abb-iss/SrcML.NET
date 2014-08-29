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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        /// <summary>
        /// Implementation method for <see cref="AbstractMonitoringService.Update"/>
        /// </summary>
        protected override void UpdateImpl() {
            _srcMonitor.UpdateArchivesAsync().Wait();
            CurrentWorkingSet.InitializeAsync().Wait();
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
