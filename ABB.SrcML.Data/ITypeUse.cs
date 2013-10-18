using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface ITypeUse : IUse<ITypeDefinition>, IResolvesToType {

        bool IsGeneric { get; }

        INamedScopeUse Prefix { get; set; }

        ReadOnlyCollection<ITypeUse> TypeParameters { get; }

        void AddTypeParameter(ITypeUse typeParameter);

        void AddTypeParameters(IEnumerable<ITypeUse> typeParameters);
    }
}