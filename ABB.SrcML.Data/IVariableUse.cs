using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {

    public interface IVariableUse : IUse<IVariableDeclaration>, IResolvesToType {
    }
}