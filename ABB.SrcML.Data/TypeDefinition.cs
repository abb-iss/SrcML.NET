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

        /// <summary> XML Name for <see cref="ParentTypes" /> </summary>
        public const string XmlParentTypesName = "ParentTypes";

        /// <summary> XML Name for <see cref="IsPartial" /> </summary>
        public const string XmlIsPartialName = "IsPartial";

        /// <summary> Creates a new type definition object </summary>
        public TypeDefinition()
            : base() {
            parentTypeCollection = new Collection<TypeUse>();
            ParentTypes = new ReadOnlyCollection<TypeUse>(parentTypeCollection);
            IsPartial = false;
        }

        /// <summary>
        /// The kind of type this object represents, e.g. class, struct, etc.
        /// </summary>
        public TypeKind Kind { get; set; }

        /// <summary> The parents of this type. </summary>
        public ReadOnlyCollection<TypeUse> ParentTypes { get; protected set; }

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
            if(this.ParentTypes.Count > 0) {
                typeWithParents = this;
            } else if(otherType.ParentTypes.Count > 0) {
                typeWithParents = otherType;
            }

            if(null != typeWithParents) {
                foreach(var parentType in typeWithParents.ParentTypes) {
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
        /// This handles the "base" keyword (C# only) and the "this" keyword. It searches for the
        /// appropriate type definition depending on the context of the
        /// </summary>
        /// <typeparam name="T">The use type</typeparam>
        /// <param name="use">The use to find the containing class for</param>
        /// <returns>The class referred to by the keyword</returns>
        public static IEnumerable<TypeDefinition> GetTypeForKeyword<T>(AbstractUse<T> use) where T : class {
            //TODO: review this method and update it for changes in TypeUse structure
            throw new NotImplementedException();

            //var typeDefinitions = Enumerable.Empty<ITypeDefinition>();
            //if(use.Name == "this") {
            //    typeDefinitions = use.ParentScopes.OfType<ITypeDefinition>().Take(1);
            //} else if(use.Name == "base" && use.ProgrammingLanguage == Language.CSharp) {
            //    typeDefinitions = from containingType in use.ParentScopes.OfType<ITypeDefinition>()
            //                      from parentType in containingType.GetParentTypes()
            //                      select parentType;
            //}
            //return typeDefinitions;
        }

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
            if(XmlParentTypesName == reader.Name) {
                foreach(var parentType in XmlSerialization.ReadChildExpressions(reader).Cast<TypeUse>()) {
                    AddParentType(parentType);
                }
            } else {
                base.ReadXmlChild(reader);
            }
        }

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
            if(null != ParentTypes) {
                XmlSerialization.WriteCollection<TypeUse>(writer, XmlParentTypesName, ParentTypes);
            }
            base.WriteXmlContents(writer);
        }
    }


    ///// <summary>
    ///// Represents a type definition
    ///// </summary>
    //[DebuggerTypeProxy(typeof(ScopeDebugView))]
    //[Serializable]
    //public class TypeDefinition : NamedScope, ITypeDefinition {
    //    private Collection<ITypeUse> ParentTypeCollection;

    //    /// <summary>
    //    /// Creates a new type definition object
    //    /// </summary>
    //    public TypeDefinition()
    //        : base() {
    //        // this.ParentTypes = new Collection<TypeUse>();
    //        this.IsPartial = false;
    //        this.ParentTypeCollection = new Collection<ITypeUse>();
    //        this.ParentTypes = new ReadOnlyCollection<ITypeUse>(ParentTypeCollection);
    //    }

    //    /// <summary>
    //    /// Copy constructor
    //    /// </summary>
    //    /// <param name="otherDefinition">The scope to copy from</param>
    //    public TypeDefinition(TypeDefinition otherDefinition)
    //        : base(otherDefinition) {
    //        this.IsPartial = otherDefinition.IsPartial;
    //        this.Kind = otherDefinition.Kind;

    //        this.ParentTypeCollection = new Collection<ITypeUse>();
    //        foreach(var parent in otherDefinition.ParentTypes) {
    //            this.AddParentType(parent);
    //        }
    //        this.ParentTypes = new ReadOnlyCollection<ITypeUse>(this.ParentTypeCollection);
    //    }

    //    /// <summary>
    //    /// Partial if this is a partial class (used in C#)
    //    /// </summary>
    //    public bool IsPartial { get; set; }

    //    /// <summary>
    //    /// The <see cref="TypeKind"/> of this type
    //    /// </summary>
    //    public TypeKind Kind { get; set; }

    //    /// <summary>
    //    /// The parent types that this type inherits from
    //    /// </summary>
    //    public ReadOnlyCollection<ITypeUse> ParentTypes { get; protected set; }

    //    /// <summary>
    //    /// This handles the "base" keyword (C# only) and the "this" keyword. It searches for the
    //    /// appropriate type definition depending on the context of the
    //    /// </summary>
    //    /// <typeparam name="T">The use type</typeparam>
    //    /// <param name="use">The use to find the containing class for</param>
    //    /// <returns>The class referred to by the keyword</returns>
    //    public static IEnumerable<ITypeDefinition> GetTypeForKeyword<T>(IUse<T> use) where T : class {
    //        var typeDefinitions = Enumerable.Empty<ITypeDefinition>();
    //        if(use.Name == "this") {
    //            typeDefinitions = use.ParentScopes.OfType<ITypeDefinition>().Take(1);
    //        } else if(use.Name == "base" && use.ProgrammingLanguage == Language.CSharp) {
    //            typeDefinitions = from containingType in use.ParentScopes.OfType<ITypeDefinition>()
    //                              from parentType in containingType.GetParentTypes()
    //                              select parentType;
    //        }
    //        return typeDefinitions;
    //    }

    //    /// <summary>
    //    /// The AddFrom function adds all of the declarations and children from
    //    /// <paramref name="otherScope"/>to this scope
    //    /// </summary>
    //    /// <param name="otherScope">The scope to add data from</param>
    //    /// <returns>the new scope</returns>
    //    public override IScope AddFrom(IScope otherScope) {
    //        var otherType = otherScope as TypeDefinition;
    //        if(otherType != null) {
    //            foreach(var parent in otherType.ParentTypes) {
    //                this.AddParentType(parent);
    //            }
    //        }
    //        return base.AddFrom(otherScope);
    //    }

    //    /// <summary>
    //    /// Adds
    //    /// <paramref name="parentTypeUse"/>as a parent type for this type definition
    //    /// </summary>
    //    /// <param name="parentTypeUse">The parent type to add</param>
    //    public void AddParentType(ITypeUse parentTypeUse) {
    //        if(null == parentTypeUse)
    //            throw new ArgumentNullException("parentTypeUse");

    //        parentTypeUse.ParentScope = this;
    //        ParentTypeCollection.Add(parentTypeUse);
    //    }

    //    /// <summary>
    //    /// Returns true if both this and
    //    /// <paramref name="otherScope"/>have the same name and are both partial.
    //    /// </summary>
    //    /// <param name="otherScope">The scope to test</param>
    //    /// <returns>true if they are the same class; false otherwise.</returns>
    //    public virtual bool CanBeMergedInto(TypeDefinition otherScope) {
    //        return base.CanBeMergedInto(otherScope) && this.IsPartial && otherScope.IsPartial;
    //    }

    //    /// <summary>
    //    /// Casts
    //    /// <paramref name="otherScope"/>to a <see cref="TypeDefinition"/> and calls
    //    /// <see cref="CanBeMergedInto(TypeDefinition)"/>
    //    /// </summary>
    //    /// <param name="otherScope">The scope to test</param>
    //    /// <returns>true if <see cref="CanBeMergedInto(TypeDefinition)"/> evaluates to
    //    /// true.</returns>
    //    public override bool CanBeMergedInto(NamedScope otherScope) {
    //        return this.CanBeMergedInto(otherScope as TypeDefinition);
    //    }

    //    /// <summary>
    //    /// Resolves all of the parent type uses for this type definition
    //    /// </summary>
    //    /// <returns>Matching parent types for this type</returns>
    //    public IEnumerable<ITypeDefinition> GetParentTypes() {
    //        var results = from typeUse in ParentTypes
    //                      from type in typeUse.FindMatchingTypes()
    //                      from nextType in type.GetParentTypesAndSelf()
    //                      select nextType;
    //        return results.Take(100);
    //    }

    //    /// <summary>
    //    /// Returns this class followed by all of its parent classes (via a call to
    //    /// <see cref="GetParentTypes()"/>
    //    /// </summary>
    //    /// <returns>An enumerable consisting of this object followed by the results of see
    //    /// cref="GetParentTypes()"/></returns>
    //    public IEnumerable<ITypeDefinition> GetParentTypesAndSelf() {
    //        yield return this;
    //        foreach(var parentType in GetParentTypes()) {
    //            yield return parentType;
    //        }
    //    }

    //    /// <summary>
    //    /// Merges this type definition with
    //    /// <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c>
    //    /// evaluates to true.
    //    /// </summary>
    //    /// <param name="otherScope">the scope to merge with</param>
    //    /// <returns>a new type definition from this and otherScope, or null if they couldn't be
    //    /// merged</returns>
    //    public override INamedScope Merge(INamedScope otherScope) {
    //        ITypeDefinition mergedScope = null;
    //        if(otherScope != null) {
    //            if(otherScope.CanBeMergedInto(this)) {
    //                mergedScope = new TypeDefinition(this);
    //                mergedScope.AddFrom(otherScope);
    //                if(mergedScope.Accessibility == AccessModifier.None) {
    //                    mergedScope.Accessibility = otherScope.Accessibility;
    //                }
    //            }
    //        }
    //        return mergedScope as INamedScope;
    //    }

    //    /// <summary>
    //    /// Removes any program elements defined in the given file. If the scope is defined entirely
    //    /// within the given file, then it removes itself from its parent.
    //    /// </summary>
    //    /// <param name="fileName">The file to remove.</param>
    //    /// <returns>A collection of any unresolved scopes that result from removing the file. The
    //    /// caller is responsible for re-resolving these as appropriate.</returns>
    //    public override Collection<IScope> RemoveFile(string fileName) {
    //        Collection<IScope> unresolvedScopes = null;
    //        if(LocationDictionary.ContainsKey(fileName)) {
    //            if(LocationDictionary.Count == 1) {
    //                //this type exists solely in the file to be deleted
    //                if(ParentScope != null) {
    //                    ParentScope.RemoveChild(this);
    //                    ParentScope = null;
    //                }
    //            } else {
    //                ////this type is defined in more than one file, delete only the parts in the given file
    //                //remove children
    //                var unresolvedChildScopes = new List<IScope>();
    //                foreach(var child in ChildScopes.ToList()) {
    //                    var result = child.RemoveFile(fileName);
    //                    if(result != null) {
    //                        unresolvedChildScopes.AddRange(result);
    //                    }
    //                }
    //                //remove method calls
    //                var callsInFile = MethodCallCollection.Where(call => call.Location.SourceFileName == fileName).ToList();
    //                foreach(var call in callsInFile) {
    //                    MethodCallCollection.Remove(call);
    //                }
    //                //remove declared variables
    //                var declsInFile = DeclaredVariablesDictionary.Where(kvp => kvp.Value.Location.SourceFileName == fileName).ToList();
    //                foreach(var kvp in declsInFile) {
    //                    DeclaredVariablesDictionary.Remove(kvp.Key);
    //                }
    //                //remove parent types
    //                var parentsInFile = ParentTypes.Where(parent => parent.Location.SourceFileName == fileName).ToList();
    //                foreach(var parent in parentsInFile) {
    //                    ParentTypeCollection.Remove(parent);
    //                }
    //                //remove parent scope candidates
    //                var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
    //                foreach(var candidate in candidatesInFile) {
    //                    ParentScopeCandidates.Remove(candidate);
    //                }
    //                //update locations
    //                LocationDictionary.Remove(fileName);

    //                if(DefinitionLocations.Any()) {
    //                    //This type is still defined somewhere, so re-add the unresolved children to it
    //                    if(unresolvedChildScopes.Count > 0) {
    //                        foreach(var child in unresolvedChildScopes) {
    //                            AddChildScope(child);
    //                        }
    //                    }
    //                } else {
    //                    //This type is no longer defined, only referenced
    //                    //Return any remaining children to be re-resolved by our parent
    //                    if(MethodCallCollection.Any()) {
    //                        Debug.WriteLine("Found Type containing method calls but with only reference locations!");
    //                        Debug.WriteLine("Type locations:");
    //                        foreach(var loc in LocationDictionary.Values) {
    //                            Debug.WriteLine(loc);
    //                        }
    //                        Debug.WriteLine("Method call locations:");
    //                        foreach(var mc in MethodCallCollection) {
    //                            Debug.WriteLine(mc.Location);
    //                        }
    //                    }
    //                    if(DeclaredVariablesDictionary.Any()) {
    //                        Debug.WriteLine("Found Type containing declared variables but with only reference locations!");
    //                        Debug.WriteLine("Type locations:");
    //                        foreach(var loc in LocationDictionary.Values) {
    //                            Debug.WriteLine(loc);
    //                        }
    //                        Debug.WriteLine("Variable locations:");
    //                        foreach(var dc in DeclaredVariablesDictionary.Values) {
    //                            Debug.WriteLine(dc.Location);
    //                        }
    //                    }

    //                    if(ParentScope != null) {
    //                        ParentScope.RemoveChild(this);
    //                        ParentScope = null;
    //                    }
    //                    unresolvedChildScopes.AddRange(ChildScopes);
    //                    //reset the UnresolvedParentScopeInUse so the children will be re-resolved by our parent
    //                    foreach(var namedChild in unresolvedChildScopes.OfType<NamedScope>()) {
    //                        namedChild.UnresolvedParentScopeInUse = null;
    //                    }
    //                    unresolvedScopes = new Collection<IScope>(unresolvedChildScopes);
    //                }
    //            }
    //        }
    //        return unresolvedScopes;
    //    }

    //    /// <summary>
    //    /// Creates a string representation for this type
    //    /// </summary>
    //    /// <returns>A string that describes this type</returns>
    //    public override string ToString() {
    //        return ToString(this.Kind.ToString());
    //    }
    //}
}