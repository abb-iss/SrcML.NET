using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public class IsReadyChangedEventArgs : EventArgs {
        public bool UpdatedReadyState { get; private set; }

        protected IsReadyChangedEventArgs() { }

        public IsReadyChangedEventArgs(bool updatedReadyState) {
            this.UpdatedReadyState = updatedReadyState;
        }
    }
}
