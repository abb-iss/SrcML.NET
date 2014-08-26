using ABB.SrcML;
using ABB.SrcML.Utilities;
using ABB.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.VisualStudio {
    public class TaskManagerService : ITaskManagerService, STaskManagerService {
        private IServiceProvider _serviceProvider;
        private LimitedConcurrencyLevelTaskScheduler _scheduler;
        private TaskFactory _factory;
        private EnvDTE.DTE _dteService;
        private EnvDTE.BuildEvents _buildEvents;
        private EnvDTE.WindowEvents _windowEvents;

        public TaskManagerService(IServiceProvider serviceProvider, IConcurrencyStrategy concurrencyStrategy) {
            this._serviceProvider = serviceProvider;
            this._scheduler = new LimitedConcurrencyLevelTaskScheduler(concurrencyStrategy.ComputeAvailableCores());
            this._factory = new TaskFactory(_scheduler);
            SubscribeToEvents();
        }

        public event EventHandler SchedulerIdled {
            add { this._scheduler.SchedulerIdled += value; }
            remove { this._scheduler.SchedulerIdled -= value; }
        }

        private void SubscribeToEvents() {
            _dteService = _serviceProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if(null != _dteService) {
                _buildEvents = _dteService.Events.BuildEvents;
                _windowEvents = _dteService.Events.WindowEvents;

                if(null != _buildEvents) {
                    _buildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;
                    _buildEvents.OnBuildDone += _buildEvents_OnBuildDone;
                }

                if(null != _windowEvents) {
                    _windowEvents.WindowActivated += _windowEvents_WindowActivated;
                }
            }
        }

        private bool IsInDebugLayout() {
            try {
                return _dteService.WindowConfigurations.ActiveConfigurationName.Contains("Debug");
            } catch(COMException) {
                return false;
            }
        }
        void _windowEvents_WindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus) {
            if(IsInDebugLayout()) {
                _scheduler.Stop();
            } else {
                _scheduler.Start();
            }
        }

        void _buildEvents_OnBuildBegin(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action) {
            _scheduler.Stop();
        }

        void _buildEvents_OnBuildDone(EnvDTE.vsBuildScope Scope, EnvDTE.vsBuildAction Action) {
            _scheduler.Start();
        }

        private void UnsubscribeFromEvents() {
            if(null != _buildEvents) {
                _buildEvents.OnBuildBegin -= _buildEvents_OnBuildBegin;
                _buildEvents.OnBuildDone -= _buildEvents_OnBuildDone;
            }
            if(null != _windowEvents) {
                _windowEvents.WindowActivated -= _windowEvents_WindowActivated;
            }
        }

        public void Start() {
            if(_scheduler != null) {
                _scheduler.Start();
            }
        }

        public void Stop() {
            if(_scheduler != null) {
                _scheduler.Stop();
            }
        }

        public TaskScheduler GlobalScheduler { get { return this._scheduler; } }

        public TaskFactory GlobalFactory { get { return this._factory; } }
    }
}
