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

using ABB.SrcML.Utilities;
using ABB.VisualStudio;
using System;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio {

    /// <summary>
    /// Abstract class for monitoring services. This contains common functionality and operations for making monitors within Visual Studio.
    /// </summary>
    public abstract class AbstractMonitoringService {
        private bool _isMonitoring;
        private bool _isUpdating;

        /// <summary>
        /// Default interval for the save timer in milliseconds
        /// </summary>
        public const int DEFAULT_SAVE_INTERVAL = 300;

        private AbstractMonitoringService() {
        }

        /// <summary>
        /// Create a new abstract monitoring service
        /// </summary>
        /// <param name="serviceProvider">The container where this service will be sited</param>
        /// <param name="taskManagerService">The task manager service for executing tasks</param>
        protected AbstractMonitoringService(SrcMLServicePackage serviceProvider, ITaskManagerService taskManagerService) {
            ServiceProvider = serviceProvider;
            SaveTimer = new ReentrantTimer(Save, GlobalScheduler);
            SaveInterval = DEFAULT_SAVE_INTERVAL;
            _isMonitoring = false;
            _isUpdating = false;
            if (taskManagerService != null)
            {
                this.TaskManager = taskManagerService;                
            }
        }

        /// <summary>
        /// The monitoring started event
        /// </summary>
        public event EventHandler MonitoringStarted;

        /// <summary>
        /// The monitoring stopped event
        /// </summary>
        public event EventHandler MonitoringStopped;

        /// <summary>
        /// The update started event
        /// </summary>
        public event EventHandler UpdateStarted;

        /// <summary>
        /// The update completed event
        /// </summary>
        public event EventHandler UpdateCompleted;

        /// <summary>
        /// True if this monitor is currently monitoring; false if not
        /// </summary>
        public bool IsMonitoring {
            get { return _isMonitoring; }
            protected set {
                if(_isMonitoring != value) {
                    _isMonitoring = value;
                    (_isMonitoring ? (Action<EventArgs>) OnMonitoringStarted : OnMonitoringStopped)(new EventArgs());
                }
            }
        }

        /// <summary>
        /// True if this monitor is currently updating; false if not
        /// </summary>
        public bool IsUpdating {
            get { return _isUpdating; }
            protected set {
                if(_isUpdating != value) {
                    _isUpdating = value;
                    (_isUpdating ? (Action<EventArgs>) OnUpdateStarted : OnUpdateCompleted)(new EventArgs());
                }
            }
        }

        /// <summary>
        /// The interval at which to save state in seconds.
        /// </summary>
        public double SaveInterval {
            get { return SaveTimer.Interval / 1000; }
            set { SaveTimer.Interval = value * 1000; }
        }

        /// <summary>
        /// The timer for periodically saving state
        /// </summary>
        public ReentrantTimer SaveTimer { get; private set; }

        /// <summary>
        /// Starts monitoring
        /// </summary>
        public virtual void StartMonitoring() {
            GlobalTaskFactory.StartNew(Setup)
                .ContinueWith((t) => Update())
                .ContinueWith((t) => Save()).ContinueWith((t) => {
                    IsMonitoring = true;
                    SaveTimer.Start();
                    StartMonitoringImpl();
                });
        }

        /// <summary>
        /// Stops monitoring
        /// </summary>
        public virtual void StopMonitoring() {
            IsMonitoring = false;
            SaveTimer.Stop();
            StopMonitoringImpl();
        }

        /// <summary>
        /// The container for this service
        /// </summary>
        protected SrcMLServicePackage ServiceProvider { get; private set; }

        /// <summary>
        /// The task manager
        /// </summary>
        protected ITaskManagerService TaskManager { get; private set; }

        /// <summary>
        /// The task factory for executing tasks
        /// </summary>
        protected TaskFactory GlobalTaskFactory { get { return (null != TaskManager ? TaskManager.GlobalFactory : Task.Factory); } }

        /// <summary>
        /// The task scheduler for scheduling tasks
        /// </summary>
        protected TaskScheduler GlobalScheduler { get { return (null != TaskManager ? TaskManager.GlobalScheduler : TaskScheduler.Default); } }

        /// <summary>
        /// Executed when <see cref="MonitoringStarted"/> is raised
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnMonitoringStarted(EventArgs e) {
            EventHandler handler = MonitoringStarted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Executed when <see cref="MonitoringStopped"/> is raised
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler handler = MonitoringStopped;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Executed when <see cref="UpdateCompleted"/> is raised
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnUpdateCompleted(EventArgs e) {
            EventHandler handler = UpdateCompleted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Executed when <see cref="UpdateStarted"/> is raised
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnUpdateStarted(EventArgs e) {
            EventHandler handler = UpdateStarted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Saves the monitor state
        /// </summary>
        protected abstract void Save();

        /// <summary>
        /// Sets up the monitor prior to monitoring
        /// </summary>
        protected abstract void Setup();

        /// <summary>
        /// Updates the monitor
        /// </summary>
        public void Update() {
            try {
                IsUpdating = true;
                UpdateImpl();
            } finally {
                IsUpdating = false;
            }
        }

        /// <summary>
        /// Implementation method for <see cref="Update"/>
        /// </summary>
        protected abstract void UpdateImpl();

        /// <summary>
        /// Implementation method for <see cref="StartMonitoring"/>
        /// </summary>
        protected abstract void StartMonitoringImpl();

        /// <summary>
        /// Implementation method for <see cref="StopMonitoring"/>
        /// </summary>
        protected abstract void StopMonitoringImpl();
    }
}