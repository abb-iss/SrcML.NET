using System;

namespace ABB.SrcML.Data {

    public interface IVariableDeclaration {

        AccessModifier Accessibility { get; set; }

        SrcMLLocation Location { get; set; }

        string Name { get; set; }

        IScope Scope { get; set; }

        ITypeUse VariableType { get; set; }
    }
}