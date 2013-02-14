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
    public class NamedVariableScope : VariableScope {
        /// <summary>
        /// The name of this scope
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// An unresolved parent scope for this object. This is used by <see cref="CPlusPlusCodeParser"/> to track
        /// unresolved scopes that contain this object. This property should point to the root of the unresolved section.
        /// </summary>
        public NamedVariableScope UnresolvedParentScope { get; set; }

        /// <summary>
        /// Create a new object
        /// </summary>
        public NamedVariableScope() : base() {
            Name = String.Empty;
            UnresolvedParentScope = null;
        }

        public NamedVariableScope(NamedVariableScope otherScope) : base(otherScope) {
            Name = otherScope.Name;
            UnresolvedParentScope = otherScope.UnresolvedParentScope;
        }

        /// <summary>
        /// The full name of this object (taken by finding all of the NamedVariableScope objects that are ancestors of this
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
        /// Adds a child scope to this object. If the child scope is a <see cref="VariableScope"/>
        /// It setups all of the unresolved links between this scope and the <paramref name="childScope"/>
        /// </summary>
        /// <param name="childScope">the child scope to add</param>
        public override void AddChildScope(VariableScope childScope) {
            var cs = childScope as NamedVariableScope;
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
        protected void AddNamedChildScope(NamedVariableScope childScope) {
            if(childScope.UnresolvedParentScope == null) {
                base.AddChildScope(childScope);
            } else {
                var root = childScope.UnresolvedParentScope;
                
                VariableScope latest = root, current;

                do {
                    current = latest;
                    latest = current.ChildScopes.FirstOrDefault();
                } while(latest != null);
                
                childScope.UnresolvedParentScope = null;
                current.AddChildScope(childScope);
                base.AddChildScope(root);
            }
        }

        /// <summary>
        /// <para>Merges NamedVariableScopes together. It works like this:</para>
        /// <para>If both objects are the same type, it merges <paramref name="otherScope"/> with this.</para>
        /// <para>If this is a subclass of NamedVariableScope and <paramref name="otherScope"/> is not, it merges otherScope with this.</para>
        /// <para>If this is a NamedVariableScope and <paramref name="otherScope"/> is a subclass, it merges this with otherScope</para>
        /// <para>If the two objects cannot be merged, it does not merge them.</para>
        /// <para><seealso cref="CanBeMergedInto(NamedVariableScope)">CanBeMergedInto is used to decide if the two objects can be merged.</seealso></para>
        /// </summary>
        /// <param name="otherScope">The scope to merge with</param>
        /// <returns>The merged scope. null if they cannot be merged.</returns>
        public override VariableScope Merge(VariableScope otherScope) {
            return this.Merge(otherScope as NamedVariableScope);
        }

        /// <summary>
        /// Merges two NamedVariableScopes together. It works like this:
        /// <list type="bullet">
        /// <item><description>If this is the same type or more specific than <paramref name="otherScope"/>, then create a new merged NamedVariableScope
        /// from <paramref name="otherscope"/> and this.</description></item>
        /// <item><description>If <paramref name="otherScope"/> is more specific than this, call <c>otherScope.Merge</c></description></item>
        /// </list>
        /// </summary>
        /// <param name="otherScope">The scope to merge with</param>
        /// <returns>The new merged scope; null if they couldn't be merged</returns>
        public virtual NamedVariableScope Merge(NamedVariableScope otherScope) {
            NamedVariableScope mergedScope = null;
            if(otherScope.CanBeMergedInto(this)) {
                // this and other scope can be merged normally
                // either they are the same type or
                // this is a subclass of NamedVariableScope and otherScope is a NamedVariableScope
                mergedScope = new NamedVariableScope(this);
                mergedScope.AddFrom(otherScope);
            } else if(this.CanBeMergedInto(otherScope) && !otherScope.CanBeMergedInto(this)) {
                // this is a NamedVariableScope and otherScope is a subclass
                // useful information (type, method, or namespace data) are in otherScope
                mergedScope = otherScope.Merge(this);
            }

            return mergedScope;
        }
        /// <summary>
        /// Overrides <see cref="VariableScope.CanBeMergedInto"/> to call <see cref="CanBeMergedInto(NamedVariableScope)"/>
        /// </summary>
        /// <param name="otherScope">the scope to test</param>
        /// <returns>true if the two objects can be merged, false otherwise</returns>
        public override bool CanBeMergedInto(VariableScope otherScope) {
            return this.CanBeMergedInto(otherScope as NamedVariableScope);
        }
        /// <summary>
        /// Two NamedVariableScope objects can be merged if they share the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if the two objects have the same <see cref="Name"/>. False, otherwise.</returns>
        public virtual bool CanBeMergedInto(NamedVariableScope otherScope) {
            return (null != otherScope && this.Name == otherScope.Name);
        }

        private string GetUnresolvedName() {
            StringBuilder sb = new StringBuilder();
            var current = UnresolvedParentScope;
            while(current != null) {
                sb.Append(current.Name);
                sb.Append(".");
                current = current.ChildScopes.FirstOrDefault() as NamedVariableScope;
            }

            return sb.ToString().TrimEnd('.');
        }

        private string GetFullName() {
            var scopes = from p in this.ParentScopes
                         let namedScope = p as NamedVariableScope
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
