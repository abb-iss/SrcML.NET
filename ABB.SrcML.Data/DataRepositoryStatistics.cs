using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ABB.SrcML.Data {
    public class DataRepositoryStatistics {
        private TextWriter _synchronizedOut;
        private TextWriter _synchronizedError;
        private Dictionary<FileEventType, ThreadSafeCounter> fileEventCounter;
        private ConcurrentDictionary<string, ConcurrentBag<string>> errorMap;
        private int numberOfErrors;
        
        public DataRepository Data { get; set; }

        public int ErrorCount { get { return numberOfErrors; } }

        public IEnumerable<string> Errors { get { return errorMap.Keys; } }

        /// <summary>
        /// Used to log informational messages from <see cref="Data"/>. The default stream is <see cref="Console.Out"/>.
        /// Out is threadsafe via the <see cref="TextWriter.Synchronized(TextWriter)"/>. Setting this to null disables informational logging.
        /// </summary>
        public TextWriter Out {
            get { return _synchronizedOut; }
            set { _synchronizedOut = (null == value ? value : TextWriter.Synchronized(value)); }
        }

        /// <summary>
        /// /// Used to log error messages from <see cref="Data"/>. The default stream is <see cref="Console.Error"/>.
        /// Error is threadsafe via the <see cref="TextWriter.Synchronized(TextWriter)"/>.
        /// Setting this to null disables error logging.
        /// </summary>
        public TextWriter Error {
            get { return _synchronizedError; }
            set { _synchronizedError = (null == value ? value : TextWriter.Synchronized(value)); }
        }

        public DataRepositoryStatistics(DataRepository data) {
            if(null == data) { throw new ArgumentNullException("data"); }
            numberOfErrors = 0;
            Data = data;

            Out = Console.Out;
            Error = Console.Error;

            errorMap = new ConcurrentDictionary<string, ConcurrentBag<string>>();            
            SetupCounterDictionary();

            ConnectToArchive();
        }

        public int CountOf(FileEventType eventType) {
            return fileEventCounter[eventType].Count;
        }

        public IEnumerable<string> GetLocationsForError(string error) {
            ConcurrentBag<string> locations;
            if(errorMap.TryGetValue(error, out locations)) {
                return locations;
            }
            return Enumerable.Empty<string>();
        }

        private void WriteError(string value) { if(null != Error) { Error.WriteLine(value); } }

        private void WriteOut(string value) { if(null != Out) { Out.WriteLine(value); } }

        private void SetupCounterDictionary() {
            fileEventCounter = new Dictionary<FileEventType, ThreadSafeCounter>();
            foreach(var eventType in Enum.GetValues(typeof(FileEventType)).Cast<FileEventType>()) {
                fileEventCounter[eventType] = new ThreadSafeCounter();
            }
        }

        private void ConnectToArchive() {
            Data.ErrorRaised += Data_ErrorRaised;
            Data.FileProcessed += Data_FileProcessed;
        }

        private void DisconnectFromArchive() {
            Data.ErrorRaised -= Data_ErrorRaised;
            Data.FileProcessed -= Data_FileProcessed;
        }

        void Data_ErrorRaised(object sender, ErrorRaisedArgs e) {
            Interlocked.Increment(ref numberOfErrors);
            ParseException pe = e.Exception as ParseException;

            if(null != pe) {
                Exception keyException = (null != pe.InnerException ? pe.InnerException : pe);

                var key = keyException.StackTrace.Split('\n')[0].Trim();
                int errorLineNumber = (pe.LineNumber < 1 ? 1 : pe.LineNumber);
                int errorColumnNumber = (pe.ColumnNumber < 1 ? 1 : pe.ColumnNumber);
                string errorLocation = String.Format("{0}({1},{2})", pe.FileName, errorLineNumber, errorColumnNumber);

                var fileList = errorMap.GetOrAdd(key, new ConcurrentBag<string>());
                fileList.Add(errorLocation);
                WriteError(String.Format("ERROR {0} {1}", key, errorLocation));
            }
        }

        void Data_FileProcessed(object sender, FileEventRaisedArgs e) {
            fileEventCounter[e.EventType].Increment();
            WriteOut(String.Format("{0} {1}", e.EventType, e.FilePath));
        }

        private class ThreadSafeCounter {
            int _counter;

            public int Count { get { return _counter; } }

            public ThreadSafeCounter() {
                _counter = 0;
            }

            public int Increment() {
                return Interlocked.Increment(ref _counter);
            }

            public int Decrement() {
                return Interlocked.Decrement(ref _counter);
            }
        }
    }
}
