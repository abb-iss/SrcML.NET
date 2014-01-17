using ABB.SrcML.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Test {
    [TestFixture,Category("Build")]
    class IConcurrencyStrategyTests {
        [Test]
        public void TestSingleCoreStrategy() {
            IConcurrencyStrategy strategy = new SingleCoreStrategy();
            Assert.AreEqual(1, strategy.ComputeAvailableCores());
        }

        [Test, ExpectedException(ExpectedException=typeof(ArgumentOutOfRangeException))]
        public void TestZeroCores() {
            IConcurrencyStrategy strategy = new ConservativeAbbCoreStrategy(0);
        }
        [Test]
        public void TestConservativeAbbCoreStrategy() {
            Dictionary<int, int> tests = new Dictionary<int, int>() {
                {1, 1},
                {2, 1},
                {3, 1},
                {4, 2},
                {5, 2},
                {8, 4},
                {9, 4},
                {16, 8},
            };

            foreach(var key in tests.Keys) {
                IConcurrencyStrategy strategy = new ConservativeAbbCoreStrategy(key);
                Assert.AreEqual(tests[key], strategy.ComputeAvailableCores(), String.Format("{0} cores should return {1}", key, tests[key]));
            }
        }
    }
}
