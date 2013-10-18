using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IMethodDefinition : INamedScope {

        bool IsConstructor { get; set; }

        bool IsDestructor { get; set; }

        ReadOnlyCollection<ParameterDeclaration> Parameters { get; }

        TypeUse ReturnType { get; set; }

        void AddMethodParameter(ParameterDeclaration parameter);

        void AddMethodParameters(IEnumerable<ParameterDeclaration> parameters);

        bool ContainsCallTo(IMethodDefinition callee);

        IEnumerable<MethodCall> GetCallsTo(IMethodDefinition callee);
    }
}