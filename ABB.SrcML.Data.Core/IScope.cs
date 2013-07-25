using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data.Core {
    public interface IScope {
        string Id { get; }
        Language ProgrammingLanguage { get; set; }
        IScope ParentScope { get; set; }
        IEnumerable<IScope> ChildScopes { get; }
        IEnumerable<IVariableDeclaration> DeclaredVariables { get; }
        IEnumerable<IMethodCall> MethodCalls { get; }
        SrcMLLocation PrimaryLocation { get; }
        IEnumerable<SrcMLLocation> Locations { get; }

        IEnumerable<T> GetChildScopes<T>() where T : IScope;
        IEnumerable<T> GetDescendantScopes<T>() where T : IScope;

    }
}
