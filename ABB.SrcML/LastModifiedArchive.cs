using System;
using System.Collections.Generic;
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
            var eventType = (lastModifiedMap.ContainsKey(fileName) ? FileEventType.FileChanged : FileEventType.FileAdded);
            lastModifiedMap[fileName] = File.GetLastWriteTime(fileName);
            OnFileChanged(new FileEventRaisedArgs(fileName, eventType));
        }

        public override void DeleteFile(string fileName) {
            if(lastModifiedMap.ContainsKey(fileName)) {
                lastModifiedMap.Remove(fileName);
                OnFileChanged(new FileEventRaisedArgs(fileName, FileEventType.FileDeleted));
            }
        }

        public override void RenameFile(string oldFileName, string newFileName) {
            if(lastModifiedMap.ContainsKey(oldFileName)) {
                lastModifiedMap.Remove(oldFileName);
                lastModifiedMap[newFileName] = File.GetLastWriteTime(newFileName);
                OnFileChanged(new FileEventRaisedArgs(oldFileName, newFileName, FileEventType.FileRenamed));
            } else {
                AddOrUpdateFile(newFileName);
            }
        }

        public override bool ContainsFile(string fileName) {
            return lastModifiedMap.ContainsKey(fileName);
        }

        public override bool IsOutdated(string fileName) {
            DateTime lastModified = File.GetLastWriteTime(fileName);
            DateTime lastModifiedInArchive;
            
            if(!lastModifiedMap.TryGetValue(fileName, out lastModifiedInArchive)) {
                return true;
            }
            return lastModified > lastModifiedInArchive;
        }

        public override IEnumerable<string> GetFiles() {
            return lastModifiedMap.Keys;
        }

        public override void Dispose() {
            base.Dispose();
        }

        public void ReadMap() {

        }

        public void SaveMap() {
        }
    }
}
