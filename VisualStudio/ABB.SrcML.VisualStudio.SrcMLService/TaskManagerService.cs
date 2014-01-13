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

        public TaskManagerService(IServiceProvider serviceProvider, IConcurrencyStrategy concurrencyStrategy) {
            this._serviceProvider = serviceProvider;
            this.GlobalScheduler = new LimitedConcurrencyLevelTaskScheduler(concurrencyStrategy.ComputeAvailableCores());
        }

        public TaskScheduler GlobalScheduler { get; private set; }
    }
}
