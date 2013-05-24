using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// Event arguments for IsReady changed events
    /// </summary>
    public class IsReadyChangedEventArgs : EventArgs {
        /// <summary>
        /// The updated ready state
        /// </summary>
        public bool ReadyState { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected IsReadyChangedEventArgs() { }

        /// <summary>
        /// Constructs a new object
        /// </summary>
        /// <param name="readyState">The updated ready state</param>
        public IsReadyChangedEventArgs(bool readyState) {
            this.ReadyState = readyState;
        }
    }
}
