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

        //TODO: should type be included in this interface?


        /// <summary>
        /// Returns the children of this entity that have the given name.
        /// This method searches only the immediate children, and not further descendants.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        IEnumerable<INamedEntity> GetNamedChildren(string name);

        /// <summary>
        /// Returns the children of this entity that have the given name, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="name">The name to search for.</param>
        IEnumerable<T> GetNamedChildren<T>(string name) where T : INamedEntity;

        /// <summary>
        /// Returns the locations where this entity appears in the source.
        /// </summary>
        IEnumerable<SrcMLLocation> GetLocations();
    }
}
