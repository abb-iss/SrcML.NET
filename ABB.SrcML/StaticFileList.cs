/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public class StaticFileList : IFileMonitor {
        private List<string> files;

        /// <summary>
        /// Creates a new StaticFileList with the given files.
        /// </summary>
        /// <param name="sourceFiles">The files to include.</param>
        public StaticFileList(IEnumerable<string> sourceFiles) {
            files = sourceFiles.ToList();
        }

        /// <summary>
        /// Creates a new StaticFileList with the files in the given directory.
        /// Note that this class does not monitor the directory and will not reflect any changes.
        /// </summary>
        /// <param name="directory">The directory containing the files to include. Any subdirectories will be included as well.</param>
        public StaticFileList(string directory) {
            if(!Directory.Exists(directory)) {
                throw new ArgumentException(string.Format("Directory {0} does not exist.", directory), "directory");
            }
            files = new List<string>(Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
        }

        #region IFileMonitor Members
        public event EventHandler<FileEventRaisedArgs> FileEventRaised;
        public void StartMonitoring() {}

        public void StopMonitoring() {}

        public List<string> GetMonitoredFiles(BackgroundWorker worker) {
            return new List<string>(files);
        }

        public void RaiseSolutionMonitorEvent(string filePath, string oldFilePath, FileEventType type) {
            throw new NotImplementedException();
        }
        #endregion
    }
}
