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

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a namespace definition
    /// </summary>
    public class NamespaceDefinition : NamedScope {
        /// <summary>
        /// Creates a new namespace definition object
        /// </summary>
        public NamespaceDefinition()
            : base() {
            this.IsAnonymous = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public NamespaceDefinition(NamespaceDefinition otherDefinition)
            : base(otherDefinition) {
            this.IsAnonymous = otherDefinition.IsAnonymous;
        }

        /// <summary>
        /// Returns true if this is an anonymous namespace
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// <para>Returns true if this namespace represents the global namespace</para>
        /// <para>A namespace is global if the <see cref="NamedScope.Name"/> is <c>String.Empty</c></para>
        /// </summary>
        public bool IsGlobal { get { return this.Name.Length == 0 && !this.IsAnonymous && this.ParentScope == null; } }

        /// <summary>
        /// Returns the fully qualified name for the given type
        /// </summary>
        /// <param name="name">A name</param>
        /// <returns>the fully qualified name (made from this namespace definition and the given name)</returns>
        public string MakeQualifiedName(string name) {
            if(this.Name.Length == 0)
                return name;
            return String.Format("{0}.{1}", this.Name, name);
        }

        /// <summary>
        /// Merges this namespace definition with <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c> evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new namespace definition from this and otherScope</returns>
        public override NamedScope Merge(NamedScope otherScope) {
            NamespaceDefinition mergedScope = null;
            if(otherScope.CanBeMergedInto(this)) {
                mergedScope = new NamespaceDefinition(this);
                mergedScope.AddFrom(otherScope);
            }

            return mergedScope;
        }

        /// <summary>
        /// Returns true if both this and <paramref name="otherScope"/> have the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same namespace; false otherwise.</returns>
        public virtual bool CanBeMergedInto(NamespaceDefinition otherScope) {
            return base.CanBeMergedInto(otherScope);
        }

        /// <summary>
        /// Casts <paramref name="otherScope"/> to a <see cref="NamespaceDefinition"/> and calls <see cref="CanBeMergedInto(NamespaceDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedInto(NamespaceDefinition)"/> evaluates to true.</returns>
        public override bool CanBeMergedInto(NamedScope otherScope) {
            return this.CanBeMergedInto(otherScope as NamespaceDefinition);
        }

        /// <summary>
        /// Removes any program elements defined in the given file.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        public virtual void RemoveFile(string fileName) {
            if(!LocationDictionary.ContainsKey(fileName)) {
                //this namespace is not defined in the given file
                return;
            }

            if(LocationDictionary.Count == 1) {
                //this namespace exists solely in the file to be deleted
                ParentScope = null;
            } else {
                //this namespace is defined in more than one file, delete only the parts in the given file
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
