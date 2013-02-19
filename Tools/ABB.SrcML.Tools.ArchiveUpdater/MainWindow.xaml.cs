using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ABB.SrcML;
using FSPath = System.IO.Path;

namespace ABB.SrcML.Tools.ArchiveUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SrcMLArchive _archive;
        private FileSystemFolderMonitor _monitor;
        public MainWindow()
        {
            var archivePath = FSPath.Combine(".", "ARCHIVE");
            _monitor = new FileSystemFolderMonitor(".", archivePath, new LastModifiedArchive(archivePath, "lastmodified.txt"));
            _archive = new SrcMLArchive(FSPath.Combine(_monitor.MonitorStorage, "srcml"));
            _monitor.RegisterNonDefaultArchive(_archive);
            _archive.FileChanged += _archive_SourceFileChanged;

            _monitor.StartMonitoring();

            InitializeComponent();
        }

        void _archive_SourceFileChanged(object sender, FileEventRaisedArgs e)
        {
            textBox1.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                new Action(() =>
                    {
                        ////if (SourceEventType.Renamed == e.EventType)
                        if (FileEventType.FileRenamed== e.EventType)
                            textBox1.AppendText(String.Format("{0} {1} to {2}\n", e.EventType, e.OldSourceFilePath, e.SourceFilePath));
                        else
                            textBox1.AppendText(String.Format("{0} {1}\n", e.EventType, e.SourceFilePath));
                    }));
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e) {
            _monitor.Dispose();
        }
    }
}
