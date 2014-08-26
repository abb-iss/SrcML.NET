/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using System.Threading.Tasks;
using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ABB.VisualStudio;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// The default implementation for <see cref="ISrcMLDataService"/>
    /// </summary>
    public class SrcMLDataService : ISrcMLDataService, SSrcMLDataService {
        private IServiceProvider _serviceProvider;
        private ITaskManagerService _taskManager;
        private ISrcMLGlobalService _srcMLService;
        private IWorkingSetRegistrarService _workingSetFactories;
        private ArchiveMonitor<SrcMLArchive> _srcMonitor;
        private DataArchive CurrentDataArchive;

        private TaskScheduler Scheduler { get { return (null == _taskManager ? TaskScheduler.Default : _taskManager.GlobalScheduler); } }

        private TaskFactory TaskFactory { get { return (null == _taskManager ? Task.Factory : _taskManager.GlobalFactory); } }

        /// <summary>
        /// The current working set object
        /// </summary>
        public AbstractWorkingSet CurrentWorkingSet { get; private set; }

        /// <summary>
        /// Raised when monitoring is started
        /// </summary>
        public event EventHandler MonitoringStarted;

        /// <summary>
        /// Raised when monitoring is stopped
        /// </summary>
        public event EventHandler MonitoringStopped;

        /// <summary>
        /// Raised when an update is started
        /// </summary>
        public event EventHandler UpdateStarted;

        /// <summary>
        /// Raised when an update is complete
        /// </summary>
        public event EventHandler UpdateCompleted;

        /// <summary>
        /// Raised when a file change is processed
        /// </summary>
        public event EventHandler<FileEventRaisedArgs> FileProcessed;

        /// <summary>
        /// If true, then <see cref="MonitoringStarted"/> has been raised and the service is currently monitoring the solution.
        /// If false, then the service is not currently monitoring.
        /// </summary>
        public bool IsMonitoring { get; private set; }

        /// <summary>
        /// If true, then <see cref="UpdateStarted"/> has been raised and the service is currently updating.
        /// If false, then the service is not currently updating.
        /// </summary>
        public bool IsUpdating { get; private set; }

        /// <summary>
        /// Creates a new data service object
        /// </summary>
        /// <param name="serviceProvider">The service provider where this service is sited</param>
        public SrcMLDataService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;

            _workingSetFactories = serviceProvider.GetService(typeof(SWorkingSetRegistrarService)) as IWorkingSetRegistrarService;
            _taskManager = _serviceProvider.GetService(typeof(STaskManagerService)) as ITaskManagerService;
            _srcMLService = _serviceProvider.GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
            
            if(_srcMLService != null) {
                if(_srcMLService.IsMonitoring) {
                    RespondToMonitoringStarted(this, new EventArgs());
                }
                SubscribeToEvents();
            }
        }

        /// <summary>
        /// Raises the file processed event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnFileProcessed(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileProcessed;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the update started event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnUpdateStarted(EventArgs e) {
            IsUpdating = true;
            EventHandler handler = UpdateStarted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the update completed event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnUpdateCompleted(EventArgs e) {
            IsUpdating = false;
            EventHandler handler = UpdateCompleted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the monitoring started event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnMonitoringStarted(EventArgs e) {
            IsMonitoring = true;
            EventHandler handler = MonitoringStarted;
            if(handler != null) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the monitoring stopped event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnMonitoringStopped(EventArgs e) {
            IsMonitoring = false;
            EventHandler handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        private void SubscribeToEvents() {
            _srcMLService.MonitoringStarted += RespondToMonitoringStarted;
            _srcMLService.MonitoringStopped += RespondToMonitoringStopped;
        }

        private void RespondToMonitoringStopped(object sender, EventArgs e) {
            if(null != CurrentWorkingSet) {
                _srcMonitor.StopMonitoring();
                CurrentWorkingSet.StopMonitoring();
            }
            OnMonitoringStopped(e);
        }

        private void RespondToMonitoringStarted(object sender, EventArgs e) {
            CurrentDataArchive = new DataArchive(_srcMLService.CurrentMonitor.MonitorStoragePath, _srcMLService.CurrentSrcMLArchive, Scheduler);
            CurrentDataArchive.Generator.ErrorLog = _srcMLService.CurrentSrcMLArchive.Generator.ErrorLog;
            CurrentDataArchive.Generator.IsLoggingErrors = true;

            _srcMonitor = new ArchiveMonitor<SrcMLArchive>(Scheduler, _srcMLService.CurrentMonitor.MonitorStoragePath, _srcMLService.CurrentSrcMLArchive, CurrentDataArchive);
            CurrentWorkingSet = _workingSetFactories.Default.CreateWorkingSet(_srcMLService.CurrentMonitor.MonitorStoragePath, CurrentDataArchive, TaskFactory);
            
            if(_srcMLService.IsUpdating) {
                _srcMLService.UpdateArchivesCompleted += GenerateDataAfterUpdate;
            } else {
                GenerateDataAfterUpdate(this, e);
            }
            OnMonitoringStarted(e);
        }

        private void RespondToFileProcessed(object sender, FileEventRaisedArgs e) {
            OnFileProcessed(e);
        }

        private void GenerateDataAfterUpdate(object sender, EventArgs e) {
            OnUpdateStarted(e);
            _srcMLService.UpdateArchivesCompleted -= GenerateDataAfterUpdate;
            OnUpdateStarted(new EventArgs());
            _srcMonitor.UpdateArchivesAsync()
                       .ContinueWith((t) => CurrentWorkingSet.InitializeAsync().Wait(),
                                     TaskContinuationOptions.OnlyOnRanToCompletion)
                       .ContinueWith((t) => {
                           OnUpdateCompleted(e);
                           _srcMonitor.StartMonitoring();
                           CurrentWorkingSet.StartMonitoring();
                       }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
