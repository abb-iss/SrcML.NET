using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    public class SrcMLArchiveMonitor : AbstractFileMonitor {
        public SrcMLArchive MonitoredArchive { get; private set; }

        public SrcMLArchiveMonitor(string baseDirectory, SrcMLArchive monitoredArchive, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : this(TaskScheduler.Default, baseDirectory, monitoredArchive, defaultArchive, otherArchives) { }

        public SrcMLArchiveMonitor(TaskScheduler scheduler, string baseDirectory, SrcMLArchive monitoredArchive, AbstractArchive defaultArchive, params AbstractArchive[] otherArchives)
            : base(scheduler, baseDirectory, defaultArchive, otherArchives) {
            MonitoredArchive = monitoredArchive;
        }

        public override IEnumerable<string> EnumerateMonitoredFiles() {
            return MonitoredArchive.GetFiles();
        }

        public override void StartMonitoring() {
            MonitoredArchive.FileChanged += MonitoredArchive_FileChanged;
        }

        public override void StopMonitoring() {
            MonitoredArchive.FileChanged -= MonitoredArchive_FileChanged;
            base.StopMonitoring();
        }

        void MonitoredArchive_FileChanged(object sender, FileEventRaisedArgs e) {
            Task task = null;
            switch(e.EventType) {
                case FileEventType.FileAdded:
                    task = AddFileAsync(e.FilePath);
                    break;
                case FileEventType.FileChanged:
                    task = UpdateFileAsync(e.FilePath);
                    break;
                case FileEventType.FileDeleted:
                    task = DeleteFileAsync(e.FilePath);
                    break;
                case FileEventType.FileRenamed:
                    task = RenameFileAsync(e.OldFilePath, e.FilePath);
                    break;
            }
            task.ContinueWith((t) => OnFileChanged(e));
        }
    }
}
