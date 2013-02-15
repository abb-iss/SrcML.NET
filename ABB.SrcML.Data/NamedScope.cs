/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// <para>This is a variable scope that can be identified by name. Its subclasses identify specific constructs
    /// in the code that have a name. It is also used by <see cref="CPlusPlusCodeParser"/> to represent unresolved
    /// scopes.</para>
    /// <para>Sub-classes of this include <see cref="TypeDefinition"/>, <see cref="NamespaceDefinition"/>,
    /// and <see cref="MethodDefinition"/></para>
    /// </summary>
    public class NamedScope : Scope {
        /// <summary>
        /// Create a new object
        /// </summary>
        public NamedScope()
            : base() {
            Name = String.Empty;
            UnresolvedParentScope = null;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherScope">The scope to copy from</param>
        public NamedScope(NamedScope otherScope)
            : base(otherScope) {
            Name = otherScope.Name;
            UnresolvedParentScope = otherScope.UnresolvedParentScope;
        }

        /// <summary>
        /// The name of this scope
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// An unresolved parent scope for this object. This is used by <see cref="CPlusPlusCodeParser"/> to track
        /// unresolved scopes that contain this object. This property should point to the root of the unresolved section.
        /// </summary>
        public NamedScope UnresolvedParentScope { get; set; }

        /// <summary>
        /// The full name of this object (taken by finding all of the NamedScope objects that are ancestors of this
        /// object.
        /// </summary>
        public string FullName {
            get {
                return GetFullName();
            }
        }

        /// <summary>
        /// The unresolved name of this object (taken by finding all of the names rooted at <see cref="UnresolvedParentScope"/>
        /// </summary>
        public string UnresolvedName {
            get {
                return GetUnresolvedName();
            }
        }

        /// <summary>
        /// Adds a child scope to this object. If the child scope is a <see cref="Scope"/>
        /// It setups all of the unresolved links between this scope and the <paramref name="childScope"/>
        /// </summary>
        /// <param name="childScope">the child scope to add</param>
        public override void AddChildScope(Scope childScope) {
            var cs = childScope as NamedScope;
            if(cs != null) {
                AddNamedChildScope(cs);
            } else {
                base.AddChildScope(childScope);
            }
        }

        /// <summary>
        /// Sets up unresolved links between this and <paramref name="childScope"/> if needed.
        /// </summary>
        /// <param name="childScope">The child scope to add</param>
        protected void AddNamedChildScope(NamedScope childScope) {
            if(childScope.UnresolvedParentScope == null) {
                base.AddChildScope(childScope);
            } else {
                var root = childScope.UnresolvedParentScope;
                
                // iterate through the unresolved parent scope and find the tail
                // once you've found the tail, add this as a child scope
                Scope latest = root, current;
                do {
                    current = latest;
                    latest = current.ChildScopes.FirstOrDefault();
                } while(latest != null);
                
                current.AddChildScope(childScope);
                base.AddChildScope(root);
            }
        }

        /// <summary>
        /// <para>Merges NamedVariableScopes together. It works like this:</para>
        /// <para>If both objects are the same type, it merges <paramref name="otherScope"/> with this.</para>
        /// <para>If this is a subclass of NamedScope and <paramref name="otherScope"/> is not, it merges otherScope with this.</para>
        /// <para>If this is a NamedScope and <paramref name="otherScope"/> is a subclass, it merges this with otherScope</para>
        /// <para>If the two objects cannot be merged, it does not merge them.</para>
        /// <para><seealso cref="CanBeMergedInto(NamedScope)">CanBeMergedInto is used to decide if the two objects can be merged.</seealso></para>
        /// </summary>
        /// <param name="otherScope">The scope to merge with</param>
        /// <returns>The merged scope. null if they cannot be merged.</returns>
        public override Scope Merge(Scope otherScope) {
            return this.Merge(otherScope as NamedScope);
        }

        /// <summary>
        /// Merges two NamedVariableScopes together. It works like this:
        /// <list type="bullet">
        /// <item><description>If this is the same type or more specific than <paramref name="otherScope"/>, then create a new merged NamedScope
        /// from <paramref name="otherScope"/> and this.</description></item>
        /// <item><description>If <paramref name="otherScope"/> is more specific than this, call <c>otherScope.Merge</c></description></item>
        /// </list>
        /// </summary>
        /// <param name="otherScope">The scope to merge with</param>
        /// <returns>The new merged scope; null if they couldn't be merged</returns>
        public virtual NamedScope Merge(NamedScope otherScope) {
            NamedScope mergedScope = null;
            if(otherScope.CanBeMergedInto(this)) {
                // this and other scope can be merged normally
                // either they are the same type or
                // this is a subclass of NamedScope and otherScope is a NamedScope
                mergedScope = new NamedScope(this);
                mergedScope.AddFrom(otherScope);
            } else if(this.CanBeMergedInto(otherScope) && !otherScope.CanBeMergedInto(this)) {
                // this is a NamedScope and otherScope is a subclass
                // useful information (type, method, or namespace data) are in otherScope
                mergedScope = otherScope.Merge(this);
            }
            
            return mergedScope;
        }
        /// <summary>
        /// Overrides <see cref="Scope.CanBeMergedInto"/> to call <see cref="CanBeMergedInto(NamedScope)"/>
        /// </summary>
        /// <param name="otherScope">the scope to test</param>
        /// <returns>true if the two objects can be merged, false otherwise</returns>
        public override bool CanBeMergedInto(Scope otherScope) {
            return this.CanBeMergedInto(otherScope as NamedScope);
        }
        /// <summary>
        /// Two NamedScope objects can be merged if they share the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if the two objects have the same <see cref="Name"/>. False, otherwise.</returns>
        public virtual bool CanBeMergedInto(NamedScope otherScope) {
            return (null != otherScope && this.Name == otherScope.Name);
        }

        private string GetUnresolvedName() {
            StringBuilder sb = new StringBuilder();
            var current = UnresolvedParentScope;
            while(current != null) {
                sb.Append(current.Name);
                sb.Append(".");
                current = current.ChildScopes.FirstOrDefault() as NamedScope;
            }

            return sb.ToString().TrimEnd('.');
        }

        private string GetFullName() {
            var scopes = from p in this.ParentScopes
                         let namedScope = p as NamedScope
                         where namedScope != null && namedScope.Name.Length > 0
                         select namedScope.Name;
            StringBuilder sb = new StringBuilder();
            foreach(var scope in scopes.Reverse()) {
                sb.Append(scope);
                sb.Append(".");
            }
            sb.Append(this.Name);
            return sb.ToString();
        }
    }
}
