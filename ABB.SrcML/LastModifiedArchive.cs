using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML {

    /// <summary>
    /// The last modified archive simply stores the last-modified times of all its files. It
    /// serializes them to disk upon <see cref="LastModifiedArchive.Dispose()">disposal</see>
    /// </summary>
    public class LastModifiedArchive : AbstractArchive {
        // private readonly object mapLock = new object();
        private volatile bool _changed;

        /// <summary>
        /// The default file name to store this archive in
        /// </summary>
        public const string DEFAULT_FILENAME = "lastmodifiedmap.txt";

        //private Dictionary<string, DateTime> lastModifiedMap;
        private ConcurrentDictionary<string, DateTime> lastModifiedMap;

        /// <summary>
        /// Creates a last modified archive that will be stored in <see cref="DEFAULT_FILENAME"/> within <see cref="Environment.CurrentDirectory"/>
        /// </summary>
        public LastModifiedArchive() : this(Environment.CurrentDirectory) { }

        /// <summary>
        /// Creates a new archive in the
        /// <paramref name="baseDirectory">specified directory</paramref> with a default file name.
        /// </summary>
        /// <param name="baseDirectory">The directory to save the map to</param>
        public LastModifiedArchive(string baseDirectory)
            : this(baseDirectory, DEFAULT_FILENAME) {
        }

        /// <summary>
        /// Creates a new archive in the
        /// <paramref name="baseDirectory">specified directory</paramref> with the given
        /// <paramref name="fileName"/></summary>
        /// <param name="baseDirectory">the directory that this archive will be stored in</param>
        /// <param name="fileName">the filename to store the mapping in</param>
        public LastModifiedArchive(string baseDirectory, string fileName)
            : this(baseDirectory, fileName, TaskScheduler.Default) { }

        /// <summary>
        /// Creates a new archive in the
        /// <paramref name="baseDirectory">specified directory</paramref> with the given
        /// <paramref name="fileName"/></summary>
        /// <param name="baseDirectory">the directory that this archive will be stored in</param>
        /// <param name="fileName">the filename to store the mapping in</param>
        /// <param name="scheduler">The task factory to use for asynchronous methods</param>
        public LastModifiedArchive(string baseDirectory, string fileName, TaskScheduler scheduler)
            : base(baseDirectory, fileName, scheduler) {
                lastModifiedMap = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            ReadMap();
            _changed = false;
        }

        /// <summary>
        /// Returns true if there are no entries in this last modified archive
        /// </summary>
        public override bool IsEmpty {
            get { return this.lastModifiedMap.Count == 0; }
        }
        /// <summary>
        /// Returns a collection of all supported file extensions.
        /// </summary>
        public override ICollection<string> SupportedExtensions {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Checks if the given file name is present in the archive
        /// </summary>
        /// <param name="fileName">The file name to test for</param>
        /// <returns>True if the file is in the archive; false otherwise</returns>
        public override bool ContainsFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            //lock(mapLock) {
            return lastModifiedMap.ContainsKey(fullPath);
            //}
        }

        /// <summary>
        /// saves this archive to disk
        /// </summary>
        public override void Dispose() {
            SaveMap();
            base.Dispose();
        }

        /// <summary>
        /// Gets all of the files stored in the archive
        /// </summary>
        /// <returns>the files in the archive</returns>
        public override Collection<string> GetFiles() {
            Collection<string> fileNames = new Collection<string>();
            //lock(mapLock) {
            foreach(var fileName in lastModifiedMap.Keys) {
                fileNames.Add(fileName);
            }
            //}
            return fileNames;
        }

        public virtual DateTime GetLastModifiedTime(string fileName) {
            if(ContainsFile(fileName)) {
                return lastModifiedMap[GetFullPath(fileName)];
            }
            return DateTime.MaxValue;
        }

        /// <summary>
        /// Checks if the archive is outdated in comparison to the original file. A file is outdated
        /// if any of the following are true: <list type="bullet"> <item><description>the file does
        /// not exist and it is in the archive</description></item> <item><description>the file is
        /// not in the archive and it exists</description></item> <item><description>The last
        /// modified time in the archive is more recent than
        /// <paramref name="fileName"/></description></item> </list>
        /// </summary>
        /// <param name="fileName">the file to check</param>
        /// <returns>True if the file is outdated; false otherwise</returns>
        public override bool IsOutdated(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool fileNameExists = File.Exists(fullPath);
            bool fileIsInArchive;
            DateTime lastModified = (fileNameExists ? File.GetLastWriteTime(fullPath) : DateTime.MinValue);
            DateTime lastModifiedInArchive;

            fileIsInArchive = this.lastModifiedMap.TryGetValue(fullPath, out lastModifiedInArchive);
            if(!fileIsInArchive) {
                lastModifiedInArchive = DateTime.MinValue;
            }

            return !(fileNameExists == fileIsInArchive && lastModified <= lastModifiedInArchive);
        }

        /// <summary>
        /// Loads this map from disk (assuming <see cref="AbstractArchive.ArchivePath"/> exists)
        /// </summary>
        public void ReadMap() {
            if(File.Exists(this.ArchivePath)) {
                foreach(var line in File.ReadLines(this.ArchivePath)) {
                    var parts = line.Split('|');
                    this.lastModifiedMap[parts[0]] = new DateTime(Int64.Parse(parts[1]));
                }
            }
        }

        public override void Save() {
            SaveMap();
        }

        /// <summary>
        /// Saves this map to disk (at <see cref="AbstractArchive.ArchivePath"/>
        /// </summary>
        public void SaveMap() {
            bool isChanged = _changed;

            if(isChanged) {
                var mapCopy = new Dictionary<string, DateTime>(lastModifiedMap, StringComparer.OrdinalIgnoreCase);
                _changed = false;

                var tempFileName = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ".txt"));

                using(var output = File.CreateText(tempFileName)) {
                    foreach(var kvp in mapCopy) {
                        output.WriteLine("{0}|{1}", kvp.Key, kvp.Value.Ticks);
                    }
                }
                File.Copy(tempFileName, this.ArchivePath, true);
                File.Delete(tempFileName);
            }
        }

        /// <summary>
        /// Adds or updates
        /// <paramref name="fileName"/>to the archive. It raises
        /// <see cref="AbstractArchive.FileChanged"/> with <see cref="FileEventType.FileChanged"/>
        /// (if the file was in the archive) or <see cref="FileEventType.FileAdded"/>.
        /// </summary>
        /// <param name="fileName">The file name to add</param>
        protected override FileEventType? AddOrUpdateFileImpl(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool fileAlreadyExists = ContainsFile(fileName);
            lastModifiedMap[fullPath] = File.GetLastWriteTime(fullPath);
            _changed = true;
            return (fileAlreadyExists ? FileEventType.FileChanged : FileEventType.FileAdded);
        }

        /// <summary>
        /// Deletes the given
        /// <paramref name="fileName"/>from the archive. It raises
        /// <see cref="AbstractArchive.FileChanged"/> with <see cref="FileEventType.FileDeleted"/>
        /// if the file was in the archive.
        /// </summary>
        /// <param name="fileName">The file to delete</param>
        protected override bool DeleteFileImpl(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool mapContainsFile = true;

            mapContainsFile = lastModifiedMap.ContainsKey(fullPath);
            DateTime result;
            if(lastModifiedMap.TryRemove(fullPath, out result)) {
                _changed = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Renames filename from
        /// <paramref name="oldFileName"/>to
        /// <paramref name="newFileName"/>. If
        /// <paramref name="oldFileName"/>is in the archive, then
        /// <see cref="AbstractArchive.FileChanged"/> is raised with
        /// <see cref="FileEventType.FileRenamed"/>. Otherwise, this method simply calls
        /// <see cref="AddOrUpdateFileImpl(string)"/>
        /// </summary>
        /// <param name="oldFileName">the old file path</param>
        /// <param name="newFileName">the new file path</param>
        protected override bool RenameFileImpl(string oldFileName, string newFileName) {
            string oldFullPath = GetFullPath(oldFileName);
            string newFullPath = GetFullPath(newFileName);

            DateTime result;
            lastModifiedMap.TryRemove(oldFullPath, out result);
            lastModifiedMap[newFullPath] = File.GetLastWriteTime(newFullPath);
            _changed = true;
            return true;
        }

        /// <summary>
        /// Gets the full path for a file name (returns the file name if
        /// <see cref="Path.IsPathRooted(string)"/> is true.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetFullPath(string fileName) {
            return (Path.IsPathRooted(fileName) ? fileName : Path.GetFullPath(fileName));
        }
    }
}
