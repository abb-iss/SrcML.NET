using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public class DirectoryScanningMonitorEventArgs : EventArgs {
        public DirectoryScanningMonitorEventArgs(string directory){
            Directory = directory;
        }

        public string Directory { get; private set; }
    }
}
