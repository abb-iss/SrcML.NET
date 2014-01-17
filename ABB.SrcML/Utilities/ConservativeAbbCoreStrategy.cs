using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    public class ConservativeAbbCoreStrategy : IConcurrencyStrategy {
        private int numberOfCores;

        public ConservativeAbbCoreStrategy() : this(Environment.ProcessorCount) { }

        public ConservativeAbbCoreStrategy(int processorCount) {
            if(processorCount < 1)
                throw new ArgumentOutOfRangeException("processorCount", processorCount, "processorCount should be greater than zero");

            numberOfCores = processorCount;
        }
        public int ComputeAvailableCores() {
            if(numberOfCores == 4 || numberOfCores >= 8)
                return numberOfCores / 2;
            if(numberOfCores > 4 && numberOfCores < 8)
                return 2;
            return 1;
        }
    }
}
