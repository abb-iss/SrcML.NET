using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class CaseStatement : ConditionBlockStatement {
        public CaseStatement() : base() {
            //TODO: implement constructor
        }

        public bool IsDefault { get; set; }
    }
}
