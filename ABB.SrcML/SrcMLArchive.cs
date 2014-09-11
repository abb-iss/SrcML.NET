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
    public class SrcMLArchive : GeneratorArchive<SrcMLGenerator> {
        /// <summary>
        /// The default diretory to store the srcML files in
        /// </summary>
        public const string DEFAULT_ARCHIVE_DIRECTORY = "srcML";

        /// <summary>
        /// Creates a new srcML archive in <see cref="DEFAULT_ARCHIVE_DIRECTORY"/> within <see cref="Environment.CurrentDirectory"/>.
        /// </summary>
        public SrcMLArchive() : this(Environment.CurrentDirectory, true) { }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        public SrcMLArchive(string baseDirectory)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, useExistingSrcML) {
        }

        /// <summary>
        /// Creates a new srcML archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// <param name="scheduler">The task scheduler to use for asynchronous tasks</param>
        public SrcMLArchive(string baseDirectory, TaskScheduler scheduler)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, true, new SrcMLGenerator(), new SrcMLFileNameMapping(Path.Combine(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY)), TaskScheduler.Default) { }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML, SrcMLGenerator generator)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, useExistingSrcML, generator) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive. The archive is created in <c>"baseDirectory\srcML"</c>.
        /// </summary>
        /// <param name="baseDirectory">the base directory</param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        /// <param name="xmlMapping">The XmlFileNameMapping to use to map source paths to xml file paths.</param>
        public SrcMLArchive(string baseDirectory, bool useExistingSrcML, SrcMLGenerator generator, AbstractFileNameMapping xmlMapping)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, useExistingSrcML, generator, xmlMapping, TaskScheduler.Default) {
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
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML)
            : this(baseDirectory, srcMLDirectory, useExistingSrcML, new SrcMLGenerator()) {
        }

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML, SrcMLGenerator generator)
            : this(baseDirectory, srcMLDirectory, useExistingSrcML, generator,
                   new SrcMLFileNameMapping(Path.Combine(baseDirectory, srcMLDirectory)), TaskScheduler.Default) {

        }

        /// <summary>
        /// Creates a new SrcMLArchive.
        /// </summary>
        /// <param name="baseDirectory">The parent of <paramref name="srcMLDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, srcMLDirectory)</c></param>
        /// <param name="srcMLDirectory">The directory to store the SrcML files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingSrcML">If True, any existing SrcML files in <see cref="AbstractArchive.ArchivePath"/> will be used. If False, these files will be deleted and potentially recreated.</param>
        /// <param name="generator">The SrcMLGenerator to use to convert source files to SrcML.</param>
        /// <param name="xmlMapping">The XmlFileNameMapping to use to map source paths to xml file paths.</param>
        /// <param name="scheduler">The task scheduler to for asynchronous tasks</param>
        public SrcMLArchive(string baseDirectory, string srcMLDirectory, bool useExistingSrcML, SrcMLGenerator generator, AbstractFileNameMapping mapping, TaskScheduler scheduler)
            : base(baseDirectory, srcMLDirectory, useExistingSrcML, generator, mapping, scheduler) { }

        /// <summary>
        /// Enumerates over each file in the archive and returns a file unit
        /// </summary>
        public IEnumerable<XElement> FileUnits {
            get {
                return from xmlFileName in GetArchivedFiles()
                       select SrcMLElement.Load(xmlFileName);
            }
        }

        protected override void OnFileChanged(FileEventRaisedArgs e) {
            if(e.EventType != FileEventType.FileDeleted) {
                e.HasSrcML = true;
            }
            base.OnFileChanged(e);
        }

        /// <summary>
        /// Check if the file extension is in the set of file types that can be processed by SrcML.NET.
        /// </summary>
        /// <param name="filePath">The file name to check.</param>
        /// <returns>True if the file can be converted to SrcML; False otherwise.</returns>
        public bool IsValidFileExtension(string filePath) {
            string fileExtension = Path.GetExtension(filePath);
            if(fileExtension != null && Generator.ExtensionMapping.ContainsKey(fileExtension)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the XElement for the specified source file. If the SrcML does not already exist in the archive, it will be created.
        /// </summary>
        /// <param name="sourceFilePath">The source file to get the root XElement for.</param>
        /// <returns>The root XElement of the source file.</returns>
        public virtual XElement GetXElementForSourceFile(string sourceFilePath) {
            if(!File.Exists(sourceFilePath)) {
                return null;
            } else {
                string xmlPath = GetArchivePath(sourceFilePath);
                
                if(!File.Exists(xmlPath)) {
                    return null;
                }
                return SrcMLElement.Load(xmlPath);
            }
        }
    }
}
