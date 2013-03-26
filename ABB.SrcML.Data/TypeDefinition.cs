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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a type definition
    /// </summary>
    [DebuggerTypeProxy(typeof(ScopeDebugView))]
    public class TypeDefinition : NamedScope {
        private Collection<TypeUse> ParentTypeCollection;

        /// <summary>
        /// Creates a new type definition object
        /// </summary>
        public TypeDefinition()
            : base() {
            // this.ParentTypes = new Collection<TypeUse>();
            this.IsPartial = false;
            this.ParentTypeCollection = new Collection<TypeUse>();
            this.ParentTypes = new ReadOnlyCollection<TypeUse>(ParentTypeCollection);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public TypeDefinition(TypeDefinition otherDefinition)
            : base(otherDefinition) {
            this.IsPartial = otherDefinition.IsPartial;
            this.Kind = otherDefinition.Kind;

            this.ParentTypeCollection = new Collection<TypeUse>();
            foreach(var parent in otherDefinition.ParentTypes) {
                this.AddParentType(parent);
            }
            this.ParentTypes = new ReadOnlyCollection<TypeUse>(this.ParentTypeCollection);
        }

        /// <summary>
        /// Partial if this is a partial class (used in C#)
        /// </summary>
        public bool IsPartial { get; set; }

        /// <summary>
        /// The <see cref="TypeKind"/> of this type
        /// </summary>
        public TypeKind Kind { get; set; }
        
        /// <summary>
        /// The parent types that this type inherits from
        /// </summary>
        public ReadOnlyCollection<TypeUse> ParentTypes { get; protected set; }

        /// <summary>
        /// Adds <paramref name="parentTypeUse"/> as a parent type for this type definition
        /// </summary>
        /// <param name="parentTypeUse">The parent type to add</param>
        public void AddParentType(TypeUse parentTypeUse) {
            if(null == parentTypeUse) throw new ArgumentNullException("parentTypeUse");

            parentTypeUse.ParentScope = this;
            ParentTypeCollection.Add(parentTypeUse);
        }

        /// <summary>
        /// Merges this type definition with <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c> evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new type definition from this and otherScope, or null if they couldn't be merged</returns>
        public override NamedScope Merge(NamedScope otherScope) {
            TypeDefinition mergedScope = null;
            if(otherScope != null) {
                if(otherScope.CanBeMergedInto(this)) {
                    mergedScope = new TypeDefinition(this);
                    mergedScope.AddFrom(otherScope);
                    if(mergedScope.Accessibility == AccessModifier.None) {
                        mergedScope.Accessibility = otherScope.Accessibility;
                    }
                }
            }
            return mergedScope;
        }
        /// <summary>
        /// Returns true if both this and <paramref name="otherScope"/> have the same name and are both partial.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same class; false otherwise.</returns>
        public virtual bool CanBeMergedInto(TypeDefinition otherScope) {
            return base.CanBeMergedInto(otherScope) && this.IsPartial && otherScope.IsPartial;
        }

        /// <summary>
        /// Casts <paramref name="otherScope"/> to a <see cref="TypeDefinition"/> and calls <see cref="CanBeMergedInto(TypeDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedInto(TypeDefinition)"/> evaluates to true.</returns>
        public override bool CanBeMergedInto(NamedScope otherScope) {
            return this.CanBeMergedInto(otherScope as TypeDefinition);
        }

        /// <summary>
        /// The AddFrom function adds all of the declarations and children from <paramref name="otherScope"/> to this scope
        /// </summary>
        /// <param name="otherScope">The scope to add data from</param>
        /// <returns>the new scope</returns>
        public override Scope AddFrom(Scope otherScope) {
            var otherType = otherScope as TypeDefinition;
            if(otherType != null) {
                foreach(var parent in otherType.ParentTypes) {
                    this.AddParentType(parent);
                }
            }
            return base.AddFrom(otherScope);
        }

        /// <summary>
        /// Removes any program elements defined in the given file.
        /// If the scope is defined entirely within the given file, then it removes itself from its parent.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        /// <returns>A collection of any unresolved scopes that result from removing the file. The caller is responsible for re-resolving these as appropriate.</returns>
        public override Collection<Scope> RemoveFile(string fileName) {
            Collection<Scope> unresolvedScopes = null;
            if(LocationDictionary.ContainsKey(fileName)) {
                if(LocationDictionary.Count == 1) {
                    //this type exists solely in the file to be deleted
                    if(ParentScope != null) {
                        ParentScope.RemoveChild(this);
                        ParentScope = null;
                    }
                } else {
                    ////this type is defined in more than one file, delete only the parts in the given file
                    //remove children
                    var unresolvedChildScopes = new List<Scope>();
                    foreach(var child in ChildScopes.ToList()) {
                        var result = child.RemoveFile(fileName);
                        if(result != null) {
                            unresolvedChildScopes.AddRange(result);
                        }
                    }
                    //remove method calls
                    var callsInFile = MethodCallCollection.Where(call => call.Location.SourceFileName == fileName).ToList();
                    foreach(var call in callsInFile) {
                        MethodCallCollection.Remove(call);
                    }
                    //remove declared variables
                    var declsInFile = DeclaredVariablesDictionary.Where(kvp => kvp.Value.Location.SourceFileName == fileName).ToList();
                    foreach(var kvp in declsInFile) {
                        DeclaredVariablesDictionary.Remove(kvp.Key);
                    }
                    //remove parent types
                    var parentsInFile = ParentTypes.Where(parent => parent.Location.SourceFileName == fileName).ToList();
                    foreach(var parent in parentsInFile) {
                        ParentTypeCollection.Remove(parent);
                    }
                    //remove parent scope candidates
                    var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
                    foreach(var candidate in candidatesInFile) {
                        ParentScopeCandidates.Remove(candidate);
                    }
                    //update locations
                    LocationDictionary.Remove(fileName);


                    if(DefinitionLocations.Any()) {
                        //This type is still defined somewhere, so re-add the unresolved children to it
                        if(unresolvedChildScopes.Count > 0) {
                            foreach(var child in unresolvedChildScopes) {
                                AddChildScope(child);
                            }
                        }
                    } else {
                        //This type is no longer defined, only referenced
                        //Return any remaining children to be re-resolved by our parent
                        if(MethodCallCollection.Any()) {
                            Debug.WriteLine("Found Type containing method calls but with only reference locations!");
                            Debug.WriteLine("Type locations:");
                            foreach(var loc in LocationDictionary.Values) {
                                Debug.WriteLine(loc);
                            }
                            Debug.WriteLine("Method call locations:");
                            foreach(var mc in MethodCallCollection) {
                                Debug.WriteLine(mc.Location);
                            }
                        }
                        if(DeclaredVariablesDictionary.Any()) {
                            Debug.WriteLine("Found Type containing declared variables but with only reference locations!");
                            Debug.WriteLine("Type locations:");
                            foreach(var loc in LocationDictionary.Values) {
                                Debug.WriteLine(loc);
                            }
                            Debug.WriteLine("Variable locations:");
                            foreach(var dc in DeclaredVariablesDictionary.Values) {
                                Debug.WriteLine(dc.Location);
                            }
                        }

                        if(ParentScope != null) {
                            ParentScope.RemoveChild(this);
                            ParentScope = null;
                        }
                        unresolvedChildScopes.AddRange(ChildScopes);
                        //reset the UnresolvedParentScopeInUse so the children will be re-resolved by our parent
                        foreach(var namedChild in unresolvedChildScopes.OfType<NamedScope>()) {
                            namedChild.UnresolvedParentScopeInUse = null;
                        }
                        unresolvedScopes = new Collection<Scope>(unresolvedChildScopes);
                    }
                }
            }
            return unresolvedScopes;
        }

        /// <summary>
        /// Creates a string representation for this type
        /// </summary>
        /// <returns>A string that describes this type</returns>
        public override string ToString() {
            return ToString(this.Kind.ToString());
        }
    }
}
