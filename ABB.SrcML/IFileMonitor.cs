/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *    Vinay Augustine (ABB Group) - Renamed API to be more clear
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML {
    /// <summary>
    /// This interface is designed for all events that can be raised from SrcML.NET.
    /// </summary>
    public interface IFileMonitor {
        event EventHandler<FileEventRaisedArgs> FileEventRaised;

        void StartMonitoring();
        void StopMonitoring();
        List<string> GetMonitoredFiles(System.ComponentModel.BackgroundWorker worker);
    }

    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum FileEventType {
        FileAdded,    // Raised when a file is added
        FileChanged,  // Raised when a file is changed
        FileDeleted,  // Raised when a file is deleted
        FileRenamed,  // Raised when a file is renamed
    }

    /// <summary>
    /// Event data of SrcML.NET events.
    /// </summary>
    public class FileEventRaisedArgs : System.EventArgs {
        protected FileEventRaisedArgs() {
        }

        public FileEventRaisedArgs(FileEventType eventType, string pathToFile)
            : this(eventType, pathToFile, pathToFile, false) {
        }

        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, bool hasSrcML)
            : this(eventType, pathToFile, pathToFile, hasSrcML) {
        }

        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, string oldPathToFile)
            : this(eventType, pathToFile, oldPathToFile, false) {
        }

        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, string oldPathToFile, bool hasSrcML) {
            this.EventType = eventType;
            this.FilePath = pathToFile;
            this.OldFilePath = oldPathToFile;
            this.HasSrcML = hasSrcML;
        }

        public FileEventType EventType {
            get;
            set;
        }

        public string OldFilePath {
            get;
            set;
        }

        public string FilePath {
            get;
            set;
        }

        public bool HasSrcML { get; set; }
    }
}
