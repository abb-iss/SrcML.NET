using System;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Event arguments for capturing a thrown exception
    /// </summary>
    public class ErrorRaisedArgs : EventArgs {

        /// <summary>
        /// Constructs a new event args object
        /// </summary>
        /// <param name="exception"></param>
        public ErrorRaisedArgs(Exception exception) {
            this.Exception = exception;
        }

        /// <summary>
        /// The exception that was thrown
        /// </summary>
        public Exception Exception { get; private set; }
    }
}