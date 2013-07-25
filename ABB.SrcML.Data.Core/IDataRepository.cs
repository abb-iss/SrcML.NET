using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Core
{
    public interface IDataRepository
    {
        ISrcMLArchive Archive { get; }
        bool IsReady { get; }

        event EventHandler<FileEventRaisedArgs> FileProcessed;
        event EventHandler<ErrorRaisedArgs> ErrorRaised;

        IScope FindScope(SourceLocation location);

        IScope FindScope(XElement element);

        IScope FindScope(string xpath);

        ICollection<IMethodCall> FindMethodCalls(SourceLocation location);

        ICollection<IMethodCall> FindMethodCalls(XElement element);

        ICollection<IMethodCall> FindMethodCalls(string xpath);
    }
}
