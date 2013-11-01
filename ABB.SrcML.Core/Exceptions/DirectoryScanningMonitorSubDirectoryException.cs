/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {

    /// <summary>
    /// This exception is thrown exclusively by
    /// <see cref="DirectoryScanningMonitor.AddDirectory(string)"/> when the parameter is a
    /// subdirectory of a directory that is already in
    /// <see cref="DirectoryScanningMonitor.MonitoredDirectories"/>.
    /// </summary>
    public class DirectoryScanningMonitorSubDirectoryException : Exception {

        /// <summary>
        /// Creates a new directory scanning monitor subdirectory exception
        /// </summary>
        /// <param name="directory">The directory that someone tried to add via see
        /// cref="DirectoryScanningMonitor.AddDirectory(string)"/></param>
        /// <param name="parentDirectory">The parent directory that caused the exception</param>
        /// <param name="monitor">The monitor</param>
        public DirectoryScanningMonitorSubDirectoryException(string directory, string parentDirectory)
            : base(String.Format("{0} is a subdirectory of {1}", directory, parentDirectory)) {
            Directory = directory;
            ParentDirectory = parentDirectory;            
        }

        /// <summary>
        /// The directory that someone tried to add via
        /// <see cref="DirectoryScanningMonitor.AddDirectory(string)"/>.
        /// </summary>
        public string Directory { get; set; }


        /// <summary>
        /// The parent directory that caused the exception.
        /// </summary>
        public string ParentDirectory { get; set; }
    }
}