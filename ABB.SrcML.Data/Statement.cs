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
    /// <summary>
    /// Represents a statement in a program.
    /// </summary>
    public class Statement : AbstractProgramElement {
        private Expression contentExpression;
        
        /// <summary> Internal list of this statement's children. </summary>
        protected List<Statement> ChildStatementsList;
        
        /// <summary> Internal list of this statements locations. </summary>
        protected List<SrcMLLocation> LocationList;
        
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
            if(null != child) {
                child.ParentStatement = this;
                ChildStatementsList.Add(child);
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


        ///// <summary>
        ///// Returns the variables declared within this statement.
        ///// </summary>
        ///// <param name="recursive">If true, the method returns the variables declared within the statement's children. If false, it does not.</param>
        //public virtual IEnumerable<VariableDeclaration> GetDeclarations(bool recursive) {
        //    if(Content != null) {
        //        foreach(var decl in Content.GetDescendantsAndSelf<VariableDeclaration>()) {
        //            yield return decl;
        //        }
        //    }

        //    //TODO: fix recursiveness to properly handle block statements, where the user would want to get the immediate children (i.e. the things in the block)
        //    //but not any lower children.
        //    if(recursive) {
        //        foreach(var decl in GetChildren().OfType<Statement>().SelectMany(s => s.GetDeclarations(true))) {
        //            yield return decl;
        //        }
        //    }
        //}

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
        public void RemoveChild(Statement child) {
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
                if(fileName.Equals(LocationList[i].SourceFileName, StringComparison.InvariantCultureIgnoreCase)) {
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
    }
}
