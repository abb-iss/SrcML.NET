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
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a program scope that has a name.
    /// </summary>
    public class NamedScope : BlockStatement, INamedEntity {
        private NamePrefix _prefix;
        private Dictionary<string, List<NamedScope>> _nameCache;

        /// <summary> The XML name for NamedScope. </summary>
        public new const string XmlName = "NamedScope";

        /// <summary> XML Name for <see cref="Name" /> </summary>
        public const string XmlNameName = "Name";

        /// <summary> XML Name for <see cref="NamePrefix" /> </summary>
        public const string XmlPrefixName = "NamePrefix";

        /// <summary> XML Name for <see cref="Accessibility" /> </summary>
        public const string XmlAccessibilityName = "Accessibility";

        /// <summary> Creates an empty NamedScope. </summary>
        public NamedScope() : base() {
            Name = string.Empty;
            Accessibility = AccessModifier.None;
            PrefixIsResolved = true;
            _nameCache = new Dictionary<string, List<NamedScope>>(StringComparer.Ordinal);
        }

        /// <summary> The name of the scope. </summary>
        public string Name { get; set; }

        /// <summary>
        /// For C/C++ methods, this property gives the specified scope that the method is defined in.
        /// For example, in the method <code>int A::B::MyFunction(char arg);</code> the NamePrefix is A::B.
        /// </summary>
        public NamePrefix Prefix {
            get { return _prefix; }
            set {
                if(value != _prefix) {
                    _prefix = value;
                    if(_prefix != null) {
                        _prefix.ParentStatement = this;
                    }
                    PrefixIsResolved = (null == _prefix);
                }
            }
        }

        public bool PrefixIsResolved { get; private set; }

        /// <summary>
        /// The accessibility for this scope, e.g. public, private, etc.
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary>
        /// Adds the given Statement to the ChildStatements collection. Nothing will be done if <paramref name="child"/> is null.
        /// Updates the internal cache of named children, if appropriate.
        /// </summary>
        /// <param name="child">The Statement to add.</param>
        public override void AddChildStatement(Statement child) {
            base.AddChildStatement(child);
            AddNamedChild(child as NamedScope);
        }

        /// <summary>
        /// Removes <paramref name="child"/> from the ChildStatements collection.
        /// Updates the internal cache of named children, if appropriate.
        /// </summary>
        /// <param name="child">The child statement to remove.</param>
        public override void RemoveChild(Statement child) {
            base.RemoveChild(child);
            RemoveNamedChild(child as NamedScope);
        }

        /// <summary>
        /// Gets the full name by finding all of the named scope ancestors and combining them.
        /// </summary>
        /// <returns>The full name for this named scope</returns>
        public string GetFullName() {
            IEnumerable<string> names;
            if(PrefixIsResolved) {
                names = (from statement in GetAncestorsAndSelf<NamedScope>()
                         where !String.IsNullOrEmpty(statement.Name)
                         select statement.Name).Reverse();
            } else {
                names = from nameUse in Prefix.Names
                        select nameUse.Name;
                names = names.Concat(Enumerable.Repeat(this.Name, 1));
            }
            
            return string.Join(".", names).TrimEnd('.');
        }

        protected void ResetPrefix() {
            if(PrefixIsResolved && null != Prefix) {
                PrefixIsResolved = false;
                var originalRoot = this.GetAncestors<NamedScope>().Skip(Prefix.Names.Count()).FirstOrDefault();

                if(null != originalRoot && null != ParentStatement) {
                    ParentStatement.RemoveChild(this);
                    originalRoot.AddChildStatement(this);
                }
            }
        }

        protected void MapPrefix(NamedScope tail) {
            var data = Enumerable.Zip(Prefix.Names.Reverse(), tail.GetAncestorsAndSelf<NamedScope>(), (name, scope) => {
                return new {
                    IsValid = (name.Name == scope.Name),
                    Location = name.Location,
                    Scope = scope,
                };
            });
            foreach(var d in data) {
                if(d.IsValid) {
                    d.Scope.AddLocation(d.Location);
                } else {
                    throw new SrcMLException("not a valid scope for this prefix");
                }
            }
            PrefixIsResolved = true;
        }

        public override Statement Merge(Statement otherStatement) {
            return Merge(otherStatement as NamedScope);
        }

        public NamedScope Merge(NamedScope otherNamedScope) {
            if(null == otherNamedScope) {
                throw new ArgumentNullException("otherNamedScope");
            }

            return Merge<NamedScope>(this, otherNamedScope);
        }

        protected static new T Merge<T>(T firstStatement, T secondStatement) where T : NamedScope, new() {
            T combinedStatement = Statement.Merge<T>(firstStatement, secondStatement);
            combinedStatement.Name = firstStatement.Name;
            combinedStatement.Accessibility = (firstStatement.Accessibility >= secondStatement.Accessibility ? firstStatement.Accessibility : secondStatement.Accessibility);
            if(null != firstStatement.Prefix) {
                combinedStatement.Prefix = firstStatement.Prefix;
            } else if(null != secondStatement.Prefix) {
                combinedStatement.Prefix = secondStatement.Prefix;
            }
            combinedStatement.PrefixIsResolved = true;
            return combinedStatement;
        }

        public override void RemoveFile(string fileName) {
            RemoveLocations(fileName);
            RemoveFileFromChildren(fileName);

            if(ToBeDeleted) {
                var orphanedChildren = (from child in ChildStatements.OfType<NamedScope>()
                                        where !child.ToBeDeleted && null != child.Prefix
                                        select child).ToList();

                foreach(var child in orphanedChildren) {
                    child.ResetPrefix();
                }
                ParentStatement = null;
            }
        }
        
        protected override void RestructureChildren() {
            var children = Statement.RestructureChildren(ChildStatements);
            _nameCache.Clear();
            ClearChildren();
            AddChildStatements(children);

            var namedChildrenWithPrefixes = (from child in children.OfType<NamedScope>()
                                             where !child.PrefixIsResolved
                                             select child).ToList<NamedScope>();

            // 2. check to see if children with prefixes can be relocated
            foreach(var child in namedChildrenWithPrefixes) {
                var firstPossibleParent = child.Prefix.FindMatches().FirstOrDefault();
                if(null != firstPossibleParent) {
                    RemoveChild(child);
                    child.MapPrefix(firstPossibleParent);
                    firstPossibleParent.AddChildStatement(child);
                    firstPossibleParent.RestructureChildren();
                }
            }
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlNameName == reader.Name) {
                this.Name = reader.ReadElementContentAsString();
            } else if(XmlAccessibilityName == reader.Name) {
                this.Accessibility = AccessModifierExtensions.FromKeywordString(reader.ReadElementContentAsString());
            } else if(XmlPrefixName == reader.Name) {
                this.Prefix = XmlSerialization.ReadChildExpression(reader) as NamePrefix;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(!string.IsNullOrEmpty(Name)) {
                writer.WriteElementString(XmlNameName, Name);
            }

            string attribute = Accessibility.ToKeywordString();
            if(AccessModifier.None != Accessibility) {
                writer.WriteElementString(XmlAccessibilityName, Accessibility.ToKeywordString());
            }
            if(null != Prefix) {
                XmlSerialization.WriteElement(writer, Prefix, XmlPrefixName);
            }

            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns the children of this statement that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// The order of children within a NamedScope does not matter, so the location of the use is not taken into account.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public override IEnumerable<T> GetNamedChildren<T>(NameUse use, bool searchDeclarations) {
            if(use == null) { throw new ArgumentNullException("use"); }
            return GetNamedChildren<T>(use.Name, searchDeclarations);
        }

        /// <summary>
        /// Returns the children of this statement named <paramref name="name"/> and the given <typeparamref name="T"/>.
        /// This method searches only the immediate children.
        /// In order to speed up the search, this method consults the internal name cache to get the list of matching <see cref="NamedScope">named scopes</see>
        /// </summary>
        /// <typeparam name="T">The type to filter on</typeparam>
        /// <param name="name">The name to search for</param>
        /// <param name="searchDeclarations">Whether to search the child declaration statements for named entities.</param>
        /// <returns>Any children of this statement named <paramref name="name"/> of type <typeparamref name="T"/></returns>
        public override IEnumerable<T> GetNamedChildren<T>(string name, bool searchDeclarations) {
            List<NamedScope> resultsList = null;
            _nameCache.TryGetValue(name, out resultsList);
            IEnumerable<T> results = resultsList != null ? resultsList.OfType<T>() : Enumerable.Empty<T>();
            if(!searchDeclarations) { return results; }

            var decls = from declStmt in GetChildren().OfType<DeclarationStatement>()
                        from decl in declStmt.GetDeclarations().OfType<T>()
                        where string.Equals(decl.Name, name, StringComparison.Ordinal)
                        select decl;

            return results.Concat(decls);
        }
        /// <summary>
        /// Returns the locations where this entity appears in the source.
        /// </summary>
        public IEnumerable<SrcMLLocation> GetLocations() {
            return Locations;
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(Prefix != null) {
                yield return Prefix;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            if(Accessibility == AccessModifier.None) {
                return Name;
            } else {
                return string.Format("{0} {1}", Accessibility.ToKeywordString(), Name);
            }
        }

        #region Private Methods
        /// <summary>
        /// Adds <paramref name="namedChild"/> to the name cache.
        /// If <paramref name="namedChild"/> is null, then nothing happens.
        /// </summary>
        /// <param name="namedChild">The named child to add</param>
        private void AddNamedChild(NamedScope namedChild) {
            if(null == namedChild) { return; }
            List<NamedScope> cacheForName;
            if(_nameCache.TryGetValue(namedChild.Name, out cacheForName)) {
                cacheForName.Add(namedChild);
            } else {
                _nameCache[namedChild.Name] = new List<NamedScope>() { namedChild };
            }
        }

        /// <summary>
        /// Removes <paramref name="namedChild"/> from the name cache.
        /// If <paramref name="namedChild"/>is null, then nothing happens.
        /// </summary>
        /// <param name="namedChild">The named child to remove</param>
        private void RemoveNamedChild(NamedScope namedChild) {
            if(null == namedChild) { return; }
            List<NamedScope> cacheForName;
            if(_nameCache.TryGetValue(namedChild.Name, out cacheForName)) {
                cacheForName.Remove(namedChild);
                if(cacheForName.Count == 0) {
                    _nameCache.Remove(namedChild.Name);
                }
            }
        }
        #endregion Private Methods
    }
}