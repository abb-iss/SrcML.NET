/******************************************************************************
 * Copyright (c) 2014 ABB Group
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML {
    /// <summary>
    /// keeps track of files by using the provided <see cref="Generator"/>. The files in the archive have their last-write time set to the corresponding file on disk
    /// </summary>
    public class GeneratorArchive : AbstractArchive {
        private AbstractFileNameMapping _fileMapping;

        /// <summary>
        /// The generator to use to power this archive
        /// </summary>
        public AbstractGenerator Generator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseDirectory">The parent directory of <paramref name="archiveDirectory"/>. <see cref="AbstractArchive.ArchivePath"/> will be set to <c>Path.Combine(baseDirectory, archiveDirectory)</c></param>
        /// <param name="archiveDirectory">The directory to store the archive files in</param>
        /// <param name="useExistingArchive">if true, use any files found in the archive directory. Otherwise, delete them</param>
        /// <param name="generator">The generator to use</param>
        /// <param name="mapping">The file name mapping</param>
        /// <param name="scheduler">The task scheduler for asynchronous tasks</param>
        public GeneratorArchive(string baseDirectory, string archiveDirectory, bool useExistingArchive, AbstractGenerator generator, AbstractFileNameMapping mapping, TaskScheduler scheduler)
        : base(baseDirectory, archiveDirectory, TaskScheduler.Default) {
            _fileMapping = mapping;
            Generator = generator;
            if(!Directory.Exists(this.ArchivePath)) {
                Directory.CreateDirectory(this.ArchivePath);
            } else if(!useExistingArchive) {
                foreach(var fileName in GetArchivedFiles().ToList()) {
                    File.Delete(fileName);
                }
            }
        }

        /// <summary>
        /// Uses <see cref="Generator"/> to generate output for <paramref name="sourcePath"/>
        /// </summary>
        /// <param name="sourcePath">the input file</param>
        public void GenerateOutputForSource(string sourcePath) {
            var archivePath = GetArchivePath(sourcePath);
            
            var directory = Path.GetDirectoryName(archivePath);
            if(!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var lastWriteTime = File.GetLastWriteTime(sourcePath);

            var tempFileName = Path.GetTempFileName();
            this.Generator.GenerateFromFile(sourcePath, tempFileName);

            for(int i = 0; i < 10; i++) {
                try {
                    File.Copy(tempFileName, archivePath, true);
                    File.SetLastWriteTime(archivePath, lastWriteTime);
                } catch(IOException) {
                    Thread.Sleep(10);
                }
            }
            File.Delete(tempFileName);
        }

        /// <summary>
        /// Gets the archive path for <paramref name="sourcePath"/>
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <returns>The full path within the archive for <paramref name="sourcePath"/></returns>
        public string GetArchivePath(string sourcePath) {
            return _fileMapping.GetTargetPath(sourcePath);
        }

        /// <summary>
        /// Gets the source path for an archive path
        /// </summary>
        /// <param name="archivePath">A path within the archive</param>
        /// <returns>The path on disk that corresponds to <paramref name="archivePath"/></returns>
        public string GetSourcePath(string archivePath) {
            return _fileMapping.GetSourcePath(archivePath);
        }

        /// <summary>
        /// Gets all of the archived files in this archive.
        /// </summary>
        /// <returns>An enumerable of the stored files</returns>
        protected IEnumerable<string> GetArchivedFiles() {
            return _fileMapping.GetTargetFiles();
        }

        #region AbstractArchive members
        /// <summary>
        /// Returns true if this archive is empty
        /// </summary>
        public override bool IsEmpty {
            get { return 0 == GetArchivedFiles().Count(); }
        }

        /// <summary>
        /// The list of extensions supported by the archive (taken from <see cref="Generator"/>)
        /// </summary>
        public override ICollection<string> SupportedExtensions {
            get { return Generator.SupportedExtensions; }
        }

        /// <summary>
        /// Uses <see cref="Generator"/> to create a copy of the file in the archive.
        /// If the file already exists in the archive, <see cref="FileEventType.FileChanged"/> is returned
        /// Otherwise, <see cref="FileEventType.FileAdded"/> is returned.
        /// </summary>
        /// <param name="fileName">The file to add or update</param>
        /// <returns>The file event type for for this operation. If null, then the method failed.</returns>
        protected override FileEventType? AddOrUpdateFileImpl(string fileName) {
            bool fileAlreadyExists = this.ContainsFile(fileName);
            if(File.Exists(fileName)) {
                GenerateOutputForSource(fileName);
                return (fileAlreadyExists ? FileEventType.FileChanged : FileEventType.FileAdded);
            }
            return null;
        }

        /// <summary>
        /// Deletes <paramref name="fileName"/> from the archive.
        /// </summary>
        /// <param name="fileName">The file to delete</param>
        /// <returns>True if the file was successfully removed from the archive</returns>
        protected override bool DeleteFileImpl(string fileName) {
            var archivePath = GetArchivePath(fileName);
            if(File.Exists(archivePath)) {
                File.Delete(archivePath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Renames the file from <paramref name="oldFileName"/> to <paramref name="newFileName"/> in the archive.
        /// </summary>
        /// <param name="oldFileName">The old file name</param>
        /// <param name="newFileName">The new file name</param>
        /// <returns>True if the rename succeeds. False otherwise.</returns>
        protected override bool RenameFileImpl(string oldFileName, string newFileName) {
            var oldArchivePath = GetArchivePath(oldFileName);
            var newAchivePath = GetArchivePath(newFileName);

            if(File.Exists(oldArchivePath)) {
                File.Delete(oldArchivePath);
            }
            if(File.Exists(newFileName)) {
                GenerateOutputForSource(newFileName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to see if the file is in the archive
        /// </summary>
        /// <param name="fileName">The file to check for</param>
        /// <returns>true if <paramref name="fileName"/> is in the archive; false otherwise</returns>
        public override bool ContainsFile(string fileName) {
            var archivePath = GetArchivePath(fileName);
            return File.Exists(archivePath);
        }

        /// <summary>
        /// Checks if the archive is up to date with respect to <paramref name="fileName"/>.
        /// If the file is <see cref="ContainsFile">not in the archive</see>, it is outdated.
        /// </summary>
        /// <param name="fileName">The file name to check for</param>
        /// <returns>true if the archive is outdated</returns>
        public override bool IsOutdated(string fileName) {
            var sourceFileInfo = new FileInfo(fileName);
            var archivePath = GetArchivePath(fileName);
            var archiveFileInfo = new FileInfo(archivePath);

            return sourceFileInfo.Exists != archiveFileInfo.Exists || sourceFileInfo.LastWriteTime != archiveFileInfo.LastWriteTime;
        }

        /// <summary>
        /// Gets all of the source file names stored in this archive
        /// </summary>
        /// <returns>A collection of the file names stored in this archive</returns>
        public override Collection<string> GetFiles() {

            var sourceFiles = from archivePath in GetArchivedFiles()
                              select GetSourcePath(archivePath);

            return new Collection<string>(sourceFiles.ToList());
        }

        /// <summary>
        /// Saves the file name mapping to disk
        /// </summary>
        public override void Save() {
            _fileMapping.SaveMapping();
        }
        #endregion AbstractArchive members

        #region IDisposable members
        /// <summary>
        /// Disposes of the internal <see cref="AbstractFileNameMapping"/> and then calls <see cref="AbstractArchive.Dispose()"/>
        /// </summary>
        public override void Dispose() {
            _fileMapping.Dispose();
            base.Dispose();
        }
        #endregion IDisposable members
    }
}
