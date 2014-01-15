using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    public class ConservativeAbbCoreStrategy : IConcurrencyStrategy {
        public int ComputeAvailableCores() {
            int numberOfCores = Environment.ProcessorCount;

            if(numberOfCores == 4 || numberOfCores >= 8)
                return numberOfCores / 2;
            return 1;
        }
    }
}
