using System;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface INamedScope : IScope {

        AccessModifier Accessibility { get; set; }

        string Id { get; }

        string Name { get; set; }

        Collection<INamedScopeUse> ParentScopeCandidates { get; set; }

        INamedScopeUse UnresolvedParentScopeInUse { get; set; }

        string GetFullName();

        INamedScope Merge(INamedScope otherScope);

        INamedScopeUse SelectUnresolvedScope();
    }
}