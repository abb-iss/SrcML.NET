/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System.Text;
using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a type definition in a program.
    /// </summary>
    public class TypeDefinition : NamedScope {
        private Collection<TypeUse> parentTypeCollection;

        /// <summary> The XML name for TypeDefinition </summary>
        public new const string XmlName = "Type";

        /// <summary> XML Name for <see cref="Kind" /> </summary>
        public const string XmlKindName = "kind";

        /// <summary> XML Name for <see cref="ParentTypeNames" /> </summary>
        public const string XmlParentTypeNamesName = "ParentTypes";

        /// <summary> XML Name for <see cref="IsPartial" /> </summary>
        public const string XmlIsPartialName = "IsPartial";

        /// <summary> Creates a new type definition object </summary>
        public TypeDefinition()
            : base() {
            parentTypeCollection = new Collection<TypeUse>();
            ParentTypeNames = new ReadOnlyCollection<TypeUse>(parentTypeCollection);
            IsPartial = false;
        }

        /// <summary>
        /// The kind of type this object represents, e.g. class, struct, etc.
        /// </summary>
        public TypeKind Kind { get; set; }

        /// <summary> The parents of this type. </summary>
        public ReadOnlyCollection<TypeUse> ParentTypeNames { get; protected set; }

        /// <summary> Indicates whether this is a partial type. </summary>
        public bool IsPartial { get; set; }

        protected override bool ToBeDeleted { get { return Locations.All(l => l.IsReference); } }

        /// <summary>
        /// Adds <paramref name="parentTypeUse"/>as a parent type for this type definition.
        /// </summary>
        /// <param name="parentTypeUse">The parent type to add</param>
        public void AddParentType(TypeUse parentTypeUse) {
            if(null == parentTypeUse)
                throw new ArgumentNullException("parentTypeUse");

            parentTypeUse.ParentStatement = this;
            parentTypeCollection.Add(parentTypeUse);
        }

        /// <summary>
        /// Instance method for getting <see cref="TypeDefinition.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for TypeDefinition</returns>
        public override string GetXmlName() { return TypeDefinition.XmlName; }

        public override Statement Merge(Statement otherStatement) {
            return Merge(otherStatement as TypeDefinition);
        }

        public TypeDefinition Merge(TypeDefinition otherType) {
            if(null == otherType) {
                throw new ArgumentNullException("otherType");
            }
            TypeDefinition combinedType = Merge<TypeDefinition>(this, otherType);
            combinedType.Kind = this.Kind;
            combinedType.IsPartial = this.IsPartial;
            TypeDefinition typeWithParents = null;
            if(this.ParentTypeNames.Count > 0) {
                typeWithParents = this;
            } else if(otherType.ParentTypeNames.Count > 0) {
                typeWithParents = otherType;
            }

            if(null != typeWithParents) {
                foreach(var parentType in typeWithParents.ParentTypeNames) {
                    combinedType.AddParentType(parentType);
                }
            }

            return combinedType;
        }

        //public override void RemoveFile(string fileName) {
        //    int definitionLocationCount = RemoveLocations(fileName);
        //    RemoveFile(fileName, 0 == definitionLocationCount);
        //}
        protected override string ComputeMergeId() {
            if(Language.Java == ProgrammingLanguage || Language.CSharp == ProgrammingLanguage && !IsPartial) {
                return base.ComputeMergeId();
            }

            char typeSpecifier;
            switch(Kind) {
                case TypeKind.Class:
                    typeSpecifier = 'C';
                    break;
                case TypeKind.Enumeration:
                    typeSpecifier = 'E';
                    break;
                case TypeKind.Interface:
                    typeSpecifier = 'I';
                    break;
                case TypeKind.Struct:
                    typeSpecifier = 'S';
                    break;
                case TypeKind.Union:
                    typeSpecifier = 'U';
                    break;
                default:
                    typeSpecifier = 'T';
                    break;
            }

            string id = String.Format("{0}:T{1}:{2}", KsuAdapter.GetLanguage(ProgrammingLanguage), typeSpecifier, this.Name);
            return id;
        }
        
        /// <summary>
        /// This handles the "this" keyword, the "base" keyword (C# only), and the "super" keyword (Java only).
        /// It searches for the appropriate type definition depending on the context of the usage.
        /// </summary>
        /// <param name="use">The use to find the containing type for</param>
        /// <returns>The type(s) referred to by the keyword</returns>
        public static IEnumerable<TypeDefinition> GetTypeForKeyword(NameUse use) {
            if(use == null) { throw new ArgumentNullException("use"); }
            if(use.ParentStatement == null) {
                throw new ArgumentException("ParentStatement is null", "use");
            }

            if(use.Name == "this") {
                //return the surrounding type definition
                return use.ParentStatement.GetAncestorsAndSelf<TypeDefinition>().Take(1);
            }
            if((use.Name == "base" && use.ProgrammingLanguage == Language.CSharp) ||
               (use.Name == "super" && use.ProgrammingLanguage == Language.Java)) {
                //return all the parent classes of the surrounding type definition
                var enclosingType = use.ParentStatement.GetAncestorsAndSelf<TypeDefinition>().FirstOrDefault();
                if(enclosingType == null) {
                    return Enumerable.Empty<TypeDefinition>();
                } else {
                    return enclosingType.GetParentTypes(true);
                }
            }

            return Enumerable.Empty<TypeDefinition>();
        }

        /// <summary>
        /// Resolves the parent type uses for this type definition.
        /// This method will only return the first 100 matches.
        /// </summary>
        /// <param name="recursive">Whether or not to recursively get the parents of this type's parents.</param>
        /// <returns>Matching parent types for this type</returns>
        public IEnumerable<TypeDefinition> GetParentTypes(bool recursive) {
            IEnumerable<TypeDefinition> results;
            if(recursive) {
                results = from typeUse in ParentTypeNames
                          from type in typeUse.ResolveType()
                          from nextType in type.GetParentTypesAndSelf(recursive)
                          select nextType;
            } else {
                results = ParentTypeNames.SelectMany(typeUse => typeUse.ResolveType());
            }
            return results.Take(100);
        }

        /// <summary>
        /// Returns this class followed by all of its parent classes (via a call to
        /// <see cref="GetParentTypes(bool)"/>
        /// </summary>
        /// <param name="recursive">Whether or not to recursively get the parents of this type's parents.</param>
        /// <returns>An enumerable consisting of this object followed by the results of <see cref="GetParentTypes(bool)"/></returns>
        public IEnumerable<TypeDefinition> GetParentTypesAndSelf(bool recursive) {
            return Enumerable.Repeat(this, 1).Concat(GetParentTypes(recursive));
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(Prefix != null) {
                yield return Prefix;
            }
            //TODO: return type parameters, once added
            foreach(var parent in ParentTypeNames) {
                yield return parent;
            }
        }

        /// <summary>
        /// Searches the parent types of this type for an INamedEntity with the given name.
        /// </summary>
        /// <param name="name">The name of the entity to search for.</param>
        /// <returns>The first matching entity found. In the case where a given parent type contains more than one matching entity, all of them are returned.</returns>
        public IEnumerable<INamedEntity> SearchParentTypes(string name) {
            foreach(var parent in GetParentTypes(true)) {
                var matches = parent.GetNamedChildren(name).ToList();
                if(matches.Any()) {
                    return matches;
                }
            }
            return Enumerable.Empty<INamedEntity>();
        }

        /// <summary>
        /// Searches the parent types of this type for entities with the given name and type, and where the given predicate is true.
        /// </summary>
        /// <typeparam name="T">The type of entities to search for.</typeparam>
        /// <param name="name">The name of the entity to search for.</param>
        /// <param name="predicate">A function to determine whether to return a given entity.</param>
        /// <returns>The first matching entity found. In the case where a given parent type contains more than one matching entity, all of them are returned.</returns>
        public IEnumerable<T> SearchParentTypes<T>(string name, Func<T, bool> predicate) where T : INamedEntity {
            foreach(var parent in GetParentTypes(true)) {
                var matches = parent.GetNamedChildren<T>(name).Where(predicate).ToList();
                if(matches.Any()) {
                    return matches;
                }
            }
            return Enumerable.Empty<T>();
        }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            var attribute = reader.GetAttribute(XmlIsPartialName);
            if(null != attribute) {
                IsPartial = XmlConvert.ToBoolean(attribute);
            }
            Kind = TypeKindExtensions.FromKeyword(reader.GetAttribute(XmlKindName));
            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlParentTypeNamesName == reader.Name) {
                foreach(var parentType in XmlSerialization.ReadChildExpressions(reader).Cast<TypeUse>()) {
                    AddParentType(parentType);
                }
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes XML attributes from this object to the XML writer
        /// </summary>
        /// <param name="writer">The XML writer</param>
        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString(XmlKindName, Kind.ToKeyword());
            if(IsPartial) {
                writer.WriteAttributeString(XmlIsPartialName, XmlConvert.ToString(IsPartial));
            }
            base.WriteXmlAttributes(writer);
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != ParentTypeNames) {
                XmlSerialization.WriteCollection<TypeUse>(writer, XmlParentTypeNamesName, ParentTypeNames);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            var signature = new StringBuilder();
            if(Accessibility != AccessModifier.None) { signature.AppendFormat("{0} ", Accessibility.ToKeywordString()); }
            if(IsPartial) { signature.Append("partial "); }
            signature.AppendFormat("{0} ", Kind.ToKeyword());
            signature.Append(Name);
            var parentsString = string.Join(", ", ParentTypeNames);
            if(!string.IsNullOrEmpty(parentsString)) {
                signature.AppendFormat(" : {0}", parentsString);
            }
            return signature.ToString();
        }
    }
}