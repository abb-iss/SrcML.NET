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
using System.Xml.Serialization;

namespace ABB.SrcML.Data {
    [XmlRoot(IsNullable=false)]
    public class Statement {
        private List<Statement> childStatementsList;
        protected List<SrcMLLocation> LocationList;
        
        public Statement() {
            childStatementsList = new List<Statement>();
            ChildStatements = new ReadOnlyCollection<Statement>(childStatementsList);
            LocationList = new List<SrcMLLocation>(1);
            Locations = new ReadOnlyCollection<SrcMLLocation>(LocationList);
        }
        
        [XmlArray(ElementName="Children")]
        public ReadOnlyCollection<Statement> ChildStatements { get; private set; }

        [XmlIgnore]
        public Statement ParentStatement { get; set; }

        public Language ProgrammingLanguage { get; set; }

        public Expression Content {get; set;}

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

        /// <summary>
        /// Adds the given Statement to the ChildStatements collection.
        /// </summary>
        /// <param name="child">The Statement to add.</param>
        public virtual void AddChildStatement(Statement child) {
            if(child == null) { throw new ArgumentNullException("child"); }

            child.ParentStatement = this;
            childStatementsList.Add(child);
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
        /// Gets all of the parents of this statement
        /// </summary>
        /// <returns>The parents of this statement</returns>
        public IEnumerable<Statement> GetAncestors() {
            return GetAncestorsAndStartingPoint(this.ParentStatement);
        }

        /// <summary>
        /// Gets all of the parents of type <typeparamref name="T"/> of this statement.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent statements by</typeparam>
        /// <returns>The parents of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestors<T>() where T : Statement {
            return GetAncestorsAndStartingPoint(this.ParentStatement).OfType<T>();
        }

        /// <summary>
        /// Gets all of parents of this statement as well as this statement.
        /// </summary>
        /// <returns>This statement followed by its parents</returns>
        public IEnumerable<Statement> GetAncestorsAndSelf() {
            return GetAncestorsAndStartingPoint(this);
        }

        /// <summary>
        /// Gets all of the parents of this statement as well as the statement itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent statements by</typeparam>
        /// <returns>This statement followed by its parent statements where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestorsAndSelf<T>() where T : Statement {
            return GetAncestorsAndStartingPoint(this).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendant statements of this statement. This is every statement that is rooted at this statement.
        /// </summary>
        /// <returns>The descendants of this statement</returns>
        public IEnumerable<Statement> GetDescendants() {
            return GetDescendants(this, false);
        }

        /// <summary>
        /// Gets all of the descendant statements of this statement where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant statements by</typeparam>
        /// <returns>The descendants of type <typeparamref name="T"/> of this statement</returns>
        public IEnumerable<T> GetDescendants<T>() where T : Statement {
            return GetDescendants(this, false).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendants of this statement as well as the statement itself.
        /// </summary>
        /// <returns>This statement, followed by all of its descendants</returns>
        public IEnumerable<Statement> GetDescendantsAndSelf() {
            return GetDescendants(this, true);
        }

        /// <summary>
        /// Gets all of the descendants of this statement as well as the statement itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant statements by</typeparam>
        /// <returns>This statement and its descendants where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetDescendantsAndSelf<T>() where T : Statement {
            return GetDescendants(this, true).OfType<T>();
        }

        public virtual bool CanBeMergedWith(Statement otherStatement) {
            return this.ComputeMergeId() == otherStatement.ComputeMergeId();
        }

        protected void ClearChildren() {
            childStatementsList.Clear();
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

        public virtual Collection<Statement> RemoveFile(string fileName) {
            int definitionLocations = 0;
            for(int i = LocationList.Count - 1; i >= 0; i--) {
                if(fileName.Equals(LocationList[i].SourceFileName, StringComparison.InvariantCultureIgnoreCase)) {
                    LocationList.RemoveAt(i);
                } else if(!LocationList[i].IsReference) {
                    ++definitionLocations;
                }
            }

            if(0 == Locations.Count) {
                ParentStatement = null;
            } else {
                for(int i = ChildStatements.Count - 1; i >= 0; i--) {
                    var result = ChildStatements[i].RemoveFile(fileName);
                    if(null == ChildStatements[i].ParentStatement) {
                        childStatementsList.RemoveAt(i);
                    }
                }
            }

            return null;
        }

        protected static T Merge<T>(T firstStatement, T secondStatement) where T : Statement, new() {
            T combinedStatement = new T();
            combinedStatement.ProgrammingLanguage = firstStatement.ProgrammingLanguage;
            combinedStatement.AddLocations(firstStatement.LocationList.Concat(secondStatement.LocationList));
            combinedStatement.AddChildStatements(firstStatement.ChildStatements.Concat(secondStatement.ChildStatements));
            combinedStatement.RestructureChildren();
            return combinedStatement;
        }

        protected virtual void RestructureChildren() {
            var restructuredChildren = RestructureChildren(ChildStatements);
            ClearChildren();
            AddChildStatements(restructuredChildren);
        }

        protected static List<Statement> RestructureChildren(IEnumerable<Statement> childStatements) {
            OrderedDictionary childStatementMap = new OrderedDictionary();
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
        /// Gets a statement and all of its ancestors
        /// </summary>
        /// <param name="startingPoint">The first node to return</param>
        /// <returns>The <paramref name="startingPoint"/> and all of it ancestors</returns>
        private static IEnumerable<Statement> GetAncestorsAndStartingPoint(Statement startingPoint) {
            var current = startingPoint;
            while(null != current) {
                yield return current;
                current = current.ParentStatement;
            }
        }

        /// <summary>
        /// Gets the <paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and all of the descendants of the <paramref name="startingPoint"/>.
        /// </summary>
        /// <param name="startingPoint">The starting point</param>
        /// <param name="returnStartingPoint">If true, return the starting point first. Otherwise, just return  the descendants.</param>
        /// <returns><paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and its descendants</returns>
        private static IEnumerable<Statement> GetDescendants(Statement startingPoint, bool returnStartingPoint) {
            if(returnStartingPoint) {
                yield return startingPoint;
            }

            foreach(var statement in startingPoint.ChildStatements) {
                foreach(var descendant in GetDescendants(statement, true)) {
                    yield return descendant;
                }
            }
        }
    }
}
