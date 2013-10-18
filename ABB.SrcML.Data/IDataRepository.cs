using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    public interface IDataRepository : IDisposable {

        event EventHandler<ErrorRaisedArgs> ErrorRaised;

        event EventHandler<FileEventRaisedArgs> FileProcessed;

        event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        ISrcMLArchive Archive { get; }

        string FileName { get; }

        IScope GlobalScope { get; }

        bool IsReady { get; }

        void AddFile(string sourceFile);

        void AddFile(XElement fileUnitElement);

        void Clear();

        Collection<IMethodCall> FindMethodCalls(SourceLocation loc);

        Collection<IMethodCall> FindMethodCalls(string xpath);

        Collection<IMethodCall> FindMethodCalls(XElement element);

        IScope FindScope(SourceLocation loc);

        IScope FindScope(string xpath);

        IScope FindScope(XElement element);

        void InitializeData();

        void InitializeDataConcurrent();

        void InitializeDataConcurrent(TaskScheduler scheduler);

        void Load(string fileName);

        void RemoveFile(string sourceFile);

        void Save();

        void Save(string fileName);
    }
}