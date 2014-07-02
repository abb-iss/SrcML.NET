/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a use of a type. It is used in declarations and inheritance specifications, among other places.
    /// </summary>
    public class TypeUse : NameUse {
        private List<TypeUse> typeParameterList;

        /// <summary> The XML name for TypeUse </summary>
        public new const string XmlName = "tu";

        /// <summary> XML Name for <see cref="TypeParameters" /> </summary>
        public const string XmlTypeParametersName = "TypeParameters";

        /// <summary> Create a new type use object. </summary>
        public TypeUse() {
            Name = String.Empty;
            typeParameterList = new List<TypeUse>();
            TypeParameters = new ReadOnlyCollection<TypeUse>(this.typeParameterList);
        }

        
        /// <summary>
        /// Returns true if <see cref="TypeParameters"/> has any elements
        /// </summary>
        public bool IsGeneric { get { return typeParameterList.Count > 0; } }

        /// <summary>
        /// Parameters for the type use (indicates that this is a generic type use)
        /// </summary>
        public ReadOnlyCollection<TypeUse> TypeParameters { get; private set; }

        /// <summary>
        /// Adds a generic type parameter to this type use
        /// </summary>
        /// <param name="typeParameter">The type parameter to add</param>
        public void AddTypeParameter(TypeUse typeParameter) {
            if(typeParameter == null) { throw new ArgumentNullException("typeParameter"); }
            typeParameter.ParentExpression = this;
            typeParameterList.Add(typeParameter);
        }

        /// <summary>
        /// Adds all of the given type parameters to this type use element
        /// </summary>
        /// <param name="typeParameters">An enumerable of type use elements to add</param>
        public void AddTypeParameters(IEnumerable<TypeUse> typeParameters) {
            typeParameterList.AddRange(typeParameters);
        }


        /// <summary>
        /// Finds all of the matches for this type
        /// </summary>
        /// <returns>All of the type definitions that match this type use</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            //TODO: review this method and update it for changes in TypeUse structure
            //TODO: handle case of C# var type. Delete CSharpVarTypeUse if no longer necessary.
            throw new NotImplementedException();

            //// if this is a built-in type, then just return that 
            //// otherwise, go hunting for matching types
            //if(BuiltInTypeFactory.IsBuiltIn(this)) {
            //    yield return BuiltInTypeFactory.GetBuiltIn(this);
            //}

            //var aliases = GetAliases();

            //if(null != Prefix) {
            //    var matches = from prefixMatch in Prefix.FindMatches()
            //                  from match in prefixMatch.GetChildScopesWithId<TypeDefinition>(this.Name)
            //                  select match;
            //    foreach(var match in matches) {
            //        yield return match;
            //    }
            //} else {
            //    // First, just call AbstractUse.FindMatches() this will search everything in
            //    // ParentScope.GetParentScopesAndSelf<TypeDefinition>() for a matching type and
            //    // return it
            //    foreach(var match in base.FindMatches()) {
            //        yield return match;
            //    }
            //}
        }

        

        /// <summary>
        /// Tests if this type use is a match for the given
        /// <paramref name="definition"/></summary>
        /// <param name="definition">the definition to compare to</param>
        /// <returns>true if the definitions match; false otherwise</returns>
        public bool Matches(TypeDefinition definition) {
            //TODO: add checking for type arguments
            return definition != null && definition.Name == this.Name;
        }

        /// <summary>
        /// Instance method for getting <see cref="TypeUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for TypeUse</returns>
        public override string GetXmlName() { return TypeUse.XmlName; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            if(Prefix != null) {
                sb.Append(Prefix);
                //sb.Append('.');
            }

            sb.Append(Name);

            if(IsGeneric) {
                sb.AppendFormat("<{0}>", String.Join(",", TypeParameters));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlTypeParametersName == reader.Name) {
                AddTypeParameters(XmlSerialization.ReadChildExpressions(reader).Cast<TypeUse>());
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(TypeParameters.Count > 0) {
                XmlSerialization.WriteCollection<TypeUse>(writer, XmlTypeParametersName, TypeParameters);
            }
            base.WriteXmlContents(writer);
        }
    }
}