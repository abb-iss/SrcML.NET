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
using System.Collections.ObjectModel;

namespace ABB.SrcML {
    /// <summary>
    /// This is an implementation of <see cref="AbstractArchive"/>. File changes trigger the addition, update, and deletion of srcML archives in
    /// the archive directory
    /// </summary>
    public class SrcMLArchive : AbstractArchive {
        private XmlFileNameMapping xmlFileNameMapping;
        
        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="xmlDirectory">The directory to store the SrcML files in.</param>
        public SrcMLArchive(string xmlDirectory)
            : this(xmlDirectory, true) {}

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="xmlDirectory">The directory to store the SrcML files in.</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        public SrcMLArchive(string xmlDirectory, bool useExistingSrcML)
            : this(xmlDirectory, useExistingSrcML, new SrcMLGenerator()) {}

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="xmlDirectory">The directory to store the SrcML files in.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string xmlDirectory, SrcMLGenerator generator)
            : this(xmlDirectory, true, generator) {}

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="xmlDirectory">The directory to store the SrcML files in.</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string xmlDirectory, bool useExistingSrcML, SrcMLGenerator generator)
            : this(xmlDirectory, useExistingSrcML, generator, new ShortXmlFileNameMapping(xmlDirectory)) {}

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="xmlDirectory">The directory to store the SrcML files in.</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <paramref name="xmlDirectory"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        /// <param name="xmlMapping">The XmlFileNameMapping to use to map source paths to xml file paths.</param>
        public SrcMLArchive(string xmlDirectory, bool useExistingSrcML, SrcMLGenerator generator, XmlFileNameMapping xmlMapping) 
        : base(xmlDirectory) {
            this.ArchivePath = xmlDirectory;
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
            GenerateXmlForSource(fileName);
            OnFileChanged(new FileEventRaisedArgs(fileName, eventType));
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
            OnFileChanged(new FileEventRaisedArgs(fileName, FileEventType.FileDeleted));
        }

        /// <summary>
        /// Gets all of the source file names stored in this archive
        /// </summary>
        /// <returns>an enumerable of file names stored in this archive</returns>
        public override IEnumerable<string> GetFiles() {
            List<string> allSrcMLedFiles = new List<string>();
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
                foreach(FileInfo fi in srcMLFiles) {
                    string sourceFilePath = GetSourcePathForXmlPath(fi.Name);
                    yield return sourceFilePath;
                }
            }
        }

        /// <summary>
        /// Checks if the srcML stored in the archive is up to date with the source file.
        /// If the file is not in the archive, it is outdated
        /// </summary>
        /// <param name="fileName">the file name to check</param>
        /// <returns>true if the source file is newer than srcML file in the archive or the file is not in the archive.</returns>
        public override bool IsOutdated(string fileName) {
            if(!File.Exists(xmlPath)) return true;
            
            var sourceFileInfo = new FileInfo(fileName);
            var xmlPath = GetXmlPathForSourcePath(fileName);
            var xmlFileInfo = new FileInfo(fileName);

            return sourceFileInfo.LastWriteTime > xmlFileInfo.LastWriteTime;
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
            OnFileChanged(new FileEventRaisedArgs(oldFileName, newFileName, FileEventType.FileRenamed));
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
        public SrcMLFile GenerateXmlForSource(string sourcePath) {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
            return this.XmlGenerator.GenerateSrcMLFromFile(sourcePath, xmlPath);
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
            return xmlFileNameMapping.GetXMLPath(sourcePath);
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
