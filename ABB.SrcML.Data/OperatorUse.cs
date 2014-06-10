using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class OperatorUse : Expression {

        public string Text { get; set; }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            return Text;
        }
    }
}
