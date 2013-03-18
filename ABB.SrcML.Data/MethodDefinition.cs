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

namespace ABB.SrcML.Data {
    /// <summary>
    /// A method definition object.
    /// </summary>
    public class MethodDefinition : NamedScope {
        //private Collection<VariableDeclaration> _parameters;
        //private Collection<ParameterDeclaration> _parameters;

        /// <summary>
        /// Creates a new method definition object
        /// </summary>
        public MethodDefinition()
            : base() {
            Parameters = new Collection<ParameterDeclaration>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public MethodDefinition(MethodDefinition otherDefinition)
            : base(otherDefinition) {
            IsConstructor = otherDefinition.IsConstructor;
            IsDestructor = otherDefinition.IsDestructor;
            Parameters = new Collection<ParameterDeclaration>();
            foreach(var parameter in otherDefinition.Parameters) {
                Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// True if this is a constructor; false otherwise
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a destructor; false otherwise
        /// </summary>
        public bool IsDestructor { get; set; }

        /// <summary>
        /// The return type for this method
        /// </summary>
        public TypeUse ReturnType { get; set; }

        /// <summary>
        /// The parameters for this method.
        /// </summary>
        public Collection<ParameterDeclaration> Parameters { get; set; }

        /// <summary>
        /// Merges this method definition with <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c> evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new method definition from this and otherScope, or null if they couldn't be merged.</returns>
        public override NamedScope Merge(NamedScope otherScope) {
            MethodDefinition mergedScope = null;
            if(otherScope != null) {
                if(otherScope.CanBeMergedInto(this)) {
                    mergedScope = new MethodDefinition(this);
                    mergedScope.AddFrom(otherScope);
                    if(mergedScope.Accessibility == AccessModifier.None) {
                        mergedScope.Accessibility = otherScope.Accessibility;
                    }
                }
            }
            return mergedScope;
        }

        /// <summary>
        /// Returns true if both this and <paramref name="otherScope"/> have the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same method; false otherwise.</returns>
        /// TODO implement better method merging
        public virtual bool CanBeMergedInto(MethodDefinition otherScope) {
            if(otherScope != null && this.Parameters.Count == otherScope.Parameters.Count) {
                var parameterComparisons = Enumerable.Zip(this.Parameters, otherScope.Parameters, (t, o) => t.VariableType.Name == o.VariableType.Name);
                return base.CanBeMergedInto(otherScope) && parameterComparisons.All(x => x);
            }
            return false;
        }

        /// <summary>
        /// Casts <paramref name="otherScope"/> to a <see cref="MethodDefinition"/> and calls <see cref="CanBeMergedInto(MethodDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedInto(MethodDefinition)"/> evaluates to true.</returns>
        public override bool CanBeMergedInto(NamedScope otherScope) {
            return this.CanBeMergedInto(otherScope as MethodDefinition);
        }

        /// <summary>
        /// The AddFrom function adds all of the declarations and children from <paramref name="otherScope"/> to this scope
        /// </summary>
        /// <param name="otherScope">The scope to add data from</param>
        /// <returns>the new scope</returns>
        public override Scope AddFrom(Scope otherScope) {
            var otherMethod = otherScope as MethodDefinition;
            if(otherMethod != null) {
                var parameters = Parameters.ToList();
                var otherParameters = otherMethod.Parameters.ToList();
                if(parameters.Count == otherParameters.Count) {
                    for(int i = 0; i < parameters.Count; i++) {
                        var param = parameters[i];
                        var otherParam = otherParameters[i];
                        if(param.VariableType.Name == otherParam.VariableType.Name) {
                            foreach(var otherLoc in otherParam.Locations) {
                                param.Locations.Add(otherLoc);
                            }
                            if(string.IsNullOrWhiteSpace(param.Name)) {
                                param.Name = otherParam.Name;
                            }
                        } else {
                            Debug.WriteLine("MethodDefinition.AddFrom: conflicting parameter types at position {0}: {1} and {2}", i, param.VariableType.Name, otherParam.VariableType.Name);
                        }
                    }
                } else {
                    Debug.WriteLine("MethodDefinition.AddFrom: adding from method with different number of parameters!");
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
            if(LocationDictionary.ContainsKey(fileName)) {
                if(LocationDictionary.Count == 1) {
                    //this scope exists solely in the file to be deleted
                    if(ParentScope != null) {
                        ParentScope.RemoveChild(this);
                        ParentScope = null;
                    }
                } else {
                    //Method is defined in more than one file, delete the stuff defined in the given file
                    //Remove the file from the children
                    var unresolvedChildScopes = new List<Scope>();
                    foreach(var child in ChildScopes.ToList()) {
                        var result = child.RemoveFile(fileName);
                        if(result != null) {
                            unresolvedChildScopes.AddRange(result);
                        }
                    }
                    if(unresolvedChildScopes.Count > 0) {
                        foreach(var child in unresolvedChildScopes) {
                            AddChildScope(child);
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
                    //remove parameter locations
                    foreach(var param in Parameters) {
                        var locationsInFile = param.Locations.Where(loc => loc.SourceFileName == fileName).ToList();
                        foreach(var loc in locationsInFile) {
                            param.Locations.Remove(loc);
                        }
                        if(param.Locations.Count == 0) {
                            Debug.WriteLine("MethodDefinition.RemoveFile: Found a method parameter with fewer locations than the rest of the method!");
                        }
                    }
                    //remove parent scope candidates
                    var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
                    foreach(var candidate in candidatesInFile) {
                        ParentScopeCandidates.Remove(candidate);
                    }
                    //update locations
                    LocationDictionary.Remove(fileName);
                    //TODO: update access modifiers based on which definitions/declarations we've deleted
                }
            }
            return null;
        }
        
    }
}
