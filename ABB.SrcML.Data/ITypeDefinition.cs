using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface ITypeDefinition : INamedScope {

        bool IsPartial { get; set; }

        TypeKind Kind { get; set; }

        ReadOnlyCollection<ITypeUse> ParentTypes { get; }

        void AddParentType(ITypeUse parentTypeUse);

        IEnumerable<ITypeDefinition> GetParentTypes();

        IEnumerable<ITypeDefinition> GetParentTypesAndSelf();
    }
}