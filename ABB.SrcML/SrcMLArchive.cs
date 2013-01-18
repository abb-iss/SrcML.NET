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
using System.Linq;
using System.IO;
using System.Xml.Linq;
using ABB.SrcML.Utilities;

namespace ABB.SrcML
{
    ////public class SrcMLArchive : AbstractArchive, ISourceFolder
    public class SrcMLArchive : AbstractArchive, ISrcMLDOTNETEvents
    {
        ////public SrcMLArchive(ISourceFolder sourceDirectory)
        public SrcMLArchive(ISrcMLDOTNETEvents sourceDirectory)
            : this(sourceDirectory, Path.Combine(sourceDirectory.FullFolderPath, ".srcml"), new Src2SrcMLRunner())
        {

        }

        ////public SrcMLArchive(ISourceFolder sourceDirectory, string xmlDirectory)
        public SrcMLArchive(ISrcMLDOTNETEvents sourceDirectory, string xmlDirectory)
            : this(sourceDirectory, xmlDirectory, new Src2SrcMLRunner())
        {

        }

        ////public SrcMLArchive(ISourceFolder sourceDirectory, string xmlDirectory, Src2SrcMLRunner generator)
        public SrcMLArchive(ISrcMLDOTNETEvents sourceDirectory, string xmlDirectory, Src2SrcMLRunner generator)
        {
            this.SourceDirectory = sourceDirectory;
            this.ArchivePath = xmlDirectory;

            this.XmlGenerator = generator;
            
            if (!Directory.Exists(this.ArchivePath))
            {
                Directory.CreateDirectory(this.ArchivePath);
            }
            ////this.SourceDirectory.SourceFileChanged += RespondToFileChangedEvent;
            this.SourceDirectory.SrcMLDOTNETEventRaised += RespondToFileChangedEvent;
        }

        ////public ISourceFolder SourceDirectory
        public ISrcMLDOTNETEvents SourceDirectory
        {
            get;
            set;
        }

        public Src2SrcMLRunner XmlGenerator
        {
            get;
            set;
        }

        #region ISrcMLDOTNETEvents Members

        public event EventHandler<SrcMLDOTNETEventArgs> SrcMLDOTNETEventRaised;

        public string FullFolderPath
        {
            get
            {
                return this.SourceDirectory.FullFolderPath;
            }
            set
            {
                this.SourceDirectory.FullFolderPath = value;
            }
        }

        public void StartWatching()
        {
            this.SourceDirectory.StartWatching();
        }

        public void StopWatching()
        {
            this.SourceDirectory.StopWatching();
        }

        #endregion

        /* //// Original code
        #region ISourceFolder Members

        public event EventHandler<SourceEventArgs> SourceFileChanged;

        public string FullFolderPath
        {
            get
            {
                return this.SourceDirectory.FullFolderPath;
            }
            set
            {
                this.SourceDirectory.FullFolderPath = value;
            }
        }

        public void StartWatching()
        {
            this.SourceDirectory.StartWatching();
        }

        public void StopWatching()
        {
            this.SourceDirectory.StopWatching();
        }

        #endregion
        */

        #region AbstractArchive Members

        public override IEnumerable<XElement> FileUnits
        {
            get
            {
                var xmlFiles = Directory.EnumerateFiles(this.ArchivePath, "*.xml", SearchOption.AllDirectories);
                foreach (var xmlFileName in xmlFiles)
                {
                    yield return XElement.Load(xmlFileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                }
            }
        }

        public override void AddUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override void DeleteUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                DeleteXmlForSourceFile(path);
            }
        }

        public override void UpdateUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override XElement GetUnitForPath(string pathToUnit)
        {
            throw new NotImplementedException();
        }
        #endregion
        

