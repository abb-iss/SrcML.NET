using System;

namespace ABB.SrcML.Data {

    public interface INamespaceDefinition : INamedScope {

        bool IsAnonymous { get; }

        bool IsGlobal { get; }

        string MakeQualifiedName(string name);
    }
}