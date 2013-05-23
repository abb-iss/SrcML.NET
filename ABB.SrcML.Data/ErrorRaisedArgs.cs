using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Event arguments for capturing a thrown exception
    /// </summary>
    public class ErrorRaisedArgs : EventArgs {
        /// <summary>
        /// The exception that was thrown
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Constructs a new event args object
        /// </summary>
        /// <param name="exception"></param>
        public ErrorRaisedArgs(Exception exception) {
            this.Exception = exception;
        }
    }
}
