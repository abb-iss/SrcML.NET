using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    public class SingleCoreStrategy : IConcurrencyStrategy {
        public int ComputeAvailableCores() {
            return 1;
        }
    }
}
