﻿/******************************************************************************
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
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    public class Expression : AbstractProgramElement {
        private List<Expression> componentsList;
        private Statement parentStmt;

        /// <summary>XML name for <see cref="Components"/></summary>
        public const string XmlComponentsName = "Components";

        /// <summary>XML name for serialization</summary>
        public const string XmlName = "Expression";

        public Expression() {
            componentsList = new List<Expression>();
            Components = new ReadOnlyCollection<Expression>(componentsList);
        }

        public ReadOnlyCollection<Expression> Components { get; private set; }
        public Expression ParentExpression { get; set;}
        public Statement ParentStatement {
            get { return parentStmt; }
            set {
                parentStmt = value;
                //all sub-expressions should also have the same parent statement
                foreach(var c in componentsList) {
                    c.ParentStatement = value;
                }
            }
        }
        public SrcMLLocation Location { get; set; }

        /// <summary>
        /// Adds the given Expression to the Components collection. Nothing will be done if <paramref name="component"/> is null.
        /// </summary>
        /// <param name="component">The component to add.</param>
        public virtual void AddComponent(Expression component) {
            if(null != component) {
                component.ParentExpression = this;
                componentsList.Add(component);
            }
        }

        /// <summary>
        /// Adds the given Expressions to the Components collection.
        /// </summary>
        /// <param name="components">The components to add.</param>
        public virtual void AddComponents(IEnumerable<Expression> components) {
            foreach(var c in components) {
                AddComponent(c);
            }
        }

        /// <summary>
        /// Returns the parent expression.
        /// </summary>
        protected override AbstractProgramElement GetParent() {
            return ParentExpression;
        }

        /// <summary>
        /// Returns the child expressions.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return componentsList;
        }

        /// <summary>
        /// Gets all of the parent expressions of this expression.
        /// </summary>
        /// <returns>The parents of this expression.</returns>
        public new IEnumerable<Expression> GetAncestors() {
            return base.GetAncestors().Cast<Expression>();
        }

        /// <summary>
        /// Gets all of parent expressions of this expression as well as this expression.
        /// </summary>
        /// <returns>This expression followed by its parents.</returns>
        public new IEnumerable<Expression> GetAncestorsAndSelf() {
            return base.GetAncestorsAndSelf().Cast<Expression>();
        }

        /// <summary>
        /// Gets all of the descendant expressions of this expression. This is every expression that is rooted at this expression.
        /// </summary>
        /// <returns>The descendants of this expression.</returns>
        public new IEnumerable<Expression> GetDescendants() {
            return base.GetDescendants().Cast<Expression>();
        }

        /// <summary>
        /// Gets all of the descendants of this expression as well as the expression itself.
        /// </summary>
        /// <returns>This expression, followed by all of its descendants.</returns>
        public new IEnumerable<Expression> GetDescendantsAndSelf() {
            return base.GetDescendantsAndSelf().Cast<Expression>();
        }

        public override string GetXmlName() { return Expression.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(SrcMLLocation.XmlName == reader.Name) {
                Location = XmlSerialization.DeserializeSrcMLLocation(reader);
            } else if(XmlComponentsName == reader.Name) {
                AddComponents(XmlSerialization.ReadChildExpressions(reader));
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Location) {
                XmlSerialization.WriteElement(writer, Location);
            }
            XmlSerialization.WriteCollection<Expression>(writer, XmlComponentsName, Components);
		}
		
        /// <summary> Returns a string representation of this expression. </summary>
        public override string ToString() {
            return string.Join(" ", componentsList);
        }
    }

    
}
