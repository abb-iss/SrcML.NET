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
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// This interface is designed for all events that can be raised from SrcML.NET's Solution Monitor.
    /// </summary>
    public interface ISolutionMonitorEvents
    {
        event EventHandler<SolutionMonitorEventArgs> SolutionMonitorEventRaised;
        string FullFolderPath
        {
            get;
            set;
        }
        void StartWatching();
        void StopWatching();
        List<string> GetAllMonitoredFiles(BackgroundWorker worker);
    }

    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum SolutionMonitorEventType
    {
        FileAdded,    // Raised when a file is added into the solution in Visual Studio
        FileChanged,  // Raised when a file is changed and saved in the solution in Visual Studio
        FileDeleted,  // Raised when a file is deleted from the solution in Visual Studio
        FileRenamed,  // Raised when a file is renamed in the solution in Visual Studio
        //MonitoringStopped,  // Raised when the solution is about to be closed in Visual Studio
        //StartupCompleted    // Raised when the starup process for the solution is completed in Visual Studio
    }

    /// <summary>
    /// Event data of SrcML.NET Solution Monitor's events.
    /// </summary>
    public class SolutionMonitorEventArgs : System.EventArgs
    {
        protected SolutionMonitorEventArgs()
        {
        }

        public SolutionMonitorEventArgs(string SourcePath, SolutionMonitorEventType eventType)
            : this(SourcePath, SourcePath, eventType)
        {
        }

        public SolutionMonitorEventArgs(string SourcePath, string oldSourcePath, SolutionMonitorEventType eventType)
        {
            this.SourceFilePath = SourcePath;
            this.OldSourceFilePath = oldSourcePath;
            this.EventType = eventType;
        }

        public SolutionMonitorEventType EventType
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
    }
}