        /// <summary>
        /// Respond to an event raised from Solution Monitor. Then raise an event to client application (e.g., Sando)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        ////public void RespondToFileChangedEvent(object sender, SourceEventArgs eventArgs)
        public void RespondToFileChangedEvent(object sender, SrcMLDOTNETEventArgs eventArgs)
        {
            var directoryName = Path.GetDirectoryName(Path.GetFullPath(eventArgs.SourceFilePath));
            var xmlFullPath = Path.GetFullPath(this.ArchivePath);
            
            if (!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                XElement xelement = null;
                switch (eventArgs.EventType)
                {
                    case SrcMLDOTNETEventType.SourceFileRenamed:
                        DeleteXmlForSourceFile(eventArgs.OldSourceFilePath);
                        goto case SrcMLDOTNETEventType.SourceFileChanged;
                    case SrcMLDOTNETEventType.SourceFileAdded:
                        goto case SrcMLDOTNETEventType.SourceFileChanged;
                    case SrcMLDOTNETEventType.SourceFileChanged:
                        xelement = GenerateXmlAndXElementForSource(eventArgs.SourceFilePath);
                        break;
                    case SrcMLDOTNETEventType.SourceFileDeleted:
                        DeleteXmlForSourceFile(eventArgs.SourceFilePath);
                        break;
                    case SrcMLDOTNETEventType.StartupCompleted:
                        // Nothing need to do?
                        break;
                    case SrcMLDOTNETEventType.MonitoringStopped:
                        // Nothing need to do?
                        break;
                }
                eventArgs.SrcMLXElement = xelement;
                OnSourceFileChanged(eventArgs);
            }
        }

        /// <summary>
        /// Regenerate srcML files only for added/changed/deleted/renamed files under a directory recursively.
        /// Last modified on 2012.10.11
        /// </summary>
        /// <param name="directoryPath"></param>
        /* //// This functionality should be within SolutionMonitor.cs now.
        public void GenerateXmlForDirectory(string directoryPath)
        {
            // Traverse source directory to generate srcML files when needed (TODO: make sure directoryPath is a full path?)
            DirectoryInfo rootDir = new DirectoryInfo(directoryPath);
            WalkSourceDirectoryTree(rootDir);

            // Traverse srcML directory to remove srcML files when needed
            //// Try to store srcML files with Base32 encoding in ONE folder
            DirectoryInfo srcMLRootDir = new DirectoryInfo(Path.GetFullPath(this.ArchivePath));
            WalkSrcMLDirectoryTree(srcMLRootDir);
        }
        */

        /* //// This functionality should be within SolutionMonitor.cs now.
        private void WalkSourceDirectoryTree(DirectoryInfo sourceDir)
        {
            FileInfo[] sourceFiles = null;
            DirectoryInfo[] sourceSubDirs = null;

            try
            {
                sourceFiles = sourceDir.GetFiles("*.*");
            }
            // In case one of the files requires permissions greater than the application provides
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (sourceFiles != null)
            {
                foreach (FileInfo fi in sourceFiles)
                {
                    ////Console.WriteLine("-> Source File: " + fi.FullName);
                    string srcMLFilePath = GetXmlPathForSourcePath(fi.FullName);
                    ////Console.WriteLine("-> srcML File: " + srcMLFilePath);
                    try
                    {
                        if (!File.Exists(srcMLFilePath))
                        {
                            // If there is not a corresponding srcML file, then generate the srcML file [Added]
                            RespondToFileChangedEvent(null, new SourceEventArgs(fi.FullName, SourceEventType.Added));
                            ////Console.WriteLine("Added");
                        }
                        else
                        {
                            // if source file's timestamp is later than its srcML file's timestamp, 
                            // then GenerateXmlForSource() [Changed]
                            DateTime sourceFileTimestamp = fi.LastWriteTime;
                            DateTime srcLMFileTimestamp = new FileInfo(srcMLFilePath).LastWriteTime;
                            if (sourceFileTimestamp.CompareTo(srcLMFileTimestamp) > 0)
                            {
                                RespondToFileChangedEvent(null, new SourceEventArgs(fi.FullName, SourceEventType.Changed));
                                Console.WriteLine("Changed");
                            }
                            else
                            {
                                //Console.WriteLine("!!! NO ACTION !!!");
                            }
                        }
                    }
                    // In case the file has been deleted since the traversal
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                sourceSubDirs = sourceDir.GetDirectories();
                foreach (DirectoryInfo sourceDirInfo in sourceSubDirs)
                {
                    //Console.WriteLine("sourceDirInfo: " + sourceDirInfo.Name);
                    if (!".srcml".Equals(sourceDirInfo.Name))
                    {
                        WalkSourceDirectoryTree(sourceDirInfo);
                    }
                }
            }
        }
        */

