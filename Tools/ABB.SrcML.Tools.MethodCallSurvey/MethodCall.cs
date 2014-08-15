using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using CallData = ABB.SrcML.Data.MethodCall;
using MethodData = ABB.SrcML.Data.MethodDefinition;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    public class MethodCall : INotifyPropertyChanged, ILocatable {
        private SrcMLArchive Archive;
        private CallData Data;
        private bool firstMatchIsValid;
        private bool hasMatches;
        private bool hasNoMatches;
        private bool isExternal;
        private int numberOfValidMatches;

        public MethodCall(SrcMLArchive archive, CallData data) {
            this.Archive = archive;
            this.Data = data;

            this.firstMatchIsValid = false;
            this.hasMatches = false;
            this.hasNoMatches = false;
            this.isExternal = false;

            this.Location = data.Location;
            //TODO fix this once new type hierarchy is in place
            //this.FullName = data.ParentScope.GetParentScopesAndSelf<NamedScope>().First().GetFullName();
            this.Id = DataHelpers.GetLocation(data.Location);
            this.Path = this.Location.SourceFileName;

            this.numberOfValidMatches = 0;
            this.SourceCode = GetSourceCode();
            PossibleMatches = new ObservableCollection<MethodDefinition>(GetMatches());
            StartMonitoringDefinitions();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool FirstMatchIsValid {
            get { return firstMatchIsValid; }
            set {
                if(value != firstMatchIsValid) {
                    this.firstMatchIsValid = value;
                    OnPropertyChanged("FirstMatchIsValid");
                }
            }
        }

        public string FullName { get; private set; }

        public bool HasMatches {
            get { return this.hasMatches; }
            set {
                if(value != hasMatches) {
                    hasMatches = value;
                    if(hasMatches) {
                        IsExternal = false;
                        HasNoMatches = false;
                    }
                    OnPropertyChanged("HasMatches");
                }
            }
        }

        public bool HasNoMatches {
            get { return this.hasNoMatches; }
            set {
                if(value != hasNoMatches) {
                    hasNoMatches = value;
                    if(hasNoMatches) {
                        SetMatchValidity(false);
                        IsExternal = false;
                    }
                    OnPropertyChanged("HasNoMatches");
                }
            }
        }

        public string Id {
            get;
            private set;
        }

        public bool IsExternal {
            get { return this.isExternal; }
            set {
                if(value != isExternal) {
                    isExternal = value;
                    if(isExternal) {
                        SetMatchValidity(false);
                        HasNoMatches = false;
                    }
                    OnPropertyChanged("IsExternal");
                }
            }
        }

        public SrcMLLocation Location { get; private set; }

        public int NumberOfValidMatches {
            get { return numberOfValidMatches; }
            set {
                if(value != numberOfValidMatches) {
                    this.numberOfValidMatches = value;
                    this.HasMatches = (this.numberOfValidMatches > 0);
                    OnPropertyChanged("NumberOfValidMatches");
                }
            }
        }

        public string Path { get; private set; }

        public ObservableCollection<MethodDefinition> PossibleMatches { get; private set; }

        public string SourceCode { get; private set; }

        public void SetMatchValidity(bool validValue) {
            foreach(var match in PossibleMatches) {
                match.IsValidForCall = validValue;
            }
        }

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private IEnumerable<MethodDefinition> GetMatches() {
            throw new NotImplementedException();
            //var matches = (from method in Data.FindMatches()
            //               select new MethodDefinition(Archive, method, this)).Distinct(new MethodDefinitionComparer());
            //return matches;
        }

        private string GetSourceCode() {
            var element = DataHelpers.GetElement(Archive, Data.Location);

            return element.ToSource();
        }

        private void method_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName.Equals("IsValidForCall")) {
                var method = sender as MethodDefinition;
                if(method != null) {
                    NumberOfValidMatches += (method.IsValidForCall ? 1 : -1);
                }
            }
        }

        private void method0_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName.Equals("IsValidForCall")) {
                var method = sender as MethodDefinition;
                if(method != null) {
                    FirstMatchIsValid = method.IsValidForCall;
                }
            }
        }

        private void StartMonitoringDefinitions() {
            MethodDefinition first = null;
            foreach(var method in PossibleMatches) {
                if(first == null) {
                    first = method;
                    method.PropertyChanged += method0_PropertyChanged;
                }
                method.PropertyChanged += method_PropertyChanged;
            }
        }
    }
}