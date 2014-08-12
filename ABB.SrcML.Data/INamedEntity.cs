using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a programming construct that has a name, e.g. a method or a field.
    /// </summary>
    public interface INamedEntity {
        /// <summary> The name of the entity. </summary>
        string Name { get; set; }

        /// <summary> The accessibility of the entity, e.g. public or private. </summary>
        AccessModifier Accessibility { get; set; }

        /// <summary>
        /// Returns the locations where this entity appears in the source.
        /// </summary>
        IEnumerable<SrcMLLocation> GetLocations();
    }
}
