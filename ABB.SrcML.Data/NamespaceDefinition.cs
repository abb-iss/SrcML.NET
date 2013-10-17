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

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a namespace definition
    /// </summary>
    [DebuggerTypeProxy(typeof(ScopeDebugView))]
    [Serializable]
    public class NamespaceDefinition : NamedScope {

        /// <summary>
        /// Creates a new namespace definition object
        /// </summary>
        public NamespaceDefinition()
            : base() { }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public NamespaceDefinition(NamespaceDefinition otherDefinition)
            : base(otherDefinition) { }

        /// <summary>
        /// Returns true if this is an anonymous namespace
        /// </summary>
        public bool IsAnonymous {
            get { return string.IsNullOrWhiteSpace(Name); }
        }

        /// <summary>
        /// <para>Returns true if this namespace represents the global namespace</para> <para>A
        /// namespace is global if the <see cref="NamedScope.Name"/> is <c>String.Empty</c></para>
        /// </summary>
        public bool IsGlobal { get { return this.IsAnonymous && this.ParentScope == null; } }

        /// <summary>
        /// Returns true if both this and
        /// <paramref name="otherScope"/>have the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same namespace; false otherwise.</returns>
        public virtual bool CanBeMergedInto(NamespaceDefinition otherScope) {
            return base.CanBeMergedInto(otherScope);
        }

        /// <summary>
        /// Casts
        /// <paramref name="otherScope"/>to a <see cref="NamespaceDefinition"/> and calls
        /// <see cref="CanBeMergedInto(NamespaceDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedInto(NamespaceDefinition)"/> evaluates to
        /// true.</returns>
        public override bool CanBeMergedInto(NamedScope otherScope) {
            return this.CanBeMergedInto(otherScope as NamespaceDefinition);
        }

        /// <summary>
        /// Returns the fully qualified name for the given type
        /// </summary>
        /// <param name="name">A name</param>
        /// <returns>the fully qualified name (made from this namespace definition and the given
        /// name) </returns>
        public string MakeQualifiedName(string name) {
            if(this.Name.Length == 0)
                return name;
            return String.Format("{0}.{1}", this.Name, name);
        }

        /// <summary>
        /// Merges this namespace definition with
        /// <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c>
        /// evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new namespace definition from this and otherScope, or null if they couldn't
        /// be merged.</returns>
        public override NamedScope Merge(NamedScope otherScope) {
            NamespaceDefinition mergedScope = null;
            if(otherScope != null) {
                if(otherScope.CanBeMergedInto(this)) {
                    mergedScope = new NamespaceDefinition(this);
                    mergedScope.AddFrom(otherScope);
                }
            }
            return mergedScope;
        }

        /// <summary>
        /// Removes any program elements defined in the given file. If the scope is defined entirely
        /// within the given file, then it removes itself from its parent.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        /// <returns>A collection of any unresolved scopes that result from removing the file. The
        /// caller is responsible for re-resolving these as appropriate.</returns>
        public override Collection<Scope> RemoveFile(string fileName) {
            Collection<Scope> unresolvedScopes = null;
            if(LocationDictionary.ContainsKey(fileName)) {
                if(LocationDictionary.Count == 1) {
                    //this namespace exists solely in the file to be deleted
                    if(ParentScope != null) {
                        ParentScope.RemoveChild(this);
                        ParentScope = null;
                    }
                } else {
                    //this namespace is defined in more than one file, delete only the parts in the given file
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
                    //remove parent scope candidates
                    var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
                    foreach(var candidate in candidatesInFile) {
                        ParentScopeCandidates.Remove(candidate);
                    }
                    //update locations
                    LocationDictionary.Remove(fileName);

                    if(DefinitionLocations.Any()) {
                        //This namespace is still defined somewhere, so re-add the unresolved children to it
                        if(unresolvedChildScopes.Count > 0) {
                            foreach(var child in unresolvedChildScopes) {
                                AddChildScope(child);
                            }
                        }
                    } else {
                        //This namespace is no longer defined, only referenced
                        //Return any remaining children to be re-resolved by our parent
                        if(MethodCallCollection.Any()) {
                            Debug.WriteLine("Found Namespace containing method calls but with only reference locations!");
                            Debug.WriteLine("Namespace locations:");
                            foreach(var loc in LocationDictionary.Values) {
                                Debug.WriteLine(loc);
                            }
                            Debug.WriteLine("Method call locations:");
                            foreach(var mc in MethodCallCollection) {
                                Debug.WriteLine(mc.Location);
                            }
                        }
                        if(DeclaredVariablesDictionary.Any()) {
                            Debug.WriteLine("Found Namespace containing declared variables but with only reference locations!");
                            Debug.WriteLine("Namespace locations:");
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
        /// Creates a string representation of this namespace
        /// </summary>
        /// <returns>A string that describes this namespace definition</returns>
        public override string ToString() {
            if(IsGlobal)
                return ToString("Namespace", "Global");
            return ToString("Namespace");
        }
    }
}