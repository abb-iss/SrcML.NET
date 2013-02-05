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
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// <para>The scope merge visitor takes two scopes and merges them together.</para>
    /// <para>It works by comparing the names and types of two scopes and merging them together.
    /// Once two scopes have merged, it iterates through the children and merges any children
    /// that need merging.</para>
    /// </summary>
    public class ScopeMergeVisitor {
        public ScopeMergeVisitor() {
        }

        /// <summary>
        /// Merges two scopes if they are equal.
        /// If the two scopes are not equal, <paramref name="firstScope"/> is returned unchanged.
        /// </summary>
        /// <param name="firstScope">the first scope</param>
        /// <param name="secondScope">the second scope</param>
        /// <returns></returns>
        public bool Merge(VariableScope firstScope, VariableScope secondScope) {
            if(firstScope.IsSameAs(secondScope)) {
                if((firstScope as NamespaceDefinition) != null) {
                    MergeNamespaceDefinitions(firstScope as NamespaceDefinition, secondScope as NamespaceDefinition);
                } else if((firstScope as MethodDefinition) != null) {
                    MergeMethodDefinitions(firstScope as MethodDefinition, secondScope as MethodDefinition);
                } else if((firstScope as TypeDefinition) != null) {
                    MergeTypeDefinitions(firstScope as TypeDefinition, secondScope as TypeDefinition);
                } else {
                    return false;
                }
                return true;
            }
            return false;
        }

        private void MergeNamespaceDefinitions(NamespaceDefinition firstScope, NamespaceDefinition secondScope) {
            foreach(var variableDeclaration in secondScope.Variables) {
                firstScope.AddDeclaredVariable(variableDeclaration);
            }

            foreach(var childOf2 in secondScope.ChildScopes) {
                bool isMerged = false;
                foreach(var childOf1 in firstScope.ChildScopes) {
                    isMerged = Merge(childOf1, childOf2);
                    if(isMerged) {
                        break;
                    }
                }
                if(!isMerged)
                    firstScope.AddChildScope(childOf2);
            }
        }

        private void MergeMethodDefinitions(MethodDefinition firstScope, MethodDefinition secondScope) {
            
        }

        private void MergeTypeDefinitions(TypeDefinition firstScope, TypeDefinition secondScope) {
            
        }

        public static bool MergeScopes(VariableScope firstScope, VariableScope secondScope) {
            return (new ScopeMergeVisitor()).Merge(firstScope, secondScope);
        }
    }
}
