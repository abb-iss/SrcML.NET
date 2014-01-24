using ABB.SrcML.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Test {
    [TestFixture, Category("Build")]
    class LimitedConcurrencyLevelTaskSchedulerTests {

        [Test, ExpectedException(ExpectedException=typeof(ArgumentOutOfRangeException))]
        public void TestZeroTaskExecution() {
            TestConcurrencyLimit(new LimitedConcurrencyLevelTaskScheduler(0));
        }
        [Test]
        public void TestSingleTaskExecution() {
            TestConcurrencyLimit(new LimitedConcurrencyLevelTaskScheduler(1));
        }

        [Test]
        public void TestDoubleTaskExecution() {
            TestConcurrencyLimit(new LimitedConcurrencyLevelTaskScheduler(2));
        }

        [Test]
        public void TestStartAndStop() {
            int NUM_TASKS = 10;
            int STOP_NUM = 3;
            int START_NUM = 6;
            int TIMEOUT = 60000;

            var scheduler = new LimitedConcurrencyLevelTaskScheduler(4);
            var factory = new TaskFactory(scheduler);
            var rng = new Random();
            int currentlyExecuting = 0;

            Action<int> testAction = (int i) => {
                int value = Interlocked.Increment(ref currentlyExecuting);
                Thread.Sleep(i);
                Interlocked.Decrement(ref currentlyExecuting);
            };

            Task[] tasks = new Task[NUM_TASKS];
            for(int i = 0; i < NUM_TASKS; i++) {
                tasks[i] = factory.StartNew(() => testAction(rng.Next(100, 500)));
                if(i == STOP_NUM) {
                    scheduler.Stop();
                    for(int j = 0; j < NUM_TASKS; j++) {
                        if(0 == currentlyExecuting) {
                            Console.WriteLine("Slept for {0} ms", j * 500);
                            break;
                        }
                        Thread.Sleep(500);
                    }
                }
                if(i >= STOP_NUM && i <= START_NUM) {
                    Assert.AreEqual(0, currentlyExecuting);
                }
                if(i == START_NUM) {
                    scheduler.Start();
                }
            }
            Task.WaitAll(tasks);
            Assert.AreEqual(0, currentlyExecuting);
        }

        private static void TestConcurrencyLimit(LimitedConcurrencyLevelTaskScheduler scheduler) {
            var factory = new TaskFactory(scheduler);
            bool IsIdled = true;

            AutoResetEvent are = new AutoResetEvent(false);
            scheduler.SchedulerIdled += (o,e) => {
                IsIdled = true;
                are.Set();
            };

            var rng = new Random();
            int currentlyExecuting = 0;
            int maxCurrentlyExecuting = 0;
            Action<int> testAction = (int i) => {
                IsIdled = false;
                int value = Interlocked.Increment(ref currentlyExecuting);
                Assert.LessOrEqual(value, scheduler.MaximumConcurrencyLevel);
                if(value > maxCurrentlyExecuting) {
                    Interlocked.Exchange(ref maxCurrentlyExecuting, value);
                }
                
                Thread.Sleep(i);
                Interlocked.Decrement(ref currentlyExecuting);
            };

            Task[] tasks = new Task[100];
            for(int i = 0; i < 100; i++) {
                tasks[i] = factory.StartNew(() => testAction(rng.Next(100, 500)));
            }
            Task.WaitAll(tasks);
            Assert.AreEqual(0, currentlyExecuting);
            Assert.AreEqual(scheduler.MaximumConcurrencyLevel, maxCurrentlyExecuting);
            Assert.IsTrue(are.WaitOne(500));
            Assert.IsTrue(IsIdled);
        }
    }
}
