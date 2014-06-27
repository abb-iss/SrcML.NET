using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// Implements the single-core concurrency strategy
    /// </summary>
    public class SingleCoreStrategy : IConcurrencyStrategy {
        /// <summary>
        /// The number of available cores is always 1
        /// </summary>
        /// <returns>1</returns>
        public int ComputeAvailableCores() {
            return 1;
        }
    }
}
