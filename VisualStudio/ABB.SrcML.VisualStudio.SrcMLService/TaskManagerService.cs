using ABB.SrcML;
using ABB.SrcML.Utilities;
using ABB.VisualStudio.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.VisualStudio {
    public class TaskManagerService : ITaskManagerService, STaskManagerService {
        private IServiceProvider _serviceProvider;
        private LimitedConcurrencyLevelTaskScheduler _scheduler;

        public TaskManagerService(IServiceProvider serviceProvider, IConcurrencyStrategy concurrencyStrategy) {
            this._serviceProvider = serviceProvider;
            this._scheduler = new LimitedConcurrencyLevelTaskScheduler(concurrencyStrategy.ComputeAvailableCores());
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
    }
}
