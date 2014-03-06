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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML {
    /// <summary>
    /// This interface is designed for all events that can be raised from SrcML.NET.
    /// </summary>
    public interface IFileMonitor : IDisposable {
        int NumberOfAllMonitoredFiles { get; }

        bool UpdateArchivesRunning { get; }

        event EventHandler<FileEventRaisedArgs> FileChanged;

        event EventHandler MonitoringStopped;

        event EventHandler UpdateArchivesStarted;
        event EventHandler UpdateArchivesCompleted;

        /// <summary>
        /// Start monitoring files
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stop monitoring files
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Get all monitored files
        /// </summary>
        /// <param name="worker"></param>
        /// <returns></returns>
        Collection<string> GetFilesFromSource();

        IEnumerable<string> GetArchivedFiles();

        void RegisterArchive(IArchive archive, bool isDefault);

        void AddFile(string filePath);

        Task AddFileAsync(string filePath);

        void DeleteFile(string filePath);

        Task DeleteFileAsync(string filePath);

        void UpdateFile(string filePath);

        Task UpdateFileAsync(string filePath);

        void RenameFile(string oldFilePath, string newFilePath);

        Task RenameFileAsync(string oldFilePath, string newFilePath);

        void Save();

        void UpdateArchives();

        Task UpdateArchivesAsync();
    }

    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum FileEventType {
        /// <summary>
        /// Raised when a file is added
        /// </summary>
        FileAdded,
        
        /// <summary>
        /// Raised when a file is changed
        /// </summary>
        FileChanged,
        
        /// <summary>
        /// Raised when a file is deleted
        /// </summary>
        FileDeleted,

        /// <summary>
        /// Raised when a file is renamed
        /// </summary>
        FileRenamed,
    }

    /// <summary>
    /// Event data of SrcML.NET events.
    /// </summary>
    public class FileEventRaisedArgs : System.EventArgs {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected FileEventRaisedArgs() {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="pathToFile"></param>
        public FileEventRaisedArgs(FileEventType eventType, string pathToFile)
            : this(eventType, pathToFile, pathToFile, false) {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="pathToFile"></param>
        /// <param name="hasSrcML"></param>
        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, bool hasSrcML)
            : this(eventType, pathToFile, pathToFile, hasSrcML) {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="pathToFile"></param>
        /// <param name="oldPathToFile"></param>
        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, string oldPathToFile)
            : this(eventType, pathToFile, oldPathToFile, false) {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="pathToFile"></param>
        /// <param name="oldPathToFile"></param>
        /// <param name="hasSrcML"></param>
        public FileEventRaisedArgs(FileEventType eventType, string pathToFile, string oldPathToFile, bool hasSrcML) {
            this.EventType = eventType;
            this.FilePath = pathToFile;
            this.OldFilePath = oldPathToFile;
            this.HasSrcML = hasSrcML;
        }

        /// <summary>
        /// Type of the file event
        /// </summary>
        public FileEventType EventType {
            get;
            set;
        }

        /// <summary>
        /// Old file path
        /// </summary>
        public string OldFilePath {
            get;
            set;
        }

        /// <summary>
        /// File path
        /// </summary>
        public string FilePath {
            get;
            set;
        }

        /// <summary>
        /// Whether has a corresponding srcML file
        /// </summary>
        public bool HasSrcML { get; set; }
    }
}
