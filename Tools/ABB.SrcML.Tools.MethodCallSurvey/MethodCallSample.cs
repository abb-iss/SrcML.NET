using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    public class MethodCallSample : INotifyPropertyChanged {
        private ObservableCollection<MethodCall> _callSampleCollection;
        private MethodCall _currentCall;
        private ObservableCollection<MethodDefinition> _currentMatchCollection;
        private DateTime _dateCreated;
        private string _projectName;
        private int _sampleSize;

        public MethodCallSample(int sampleSize)
            : this(null, null, sampleSize) {
        }

        public MethodCallSample(SrcMLArchive archive, AbstractWorkingSet data, int sampleSize) {
            this.Archive = archive;
            this.Data = data;
            this.Date = DateTime.Now;
            this._projectName = string.Empty;
            this.SampleSize = sampleSize;

            FirstMatchIsValid = new Statistic("FirstMatchIsValid", SampleSize);
            HasMatches = new Statistic("HasMatches", SampleSize);
            HasNoMatches = new Statistic("HasNoMatches", SampleSize);
            IsExternal = new Statistic("IsExternal", SampleSize);

            FirstMatchIsValid.PropertyChanged += StatisticChanged;
            HasMatches.PropertyChanged += StatisticChanged;
            HasNoMatches.PropertyChanged += StatisticChanged;
            IsExternal.PropertyChanged += StatisticChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SrcMLArchive Archive { get; set; }

        public ObservableCollection<MethodCall> CallSample {
            get { return this._callSampleCollection; }
            set {
                if(value != _callSampleCollection) {
                    _callSampleCollection = value;
                    OnPropertyChanged("CallSample");
                }
            }
        }

        public MethodCall CurrentCall {
            get { return this._currentCall; }
            set {
                if(value != _currentCall) {
                    _currentCall = value;
                    OnPropertyChanged("CurrentCall");
                }
            }
        }

        public ObservableCollection<MethodDefinition> CurrentMatches {
            get { return this._currentMatchCollection; }
            set {
                if(value != _currentMatchCollection) {
                    _currentMatchCollection = value;
                    OnPropertyChanged("CurrentMatches");
                }
            }
        }

        public AbstractWorkingSet Data { get; set; }

        public DateTime Date {
            get { return this._dateCreated; }
            set {
                if(value != _dateCreated) {
                    _dateCreated = value;
                    OnPropertyChanged("Date");
                }
            }
        }

        public Statistic FirstMatchIsValid { get; private set; }

        public Statistic HasMatches { get; private set; }

        public Statistic HasNoMatches { get; private set; }

        public Statistic IsExternal { get; private set; }

        public string ProjectName {
            get { return this._projectName; }
            set {
                if(value != _projectName) {
                    _projectName = value;
                    OnPropertyChanged("ProjectName");
                }
            }
        }

        public int SampleSize {
            get { return this._sampleSize; }
            set {
                if(value != _sampleSize) {
                    _sampleSize = value;
                    OnPropertyChanged("SampleSize");
                }
            }
        }

        public ObservableCollection<MethodCall> GetSampleOfMethodCalls(BackgroundWorker worker) {
            // TODO reimplement once the MethodCalls property has been added
            //try {
            //    NamespaceDefinition globalScope;
            //    if(Data.TryLockGlobalScope(Timeout.Infinite, out globalScope)) {
            //        var allCalls = from scope in globalScope.GetDescendants()
            //                       from call in scope.MethodCalls
            //                       select call;
            //        int numberOfCalls = allCalls.Count();

            //        if(null != worker) {
            //            worker.ReportProgress(0, String.Format("Found {0} calls", numberOfCalls));
            //        }

            //        var rng = new Random();
            //        var randomCallSample = allCalls.OrderBy(x => rng.Next()).Take(SampleSize);

            //        var calls = from call in randomCallSample
            //                    select new MethodCall(Archive, call);

            //        var callCollection = new ObservableCollection<MethodCall>(calls.Take(SampleSize));
            //        if(null != worker) {
            //            worker.ReportProgress(100, String.Format("Showing {0} / {1} calls", SampleSize, numberOfCalls));
            //        }
            //        return callCollection;
            //    }
            //} finally {
            //    Data.ReleaseGlobalScopeLock();
            //}
            //return null;
            throw new NotImplementedException();
        }

        public void StartMonitoringCalls() {
            FirstMatchIsValid.Connect(this);
            HasMatches.Connect(this);
            HasNoMatches.Connect(this);
            IsExternal.Connect(this);
        }

        public void WriteStatistics(string fileName) {
            List<string> contents = new List<string>();
            if(File.Exists(fileName)) {
            }
            contents.Add(String.Format(""));
            File.WriteAllLines(fileName, contents);
        }

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void StatisticChanged(object sender, PropertyChangedEventArgs e) {
            OnPropertyChanged((sender as Statistic).Name);
        }
    }
}