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
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a type definition
    /// </summary>
    public class TypeDefinition : NamedScope {
        /// <summary>
        /// Creates a new type definition object
        /// </summary>
        public TypeDefinition()
            : base() {
            this.ParentTypes = new Collection<TypeUse>();
            this.IsPartial = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public TypeDefinition(TypeDefinition otherDefinition)
            : base(otherDefinition) {
            this.IsPartial = otherDefinition.IsPartial;
            this.Kind = otherDefinition.Kind;

            this.ParentTypes = new Collection<TypeUse>();
            foreach(var parent in otherDefinition.ParentTypes) {
                this.ParentTypes.Add(parent);
            }
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
        public Collection<TypeUse> ParentTypes { get; set; }

        /// <summary>
        /// Merges this type definition with <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c> evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new type definition from this and otherScope</returns>
        public override NamedScope Merge(NamedScope otherScope) {
            TypeDefinition mergedScope = null;
            if(otherScope.CanBeMergedInto(this)) {
                mergedScope = new TypeDefinition(this);
                mergedScope.AddFrom(otherScope);
                if(mergedScope.Accessibility == AccessModifier.None) {
                    mergedScope.Accessibility = otherScope.Accessibility;
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
        /// Removes any program elements defined in the given file.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        public override void RemoveFile(string fileName) {
            if(!LocationDictionary.ContainsKey(fileName)) {
                //this type is not defined in the given file
                return;
            }

            if(LocationDictionary.Count == 1) {
                //this type exists solely in the file to be deleted
                ParentScope = null;
            } else {
                //this type is defined in more than one file, delete only the parts in the given file

                //TODO: need special handling based on whether this is a partial class, or C++, or what
                if(IsPartial) {
                    //remove children
                    var childrenToRemove = new List<Scope>();
                    foreach(var child in ChildScopeCollection) {
                        if(child.ExistsInFile(fileName)) {
                            child.RemoveFile(fileName);
                            if(child.ParentScope == null) {
                                //child has deleted itself
                                childrenToRemove.Add(child);
                            }
                        }
                    }
                    foreach(var child in childrenToRemove) {
                        ChildScopeCollection.Remove(child);
                    }

                    //remove method calls
                    var callsInFile = MethodCallCollection.Where(call => call.Location.SourceFileName == fileName);
                    foreach(var call in callsInFile) {
                        MethodCallCollection.Remove(call);
                    }

                    //remove declared variables
                    var declsInFile = DeclaredVariablesDictionary.Where(kvp => kvp.Value.Location.SourceFileName == fileName);
                    foreach(var kvp in declsInFile) {
                        DeclaredVariablesDictionary.Remove(kvp.Key);
                    }

                    //update locations
                    LocationDictionary.Remove(fileName);
                    //TODO: update PrimaryLocation?
                }
            }
        }
    }
}
