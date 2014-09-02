/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using ABB.VisualStudio;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio {

    /// <summary>
    /// This is the interface that will be implemented by the global service exposed by the package
    /// defined in ABB.SrcML.VisualStudio.SrcMLService. Notice that we have to define this interface
    /// as COM visible so that it will be possible to query for it from the native version of
    /// IServiceProvider.
    /// </summary>
    [Guid(GuidList.ISrcMLGlobalServiceId), ComVisible(true)]
    public interface ISrcMLGlobalService {

        /// <summary>
        /// Even to indicate that a directory has been added to the file monitor.
        /// </summary>
        event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryAdded;

        /// <summary>
        /// Event to indicate that a directory has been removed from the file monitor
        /// </summary>
        event EventHandler<DirectoryScanningMonitorEventArgs> DirectoryRemoved;

        event EventHandler MonitoringStarted;

        /// <summary>
        /// Event to indicate that <see cref="StopMonitoring()"/> has been called
        /// </summary>
        event EventHandler MonitoringStopped;

        /// <summary>
        /// Event to indicate that a source file has changed
        /// </summary>
        event EventHandler<FileEventRaisedArgs> SourceFileChanged;

        event EventHandler UpdateArchivesStarted;
        
        event EventHandler UpdateArchivesCompleted;

        SourceMonitor CurrentMonitor { get; }

        SrcMLArchive CurrentSrcMLArchive { get; }

        bool IsMonitoring { get; }

        bool IsUpdating { get; }

        /// <summary>
        /// Collection of directories monitored by the service. The list of directories is persisted
        /// for each solution. Modify the collection by calling
        /// <see cref="AddDirectoryToMonitor(string)"/> and
        /// <see cref="RemoveDirectoryFromMonitor(string)"/>
        /// </summary>
        ReadOnlyCollection<string> MonitoredDirectories { get; }
        
        /// <summary>
        /// The interval at which the service persists its data to disk (in seconds).
        /// </summary>
        double SaveInterval { get; set; }

        /// <summary>
        /// The interval at which the service scans <see cref="MonitoredDirectories"/>
        /// </summary>
        double ScanInterval { get; set; }

        /// <summary>
        /// Add a directory to <see cref="MonitoredDirectories"/>
        /// </summary>
        /// <param name="pathToDirectory">The directory path to start monitoring</param>
        void AddDirectoryToMonitor(string pathToDirectory);

        /// <summary>
        /// Gets the unit XElement for the given
        /// <paramref name="sourceFilePath"/></summary>
        /// <param name="sourceFilePath">The path to get a unit XElement for</param>
        /// <returns>A unit XElement for
        /// <paramref name="sourceFilePath"/></returns>
        XElement GetXElementForSourceFile(string sourceFilePath);

        /// <summary>
        /// Remove a directory from <see cref="MonitoredDirectories"/>
        /// </summary>
        /// <param name="pathToDirectory">The directory to stop monitoring</param>
        void RemoveDirectoryFromMonitor(string pathToDirectory);
        
        /// <summary>
        /// Tells the srcML service to delete all of the data for this solution the next time it is opened
        /// </summary>
        void Reset();

        /// <summary>
        /// Start monitoring the current solution
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop monitoring the current solution
        /// </summary>
        void StopMonitoring();
    }

    /// <summary>
    /// The goal of this interface is actually just to define a Type (or Guid from the native
    /// client's point of view) that will be used to identify the service. In theory, we could use
    /// the interface defined above, but it is a good practice to always define a new type as the
    /// service's identifier because a service can expose different interfaces.
    ///
    /// It is registered at: HKEY_USERS\S-1-5-21-1472859983-109138142-169162935-138973\Software\Microsoft\VisualStudio\11.0Exp_Config\Services\{fafafdfb-60f3-47e4-b38c-1bae05b44241}
    /// </summary>
    [Guid(GuidList.SSrcMLGlobalServiceId)]
    public interface SSrcMLGlobalService {
    }
}