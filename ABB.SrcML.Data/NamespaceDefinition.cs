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

using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a namespace definition in a program.
    /// </summary>
    [DebuggerTypeProxy(typeof(StatementDebugView))]
    public class NamespaceDefinition : NamedScope {
        /// <summary> The XML name for NamespaceDefinition. </summary>
        public new const string XmlName = "Namespace";

        /// <summary>
        /// Creates a new NamespaceDefinition object.
        /// </summary>
        public NamespaceDefinition() : base() {}

        /// <summary>
        /// Returns true if this is an anonymous namespace
        /// </summary>
        public bool IsAnonymous {
            get { return string.IsNullOrWhiteSpace(Name); }
        }

        /// <summary>
        /// <para>Returns true if this namespace represents the global namespace</para> <para>A
        /// namespace is global if the <see cref="NamedScope.Name"/> is <c>String.Empty</c> and
        /// the namespace has no parent.</para>
        /// </summary>
        public bool IsGlobal {
            get { return this.IsAnonymous && this.ParentStatement == null; }
        }

        protected override bool ToBeDeleted { get { return Locations.All(l => l.IsReference); } }

        /// <summary>
        /// Instance method for getting <see cref="NamespaceDefinition.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for NamespaceDefinition</returns>
        public override string GetXmlName() { return NamespaceDefinition.XmlName; }

        public override Statement Merge(Statement otherStatement) {
            return Merge(otherStatement as NamespaceDefinition);
        }

        public NamespaceDefinition Merge(NamespaceDefinition otherNamespaceDefinition) {
            if(null == otherNamespaceDefinition)
                throw new ArgumentNullException("otherNamespaceDefinition");

            NamespaceDefinition combinedNamespace = Merge<NamespaceDefinition>(this, otherNamespaceDefinition);
            combinedNamespace.Name = this.Name;
            return combinedNamespace;
        }

        protected override string ComputeMergeId() {
            if(IsGlobal) {
                return "NG";
            } else if(IsAnonymous) {
                return base.ComputeMergeId();
            } else {
                return String.Format("{0}:N:{1}", KsuAdapter.GetLanguage(ProgrammingLanguage), this.Name);
            }
        }

        /// <summary>
        /// Returns the children of this namespace that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// If this is a global namespace, and the lanugage is C or C++, then only children that occur in the same file as, and prior to, the use will be returned.
        /// If there are no such children, then all matching children will be returned.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public override IEnumerable<T> GetNamedChildren<T>(NameUse use, bool searchDeclarations) {
            if(use == null) { throw new ArgumentNullException("use"); }
            var matches = base.GetNamedChildren<T>(use, searchDeclarations);
            if(IsGlobal && (ProgrammingLanguage == Language.C || ProgrammingLanguage == Language.CPlusPlus)) {
                Func<INamedEntity, bool> occursBeforeUse = delegate(INamedEntity match) {
                                                               var matchLocs = match.GetLocations().ToList();
                                                               if(matchLocs.Count == 1
                                                                  && string.Compare(matchLocs[0].SourceFileName, use.Location.SourceFileName, StringComparison.OrdinalIgnoreCase) == 0
                                                                  && PositionComparer.CompareLocation(matchLocs[0], use.Location) >= 0) {
                                                                   //match occurs exclusively after the use, so don't include
                                                                   return false;
                                                               }
                                                               return true;
                                                           };
                return matches.Where(m => occursBeforeUse(m));
            } else {
                return matches;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            if(Accessibility == AccessModifier.None) {
                return string.Format("namespace {0}", Name);
            } else {
                return string.Format("{0} namespace {1}", Accessibility.ToKeywordString(), Name);
            }
        }
    }
}