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

        public LastModifiedArchive(string storageDirectory, string fileName)
        : this(Path.Combine(storageDirectory, fileName)) {
            
        }

        public LastModifiedArchive(string fileName)
            : base(fileName) {
                lastModifiedMap = new Dictionary<string, DateTime>(StringComparer.InvariantCultureIgnoreCase);
        }

        public override ICollection<string> SupportedExtensions {
            get { throw new NotImplementedException(); }
        }

        public override void AddOrUpdateFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            FileEventType eventType;
            lock(mapLock) {
                eventType = (lastModifiedMap.ContainsKey(fullPath) ? FileEventType.FileChanged : FileEventType.FileAdded);
                lastModifiedMap[fullPath] = File.GetLastWriteTime(fullPath);
            }

            OnFileChanged(new FileEventRaisedArgs(fullPath, eventType));
        }

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
                OnFileChanged(new FileEventRaisedArgs(fullPath, FileEventType.FileDeleted));
            }
        }

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
                OnFileChanged(new FileEventRaisedArgs(oldFullPath, newFullPath, FileEventType.FileRenamed));
            } else {
                AddOrUpdateFile(newFullPath);
            }
        }

        public override bool ContainsFile(string fileName) {
            string fullPath = GetFullPath(fileName);
            lock(mapLock) {
                return lastModifiedMap.ContainsKey(fullPath);
            }
        }

        public override bool IsOutdated(string fileName) {
            string fullPath = GetFullPath(fileName);
            bool fileNameExists = File.Exists(fullPath);
            bool fileIsInArchive;
            DateTime lastModified = File.GetLastWriteTime(fullPath);
            DateTime lastModifiedInArchive;

            lock(mapLock) {
                fileIsInArchive = this.lastModifiedMap.TryGetValue(fullPath, out lastModifiedInArchive);
            }

            return !(fileNameExists && fileIsInArchive && lastModified <= lastModifiedInArchive);
        }

        public override IEnumerable<string> GetFiles() {
            Collection<string> fileNames = new Collection<string>();
            lock(mapLock) {
                foreach(var fileName in lastModifiedMap.Keys) {
                    fileNames.Add(fileName);
                }
            }
            return fileNames;
        }

        public override void Dispose() {
            base.Dispose();
        }

        public void ReadMap() {

        }

        public void SaveMap() {
        }

        private string GetFullPath(string fileName) {
            return (Path.IsPathRooted(fileName) ? fileName : Path.GetFullPath(fileName));
        }
    }
}
