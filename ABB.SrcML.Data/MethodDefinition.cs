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
    /// A method definition object.
    /// </summary>
    public class MethodDefinition : NamedScope {
        private Collection<VariableDeclaration> _parameters;

        /// <summary>
        /// Creates a new method definition object
        /// </summary>
        public MethodDefinition()
            : base() {
            this._parameters = new Collection<VariableDeclaration>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherDefinition">The scope to copy from</param>
        public MethodDefinition(MethodDefinition otherDefinition)
            : base(otherDefinition) {
            IsConstructor = otherDefinition.IsConstructor;
            IsDestructor = otherDefinition.IsDestructor;
            this._parameters = new Collection<VariableDeclaration>();
            foreach(var parameter in otherDefinition._parameters) {
                this._parameters.Add(parameter);
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
        /// The parameters for this method. Replacing this collection causes the <see cref="Scope.DeclaredVariables"/> to be updated.
        /// </summary>
        /// TODO make the updating of the parameters collection more robust (you can't add an element to it and have DeclaredVariables updated.
        public Collection<VariableDeclaration> Parameters {
            get { return this._parameters; }
            set {
                var oldParameters = this._parameters;
                this._parameters = value;
                
                foreach(var parameter in oldParameters) {
                    this.DeclaredVariablesDictionary.Remove(parameter.Name);
                }
                
                foreach(var parameter in this._parameters) {
                    this.AddDeclaredVariable(parameter);
                }
            }
        }

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
            if(this.Parameters.Count == otherScope.Parameters.Count) {
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
                //TODO: add parameters from other method?
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
                    foreach(var child in ChildScopeCollection.ToList()) {
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
                    //update locations
                    LocationDictionary.Remove(fileName);
                    //TODO: do something about the parameters?
                    //TODO: update access modifiers based on which definitions/declarations we've deleted
                }
            }
            return null;
        }
    }
}
