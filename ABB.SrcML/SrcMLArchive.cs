/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using ABB.SrcML.Utilities;

namespace ABB.SrcML {
    public class SrcMLArchive : AbstractArchive {
        private BackgroundWorker startupWorker;
        
        public SrcMLArchive(IFileMonitor fileMonitor, string xmlDirectory)
            : this(fileMonitor, xmlDirectory, new SrcMLGenerator()) {}

        public SrcMLArchive(IFileMonitor fileMonitor, string xmlDirectory, SrcMLGenerator generator) {
            this.FileMonitor = fileMonitor;
            this.ArchivePath = xmlDirectory;
            this.XmlGenerator = generator;

            if(!Directory.Exists(this.ArchivePath)) {
                Directory.CreateDirectory(this.ArchivePath);
            }

            this.FileMonitor.FileEventRaised += RespondToFileEvent;
        }

        public IFileMonitor FileMonitor { get; set; }

        public SrcMLGenerator XmlGenerator { get; set; }

        public event EventHandler<FileEventRaisedArgs> SourceFileChanged;
        public event EventHandler<EventArgs> StartupCompleted;
        public event EventHandler<EventArgs> MonitoringStopped;

        public void StartWatching() {
            // run background thread for startup
            startupWorker = new BackgroundWorker();
            startupWorker.WorkerSupportsCancellation = true;
            startupWorker.DoWork += new DoWorkEventHandler(_runStartupInBackground_DoWork);
            startupWorker.RunWorkerAsync();

            this.FileMonitor.StartMonitoring();
        }

        public void StopWatching() {
            try {
                this.FileMonitor.StopMonitoring();

                // Disable the startup background worker
                if(startupWorker != null) {
                    startupWorker.CancelAsync();
                }
            }
            finally {
                // maybe not necessary
                OnMonitoringStopped(new EventArgs());
            }
        }

        #region AbstractArchive Members

