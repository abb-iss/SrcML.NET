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

        /// <summary> The statement containing this expression. </summary>
        public override Statement ParentStatement {
            get { return base.ParentStatement; }
            set {
                base.ParentStatement = value;
                foreach(var param in TypeParameters) { param.ParentStatement = value; }
            }
        }

        /// <summary>
        /// Adds a generic type parameter to this type use
        /// </summary>
        /// <param name="typeParameter">The type parameter to add</param>
        public void AddTypeParameter(TypeUse typeParameter) {
            if(typeParameter == null) { throw new ArgumentNullException("typeParameter"); }
            typeParameter.ParentExpression = this;
            typeParameter.ParentStatement = this.ParentStatement;
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
        /// Returns the child expressions, including the TypeParameters.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return TypeParameters.Concat(base.GetChildren());
        }

        /// <summary>
        /// Finds all of the matches for this type
        /// </summary>
        /// <returns>All of the type definitions that match this type use</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            if(ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }

            // if this is a built-in type, then just return that 
            // otherwise, go hunting for matching types
            if(BuiltInTypeFactory.IsBuiltIn(this)) {
                return Enumerable.Repeat(BuiltInTypeFactory.GetBuiltIn(this), 1);
            }

            //If there's a prefix, resolve that and search under results
            if(Prefix != null) {
                return Prefix.FindMatches().SelectMany(ns => ns.GetNamedChildren<TypeDefinition>(this.Name));
            }

            //If there's a calling expression, match and search under results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                IEnumerable<TypeDefinition> matches = Enumerable.Empty<TypeDefinition>();
                foreach(var scope in callingScopes) {
                    var localMatches = scope.GetNamedChildren<TypeDefinition>(this.Name).ToList();
                    var callingType = scope as TypeDefinition;
                    if(!localMatches.Any() && callingType != null) {
                        //also search under the base types of the calling scope
                        matches = matches.Concat(callingType.SearchParentTypes<TypeDefinition>(this.Name, e => true));
                    } else {
                        matches = matches.Concat(localMatches);
                    }
                }
                return matches;
            }

            //handle C# var keyword
            if(Name == "var" && ProgrammingLanguage == Language.CSharp) {
                var varDecl = ParentExpression as VariableDeclaration;
                if(varDecl != null) {
                    if(varDecl.Initializer != null) {
                        return varDecl.Initializer.ResolveType();
                    }
                    if(varDecl.Range != null) {
                        //TODO: update to determine type of items within the collection and return that
                        return varDecl.Range.ResolveType();
                    }
                    return Enumerable.Empty<TypeDefinition>();
                }
            }

            //search enclosing scopes and base types
            foreach(var scope in ParentStatement.GetAncestors<NamedScope>()) {
                var matches = scope.GetNamedChildren<TypeDefinition>(this).Where(SignatureMatches).ToList();
                if(matches.Any()) {
                    return matches;
                }
                var typeDef = scope as TypeDefinition;
                if(typeDef != null) {
                    var baseTypeMatches = typeDef.SearchParentTypes<TypeDefinition>(this.Name, SignatureMatches).ToList();
                    if(baseTypeMatches.Any()) {
                        return baseTypeMatches;
                    }
                }
            }

            //search if there is an alias for this name
            foreach(var alias in GetAliases()) {
                if(alias.AliasName == this.Name) {
                    var targetName = alias.Target as NameUse;
                    if(targetName == null) {
                        //Target is not a NameUse, probably an Expression
                        targetName = alias.Target.GetDescendantsAndSelf<NameUse>().LastOrDefault();
                    }
                    if(targetName != null) {
                        return targetName.FindMatches().OfType<TypeDefinition>();
                    }
                }
            }

            //we didn't find it locally, search under imported namespaces
            return (from import in GetImports()
                    from match in import.ImportedNamespace.GetDescendantsAndSelf<NameUse>().Last().FindMatches().OfType<NamedScope>()
                    from child in match.GetNamedChildren<TypeDefinition>(this.Name)
                    select child);
        }

        /// <summary>
        /// Finds TypeDefinitions that match this use.
        /// </summary>
        public override IEnumerable<INamedEntity> FindMatches() {
            return ResolveType();
        }

        /// <summary>
        /// Tests if this type use matches the signature for the given <paramref name="definition"/>.
        /// </summary>
        /// <param name="definition">the definition to compare to</param>
        /// <returns>true if the signatures match; false otherwise</returns>
        public bool SignatureMatches(TypeDefinition definition) {
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