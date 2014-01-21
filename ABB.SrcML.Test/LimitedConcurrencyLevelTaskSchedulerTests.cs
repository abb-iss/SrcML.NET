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
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(4);
            var factory = new TaskFactory(scheduler);
            AutoResetEvent are = new AutoResetEvent(true);
            var rng = new Random();
            int currentlyExecuting = 0;
            int maxCurrentlyExecuting = 0;
            Action<int> testAction = (int i) => {
                int value = Interlocked.Increment(ref currentlyExecuting);
                Thread.Sleep(i);
                Interlocked.Decrement(ref currentlyExecuting);
            };
            Task[] tasks = new Task[100];
            for(int i = 0; i < 100; i++) {
                tasks[i] = factory.StartNew(() => testAction(rng.Next(100, 500)));
                if(i == 25) {
                    scheduler.Stop();
                }
                if(i == 75) {
                    Assert.AreEqual(0, currentlyExecuting);
                    scheduler.Start();
                }
            }

            Task.WaitAll(tasks, 5000);
        }

        private static void TestConcurrencyLimit(LimitedConcurrencyLevelTaskScheduler scheduler) {
            var factory = new TaskFactory(scheduler);
            var rng = new Random();
            int currentlyExecuting = 0;
            int maxCurrentlyExecuting = 0;
            Action<int> testAction = (int i) => {
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
        }
    }
}
