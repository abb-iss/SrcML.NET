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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;

namespace ABB.SrcML {
    /// <summary>
    /// This is an implementation of <see cref="AbstractArchive"/>. File changes trigger the addition, update, and deletion of srcML archives in
    /// the archive directory
    /// </summary>
    public class SrcMLArchive : AbstractArchive {
        private XmlFileNameMapping xmlFileNameMapping;

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        public SrcMLArchive(string baseDirectory)
            : this(baseDirectory, "srcML") {
        }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML)
            : this(baseDirectory, "srcML", useExistingSrcML) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML, SrcMLGenerator generator)
            : this(baseDirectory, "srcML", useExistingSrcML, generator) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        /// <param name="xmlMapping">The XmlFileNameMapping to use to map source paths to xml file paths.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML, SrcMLGenerator generator, XmlFileNameMapping xmlMapping)
            : this(baseDirectory, "srcML", useExistingSrcML, generator, xmlMapping) {
        }
        /// <summary>
        /// Creates a new SrcMLArchive. By default, any existing srcML will be used.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory)
            : this(baseDirectory, srcMLDirectory, true) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML)
            : this(baseDirectory, srcMLDirectory, useExistingSrcML, new SrcMLGenerator()) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML, SrcMLGenerator generator)
            : this(baseDirectory, srcMLDirectory, useExistingSrcML, generator,
                   new ShortXmlFileNameMapping(Path.Combine(baseDirectory, srcMLDirectory))) {

        }

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        /// <param name="xmlMapping">The XmlFileNameMapping to use to map source paths to xml file paths.</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML, SrcMLGenerator generator, XmlFileNameMapping xmlMapping) 
            : base(baseDirectory, srcMLDirectory) {
            this.XmlGenerator = generator;
            this.xmlFileNameMapping = xmlMapping;

            if(!Directory.Exists(this.ArchivePath)) {
                Directory.CreateDirectory(this.ArchivePath);
            } else {
                if(!useExistingSrcML) {
                    foreach(var file in Directory.GetFiles(ArchivePath, "*.xml")) {
                        File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// The SrcML generator used to generate srcML
        /// </summary>
        public SrcMLGenerator XmlGenerator { get; set; }

        /// <summary>
        /// Enumerates over each file in the archive and returns a file unit
        /// </summary>
        public IEnumerable<XElement> FileUnits {
            get {
                var xmlFiles = Directory.EnumerateFiles(this.ArchivePath, "*.xml", SearchOption.AllDirectories);
                foreach(var xmlFileName in xmlFiles) {
                    yield return XElement.Load(xmlFileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                }
            }
        }

        /// <summary>
        /// Enumerates over each file in the archive and returns a file list
        /// </summary>        
        public List<string> ArchivedXmlFiles() {
            var xmlFiles = Directory.EnumerateFiles(this.ArchivePath, "*.xml", SearchOption.AllDirectories);
            List<string> xmlFiles_list = new List<string>(xmlFiles.ToArray());
            return xmlFiles_list;
        }



        #region IDisposable Members

        /// <summary>
        /// Disposes of the internal <see cref="XmlFileNameMapping"/> and then calls <see cref="AbstractArchive.Dispose()"/>
        /// </summary>
        public override void Dispose() {
            xmlFileNameMapping.Dispose();
            base.Dispose();
        }

        #endregion

        #region AbstractArchive Members

        /// <summary>
        /// The list of extensions supported by the archive (taken from <see cref="XmlGenerator"/>)
        /// </summary>
        public override ICollection<string> SupportedExtensions {
            get { return this.XmlGenerator.ExtensionMapping.Keys; }
        }

        /// <summary>
        /// generates srcML for the given file. It raises <see cref="AbstractArchive.FileChanged"/> when finished.
        /// </summary>
        /// <param name="fileName">the file to add or update</param>
        public override void AddOrUpdateFile(string fileName) {
            FileEventType eventType = FileEventType.FileAdded;
            if(this.ContainsFile(fileName)) {
                eventType = FileEventType.FileChanged;
            }
            if(File.Exists(fileName)) {
                GenerateXmlForSource(fileName);
                OnFileChanged(new FileEventRaisedArgs(eventType, fileName, true));
            }
        }

        /// <summary>
        /// Checks to see if the file has a companions srcML file in the archive
        /// </summary>
        /// <param name="fileName">the file to check for</param>
        /// <returns>true if the file is in the archive; false otherwise</returns>
        public override bool ContainsFile(string fileName) {
            var xmlPath = GetXmlPathForSourcePath(fileName);
            return File.Exists(xmlPath);
        }

        /// <summary>
        /// Deletes the srcML document for the given file. It raises <see cref="AbstractArchive.FileChanged"/> when finished.
        /// </summary>
        /// <param name="fileName">the file to delete</param>
        public override void DeleteFile(string fileName) {
            var xmlPath = GetXmlPathForSourcePath(fileName);
            if(File.Exists(xmlPath)) {
                File.Delete(xmlPath);
            }
            OnFileChanged(new FileEventRaisedArgs(FileEventType.FileDeleted, fileName, false));
        }

        /// <summary>
        /// Gets all of the source file names stored in this archive
        /// </summary>
        /// <returns>an enumerable of file names stored in this archive</returns>
        public override Collection<string> GetFiles() {
            Collection<string> allSrcMLedFiles = new Collection<string>();
            DirectoryInfo srcMLDir = new DirectoryInfo(Path.GetFullPath(this.ArchivePath));
            FileInfo[] srcMLFiles = null;
            try {
                srcMLFiles = srcMLDir.GetFiles("*.xml");
            }
                // In case one of the files requires permissions greater than the application provides
            catch(UnauthorizedAccessException e) {
                Console.WriteLine(e.Message);
            } catch(DirectoryNotFoundException e) {
                Console.WriteLine(e.Message);
            }
            if(srcMLFiles != null) {
                var results = from fInfo in srcMLFiles
                              select GetSourcePathForXmlPath(fInfo.Name);
                return new Collection<string>(results.ToList<string>());
            }
            return null;
        }

        /// <summary>
        /// Checks if the srcML stored in the archive is up to date with the source file.
        /// If the file is not in the archive, it is outdated
        /// </summary>
        /// <param name="fileName">the file name to check</param>
        /// <returns>true if the source file is newer OR older than its srcML file in the archive or the file is not in the archive.</returns>
        public override bool IsOutdated(string fileName) {
            var sourceFileInfo = new FileInfo(fileName);
            var xmlPath = GetXmlPathForSourcePath(fileName);
            var xmlFileInfo = new FileInfo(xmlPath);

            return sourceFileInfo.Exists != xmlFileInfo.Exists || sourceFileInfo.LastWriteTime != xmlFileInfo.LastWriteTime;
        }

        /// <summary>
        /// Deletes the old XML file and generates the new one
        /// </summary>
        /// <param name="oldFileName">the old file name</param>
        /// <param name="newFileName">the new file name</param>
        public override void RenameFile(string oldFileName, string newFileName) {
            var oldXmlPath = GetXmlPathForSourcePath(oldFileName);
            var newXmlPath = GetXmlPathForSourcePath(newFileName);

            if(File.Exists(oldXmlPath)) {
                File.Delete(oldXmlPath);
            }
            GenerateXmlForSource(newFileName);
            OnFileChanged(new FileEventRaisedArgs(FileEventType.FileRenamed, newFileName, oldFileName, true));
        }

        #endregion AbstractArchive Members

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
        /// Generate a srcML File for a source code file. Now use this method instead of GenerateXmlAndXElementForSource()
        /// </summary>
        /// <param name="sourcePath"></param>
        public SrcMLFile GenerateXmlForSource(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            // Set the timestamp to the same as the source file
            // Will be useful in the method of public override bool IsOutdated(string fileName)
            SrcMLFile srcMLFile = this.XmlGenerator.GenerateSrcMLFromFile(sourcePath, xmlPath);
            FileInfo srcFI = new FileInfo(sourcePath);
            File.SetLastWriteTime(xmlPath, srcFI.LastWriteTime);

            return srcMLFile;
        }

        
        /// <summary>
        /// Concurrency Generate SrcML from source file: ZL 03/11/2013
        /// </summary>
        /// <param name="listOfSourcePath"></param>
        /// <param name="levelOfConcurrency"></param>
        public void ConcurrentGenerateXmlForSource(List<string> listOfSourcePath, int levelOfConcurrency) {
            List<string> missedFiles = new List<string>();

            ParallelOptions option = new ParallelOptions();
            option.MaxDegreeOfParallelism = levelOfConcurrency;

            Parallel.ForEach(listOfSourcePath, option, currentFile => {
                string fileName = currentFile;
                try {
                    GenerateXmlForSource(fileName);
                } catch(Exception e) {
                    Trace.WriteLine(fileName + " " + e.Message);
                    missedFiles.Add(fileName);
                }
            });

            Task.WaitAll();

            //As a remedial action, regenerate the file missed in the last step
            if(missedFiles.Count > 0) {
                foreach(string fileName in missedFiles)
                    GenerateXmlForSource(fileName);
            }
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
        /// Returns the corresponding srcML file path for the given source file.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public string GetXmlPathForSourcePath(string sourcePath) {
            return xmlFileNameMapping.GetXmlPath(sourcePath);
        }

        /// <summary>
        /// Get the corresponding source file path for a specific srcML file.
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public string GetSourcePathForXmlPath(string xmlPath) {
            return xmlFileNameMapping.GetSourcePath(xmlPath);
        }

        /// <summary>
        /// Gets the XElement for the specified source file. If the SrcML does not already exist in the archive, it will be created.
        /// </summary>
        /// <param name="sourceFilePath">The source file to get the root XElement for.</param>
        /// <returns>The root XElement of the source file.</returns>
        public XElement GetXElementForSourceFile(string sourceFilePath) {
            string xmlPath = GetXmlPathForSourcePath(sourceFilePath);
            SrcMLFile srcMLFile;
            if(File.Exists(xmlPath)) {
                srcMLFile = new SrcMLFile(xmlPath);
            } else {
                srcMLFile = GenerateXmlForSource(sourceFilePath);
            }
            return srcMLFile.FileUnits.FirstOrDefault();
        }
    }
}
