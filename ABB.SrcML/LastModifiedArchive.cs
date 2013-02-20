using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    /// <summary>
    /// The last modified archive simply stores the last-modified times of all its files.
    /// It serializes them to disk upon <see cref="LastModifiedArchive.Dispose()">disposal</see>
    /// </summary>
    public class LastModifiedArchive : AbstractArchive {
        
        private Dictionary<string, DateTime> lastModifiedMap;
        private readonly object mapLock = new object();

        public LastModifiedArchive(string baseDirectory)
            : this(baseDirectory, "lastmodifiedmap.txt") {
        }
        /// <summary>
        /// Creates a new archive in the <paramref name="storageDirectory">specified directory</paramref> with the given <paramref name="fileName"/>
        /// </summary>
        /// <param name="baseDirectory">the directory that this archive will be stored in</param>
        /// <param name="fileName">the filename to store the mapping in</param>
        public LastModifiedArchive(string baseDirectory, string fileName)
            : base(baseDirectory, fileName) {
            lastModifiedMap = new Dictionary<string, DateTime>(StringComparer.InvariantCultureIgnoreCase);
            ReadMap();
        }

        public override ICollection<string> SupportedExtensions {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Adds or updates <paramref name="fileName"/> to the archive. It raises <see cref="AbstractArchive.FileChanged"/> with
        /// <see cref="FileEventType.FileChanged"/> (if the file was in the archive) or <see cref="FileEventType.FileAdded"/>.
        /// </summary>
        /// <param name="fileName">The file name to add</param>
        public override void AddOrUpdateFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            FileEventType eventType;
            lock(mapLock) {
                eventType = (lastModifiedMap.ContainsKey(fullPath) ? FileEventType.FileChanged : FileEventType.FileAdded);
                lastModifiedMap[fullPath] = File.GetLastWriteTime(fullPath);
            }

            OnFileChanged(new FileEventRaisedArgs(eventType, fullPath));
        }

        /// <summary>
        /// Deletes the given <paramref name="fileName"/> from the archive. It raises <see cref="AbstractArchive.FileChanged"/> with
        /// <see cref="FileEventType.FileDeleted"/> if the file was in the archive.
        /// </summary>
        /// <param name="fileName">The file to delete</param>
        public override void DeleteFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool mapContainsFile = true;
            lock(mapLock) {
                mapContainsFile = lastModifiedMap.ContainsKey(fullPath);
                if(mapContainsFile) {
                    lastModifiedMap.Remove(fullPath);
                }
            }
            if(mapContainsFile) {
                OnFileChanged(new FileEventRaisedArgs(FileEventType.FileDeleted, fullPath));
            }
        }

        /// <summary>
        /// Renames filename from <paramref name="oldFileName"/> to <paramref name="newFileName"/>. If <paramref name="oldFileName"/> is
        /// in the archive, then <see cref="AbstractArchive.FileChanged"/> is raised with <see cref="FileEventType.FileRenamed"/>. Otherwise, this method simply calls <see cref="AddOrUpdateFile(string)"/>
        /// </summary>
        /// <param name="oldFileName">the old file path</param>
        /// <param name="newFileName">the new file path</param>
        public override void RenameFile(string oldFileName, string newFileName) {
            string oldFullPath = GetFullPath(oldFileName);
            string newFullPath = GetFullPath(newFileName);

            bool mapContainsFile = true;
            lock(mapLock) {
                mapContainsFile = lastModifiedMap.ContainsKey(oldFullPath);
                if(mapContainsFile) {
                    lastModifiedMap.Remove(oldFullPath);
                    lastModifiedMap[newFullPath] = File.GetLastWriteTime(newFullPath);
                }
            }
            if(mapContainsFile) {
                OnFileChanged(new FileEventRaisedArgs(FileEventType.FileRenamed, newFullPath, oldFullPath));
            } else {
                AddOrUpdateFile(newFullPath);
            }
        }

        /// <summary>
        /// Checks if the given file name is present in the archive
        /// </summary>
        /// <param name="fileName">The file name to test for</param>
        /// <returns>True if the file is in the archive; false otherwise</returns>
        public override bool ContainsFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            lock(mapLock) {
                return lastModifiedMap.ContainsKey(fullPath);
            }
        }

        /// <summary>
        /// Checks if the archive is outdated in comparison to the original file. A file is outdated if any of the following are true:
        /// <list type="bullet">
        /// <item><description>the file does not exist and it is in the archive</description></item>
        /// <item><description>the file is not in the archive and it exists</description></item>
        /// <item><description>The last modified time in the archive is more recent than <paramref name="fileName"/></description></item>
        /// </list>
        /// </summary>
        /// <param name="fileName">the file to check</param>
        /// <returns>True if the file is outdated; false otherwise</returns>
        public override bool IsOutdated(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool fileNameExists = File.Exists(fullPath);
            bool fileIsInArchive;
            DateTime lastModified = (fileNameExists ? File.GetLastWriteTime(fullPath) : DateTime.MinValue);
            DateTime lastModifiedInArchive;

            lock(mapLock) {
                fileIsInArchive = this.lastModifiedMap.TryGetValue(fullPath, out lastModifiedInArchive);
                if(!fileIsInArchive) {
                    lastModifiedInArchive = DateTime.MinValue;
                }
            }

            return !(fileNameExists == fileIsInArchive && lastModified <= lastModifiedInArchive);
        }

        /// <summary>
        /// Gets all of the files stored in the archive
        /// </summary>
        /// <returns>the files in the archive</returns>
        public override IEnumerable<string> GetFiles() {
            Collection<string> fileNames = new Collection<string>();
            lock(mapLock) {
                foreach(var fileName in lastModifiedMap.Keys) {
                    fileNames.Add(fileName);
                }
            }
            return fileNames;
        }

        /// <summary>
        /// saves this archive to disk
        /// </summary>
        public override void Dispose() {
            SaveMap();
            base.Dispose();
        }

        /// <summary>
        /// Loads this map from disk (assuming <see cref="AbstractArchive.ArchivePath"/> exists)
        /// </summary>
        public void ReadMap() {
            if(File.Exists(this.ArchivePath)) {
                lock(mapLock) {
                    this.lastModifiedMap.Clear();
                    foreach(var line in File.ReadLines(this.ArchivePath)) {
                        var parts = line.Split('|');
                        this.lastModifiedMap[parts[0]] = DateTime.Parse(parts[1]);
                    }
                }
            }
        }

        /// <summary>
        /// Saves this map to disk (at <see cref="AbstractArchive.ArchivePath"/>
        /// </summary>
        public void SaveMap() {
            if(File.Exists(this.ArchivePath)) {
                File.Delete(this.ArchivePath);
            }

            using(var output = File.CreateText(this.ArchivePath)) {
                lock(mapLock) {
                    foreach(var kvp in lastModifiedMap) {
                        output.WriteLine("{0}|{1}", kvp.Key, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the full path for a file name (returns the file name if <see cref="Path.IsPathRooted(string)"/> is true.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetFullPath(string fileName) {
            return (Path.IsPathRooted(fileName) ? fileName : Path.GetFullPath(fileName));
        }
    }
}
