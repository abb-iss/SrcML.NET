using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {

    public interface IRootedObject {

        /// <summary>
        /// The parent scope for this calling object
        /// </summary>
        IScope ParentScope { get; set; }
    }
}