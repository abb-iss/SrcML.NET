using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using MethodData = ABB.SrcML.Data.MethodDefinition;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    public class MethodDefinition : INotifyPropertyChanged, ILocatable {
        private SrcMLArchive Archive;
        private MethodData Data;
        private bool isValid;

        public MethodDefinition(SrcMLArchive archive, MethodData data, MethodCall fromCall) {
            this.Archive = archive;
            this.SourceCall = fromCall;
            this.Data = data;

            this.isValid = false;

            this.Location = data.PrimaryLocation;
            this.FullName = Data.GetFullName();
            this.Id = DataHelpers.GetLocation(Location);
            this.Path = Location.SourceFileName;

            this.Signature = GetMethodSignature();
        }

        protected MethodDefinition() {
            this.isValid = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string FullName { get; protected set; }

        public string Id {
            get;
            private set;
        }

        public bool IsValidForCall {
            get { return this.isValid; }
            set {
                if(value != isValid) {
                    isValid = value;
                    OnPropertyChanged("IsValidForCall");
                }
            }
        }

        public SrcMLLocation Location { get; private set; }

        public string Path {
            get;
            private set;
        }

        public string Signature { get; protected set; }

        public MethodCall SourceCall { get; protected set; }

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private string GetMethodSignature() {
            var methodElement = DataHelpers.GetElement(Archive, Data.PrimaryLocation);
            if(methodElement.Element(SRC.Block) != null) {
                methodElement.Element(SRC.Block).Remove();
            }
            return methodElement.ToSource().Trim();
        }
    }
}