using System;

namespace ABB.SrcML.Data {

    public interface IVariableDeclaration : IRootedObject {

        AccessModifier Accessibility { get; set; }

        SrcMLLocation Location { get; set; }

        string Name { get; set; }

        ITypeUse VariableType { get; set; }
    }
}