        public override IEnumerable<XElement> FileUnits {
            get {
                var xmlFiles = Directory.EnumerateFiles(this.ArchivePath, "*.xml", SearchOption.AllDirectories);
                foreach(var xmlFileName in xmlFiles) {
                    yield return XElement.Load(xmlFileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                }
            }
        }

        public override void AddUnits(IEnumerable<XElement> units) {
            foreach(var unit in units) {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override void DeleteUnits(IEnumerable<XElement> units) {
            foreach(var unit in units) {
                var path = this.GetPathForUnit(unit);
                DeleteXmlForSourceFile(path);
            }
        }

        public override void UpdateUnits(IEnumerable<XElement> units) {
            foreach(var unit in units) {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override XElement GetUnitForPath(string pathToUnit) {
            throw new NotImplementedException();
        }
        #endregion


        /// <summary>
        /// Run in background when starting up the solution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="anEvent"></param>
        private void _runStartupInBackground_DoWork(object sender, DoWorkEventArgs anEvent) {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var worker = sender as BackgroundWorker;
            try {
                List<string> allMonitoredFiles = FileMonitor.GetMonitoredFiles(worker);
                foreach(string sourceFilePath in allMonitoredFiles) {
                    // PROBLEM: cannot generate index in Sando for .txt, .xml etc files
                    ProcessSingleSourceFile(sourceFilePath);
                }

                List<string> allSrcMLedFiles = GetAllSrcMLedFiles();
                foreach(string sourceFilePath in allSrcMLedFiles) {
                    // PROBLEM: cannot generate index in Sando for .txt, .xml etc files
                    ProcessSingleSourceFile(sourceFilePath);
                }
            } finally {
                OnStartupCompleted(new EventArgs());
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// Get all "SrcMLed" files in this solution.
        /// TODO: maybe use KeyValuePairs instead of List for better performance
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllSrcMLedFiles() {
            List<string> allSrcMLedFiles = new List<string>();
            DirectoryInfo srcMLDir = new DirectoryInfo(Path.GetFullPath(this.ArchivePath));
            FileInfo[] srcMLFiles = null;
            try {
                srcMLFiles = srcMLDir.GetFiles("*.*");
            }
                // In case one of the files requires permissions greater than the application provides
            catch(UnauthorizedAccessException e) {
                Console.WriteLine(e.Message);
            } catch(DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
            }

            if(srcMLFiles != null) {
                foreach(FileInfo fi in srcMLFiles) {
                    string sourceFilePath = GetSourcePathForXmlPath(fi.Name);
                    allSrcMLedFiles.Add(sourceFilePath);
                }
            }
            return allSrcMLedFiles;
        }

        /// <summary>
        /// Process a single source file to add or change the corresponding srcML file, or do nothing.
        /// TODO: GetXmlPathForSourcePath() twice
        /// PROBLEM: cannot generate index in Sando for .txt, .xml etc files.  Have to raise solution monitor events too.
        /// </summary>
        /// <param name="sourceFilePath"></param>
        public void ProcessSingleSourceFile(string sourceFilePath) {
            //if (IsValidFileExtension(sourceFilePath))
            //{
            if(!File.Exists(sourceFilePath)) {
                // If there is not such a source file, then delete the corresponding srcML file
                if(IsValidFileExtension(sourceFilePath)) {
                    RespondToFileEvent(null, new FileEventRaisedArgs(sourceFilePath, FileEventType.FileDeleted));
                } else {
                    this.FileMonitor.RaiseSolutionMonitorEvent(sourceFilePath, null, FileEventType.FileDeleted);
                }
            } else {
                string srcMLFilePath = GetXmlPathForSourcePath(sourceFilePath);
                if(!File.Exists(srcMLFilePath)) {
                    // If there is not a corresponding srcML file, then generate the srcML file
                    if(IsValidFileExtension(sourceFilePath)) {
                        RespondToFileEvent(null, new FileEventRaisedArgs(sourceFilePath, FileEventType.FileAdded));
                    } else {
                        this.FileMonitor.RaiseSolutionMonitorEvent(sourceFilePath, null, FileEventType.FileAdded);
                    }
                } else {
                    DateTime sourceFileTimestamp = new FileInfo(sourceFilePath).LastWriteTime;
                    DateTime srcLMFileTimestamp = new FileInfo(srcMLFilePath).LastWriteTime;
                    if(sourceFileTimestamp.CompareTo(srcLMFileTimestamp) > 0) {
                        // If source file's timestamp is later than its srcML file's timestamp, then generate the srcML file, otherwise do nothing
                        if(IsValidFileExtension(sourceFilePath)) {
                            RespondToFileEvent(null, new FileEventRaisedArgs(sourceFilePath, FileEventType.FileChanged));
                        } else {
                            this.FileMonitor.RaiseSolutionMonitorEvent(sourceFilePath, null, FileEventType.FileChanged);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the file extension is in the set of file types that can be processed by SrcML.NET.
        /// </summary>
        /// <param name="filePath">The file name to check.</param>
        /// <returns>True if the file can be converted to SrcML; False otherwise.</returns>
        public bool IsValidFileExtension(string filePath) {
            string fileExtension = Path.GetExtension(filePath);
            if(fileExtension != null && XmlGenerator.ExtensionMapping.ContainsKey(fileExtension)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Respond to an event raised from Solution Monitor. Then raise a new event to client application (e.g., Sando)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        public void RespondToFileEvent(object sender, FileEventRaisedArgs eventArgs) {
            string sourceFilePath = eventArgs.SourceFilePath;
            string oldSourceFilePath = eventArgs.OldSourceFilePath;

            var directoryName = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
            var xmlFullPath = Path.GetFullPath(this.ArchivePath);

            if(!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase) && IsValidFileExtension(sourceFilePath)) {
                XElement xElement = null;
                switch(eventArgs.EventType) {
                    case FileEventType.FileAdded:
                        xElement = GenerateXmlAndXElementForSource(sourceFilePath);
                        break;
                    case FileEventType.FileChanged:
                        xElement = GenerateXmlAndXElementForSource(sourceFilePath);
                        break;
                    case FileEventType.FileDeleted:
                        DeleteXmlForSourceFile(sourceFilePath);
                        break;
                    case FileEventType.FileRenamed:
                        DeleteXmlForSourceFile(oldSourceFilePath);
                        xElement = GenerateXmlAndXElementForSource(sourceFilePath);
                        break;
                }

                eventArgs.SrcMLXElement = xElement;
                OnSourceFileChanged(eventArgs);
            }
        }

        /// <summary>
        /// Generate both a srcML File and a XElement of the content of this file for a source code file.
        /// </summary>
        /// <param name="sourcePath">The full path of the source code file.</param>
        /// <returns>The XElement of the content of the generated srcML file.</returns>
        public XElement GenerateXmlAndXElementForSource(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            return this.XmlGenerator.GenerateSrcMLAndXElementFromFile(sourcePath, xmlPath);
        }

        /// <summary>
        /// Generate both a srcML File and a string of the content of this file for a source code file.
        /// </summary>
        /// <param name="sourcePath">The full path of the source code file.</param>
        /// <returns>The string of the content of the generated srcML file.</returns>
        public string GenerateXmlAndStringForSource(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            return this.XmlGenerator.GenerateSrcMLAndStringFromFile(sourcePath, xmlPath);
        }

        /// <summary>
        /// Generate a srcML File for a source code file. Now use this method instead of GenerateXmlAndXElementForSource()
        /// </summary>
        /// <param name="sourcePath"></param>
        public void GenerateXmlForSource(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            this.XmlGenerator.GenerateSrcMLFromFile(sourcePath, xmlPath);
        }

        /// <summary>
        /// Delete the srcML file for a specified source file.
        /// </summary>
        /// <param name="sourcePath"></param>
        public void DeleteXmlForSourceFile(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            if(File.Exists(xmlPath)) {
                File.Delete(xmlPath);
            }

            /*
            if (!Directory.Exists(sourceDirectory))
            {
                var xmlDirectory = Path.GetDirectoryName(xmlPath);
                Directory.Delete(xmlDirectory);
            }
            */
        }

        /// <summary>
        /// Get the corresponding srcML file path for a specific source file.
        /// For single folder storage algorithm
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public string GetXmlPathForSourcePath(string sourcePath) {
            string fullPath = (Path.IsPathRooted(sourcePath)) ? sourcePath : Path.GetFullPath(sourcePath);
            //if (!fullPath.StartsWith(this.SourceDirectory.FullFolderPath, StringComparison.InvariantCultureIgnoreCase))
            //{
            //    throw new IOException(String.Format("{0} is not rooted in {1}", sourcePath, this.SourceDirectory));
            //}
            //string srcMLFileName = Base32.ToBase32String(fullPath);               // Base32 encoding
            string srcMLFileName = fullPath.Replace("\\", "-").Replace(":", "=");   // Simple encoding
            string xmlPath = Path.Combine(this.ArchivePath, srcMLFileName) + ".xml";
            return xmlPath;
        }

        /// <summary>
        /// Get the corresponding source file path for a specific srcML file.
        /// For single folder storage algorithm
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public string GetSourcePathForXmlPath(string xmlPath) {
            string sourcePath = xmlPath.Substring(0, xmlPath.Length - 4);
            //sourcePath = Base32.FromBase32String(sourcePath);                     // Base32 decoding
            sourcePath = sourcePath.Replace("=", ":").Replace("-", "\\");           // Simple decoding
            return sourcePath;
        }

        /// <summary>
        /// Gets the XElement for the specified source file.
        /// </summary>
        /// <param name="sourceFilePath">The source file to get the root XElement for.</param>
        /// <returns>The root XElement of the source file, or null if the file does not exist in the archive.</returns>
        public XElement GetXElementForSourceFile(string sourceFilePath) {
            string xmlPath = GetXmlPathForSourcePath(sourceFilePath);
            if(!File.Exists(xmlPath)) {
                return null;
            }
            var srcMLFile = new SrcMLFile(sourceFilePath);
            return srcMLFile.FileUnits.FirstOrDefault();
        }

        /// <summary>
        /// Raise a SrcML.NET event (SourceFileAdded, SourceFileChanged, SourceFileDeleted, SourceFileRenamed)
        /// </summary>
        /// <param name="e"></param>
        ////protected virtual void OnSourceFileChanged(SourceEventArgs e)
        protected virtual void OnSourceFileChanged(FileEventRaisedArgs e) {
            EventHandler<FileEventRaisedArgs> handler = SourceFileChanged;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnStartupCompleted(EventArgs e) {
            EventHandler<EventArgs> handler = StartupCompleted;
            if(handler != null) {
                handler(this, e);
            }
        }

        protected virtual void OnMonitoringStopped(EventArgs e) {
            EventHandler<EventArgs> handler = MonitoringStopped;
            if(handler != null) {
                handler(this, e);
            }
        }

        
    }
}
