using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// The ready-state notifier contains a boolean property and an event that fires whenever the state changes
    /// </summary>
    public class ReadyNotifier : IDisposable {
        bool _ready;

        /// <summary>
        /// Constructs the ReadyNotifier object
        /// </summary>
        /// <param name="parent"></param>
        public ReadyNotifier(object parent) {
            _ready = true;
            this.Parent = parent;
        }

        /// <summary>
        /// True if the state is "ready". Changing this value causes <see cref="IsReadyChanged"/> to fire
        /// </summary>
        public bool IsReady {
            get { return _ready; }
            set {
                bool stateChanged = (_ready != value);
                if(stateChanged) {
                    _ready = value;
                    OnIsReadyChanged(new IsReadyChangedEventArgs(_ready));
                }
            }
        }

        /// <summary>
        /// The parent object that should be used as the "sender" for <see cref="IsReadyChanged"/>
        /// </summary>
        public object Parent { get; private set; }

        /// <summary>
        /// This event fires whenever the value of <see cref="IsReady"/> changes
        /// </summary>
        public event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        /// <summary>
        /// Disposes of this notifier object
        /// </summary>
        public void Dispose() {
            IsReadyChanged = null;
        }

        private void OnIsReadyChanged(IsReadyChangedEventArgs e) {
            EventHandler<IsReadyChangedEventArgs> handler = IsReadyChanged;
            if(handler != null) {
                handler(this.Parent, e);
            }
        }
    }
}
