using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Utilities {
    public class TaskManager : IDisposable {
        private bool _isReady;
        private int _runningTasks;
        private Object _parent;

        /// <summary>
        /// Event fires when the <see cref="IsReady"/> property changes
        /// </summary>
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        public TaskManager(Object parent) : this(parent, TaskScheduler.Default) { }

        public TaskManager(Object parent, TaskScheduler scheduler) {
            _parent = parent;
            Scheduler = scheduler;
            _runningTasks = 0;
            _isReady = true;
        }

        private TaskManager() { }

        /// <summary>
        /// Archives are "ready" when they have no running tasks. This property automatically changes to false
        /// when the number of running tasks is zero. Whenever the value changes, the <see cref="IsReadyChanged"/> event fires.
        /// </summary>
        public bool IsReady {
            get { return this._isReady; }
            protected set {
                if(value != this._isReady) {
                    _isReady = value;
                    OnIsReadyChanged(new IsReadyChangedEventArgs(_isReady));
                }
            }
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
        /// event handler for <see cref="IsReadyChanged"/>
        /// </summary>
        /// <param name="e">event arguments</param>
        protected virtual void OnIsReadyChanged(IsReadyChangedEventArgs e) {
            EventHandler<IsReadyChangedEventArgs> handler = IsReadyChanged;
            if(handler != null) {
                handler(_parent, e);
            }
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
            IsReadyChanged = null;
        }
    }
}
