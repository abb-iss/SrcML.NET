using ABB.SrcML.Data;
using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FilePath = System.IO.Path;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private BackgroundWorker loadWorker;
        private BackgroundWorker sampleWorker;

        public MainWindow() {
            InitializeComponent();

            this.Sample = new MethodCallSample(25);
            this.DataContext = this.Sample;

            this.Editor = @"C:\Program Files (x86)\Notepad++\notepad++.exe";

            loadWorker = new BackgroundWorker();
            loadWorker.WorkerReportsProgress = true;
            loadWorker.DoWork += loadWorker_DoWork;
            loadWorker.ProgressChanged += ProgressChanged;
            loadWorker.RunWorkerCompleted += loadWorker_RunWorkerCompleted;

            sampleWorker = new BackgroundWorker();
            sampleWorker.WorkerReportsProgress = true;
            sampleWorker.DoWork += sampleWorker_DoWork;
            sampleWorker.ProgressChanged += ProgressChanged;
            sampleWorker.RunWorkerCompleted += sampleWorker_RunWorkerCompleted;
        }

        public string Editor { get; set; }

        public MethodCallSample Sample { get; private set; }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e) {
            Control control = sender as Control;
            if(null != control) {
                var l = control.DataContext as ILocatable;
                Process p = new Process();

                if(null != Editor && System.IO.File.Exists(Editor)) {
                    p.StartInfo.FileName = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
                    p.StartInfo.Arguments = String.Format("-nosession -ro -n{0} -c{1} \"{2}\"", l.Location.StartingLineNumber, l.Location.StartingColumnNumber, l.Path);
                } else {
                    p.StartInfo.FileName = l.Path;
                }

                p.Start();
            }
            e.Handled = true;
        }

        private void CallList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedCall = CallList.SelectedItem as MethodCall;
            if(null != selectedCall) {
                Sample.CurrentMatches = selectedCall.PossibleMatches;
            }
        }

        private void LoadArchive(BackgroundWorker worker, string pathToMappingFile, out SrcMLArchive archive, out AbstractWorkingSet data, out string projectName) {
            throw new NotImplementedException();
            //string pathToArchive = FilePath.GetDirectoryName(pathToMappingFile);
            //string archiveDirectoryName = FilePath.GetFileName(pathToArchive);
            //string baseDirectory = FilePath.GetFullPath(FilePath.GetDirectoryName(pathToArchive));

            //projectName = FilePath.GetFileName(baseDirectory);
            //worker.ReportProgress(0, String.Format("Loading {0}", projectName));
            //archive = new SrcMLArchive(baseDirectory, archiveDirectoryName);
            //int numberOfFiles = archive.FileUnits.Count();
            //worker.ReportProgress(0, String.Format("Loading {0} ({1} files)", projectName, numberOfFiles));

            //data = new DataRepository(archive);
            //int i = 0;
            //foreach(var unit in archive.FileUnits) {
            //    try {
            //        data.AddFile(unit);
            //    } catch(Exception) {
            //    }

            //    if(++i % 25 == 0) {
            //        int percentComplete = (int) (100 * (double) i / (double) numberOfFiles);
            //        worker.ReportProgress(percentComplete, String.Format("Loading {0} ({1} / {2} files)", projectName, i, numberOfFiles));
            //    }
            //}
            //worker.ReportProgress(100, String.Format("Loaded {0} ({1} files)", baseDirectory, i));
        }

        private void loadWorker_DoWork(object sender, DoWorkEventArgs e) {
            throw new NotImplementedException();
            //SrcMLArchive archive;
            //DataRepository data;
            //string projectName;
            //LoadArchive(sender as BackgroundWorker, e.Argument as string, out archive, out data, out projectName);
            //string commonPath = FileHelper.GetCommonPath(archive.GetFiles());
            //List<object> results = new List<object>();
            //results.Add(archive);
            //results.Add(data);
            //results.Add(projectName);
            //e.Result = results;
        }

        private void loadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            throw new NotImplementedException();
            //List<object> results = e.Result as List<object>;
            //Sample.Archive = results[0] as SrcMLArchive;
            //Sample.Data = results[1] as DataRepository;
            //Sample.ProjectName = results[2] as string;
            //StatusBarProgress.Value = 0;
            //sampleWorker.RunWorkerAsync(Sample);
        }

        private void MenuItemOpenArchive_Click(object sender, RoutedEventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "mapping";
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Mapping Files|mapping.txt";

            Nullable<bool> result = dialog.ShowDialog();
            if(result == true) {
                string mappingFileName = dialog.FileName;
                loadWorker.RunWorkerAsync(mappingFileName);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e) {
            StatusBarLabel.Content = e.UserState as string;
            StatusBarProgress.Value = e.ProgressPercentage;
        }

        private void sampleWorker_DoWork(object sender, DoWorkEventArgs e) {
            MethodCallSample sampler = e.Argument as MethodCallSample;

            var samples = sampler.GetSampleOfMethodCalls(sender as BackgroundWorker);
            e.Result = samples;
        }

        private void sampleWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Sample.CallSample = e.Result as ObservableCollection<MethodCall>;
            Sample.StartMonitoringCalls();
            CallList_SelectionChanged(sender, null);
        }
    }
}