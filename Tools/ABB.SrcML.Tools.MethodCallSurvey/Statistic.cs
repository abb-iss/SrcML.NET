using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    public class Statistic : INotifyPropertyChanged {
        private int _count;
        private int _sampleSize;
        private double _value;

        public Statistic(string name, int sampleSize) {
            this._count = 0;
            this.SampleSize = sampleSize;
            this.Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count {
            get { return this._count; }
            set {
                if(value != _count) {
                    _count = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        public string Name { get; private set; }

        public int SampleSize {
            get { return this._sampleSize; }
            set {
                if(value != _sampleSize) {
                    _sampleSize = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        public double Value {
            get {
                return _sampleSize == 0 ? 0 : (double) Count / (double) SampleSize * 100.0;
            }
        }

        public static bool? GetPropertyValue(object source, string propertyName) {
            var type = source.GetType();
            if(type != null) {
                var property = type.GetProperty(propertyName);
                if(property != null) {
                    return property.GetValue(source, null) as bool?;
                }
            }
            return null;
        }

        public void Connect(MethodCallSample sample) {
            sample.PropertyChanged += sample_PropertyChanged;
            foreach(var call in sample.CallSample) {
                call.PropertyChanged += call_PropertyChanged;
            }
        }

        public override string ToString() {
            return this.Value.ToString();
        }

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void call_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName == this.Name) {
                var newValue = GetPropertyValue(sender, this.Name);
                if(newValue != null) {
                    this.Count += (newValue == true ? 1 : -1);
                }
            }
        }

        private void sample_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if(e.PropertyName == "SampleSize") {
                var sample = (sender as MethodCallSample);
                if(sample != null) {
                    this.SampleSize = sample.SampleSize;
                }
            }
        }
    }
}