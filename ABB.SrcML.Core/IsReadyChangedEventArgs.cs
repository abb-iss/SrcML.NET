using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
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
        /// <param name="updatedReadyState">The updated ready state</param>
        public IsReadyChangedEventArgs(bool updatedReadyState) {
            this.ReadyState = updatedReadyState;
        }
    }
}
