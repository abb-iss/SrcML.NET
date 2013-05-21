using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class ParseErrorRaisedArgs : EventArgs {
        public ParseException Exception { get; private set; }

        public ParseErrorRaisedArgs(ParseException exception) {
            this.Exception = exception;
        }
    }
}
