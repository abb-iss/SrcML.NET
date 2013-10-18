using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IMethodDefinition : INamedScope {

        bool IsConstructor { get; set; }

        bool IsDestructor { get; set; }

        ReadOnlyCollection<IParameterDeclaration> Parameters { get; }

        ITypeUse ReturnType { get; set; }

        void AddMethodParameter(IParameterDeclaration parameter);

        void AddMethodParameters(IEnumerable<IParameterDeclaration> parameters);

        bool ContainsCallTo(IMethodDefinition callee);

        IEnumerable<MethodCall> GetCallsTo(IMethodDefinition callee);
    }
}