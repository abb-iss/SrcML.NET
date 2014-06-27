using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// Computes the nubmer of cores available. This is a "conservative" strategy that is meant leave CPU resources free for other processes
    /// </summary>
    public class ConservativeAbbCoreStrategy : IConcurrencyStrategy {
        private int numberOfCores;

        /// <summary>
        /// Creates a new conservative core strategy based on <see cref="Environment.ProcessorCount"/>
        /// </summary>
        public ConservativeAbbCoreStrategy() : this(Environment.ProcessorCount) { }

        /// <summary>
        /// Creates a new conservative core strategy based on <paramref name="processorCount"/>
        /// </summary>
        /// <param name="processorCount"></param>
        public ConservativeAbbCoreStrategy(int processorCount) {
            if(processorCount < 1)
                throw new ArgumentOutOfRangeException("processorCount", processorCount, "processorCount should be greater than zero");

            numberOfCores = processorCount;
        }
        /// <summary>
        /// If the number of cores is 4 or greater than or equal to 8, then half that value is returned as available.
        /// If the number of cores is greater than 4 and less than 8, then 2 is returned.
        /// Otherwise, 1 is returned.
        /// </summary>
        /// <returns>The number of available cores as described above</returns>
        public int ComputeAvailableCores() {
            if(numberOfCores == 4 || numberOfCores >= 8)
                return numberOfCores / 2;
            if(numberOfCores > 4 && numberOfCores < 8)
                return 2;
            return 1;
        }
    }
}
