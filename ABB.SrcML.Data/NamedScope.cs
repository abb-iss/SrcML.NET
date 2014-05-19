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

    public class NamedScope : BlockStatement, INamedEntity {
        public NamedScope() : base() {
            Name = string.Empty;
            Accessibility = AccessModifier.None;
        }

        public string Name { get; set; }

        /// <summary>
        /// For C/C++ methods, this property gives the specified scope that the method is defined in.
        /// For example, in the method <code>int A::B::MyFunction(char arg);</code> the NamePrefix is A::B.
        /// </summary>
        public NamePrefix Prefix { get; set; }

        public AccessModifier Accessibility { get; set; }
        /// <summary>
        /// Gets the full name by finding all of the named scope ancestors and combining them.
        /// </summary>
        /// <returns>The full name for this named scope</returns>
        public string GetFullName() {
            var names = from statement in GetAncestorsAndSelf<NamedScope>()
                        where !String.IsNullOrEmpty(statement.Name)
                        select statement.Name;
            return string.Join(".", names.Reverse()).TrimEnd('.');
        }

        public override Statement Merge(Statement otherStatement) {
            return Merge(otherStatement as NamedScope);
        }

        public NamedScope Merge(NamedScope otherNamedScope) {
            if(null == otherNamedScope) {
                throw new ArgumentNullException("otherNamedScope");
            }

            var combinedScope = Merge<NamedScope>(this, otherNamedScope);
            combinedScope.Name = this.Name;
            return combinedScope;
        }
        protected override void RestructureChildren() {
            var namedChildrenWithPrefixes = (from child in ChildStatements.OfType<NamedScope>()
                                             where !(null == child.Prefix || child.Prefix.IsResolved)
                                             select child).ToList<NamedScope>();
            
            // 1. restructure all of the children that don't have prefixes
            var restructuredChildren = Statement.RestructureChildren(ChildStatements.Except(namedChildrenWithPrefixes));
            ClearChildren();
            AddChildStatements(restructuredChildren);

            // 2. check to see if children with prefixes can be relocated
            foreach(var child in namedChildrenWithPrefixes) {
                var firstPossibleParent = child.Prefix.FindMatches(this).FirstOrDefault();
                if(null != firstPossibleParent) {
                    child.Prefix.MapPrefix(firstPossibleParent);
                    firstPossibleParent.AddChildStatement(child);
                    firstPossibleParent.RestructureChildren();
                } else {
                    AddChildStatement(child);
                }
            }
        }
    }


    ///// <summary>
    ///// <para>This is a variable scope that can be identified by name. Its subclasses identify
    ///// specific constructs in the code that have a name. It is also used by
    ///// <see cref="CPlusPlusCodeParser"/> to represent unresolved scopes.</para> <para>Sub-classes
    ///// of this include <see cref="ITypeDefinition"/>, <see cref="NamespaceDefinition"/>, and
    ///// <see cref="MethodDefinition"/></para>
    ///// </summary>
    //[DebuggerTypeProxy(typeof(ScopeDebugView))]
    //[Serializable]
    //public class NamedScope : Scope, INamedScope {

    //    /// <summary>
    //    /// Create a new object
    //    /// </summary>
    //    public NamedScope()
    //        : base() {
    //        Accessibility = AccessModifier.None;
    //        Name = String.Empty;
    //        UnresolvedParentScopeInUse = null;
    //        ParentScopeCandidates = new Collection<INamedScopeUse>();
    //    }

    //    /// <summary>
    //    /// Copy constructor
    //    /// </summary>
    //    /// <param name="otherScope">The scope to copy from</param>
    //    public NamedScope(NamedScope otherScope)
    //        : base(otherScope) {
    //        Accessibility = otherScope.Accessibility;
    //        Name = otherScope.Name;
    //        ParentScopeCandidates = new Collection<INamedScopeUse>();
    //        foreach(var candidate in otherScope.ParentScopeCandidates) {
    //            ParentScopeCandidates.Add(candidate);
    //        }
    //        UnresolvedParentScopeInUse = otherScope.UnresolvedParentScopeInUse;
    //    }

    //    /// <summary>
    //    /// The access modifier for this scope
    //    /// </summary>
    //    public AccessModifier Accessibility { get; set; }

    //    /// <summary>
    //    /// The identifier for named scopes is the <see cref="Name"/>.
    //    /// </summary>
    //    public override string Id {
    //        get {
    //            return this.Name;
    //        }
    //    }

    //    /// <summary>
    //    /// The name of this scope
    //    /// </summary>
    //    public string Name { get; set; }

    //    /// <summary>
    //    /// Collection of possible parent scope candidates
    //    /// </summary>
    //    public Collection<INamedScopeUse> ParentScopeCandidates { get; set; }

    //    /// <summary>
    //    /// This indicates which unresolved parent scope has been used to link this object with a
    //    /// parent object
    //    /// </summary>
    //    public INamedScopeUse UnresolvedParentScopeInUse { get; set; }

    //    /// <summary>
    //    /// Adds a child scope to this object. If the child scope is a <see cref="Scope"/> It setups
    //    /// all of the unresolved links between this scope and the
    //    /// <paramref name="childScope"/></summary>
    //    /// <param name="childScope">the child scope to add</param>
    //    public override void AddChildScope(IScope childScope) {
    //        var cs = childScope as NamedScope;
    //        if(cs != null) {
    //            AddNamedChildScope(cs);
    //        } else {
    //            base.AddChildScope(childScope);
    //        }
    //    }

    //    /// <summary>
    //    /// The AddFrom function adds all of the declarations and children from
    //    /// <paramref name="otherScope"/>to this scope
    //    /// </summary>
    //    /// <param name="otherScope">The scope to add data from</param>
    //    /// <returns>the new scope</returns>
    //    public override IScope AddFrom(IScope otherScope) {
    //        var otherNamedScope = otherScope as INamedScope;
    //        if(otherNamedScope != null) {
    //            foreach(var candidate in otherNamedScope.ParentScopeCandidates) {
    //                this.ParentScopeCandidates.Add(candidate);
    //            }
    //        }
    //        return base.AddFrom(otherScope);
    //    }

    //    /// <summary>
    //    /// Overrides <see cref="IScope.CanBeMergedInto"/> to call
    //    /// <see cref="CanBeMergedInto(INamedScope)"/>
    //    /// </summary>
    //    /// <param name="otherScope">the scope to test</param>
    //    /// <returns>true if the two objects can be merged, false otherwise</returns>
    //    public override bool CanBeMergedInto(IScope otherScope) {
    //        return this.CanBeMergedInto(otherScope as NamedScope);
    //    }

    //    /// <summary>
    //    /// Two NamedScope objects can be merged if they share the same name.
    //    /// </summary>
    //    /// <param name="otherScope">The scope to test</param>
    //    /// <returns>true if the two objects have the same <see cref="Name"/>. False,
    //    /// otherwise.</returns>
    //    public virtual bool CanBeMergedInto(NamedScope otherScope) {
    //        return (null != otherScope && this.Name == otherScope.Name);
    //    }

    //    /// <summary>
    //    /// Gets the full name by finding all of the named scope ancestors and combining them.
    //    /// </summary>
    //    /// <returns>The full name for this scope</returns>
    //    public string GetFullName() {
    //        var names = from scope in GetParentScopesAndSelf<INamedScope>()
    //                    where !String.IsNullOrEmpty(scope.Name)
    //                    select scope.Name;
    //        return String.Join(".", names.Reverse()).TrimEnd('.');
    //    }

    //    /// <summary>
    //    /// <para>Merges NamedVariableScopes together. It works like this:</para> <para>If both
    //    /// objects are the same type, it merges
    //    /// <paramref name="otherScope"/>with this.</para> <para>If this is a subclass of NamedScope
    //    /// and
    //    /// <paramref name="otherScope"/>is not, it merges otherScope with this.</para> <para>If
    //    /// this is a NamedScope and
    //    /// <paramref name="otherScope"/>is a subclass, it merges this with otherScope</para>
    //    /// <para>If the two objects cannot be merged, it does not merge them.</para>
    //    /// <para><seealso cref="CanBeMergedInto(NamedScope)">CanBeMergedInto is used to decide if
    //    /// the two objects can be merged.</seealso></para>
    //    /// </summary>
    //    /// <param name="otherScope">The scope to merge with</param>
    //    /// <returns>The merged scope. null if they cannot be merged.</returns>
    //    public override IScope Merge(IScope otherScope) {
    //        return this.Merge(otherScope as NamedScope);
    //    }

    //    /// <summary>
    //    /// Merges two NamedVariableScopes together. It works like this: <list type="bullet">
    //    /// <item><description>If this is the same type or more specific than
    //    /// <paramref name="otherScope"/>, then create a new merged NamedScope from
    //    /// <paramref name="otherScope"/>and this.</description></item> <item><description>If
    //    /// <paramref name="otherScope"/>is more specific than this, call
    //    /// <c>otherScope.Merge</c></description></item> </list>
    //    /// </summary>
    //    /// <param name="otherScope">The scope to merge with</param>
    //    /// <returns>The new merged scope; null if they couldn't be merged</returns>
    //    public virtual INamedScope Merge(INamedScope otherScope) {
    //        INamedScope mergedScope = null;
    //        if(otherScope != null) {
    //            if(otherScope.CanBeMergedInto(this)) {
    //                // this and other scope can be merged normally either they are the same type or
    //                // this is a subclass of NamedScope and otherScope is a NamedScope
    //                mergedScope = new NamedScope(this);
    //                mergedScope.AddFrom(otherScope);
    //            } else if(this.CanBeMergedInto(otherScope) && !otherScope.CanBeMergedInto(this)) {
    //                // this is a NamedScope and otherScope is a subclass useful information (type,
    //                // method, or namespace data) are in otherScope
    //                mergedScope = otherScope.Merge(this);
    //            }
    //        }
    //        return mergedScope;
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
    //                //this scope exists solely in the file to be deleted
    //                if(ParentScope != null) {
    //                    ParentScope.RemoveChild(this);
    //                    ParentScope = null;
    //                }
    //            } else {
    //                //this NamedScope is defined in more than one file, delete only the parts in the given file
    //                //Remove the file from the children
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
    //                //remove parent scope candidates
    //                var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
    //                foreach(var candidate in candidatesInFile) {
    //                    ParentScopeCandidates.Remove(candidate);
    //                }
    //                //update locations
    //                LocationDictionary.Remove(fileName);

    //                if(DefinitionLocations.Any()) {
    //                    //This NamedScope is still defined somewhere, so re-add the unresolved children to it
    //                    if(unresolvedChildScopes.Count > 0) {
    //                        foreach(var child in unresolvedChildScopes) {
    //                            AddChildScope(child);
    //                        }
    //                    }
    //                } else {
    //                    //This NamedScope is no longer defined, only referenced
    //                    //Return any remaining children to be re-resolved by our parent
    //                    if(MethodCallCollection.Any()) {
    //                        Debug.WriteLine("Found Namespace containing method calls but with only reference locations!");
    //                        Debug.WriteLine("Namespace locations:");
    //                        foreach(var loc in LocationDictionary.Values) {
    //                            Debug.WriteLine(loc);
    //                        }
    //                        Debug.WriteLine("Method call locations:");
    //                        foreach(var mc in MethodCallCollection) {
    //                            Debug.WriteLine(mc.Location);
    //                        }
    //                    }
    //                    if(DeclaredVariablesDictionary.Any()) {
    //                        Debug.WriteLine("Found Namespace containing declared variables but with only reference locations!");
    //                        Debug.WriteLine("Namespace locations:");
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
    //                    foreach(var namedChild in unresolvedChildScopes.OfType<INamedScope>()) {
    //                        namedChild.UnresolvedParentScopeInUse = null;
    //                    }
    //                    unresolvedScopes = new Collection<IScope>(unresolvedChildScopes);
    //                }
    //            }
    //        }
    //        return unresolvedScopes;
    //    }

    //    /// <summary>
    //    /// Selects the most likely unresolved path to this element. Currently, it always selects
    //    /// the first element. Calling this sets <see cref="UnresolvedParentScopeInUse"/>.
    //    /// </summary>
    //    /// <returns><see cref="UnresolvedParentScopeInUse"/> unless there are no
    //    /// <see cref="ParentScopeCandidates"/>. Then it returns null</returns>
    //    public INamedScopeUse SelectUnresolvedScope() {
    //        if(ParentScopeCandidates.Any()) {
    //            UnresolvedParentScopeInUse = ParentScopeCandidates.First();
    //            return UnresolvedParentScopeInUse;
    //        }
    //        return null;
    //    }

    //    /// <summary>
    //    /// Creates a string representation of this named scope
    //    /// </summary>
    //    /// <returns>String that describes this named scope</returns>
    //    public override string ToString() {
    //        return ToString("Named Scope");
    //    }

    //    /// <summary>
    //    /// Sets up unresolved links between this and
    //    /// <paramref name="childScope"/>if needed.
    //    /// </summary>
    //    /// <param name="childScope">The child scope to add</param>
    //    protected void AddNamedChildScope(INamedScope childScope) {
    //        var scopeToAdd = childScope;
    //        if(childScope.UnresolvedParentScopeInUse == null && childScope.ParentScopeCandidates.Any()) {
    //            var selectedScope = childScope.SelectUnresolvedScope();
    //            scopeToAdd = selectedScope.CreateScope();

    //            IScope latest = scopeToAdd, current;
    //            do {
    //                current = latest;
    //                latest = current.ChildScopes.FirstOrDefault();
    //            } while(latest != null);

    //            current.AddChildScope(childScope);
    //        }
    //        base.AddChildScope(scopeToAdd);
    //    }

    //    private string GetUnresolvedName() {
    //        StringBuilder sb = new StringBuilder();
    //        //var current = UnresolvedParentScope;
    //        var current = UnresolvedParentScopeInUse;
    //        while(current != null) {
    //            sb.Append(current.Name);
    //            sb.Append(".");
    //            current = current.ChildScopeUse;
    //            //current = current.ChildScopes.FirstOrDefault() as NamedScope;
    //        }

    //        return sb.ToString().TrimEnd('.');
    //    }
    //}
}