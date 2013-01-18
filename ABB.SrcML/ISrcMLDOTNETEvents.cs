/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// This interface is designed for all events that can be raised from SrcML.NET.
    /// </summary>
    public interface ISrcMLDOTNETEvents
    {
        event EventHandler<SrcMLDOTNETEventArgs> SrcMLDOTNETEventRaised;

        string FullFolderPath
        {
            get;
            set;
        }

        void StartWatching();
        void StopWatching();
    }

    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum SrcMLDOTNETEventType
    {
        SourceFileAdded,    // Raised when a source file is added into the solution in Visual Studio
        SourceFileChanged,  // Raised when a source file is changed and saved in the solution in Visual Studio
        SourceFileDeleted,  // Raised when a source file is deleted from the solution in Visual Studio
        SourceFileRenamed,  // Raised when a source file is renamed in the solution in Visual Studio
        MonitoringStopped,  // Raised when the solution is about to be closed in Visual Studio
        StartupCompleted    // Raised when the starup process for the solution is completed in Visual Studio
    }

    /// <summary>
    /// Event data of SrcML.NET events.
    /// </summary>
    public class SrcMLDOTNETEventArgs : System.EventArgs
    {
        protected SrcMLDOTNETEventArgs()
        {
        }

        public SrcMLDOTNETEventArgs(string SourcePath, SrcMLDOTNETEventType eventType)
            : this(SourcePath, SourcePath, null, eventType)
        {
        }

        public SrcMLDOTNETEventArgs(string SourcePath, XElement xelement, SrcMLDOTNETEventType eventType)
            : this(SourcePath, SourcePath, xelement, eventType)
        {
        }

        public SrcMLDOTNETEventArgs(string SourcePath, string oldSourcePath, SrcMLDOTNETEventType eventType)
            : this(SourcePath, oldSourcePath, null, null, eventType)
        {
        }

        public SrcMLDOTNETEventArgs(string SourcePath, string oldSourcePath, XElement xelement, SrcMLDOTNETEventType eventType)
            : this(SourcePath, oldSourcePath, xelement, null, eventType)
        {
        }

        public SrcMLDOTNETEventArgs(string SourcePath, string oldSourcePath, XElement xelement, string SrcMLPath, SrcMLDOTNETEventType eventType)
        {
            this.SourceFilePath = SourcePath;
            this.OldSourceFilePath = oldSourcePath;
            this.SrcMLXElement = xelement;
            this.SrcMLFilePath = SrcMLPath;
            this.EventType = eventType;
        }

        public SrcMLDOTNETEventType EventType
        {
            get;
            set;
        }

        public string OldSourceFilePath
        {
            get;
            set;
        }

        public string SourceFilePath
        {
            get;
            set;
        }

        public XElement SrcMLXElement
        {
            get;
            set;
        }

        public string SrcMLFilePath
        {
            get;
            set;
        }
    }
}
