/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a statement in a program.
    /// </summary>
    public class Statement : AbstractProgramElement {
        private Expression contentExpression;
        
        /// <summary> Internal list of this statement's children. </summary>
        protected List<Statement> ChildStatementsList;
        
        /// <summary> Internal list of this statements locations. </summary>
        protected List<SrcMLLocation> LocationList;

        /// <summary>
        /// A collection of the AliasStatement and ImportStatements in the children of this statement.
        /// These aliases/imports are stored in reverse document order.
        /// The dictionary key is the file name.
        /// </summary>
        protected Dictionary<string, SortedSet<Statement>> AliasMap;
        
        /// <summary>XML name for the <see cref="ChildStatements"/> property</summary>
        public const string XmlChildrenName = "ChildStatements";

        /// <summary>XML name for the <see cref="Content"/> property</summary>
        public const string XmlContentName = "Content";

        /// <summary>XML name for the <see cref="Locations"/> property</summary>
        public const string XmlLocationsName = "Locations";
        
        /// <summary>XML name for serialization</summary>
        public const string XmlName = "Statement";

        /// <summary>Creates a new empty Statement.</summary>
        public Statement() {
            ChildStatementsList = new List<Statement>();
            ChildStatements = new ReadOnlyCollection<Statement>(ChildStatementsList);
            LocationList = new List<SrcMLLocation>(1);
            Locations = new ReadOnlyCollection<SrcMLLocation>(LocationList);
            AliasMap = new Dictionary<string, SortedSet<Statement>>(StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// The statements that are nested below this one. 
        /// For example, the members of a class are children of the class statement, or the statements in an if-block are children of the if-statement.
        /// </summary>
        public ReadOnlyCollection<Statement> ChildStatements { get; private set; }

        /// <summary>The statement that this statement is a child of.</summary>
        public Statement ParentStatement { get; set; }

        /// <summary>The expression, if any, contained within the statement.</summary>
        public Expression Content {
            get { return contentExpression; }
            set {
                contentExpression = value;
                if(contentExpression != null) {
                    contentExpression.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// The locations in the code where this statement is defined. 
        /// There may be multiple locations in the case of, for example, a method definition that has separate prototype and definition statements.
        /// </summary>
        public ReadOnlyCollection<SrcMLLocation> Locations { get; private set; }

        /// <summary>
        /// The first non-reference location for the Statement.
        /// </summary>
        public SrcMLLocation PrimaryLocation {
            get {
                var definitionLoc = LocationList.FirstOrDefault(l => !l.IsReference);
                if(definitionLoc != null) {
                    return definitionLoc;
                }
                return LocationList.FirstOrDefault();
            }
        }

        protected virtual bool ToBeDeleted { get { return 0 == Locations.Count; } }
        /// <summary>
        /// Adds the given Statement to the ChildStatements collection. Nothing will be done if <paramref name="child"/> is null.
        /// </summary>
        /// <param name="child">The Statement to add.</param>
        public virtual void AddChildStatement(Statement child) {
            if(null == child) { return; }

            child.ParentStatement = this;
            ChildStatementsList.Add(child);
            if(child is AliasStatement || child is ImportStatement) {
                AddAliasStatement(child);
            }
        }

        /// <summary>
        /// Adds the given Statements to the ChildStatements collection.
        /// </summary>
        /// <param name="children">The Statements to add.</param>
        public void AddChildStatements(IEnumerable<Statement> children) {
            foreach(var child in children) {
                AddChildStatement(child);
            }
        }

        /// <summary>
        /// Add the given SrcMLLocation to the Locations collection.
        /// </summary>
        /// <param name="location">The location to add.</param>
        public virtual void AddLocation(SrcMLLocation location) {
            LocationList.Add(location);
        }

        /// <summary>
        /// Add the given SrcMLLocations to the Locations collection
        /// </summary>
        /// <param name="locations">The locations to add</param>
        public virtual void AddLocations(IEnumerable<SrcMLLocation> locations) {
            LocationList.AddRange(locations);
        }

        /// <summary>
        /// Finds all of the expressions in this statement of type <typeparamref name="TExpression"/>. This method searches all of the child
        /// expressions and their descendants.
        /// </summary>
        /// <typeparam name="TExpression">The expression type to search for</typeparam>
        /// <returns>All expressions in this statement of type <typeparamref name="TExpression"/></returns>
        public IEnumerable<TExpression> FindExpressions<TExpression>() where TExpression : Expression {
            return FindExpressions<TExpression>(false);
        }

        /// <summary>
        /// Finds all of the expressions in this statement of type <typeparamref name="TExpression"/>. This method searches all of the child
        /// expressions and their descendants.
        /// </summary>
        /// <typeparam name="TExpression">The expression type to search for</typeparam>
        /// <param name="searchDescendantStatements">If true, this will also return expressions from all of the descendant statements</param>
        /// <returns>All expressions rooted at this statement of type <typeparamref name="TExpression"/></returns>
        public IEnumerable<TExpression> FindExpressions<TExpression>(bool searchDescendantStatements) where TExpression : Expression {
            IEnumerable<TExpression> results;
            if(searchDescendantStatements) {
                results = from statement in GetDescendantsAndSelf()
                          from expr in statement.FindExpressions<TExpression>(false)
                          select expr;
            } else {
                results = from content in GetExpressions()
                          from expr in content.GetDescendantsAndSelf<TExpression>()
                          select expr;
            }
            
            return results;
        }

        /// <summary>
        /// Returns the parent statement.
        /// </summary>
        protected override AbstractProgramElement GetParent() {
            return ParentStatement;
        }

        /// <summary>
        /// Returns the child statements.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return ChildStatementsList;
        }

        /// <summary>
        /// Gets all of the parents of this statement
        /// </summary>
        /// <returns>The parents of this statement</returns>
        public new IEnumerable<Statement> GetAncestors() {
            return base.GetAncestors().Cast<Statement>();
        }

        /// <summary>
        /// Gets all of parents of this statement as well as this statement.
        /// </summary>
        /// <returns>This statement followed by its parents</returns>
        public new IEnumerable<Statement> GetAncestorsAndSelf() {
            return base.GetAncestorsAndSelf().Cast<Statement>();
        }

        /// <summary>
        /// Gets all of the descendant statements of this statement. This is every statement that is rooted at this statement.
        /// </summary>
        /// <returns>The descendants of this statement</returns>
        public new IEnumerable<Statement> GetDescendants() {
            return base.GetDescendants().Cast<Statement>();
        }

        /// <summary>
        /// Gets all of the descendants of this statement as well as the statement itself.
        /// </summary>
        /// <returns>This statement, followed by all of its descendants</returns>
        public new IEnumerable<Statement> GetDescendantsAndSelf() {
            return base.GetDescendantsAndSelf().Cast<Statement>();
        }

        /// <summary>
        /// Returns the siblings of this statement (i.e. the children of its parent) that occur before this statement.
        /// The siblings are returned in document order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This statement is not a child of its parent.</exception>
        public new IEnumerable<Statement> GetSiblingsBeforeSelf() {
            return base.GetSiblingsBeforeSelf().Cast<Statement>();
        }

        /// <summary>
        /// Returns the siblings of this statement (i.e. the children of its parent) that occur after this statement.
        /// The siblings are returned in document order.
        /// </summary>
        /// <exception cref="InvalidOperationException">This statement is not a child of its parent.</exception>
        public new IEnumerable<Statement> GetSiblingsAfterSelf() {
            return base.GetSiblingsAfterSelf().Cast<Statement>();
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public virtual IEnumerable<Expression> GetExpressions() {
            if(Content != null) {
                yield return Content;
            }
        }

        /// <summary>
        /// Returns the children of this statement that have the given name.
        /// This method searches only the immediate children, and not further descendants.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        public IEnumerable<INamedEntity> GetNamedChildren(string name) {
            return GetNamedChildren<INamedEntity>(name, true);
        }

        /// <summary>
        /// Returns the children of this statement that have the same name as the given <paramref name="use"/>.
        /// This method searches only the immediate children, and not further descendants.
        /// If the <paramref name="use"/> occurs within this statement, this method will return only the children
        /// that occur prior to that use.
        /// </summary>
        /// <param name="use">The use containing the name to search for.</param>
        public IEnumerable<INamedEntity> GetNamedChildren(NameUse use) {
            return GetNamedChildren<INamedEntity>(use, true);
        }

        /// <summary>
        /// Returns the children of this statement that have the given name, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="name">The name to search for.</param>
        public IEnumerable<T> GetNamedChildren<T>(string name) where T : INamedEntity {
            bool searchDeclarations = typeof(T).IsInterface || typeof(T).IsSubclassOf(typeof(Expression));
            return GetNamedChildren<T>(name, searchDeclarations);
        }

        /// <summary>
        /// Returns the children of this statement that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// If the <paramref name="use"/> occurs within this statement, this method will return only the children
        /// that occur prior to that use.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        public IEnumerable<T> GetNamedChildren<T>(NameUse use) where T : INamedEntity {
            bool searchDeclarations = typeof(T).IsInterface || typeof(T).IsSubclassOf(typeof(Expression));
            return GetNamedChildren<T>(use, searchDeclarations);
        }

        /// <summary>
        /// Returns the children of this statement that have the given name, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="name">The name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public virtual IEnumerable<T> GetNamedChildren<T>(string name, bool searchDeclarations) where T : INamedEntity {
            var scopes = GetChildren().OfType<T>().Where(ns => string.Equals(ns.Name, name, StringComparison.Ordinal));
            if(!searchDeclarations) { return scopes; }

            var decls = from declStmt in GetChildren().OfType<DeclarationStatement>()
                        from decl in declStmt.GetDeclarations().OfType<T>()
                        where string.Equals(decl.Name, name, StringComparison.Ordinal)
                        select decl;
            return scopes.Concat(decls);
        }

        /// <summary>
        /// Returns the children of this statement that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// If the <paramref name="use"/> occurs within this statement, this method will return only the children
        /// that occur prior to that use.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public virtual IEnumerable<T> GetNamedChildren<T>(NameUse use, bool searchDeclarations) where T : INamedEntity {
            if(use == null) { throw new ArgumentNullException("use"); }
            //location comparison is only valid if the use occurs within this statement (or its children)
            bool filterLocation = PrimaryLocation.Contains(use.Location);
            if(filterLocation) {
                var scopes = GetChildren().OfType<T>().Where(ns => string.Equals(ns.Name, use.Name, StringComparison.Ordinal)
                                                                   && PositionComparer.CompareLocation(PrimaryLocation, use.Location) < 0);
                if(!searchDeclarations) { return scopes; }

                //this will return the var decls in document order
                var decls = from declStmt in GetChildren().OfType<DeclarationStatement>()
                            where PositionComparer.CompareLocation(declStmt.PrimaryLocation, use.Location) < 0
                            from decl in declStmt.GetDeclarations().OfType<T>()
                            where string.Equals(decl.Name, use.Name, StringComparison.Ordinal)
                            select decl;
                return scopes.Concat(decls);
            } else {
                return GetNamedChildren<T>(use.Name, searchDeclarations);
            }
        }

        public virtual bool CanBeMergedWith(Statement otherStatement) {
            return this.ComputeMergeId() == otherStatement.ComputeMergeId();
        }

        /// <summary> Clears the <see cref="ChildStatements"/> collection. </summary>
        protected void ClearChildren() {
            ChildStatementsList.Clear();
        }

        protected virtual string ComputeMergeId() {
            return this.GetHashCode().ToString();
        }

        public virtual Statement Merge(Statement otherStatement) {
            if(null == otherStatement) {
                throw new ArgumentNullException("otherStatement");
            }
            return Merge<Statement>(this, otherStatement);
        }

        /// <summary>
        /// Removes <paramref name="child"/> from <see cref="ChildStatements"/>.
        /// </summary>
        /// <param name="child">The child statement to remove.</param>
        public virtual void RemoveChild(Statement child) {
            ChildStatementsList.Remove(child);
        }

        public virtual void RemoveFile(string fileName) {
            RemoveLocations(fileName);

            RemoveFileFromChildren(fileName);

            if(ToBeDeleted) {
                ParentStatement = null;
            }
        }

        protected static T Merge<T>(T firstStatement, T secondStatement) where T : Statement, new() {
            T combinedStatement = new T();
            combinedStatement.ProgrammingLanguage = firstStatement.ProgrammingLanguage;
            combinedStatement.AddLocations(firstStatement.LocationList.Concat(secondStatement.LocationList));
            combinedStatement.AddChildStatements(firstStatement.ChildStatements.Concat(secondStatement.ChildStatements));
            combinedStatement.RestructureChildren();
            return combinedStatement;
        }

        protected void RemoveLocations(string fileName) {
            for(int i = LocationList.Count - 1; i >= 0; i--) {
                if(fileName.Equals(LocationList[i].SourceFileName, StringComparison.OrdinalIgnoreCase)) {
                    LocationList.RemoveAt(i);
                }
            }
        }

        protected void RemoveFileFromChildren(string fileName) {
            for(int i = ChildStatements.Count - 1; i >= 0; i--) {
                ChildStatements[i].RemoveFile(fileName);
                if(ChildStatements[i].ToBeDeleted) {
                    ChildStatementsList.RemoveAt(i);
                }
            }
        }
        protected virtual void RestructureChildren() {
            var restructuredChildren = RestructureChildren(ChildStatements);
            ClearChildren();
            AddChildStatements(restructuredChildren);
        }

        protected static List<Statement> RestructureChildren(IEnumerable<Statement> childStatements) {
            OrderedDictionary childStatementMap = new OrderedDictionary(StringComparer.Ordinal);
            foreach(var child in childStatements) {
                string mergeId = child.ComputeMergeId();
                Statement mergedChild;
                if(childStatementMap.Contains(mergeId)) {
                    mergedChild = childStatementMap[mergeId] as Statement;
                    childStatementMap[mergeId] = mergedChild.Merge(child);
                } else {
                    childStatementMap[mergeId] = child;
                }
            }
            return new List<Statement>(childStatementMap.Values.OfType<Statement>());
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlChildrenName == reader.Name) {
                AddChildStatements(XmlSerialization.ReadChildStatements(reader));
            } else if(XmlLocationsName == reader.Name) {
                AddLocations(XmlSerialization.ReadChildSrcMLLocations(reader));
            } else if(XmlContentName == reader.Name) {
                Content = XmlSerialization.ReadChildExpression(reader);
            }
        }

        /// <summary> Returns the XML name for this program element. </summary>
        public override string GetXmlName() { return Statement.XmlName; }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            XmlSerialization.WriteCollection<SrcMLLocation>(writer, XmlLocationsName, Locations);
            
            XmlSerialization.WriteCollection<Statement>(writer, XmlChildrenName, ChildStatements);

            if(null != Content) {
                XmlSerialization.WriteElement(writer, Content, XmlContentName);
            }
        }

        /// <summary>
        /// Returns the children of this statement that are file specific and occur prior to the given location (and in the same file).
        /// File specific statements may include items such as ImportStatements or AliasStatements.
        /// The returned statements are sorted in reverse document order.
        /// </summary>
        /// <param name="loc">The location to find the file specific statements for.</param>
        public IEnumerable<Statement> GetFileSpecificStatements(SourceLocation loc) {
            SortedSet<Statement> allList;
            if(!AliasMap.TryGetValue(loc.SourceFileName, out allList)) { return Enumerable.Empty<ImportStatement>(); }
            if(allList == null) { return Enumerable.Empty<ImportStatement>(); }

            return allList.SkipWhile(s => ReversePositionComparer.CompareLocation(s.PrimaryLocation, loc) <= 0);
        }

        /// <summary>
        /// Returns the innermost statement that surrounds the given source location.
        /// </summary>
        /// <param name="loc">The source location to search for.</param>
        /// <returns>The lowest child of this statement that surrounds the given location, or null if it cannot be found.</returns>
        public Statement GetStatementForLocation(SourceLocation loc) {
            //first search in children
            var foundStmt = GetChildren().Cast<Statement>().Select(s => s.GetStatementForLocation(loc)).FirstOrDefault(s => s != null);
            //if loc not found, check ourselves
            if(foundStmt == null && this.ContainsLocation(loc)) {
                foundStmt = this;
            }
            return foundStmt;
        }

        /// <summary>
        /// Returns the innermost statement that surrounds the given source location.
        /// </summary>
        /// <param name="xpath">The xpath to search for.</param>
        /// <returns>The lowest child of this statement that contains the given xpath, or null if it cannot be found.</returns>
        public Statement GetStatementForLocation(string xpath) {
            //first search in children
            var foundStmt = GetChildren().Cast<Statement>().Select(s => s.GetStatementForLocation(xpath)).FirstOrDefault(s => s != null);
            //if loc not found, check ourselves
            if(foundStmt == null && this.ContainsLocation(xpath)) {
                foundStmt = this;
            }
            return foundStmt;
        }

        /// <summary>
        /// Returns true if this statement surrounds the given source location.
        /// </summary>
        /// <param name="loc">The source location to look for.</param>
        /// <returns>True if this is a container for the given location, False otherwise.</returns>
        public virtual bool ContainsLocation(SourceLocation loc) {
            return Locations.Any(l => l.Contains(loc));
        }

        /// <summary>
        /// Returns true if this statement contains the given XElement. A statement
        /// contains an element if <see cref="SrcMLLocation.XPath"/> is a prefix for the XPath for
        /// <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to look for</param>
        /// <returns>true if this statement contains <paramref name="element"/>. False otherwise.</returns>
        public bool ContainsLocation(XElement element) {
            return ContainsLocation(element.GetXPath());
        }

        /// <summary>
        /// Returns true if this statement contains the given XPath. A statement contains
        /// an xpath if <see cref="SrcMLLocation.XPath"/> is a prefix for <paramref name="xpath"/>
        /// </summary>
        /// <param name="xpath">The xpath to look for.</param>
        /// <returns>True if this statement contains the given xpath. False, otherwise.</returns>
        public virtual bool ContainsLocation(string xpath) {
            return Locations.Any(l => xpath.StartsWith(l.XPath));
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("{0};", Content);
        }

        #region Private Methods
        private void AddAliasStatement(Statement child) {
            var loc = child.PrimaryLocation;
            SortedSet<Statement> aliasList;
            AliasMap.TryGetValue(loc.SourceFileName, out aliasList);
            if(aliasList == null) {
                aliasList = new SortedSet<Statement>(new ReversePositionComparer());
                AliasMap[loc.SourceFileName] = aliasList;
            }

            aliasList.Add(child);
        }

        #endregion Private methods

        /// <summary>
        /// Sorts Statements based on their starting line/column, in document order.
        /// The file names are ignored.
        /// </summary>
        protected class PositionComparer : Comparer<Statement> {

            /// <summary>
            /// Returns a negative number if x comes before y, 0 if they are equal, or a positive number if x comes after y.
            /// </summary>
            public override int Compare(Statement x, Statement y) {
                if(object.Equals(x, y)) { return 0; }
                if(x == null) { return 1; }
                if(y == null) { return -1; }

                return CompareLocation(x.PrimaryLocation, y.PrimaryLocation);
            }

            /// <summary>
            /// Returns a negative number if x comes before y, 0 if they are equal, or a positive number if x comes after y.
            /// </summary>
            public static int CompareLocation(SourceLocation x, SourceLocation y) {
                if(object.Equals(x, y)) { return 0; }
                if(x == null) { return 1; }
                if(y == null) { return -1; }
                if(x.StartingLineNumber < y.StartingLineNumber ||
                   (x.StartingLineNumber == y.StartingLineNumber && x.StartingColumnNumber < y.StartingColumnNumber)) {
                    return -1;
                }
                if(x.StartingLineNumber == y.StartingLineNumber &&
                   x.StartingColumnNumber == y.StartingColumnNumber) {
                    return 0;
                }
                return 1;
            }
        }

        /// <summary>
        /// Sorts Statements based on their starting line/column, in reverse document order.
        /// The file names are ignored.
        /// </summary>
        protected class ReversePositionComparer : Comparer<Statement> {

            /// <summary>
            /// Returns a negative number if x comes before y, 0 if they are equal, or a positive number if x comes after y.
            /// </summary>
            public override int Compare(Statement x, Statement y) {
                if(object.Equals(x, y)) { return 0; }
                if(x == null) { return 1; }
                if(y == null) { return -1; }

                return CompareLocation(x.PrimaryLocation, y.PrimaryLocation);
            }

            /// <summary>
            /// Returns a negative number if x comes before y, 0 if they are equal, or a positive number if x comes after y.
            /// </summary>
            public static int CompareLocation(SourceLocation x, SourceLocation y) {
                if(object.Equals(x, y)) { return 0; }
                if(x == null) { return 1; }
                if(y == null) { return -1; }
                if(x.StartingLineNumber < y.StartingLineNumber ||
                   (x.StartingLineNumber == y.StartingLineNumber && x.StartingColumnNumber < y.StartingColumnNumber)) {
                    return 1;
                }
                if(x.StartingLineNumber == y.StartingLineNumber &&
                   x.StartingColumnNumber == y.StartingColumnNumber) {
                    return 0;
                }
                return -1;
            }
        }
    }
}