        /*
        private void WalkSrcMLDirectoryTree(DirectoryInfo srcMLDir)
        {
            FileInfo[] srcMLFiles = null;
            DirectoryInfo[] srcMLSubDirs = null;

            try
            {
                srcMLFiles = srcMLDir.GetFiles("*.*");
            }
            // In case one of the files requires permissions greater than the application provides
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (srcMLFiles != null)
            {
                foreach (FileInfo fi in srcMLFiles)
                {
                    Console.WriteLine("<- srcML File: " + fi.FullName);
                    string sourceFilePath = GetSourcePathForXmlPath(fi.FullName);
                    Console.WriteLine("<- Source File: " + sourceFilePath);
                    try
                    {
                        if (!File.Exists(sourceFilePath))
                        {
                            // If there is not a corresponding source file, then delete the srcML file [Deleted]
                            RespondToFileChangedEvent(null, new SourceEventArgs(sourceFilePath, SourceEventType.Deleted));
                            Console.WriteLine("Deleted");
                        }
                        else
                        {
                            //Console.WriteLine("!!! NO ACTION !!!");
                        }
                    }
                    // In case the file has been deleted since the traversal
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                srcMLSubDirs = srcMLDir.GetDirectories();
                foreach (DirectoryInfo srcMLDirInfo in srcMLSubDirs)
                {
                    //Console.WriteLine("srcMLDirInfo: " + srcMLDirInfo.Name);
                    WalkSrcMLDirectoryTree(srcMLDirInfo);
                }
            }
        }
        */

        /* //// This functionality should be within SolutionMonitor.cs now.
        private void WalkSrcMLDirectoryTree(DirectoryInfo srcMLDir)
        {
            FileInfo[] srcMLFiles = null;

            try
            {
                srcMLFiles = srcMLDir.GetFiles("*.*");
            }
            // In case one of the files requires permissions greater than the application provides
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (srcMLFiles != null)
            {
                foreach (FileInfo fi in srcMLFiles)
                {
                    ////Console.WriteLine("<- srcML File: " + fi.FullName);
                    string sourceFilePath = GetSourcePathForXmlPath(fi.FullName);
                    ////Console.WriteLine("<- Source File: " + sourceFilePath);
                    try
                    {
                        if (!File.Exists(sourceFilePath))
                        {
                            // If there is not a corresponding source file, then delete the srcML file [Deleted]
                            RespondToFileChangedEvent(null, new SourceEventArgs(sourceFilePath, SourceEventType.Deleted));
                            Console.WriteLine("Deleted");
                        }
                        else
                        {
                            //Console.WriteLine("!!! NO ACTION !!!");
                        }
                    }
                    // In case the file has been deleted since the traversal
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
        */

        /// <summary>
        /// Added by JZ on 12/3/2012
        /// Generate both a srcML File and a string of the content of this file for a source code file.
        /// </summary>
        /// <param name="sourcePath">The full path of the source code file.</param>
        /// <returns>The string of the content of the generated srcML file.</returns>
        public string GenerateXmlAndStringForSource(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return this.XmlGenerator.GenerateSrcMLAndStringFromFile(sourcePath, xmlPath);
        }

