using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Utilities {

    // Provides a task scheduler that ensures a maximum concurrency level while
    // running on top of the thread pool.
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler {
        private const int IDLE_DELAY = 250;

        // Indicates whether the current thread is processing work items.
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;

        // The list of tasks to be executed
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

        // The maximum concurrency level allowed by this scheduler.
        private readonly int _maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items.
        private int _delegatesQueuedOrRunning;

        private int DelegatesQueuedOrRunning {
            get { return _delegatesQueuedOrRunning; }
            set {
                Interlocked.Exchange(ref _delegatesQueuedOrRunning, value);
                if(value == 0) {
                    var timer = new Timer((state) => {
                        bool isIdle = false;
                        lock(_tasks) {
                            isIdle = (_tasks.Count == 0);
                        }
                        if(isIdle) {
                            OnSchedulerIdled(new EventArgs());
                        }
                    }, null, IDLE_DELAY, Timeout.Infinite);
                }
            }
        }

        // true if the scheduler is "started" (true by default)
        // false if Stop() has been called
        private bool _isStarted;

        // Creates a new instance with the specified degree of parallelism.
        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
            : this(maxDegreeOfParallelism, true) { }

        public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism, bool isStarted) {
            if(maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _isStarted = isStarted;
            _delegatesQueuedOrRunning = 0;
        }

        public event EventHandler SchedulerIdled;

        /// <summary>
        /// Causes the scheduler to start executing work items. By default the
        /// scheduler is "started" when it is constructed.
        /// </summary>
        public void Start() {
            if(!_isStarted) {
                _isStarted = true;
                if(DelegatesQueuedOrRunning < _maxDegreeOfParallelism) {
                    ++DelegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        /// <summary>
        /// Causes the scheduler to stop executing work items. Calling <see cref="Start()"/>
        /// will cause execution to resume. This does not cancel any tasks. Instead, it just
        /// prevents new ones from executing.
        /// </summary>
        public void Stop() {
            _isStarted = false;
        }

        // Queues a task to the scheduler.
        protected sealed override void QueueTask(Task task) {
            // Add the task to the list of tasks to be processed.  If there aren't enough
            // delegates currently queued or running to process tasks, schedule another.
            lock(_tasks) {
                _tasks.AddLast(task);
                if(DelegatesQueuedOrRunning < _maxDegreeOfParallelism && _isStarted) {
                    ++DelegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }
        protected virtual void OnSchedulerIdled(EventArgs e) {
            EventHandler handler = SchedulerIdled;
            if(null != handler) {
                handler(this, e);
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler.
        private void NotifyThreadPoolOfPendingWork() {
            ThreadPool.UnsafeQueueUserWorkItem(_ => {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try {
                    // Process all available items in the queue.
                    while(true) {
                        Task item;
                        lock(_tasks) {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if(_tasks.Count == 0 || !_isStarted) {
                                --DelegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        base.TryExecuteTask(item);
                    }
                } finally {
                    // We're done processing items on the current thread
                    _currentThreadIsProcessingItems = false;
                }
            }, null);
        }

        // Attempts to execute the specified task on the current thread.
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            // If this thread isn't already processing a task, we don't support inlining
            if(!_currentThreadIsProcessingItems)
                return false;

            // If the task was previously queued, remove it from the queue
            if(taskWasPreviouslyQueued)
                // Try to run the task.
                if(TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

        // Attempt to remove a previously scheduled task from the scheduler.
        protected sealed override bool TryDequeue(Task task) {
            lock(_tasks)
                return _tasks.Remove(task);
        }

        // Gets the maximum concurrency level supported by this scheduler.
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

        // Gets an enumerable of the tasks currently scheduled on this scheduler.
        protected sealed override IEnumerable<Task> GetScheduledTasks() {
            bool lockTaken = false;
            try {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if(lockTaken)
                    return _tasks;
                else
                    throw new NotSupportedException();
            } finally {
                if(lockTaken)
                    Monitor.Exit(_tasks);
            }
        }
    }
}