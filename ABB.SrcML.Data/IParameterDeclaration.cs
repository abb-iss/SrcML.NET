using System;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IParameterDeclaration : IVariableDeclaration {

        bool HasDefaultValue { get; set; }

        Collection<SrcMLLocation> Locations { get; }
    }
}