        /// <summary>
        /// Added by JZ on 12/4/2012
        /// Generate both a srcML File and a XElement of the content of this file for a source code file.
        /// </summary>
        /// <param name="sourcePath">The full path of the source code file.</param>
        /// <returns>The XElement of the content of the generated srcML file.</returns>
        public XElement GenerateXmlAndXElementForSource(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return this.XmlGenerator.GenerateSrcMLAndXElementFromFile(sourcePath, xmlPath);
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        /// <param name="sourcePath"></param>
        public void GenerateXmlForSource(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            this.XmlGenerator.GenerateSrcMLFromFile(sourcePath, xmlPath);
        }

        public void DeleteXmlForSourceFile(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            if (File.Exists(xmlPath))
            {
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

        // Version for hierarchy folders storage
        /*
        public string GetXmlPathForSourcePath(string sourcePath)
        {
            string fullPath = String.Empty;
            if (Path.IsPathRooted(sourcePath))
            {
                fullPath = sourcePath;
            }
            else
            {
                fullPath = Path.GetFullPath(sourcePath);
            }

            if (!fullPath.StartsWith(this.SourceDirectory.FullFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", sourcePath, this.SourceDirectory));
            }

            var dirLength = this.SourceDirectory.FullFolderPath.Length;
            if (Path.PathSeparator != this.SourceDirectory.FullFolderPath[dirLength - 1])
                dirLength++;

            string relativePath = fullPath.Substring(dirLength);
            string xmlPath = Path.Combine(this.ArchivePath, relativePath);

            xmlPath = xmlPath + ".xml";

            return xmlPath;
        }
        */

        // Version for single folder storage
        public string GetXmlPathForSourcePath(string sourcePath)
        {
            string fullPath = String.Empty;
            if (Path.IsPathRooted(sourcePath))
            {
                fullPath = sourcePath;
            }
            else
            {
                fullPath = Path.GetFullPath(sourcePath);
            }

            if (!fullPath.StartsWith(this.SourceDirectory.FullFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", sourcePath, this.SourceDirectory));
            }

            //Console.WriteLine("fullPath = " + fullPath);
            string srcMLFileName = Base32.ToBase32String(fullPath);
            //Console.WriteLine("Encoded srcMLFileName = " + srcMLFileName);
            string xmlPath = Path.Combine(this.ArchivePath, srcMLFileName);
            xmlPath = xmlPath + ".xml";

            return xmlPath;
        }

        // Version for hierarchy folders storage
        /*
        public string GetSourcePathForXmlPath(string xmlPath)
        {
            string fullPath = String.Empty;
            fullPath = (Path.IsPathRooted(xmlPath)) ? xmlPath : Path.GetFullPath(xmlPath);

            // ?? SourceDirectory and ArchivePath is not treated as the same type? (ISourceFolder sourceDirectory, string xmlDirectory)
            if (!fullPath.StartsWith(this.ArchivePath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", xmlPath, this.ArchivePath));
            }

            string relativePath = fullPath.Substring(this.ArchivePath.Length, fullPath.Length - this.ArchivePath.Length - 4);
            string sourcePath = this.SourceDirectory.FullFolderPath + relativePath;

            return sourcePath;
        }
        */

        // Version for single folder storage
        public string GetSourcePathForXmlPath(string xmlPath)
        {
            string fullPath = String.Empty;
            fullPath = (Path.IsPathRooted(xmlPath)) ? xmlPath : Path.GetFullPath(xmlPath);

            // ?? SourceDirectory and ArchivePath is not treated as the same type? (ISourceFolder sourceDirectory, string xmlDirectory)
            if (!fullPath.StartsWith(this.ArchivePath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", xmlPath, this.ArchivePath));
            }

            string relativePath = fullPath.Substring(this.ArchivePath.Length + 1, fullPath.Length - this.ArchivePath.Length - 5);
            //Console.WriteLine("relativePath = " + relativePath);
            relativePath = Base32.FromBase32String(relativePath);
            //Console.WriteLine("relativePath decoded = " + relativePath);
            relativePath = relativePath.Substring(this.SourceDirectory.FullFolderPath.Length + 1);
            //Console.WriteLine("relativePath decoded = " + relativePath);
            string sourcePath = Path.Combine(this.SourceDirectory.FullFolderPath, relativePath);
            
            return sourcePath;
        }

        ////protected virtual void OnSourceFileChanged(SourceEventArgs e)
        protected virtual void OnSourceFileChanged(SrcMLDOTNETEventArgs e)
        {
            ////EventHandler<SourceEventArgs> handler = SourceFileChanged;
            EventHandler<SrcMLDOTNETEventArgs> handler = SrcMLDOTNETEventRaised;

            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
