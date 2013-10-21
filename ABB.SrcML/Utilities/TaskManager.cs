using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Utilities {
    public class TaskManager : IDisposable {
        private int _runningTasks;
        private Object _parent;
        private ReadyNotifier ReadyState;

        /// <summary>
        /// Event fires when the <see cref="IsReady"/> property changes
        /// </summary>
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged {
            add { this.ReadyState.IsReadyChanged += value; }
            remove { this.ReadyState.IsReadyChanged -= value; }
        }

        public TaskManager(Object parent) : this(parent, new LimitedConcurrencyLevelTaskScheduler(1)) { }

        public TaskManager(Object parent, TaskScheduler scheduler) {
            _parent = parent;
            //TODO - HARDCODING SCHEDULER
            Scheduler = new LimitedConcurrencyLevelTaskScheduler(1);
            _runningTasks = 0;
            ReadyState = new ReadyNotifier(_parent);
        }

        private TaskManager() { }

        /// <summary>
        /// Archives are "ready" when they have no running tasks. This property automatically changes to false
        /// when the number of running tasks is zero. Whenever the value changes, the <see cref="IsReadyChanged"/> event fires.
        /// </summary>
        public bool IsReady {
            get { return this.ReadyState.IsReady; }
            private set { this.ReadyState.IsReady = value; }
        }

        /// <summary>
        /// Task scheduler for the asynchronous methods
        /// </summary>
        public TaskScheduler Scheduler { get; set; }

        /// <summary>
        /// Runs the specified action on this thread. The action will be run with the following continuations: <see cref="DecrementOnCompletion"/> and <see cref="LogExceptions"/>
        /// </summary>
        /// <param name="task">The task to run.</param>
        public void Run(Task task) {
            IncrementTask();
            DecrementOnCompletion(task);
            task.RunSynchronously();
        }

        /// <summary>
        /// Runs the specified action on <see cref="TaskFactory"/>. The action will be run with the following continuations: <see cref="DecrementOnCompletion"/> and <see cref="LogExceptions"/> 
        /// </summary>
        /// <param name="task"></param>
        public void RunAsync(Task task) {
            IncrementTask();
            DecrementOnCompletion(task);
            task.Start(this.Scheduler);
        }

        /// <summary>
        /// Convenience function for adding a continuation that will call <see cref="DecrementTask"/> upon task completion.
        /// </summary>
        /// <param name="task"></param>
        private void DecrementOnCompletion(Task task) {
            task.ContinueWith(t => DecrementTask());
        }

        /// <summary>
        /// The number of tasks that are currently running
        /// </summary>
        protected int CountOfRunningTasks {
            get { return _runningTasks; }
        }

        /// <summary>
        /// Increments <see cref="CountOfRunningTasks"/> (called via the <see cref="Run"/> method). If <see cref="IsReady"/> is true, this sets it to false.
        /// </summary>
        private void IncrementTask() {
            Interlocked.Increment(ref _runningTasks);
            if(IsReady) {
                IsReady = false;
            }
        }

        /// <summary>
        /// Decrements <see cref="CountOfRunningTasks"/> (called via the <see cref="DecrementOnCompletion"/> which is used in the <see cref="Run"/> method.
        /// If the number of tasks becomes zero, then <see cref="IsReady"/> is set to true.
        /// </summary>
        private void DecrementTask() {
            Interlocked.Decrement(ref _runningTasks);
            if(_runningTasks <= 0) {
                IsReady = true;
            }
        }

        public void Dispose() {
            ReadyState.Dispose();
        }

        // Provides a task scheduler that ensures a maximum concurrency level while  
        // running on top of the thread pool. 
        public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
        {
            // Indicates whether the current thread is processing work items.
            [ThreadStatic]
            private static bool _currentThreadIsProcessingItems;

            // The list of tasks to be executed  
            private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks) 

            // The maximum concurrency level allowed by this scheduler.  
            private readonly int _maxDegreeOfParallelism;

            // Indicates whether the scheduler is currently processing work items.  
            private int _delegatesQueuedOrRunning = 0;

            // Creates a new instance with the specified degree of parallelism.  
            public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
            {
                if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
                _maxDegreeOfParallelism = maxDegreeOfParallelism;
            }

            // Queues a task to the scheduler.  
            protected sealed override void QueueTask(Task task)
            {
                // Add the task to the list of tasks to be processed.  If there aren't enough  
                // delegates currently queued or running to process tasks, schedule another.  
                lock (_tasks)
                {
                    _tasks.AddLast(task);
                    if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
                    {
                        ++_delegatesQueuedOrRunning;
                        NotifyThreadPoolOfPendingWork();
                    }
                }
            }

            // Inform the ThreadPool that there's work to be executed for this scheduler.  
            private void NotifyThreadPoolOfPendingWork()
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    // Note that the current thread is now processing work items. 
                    // This is necessary to enable inlining of tasks into this thread.
                    _currentThreadIsProcessingItems = true;
                    try
                    {
                        // Process all available items in the queue. 
                        while (true)
                        {
                            Task item;
                            lock (_tasks)
                            {
                                // When there are no more items to be processed, 
                                // note that we're done processing, and get out. 
                                if (_tasks.Count == 0)
                                {
                                    --_delegatesQueuedOrRunning;
                                    break;
                                }

                                // Get the next item from the queue
                                item = _tasks.First.Value;
                                _tasks.RemoveFirst();
                            }

                            // Execute the task we pulled out of the queue 
                            base.TryExecuteTask(item);
                        }
                    }
                    // We're done processing items on the current thread 
                    finally { _currentThreadIsProcessingItems = false; }
                }, null);
            }

            // Attempts to execute the specified task on the current thread.  
            protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                // If this thread isn't already processing a task, we don't support inlining 
                if (!_currentThreadIsProcessingItems) return false;

                // If the task was previously queued, remove it from the queue 
                if (taskWasPreviouslyQueued)
                    // Try to run the task.  
                    if (TryDequeue(task))
                        return base.TryExecuteTask(task);
                    else
                        return false;
                else
                    return base.TryExecuteTask(task);
            }

            // Attempt to remove a previously scheduled task from the scheduler.  
            protected sealed override bool TryDequeue(Task task)
            {
                lock (_tasks) return _tasks.Remove(task);
            }

            // Gets the maximum concurrency level supported by this scheduler.  
            public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

            // Gets an enumerable of the tasks currently scheduled on this scheduler.  
            protected sealed override IEnumerable<Task> GetScheduledTasks()
            {
                bool lockTaken = false;
                try
                {
                    Monitor.TryEnter(_tasks, ref lockTaken);
                    if (lockTaken) return _tasks;
                    else throw new NotSupportedException();
                }
                finally
                {
                    if (lockTaken) Monitor.Exit(_tasks);
                }
            }
        }

    }
}
