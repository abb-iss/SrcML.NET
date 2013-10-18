using System;
using System.Collections.Generic;

namespace ABB.SrcML.Data {

    public interface IAlias {

        INamedScopeUse ImportedNamedScope { get; set; }

        INamedScopeUse ImportedNamespace { get; set; }

        bool IsNamespaceImport { get; }

        SrcMLLocation Location { get; set; }

        Language ProgrammingLanguage { get; set; }

        IEnumerable<INamespaceDefinition> FindMatchingNamespace(INamespaceDefinition rootScope);

        string GetFullName();

        string GetNamespaceName();

        bool IsAliasFor(INamedScope namedScope);

        bool IsAliasFor<DEFINITION>(IUse<DEFINITION> use) where DEFINITION : class;
    }
}