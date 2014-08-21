using System.Threading.Tasks;
using ABB.SrcML.Data;
using ABB.SrcML.VisualStudio.Interfaces;
using ABB.VisualStudio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.VisualStudio.SrcMLService {
    public class SrcMLDataService : ISrcMLDataService, SSrcMLDataService {
        private IServiceProvider _serviceProvider;
        private ITaskManagerService _taskManager;
        private ISrcMLGlobalService _srcMLService;
        private ArchiveMonitor<SrcMLArchive> _srcMonitor;

        private DataArchive CurrentDataArchive;

        public AbstractWorkingSet CurrentWorkingSet { get; private set; }

        private TaskScheduler Scheduler {
            get {
                return (null == _taskManager ? TaskScheduler.Default : _taskManager.GlobalScheduler);
            }
        }
        
        public event EventHandler MonitoringStarted;
        public event EventHandler MonitoringStopped;

        public event EventHandler UpdateStarted;
        public event EventHandler UpdateCompleted;

        public event EventHandler<FileEventRaisedArgs> FileProcessed;

        public bool IsMonitoring { get; private set; }

        public bool IsUpdating { get; private set; }

        public SrcMLDataService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;

            _taskManager = _serviceProvider.GetService(typeof(STaskManagerService)) as ITaskManagerService;
            _srcMLService = _serviceProvider.GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;

            if(_srcMLService != null) {
                if(_srcMLService.IsMonitoring) {
                    RespondToMonitoringStarted(this, new EventArgs());
                }
                SubscribeToEvents();
            }
        }

        private void SubscribeToEvents() {
            _srcMLService.MonitoringStarted += RespondToMonitoringStarted;
            _srcMLService.MonitoringStopped += RespondToMonitoringStopped;
        }

        void RespondToMonitoringStopped(object sender, EventArgs e) {
            if(null != CurrentWorkingSet) {
                _srcMonitor.StopMonitoring();
                CurrentWorkingSet.StopMonitoring();
            }
            OnMonitoringStopped(e);
        }

        void RespondToMonitoringStarted(object sender, EventArgs e) {
            CurrentDataArchive = new DataArchive(_srcMLService.CurrentMonitor.MonitorStoragePath, _srcMLService.CurrentSrcMLArchive, Scheduler);
            _srcMonitor = new ArchiveMonitor<SrcMLArchive>(Scheduler, _srcMLService.CurrentMonitor.MonitorStoragePath, _srcMLService.CurrentSrcMLArchive, CurrentDataArchive);
            CurrentWorkingSet = new CompleteWorkingSet();
            CurrentWorkingSet.Factory = new TaskFactory(Scheduler);
            CurrentWorkingSet.Archive = CurrentDataArchive;
            if(_srcMLService.IsUpdating) {
                _srcMLService.UpdateArchivesCompleted += GenerateDataAfterUpdate;
            } else {
                GenerateDataAfterUpdate(this, e);
            }
            OnMonitoringStarted(e);
        }

        void RespondToFileProcessed(object sender, FileEventRaisedArgs e) {
            OnFileProcessed(e);
        }

        void GenerateDataAfterUpdate(object sender, EventArgs e) {
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

        protected virtual void OnFileProcessed(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = FileProcessed;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateStarted(EventArgs e) {
            IsUpdating = true;
            EventHandler handler = UpdateStarted;
            if(null != handler) {
                handler(this, e);
            }
        }

        protected virtual void OnUpdateCompleted(EventArgs e) {
            IsUpdating = false;
            EventHandler handler = UpdateCompleted;
            if(null != handler) {
                handler(this, e);
            }
        }
        protected virtual void OnMonitoringStarted(EventArgs e) {
            IsMonitoring = true;
            EventHandler handler = MonitoringStarted;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMonitoringStopped(EventArgs e) {
            IsMonitoring = false;
            EventHandler handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }
    }
}
