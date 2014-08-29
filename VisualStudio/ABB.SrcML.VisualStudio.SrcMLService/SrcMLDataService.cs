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
using ABB.SrcML.Utilities;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// The default implementation for <see cref="ISrcMLDataService"/>
    /// </summary>
    public class SrcMLDataService : ISrcMLDataService, SSrcMLDataService {
        private bool _isMonitoring;
        private bool _isUpdating;
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
        public bool IsMonitoring {
            get { return _isMonitoring; }
            private set {
                if(_isMonitoring != value) {
                    _isMonitoring = value;
                    if(_isMonitoring) {
                        OnMonitoringStarted(new EventArgs());
                    } else {
                        OnMonitoringStopped(new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// If true, then <see cref="UpdateStarted"/> has been raised and the service is currently updating.
        /// If false, then the service is not currently updating.
        /// </summary>
        public bool IsUpdating {
            get { return _isUpdating; }
            private set {
                if(_isUpdating != value) {
                    _isUpdating = value;
                    if(_isUpdating) {
                        OnUpdateStarted(new EventArgs());
                    } else {
                        OnUpdateCompleted(new EventArgs());
                    }
                }
            }
        }


        /// <summary>
        /// Creates a new data service object
        /// </summary>
        /// <param name="serviceProvider">The service provider where this service is sited</param>
        public SrcMLDataService(IServiceProvider serviceProvider) {
            _isMonitoring = false;
            _isUpdating = false;
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

            try {
                if(null != CurrentWorkingSet) {
                    IsMonitoring = false;
                    CurrentWorkingSet.StopMonitoring();
                    _srcMonitor.StopMonitoring();
                    CurrentWorkingSet.Dispose();
                    _srcMonitor.Dispose();
                    CurrentWorkingSet = null;
                    CurrentDataArchive = null;
                    _srcMonitor = null;
                }
            } catch(Exception ex) {
                SrcMLFileLogger.DefaultLogger.Error(SrcMLExceptionFormatter.CreateMessage(ex, "Exception in SrcMLDataService.RespondToMonitoringStopped"));
            }
            
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
            IsMonitoring = true;
        }

        private void RespondToFileProcessed(object sender, FileEventRaisedArgs e) {
            OnFileProcessed(e);
        }

        private void GenerateDataAfterUpdate(object sender, EventArgs e) {
            _srcMLService.UpdateArchivesCompleted -= GenerateDataAfterUpdate;
            IsUpdating = true;
            _srcMonitor.UpdateArchivesAsync()
                       .ContinueWith((t) => {
                           _srcMonitor.Save();
                           CurrentWorkingSet.InitializeAsync().Wait();
                       }, TaskContinuationOptions.OnlyOnRanToCompletion)
                       .ContinueWith((t) => {
                           IsUpdating = false;
                           _srcMonitor.StartMonitoring();
                           CurrentWorkingSet.StartMonitoring();
                       }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
