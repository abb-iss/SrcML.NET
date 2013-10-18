using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IMethodCall : IUse<IMethodDefinition>, IResolvesToType {

        Collection<IResolvesToType> Arguments { get; set; }

        bool IsConstructor { get; set; }

        bool IsDestructor { get; set; }

        IEnumerable<string> GetPossibleNames();
    }
}