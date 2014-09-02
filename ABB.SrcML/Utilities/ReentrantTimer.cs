/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ABB.SrcML.Utilities {

    /// <summary>
    /// <para>The reentrant timer provides a reentrant aware timer similar to the example provided in <see cref="Timer.Stop()"/>.
    /// It provides an interface very similar to Timer. It is not, however, a direct sub-class.</para>
    /// <para>It operates in a similar fashion to the Timer class. If you subscribe to the <see cref="Elapsed"/> method, your event handler will
    /// execute every <see cref="Interval"/>. It differs from <see cref="Timer"/> in that the event handlers will only execute if they are not already
    /// executing.</para>
    /// <para>ReentrantTimer provides an additional method: <see cref="ExecuteWhenIdle(Action)"/>. This method will wait for the timer to become idle
    /// before trying to execute the specified action.</para>
    /// </summary>
    public class ReentrantTimer : IDisposable {
        private const int RUNNING = 1;
        private const int IDLE = 0;

        private int syncPoint;

        private Timer _timer;
        private readonly Action _timerAction;
        private TaskScheduler _scheduler;

        /// <summary>
        /// A client can subscribe to this event in order to execute in <see cref="Interval"/>. If <see cref="AutoReset"/> is set to false, then this event
        /// will be raised only once. Elapsed will not execute if either of the following are currently executing:
        /// <list type="ordered">
        ///     <item>A previously raised elapsed event</item>
        ///     <item>A call to <see cref="ExecuteWhenIdle(Action)"/></item>
        /// </list>
        /// </summary>
        public event ElapsedEventHandler Elapsed;

        public ReentrantTimer(double interval, Action action, TaskScheduler scheduler) {
            this._timer = new Timer(interval);
            this._timerAction = action;
            this._scheduler = scheduler;
            this._timer.Elapsed += _timer_Elapsed;
        }

        /// <summary>
        /// Create a reentrant timer with an <see cref="Interval"/> of 100ms.
        /// </summary>
        public ReentrantTimer(Action action, TaskScheduler scheduler)
            : this(100, action, scheduler) { }

        public ReentrantTimer(Action action)
            : this(100, action, TaskScheduler.Default) { }

        /// <summary>
        /// If auto reset is set to true, the timer will automatically reset. If false, it will only trigger false.
        /// </summary>
        public bool AutoReset {
            get { return this._timer.AutoReset; }
            set { this._timer.AutoReset = value; }
        }

        /// <summary>
        /// Setting enabled to true causes the timer to start. Setting it to false causes it to stop.
        /// </summary>
        public bool Enabled {
            get { return this._timer.Enabled; }
            set { this._timer.Enabled = value; }
        }

        /// <summary>
        /// The interval at which the timer is triggered in milliseconds.
        /// </summary>
        public double Interval {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        /// <summary>
        /// Waits for the timer to be idle (i.e. not executing anything) and then executes <paramref name="action"/>
        /// </summary>
        /// <param name="action">The action to execute</param>
        public Task ExecuteWhenIdle(Action action) {
            var task = new Task(() => {
                while(RUNNING == Interlocked.CompareExchange(ref syncPoint, RUNNING, IDLE)) {
                    Thread.Sleep(1);
                }
                action();
            });
            SetSyncToIdleOnCompletion(task);
            task.Start(_scheduler);
            return task;
        }

        /// <summary>
        /// This is identical to setting <see cref="Enabled"/> to true.
        /// </summary>
        public void Start() {
            _timer.Start();
        }

        /// <summary>
        /// This is identical to setting <see cref="Enabled"/> to false.
        /// </summary>
        public void Stop() {
            _timer.Stop();
        }

        /// <summary>
        /// Disposes of this object
        /// </summary>
        public void Dispose() {
            _timer.Dispose();
        }

        /// <summary>
        /// Executed when <see cref="Elapsed"/> is triggered
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected void OnElapsed(ElapsedEventArgs e) {
            ElapsedEventHandler handler = Elapsed;
            if(handler != null) {
                handler(this, e);
            }
        }

        private void SetSyncToIdleOnCompletion(Task task) {
            task.ContinueWith(t => syncPoint = IDLE);
        }
        
        private void _timer_Elapsed(object sender, ElapsedEventArgs e) {
            int sync = Interlocked.CompareExchange(ref syncPoint, RUNNING, IDLE);
            
            if(IDLE == sync) {
                var task = new Task(_timerAction);
                SetSyncToIdleOnCompletion(task);
                task.Start(this._scheduler);
            }
        }
    }
}