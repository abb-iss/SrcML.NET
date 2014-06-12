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

using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ABB.SrcML.Data {
    /// <summary>
    /// An abstract class representing a thing in a program. This exists to hold functionality common to
    /// both Statements and Expressions.
    /// </summary>
    public abstract class AbstractProgramElement : IXmlElement {

        public const string LanguageXmlName = "lang";

        /// <summary>The language that this statement is written in.</summary>
        public Language ProgrammingLanguage { get; set; }

        /// <summary>Returns the parent of this element.</summary>
        protected abstract AbstractProgramElement GetParent();
        /// <summary>Returns the children of this element.</summary>
        protected abstract IEnumerable<AbstractProgramElement> GetChildren();

        public abstract string GetXmlName();

        /// <summary>
        /// Gets all of the parents of this element
        /// </summary>
        /// <returns>The parents of this element</returns>
        public IEnumerable<AbstractProgramElement> GetAncestors() {
            return GetAncestorsAndStartingPoint(this.GetParent());
        }

        /// <summary>
        /// Gets all of the parents of type <typeparamref name="T"/> of this element.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent elements by</typeparam>
        /// <returns>The parents of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestors<T>() where T : AbstractProgramElement {
            return GetAncestorsAndStartingPoint(this.GetParent()).OfType<T>();
        }

        /// <summary>
        /// Gets all of parents of this element as well as this element.
        /// </summary>
        /// <returns>This element followed by its parents</returns>
        public IEnumerable<AbstractProgramElement> GetAncestorsAndSelf() {
            return GetAncestorsAndStartingPoint(this);
        }

        /// <summary>
        /// Gets all of the parents of this element as well as the element itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the parent elements by</typeparam>
        /// <returns>This element followed by its parent elements where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetAncestorsAndSelf<T>() where T : AbstractProgramElement {
            return GetAncestorsAndStartingPoint(this).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendant elements of this statement. This is every element that is rooted at this element.
        /// </summary>
        /// <returns>The descendants of this statement</returns>
        public IEnumerable<AbstractProgramElement> GetDescendants() {
            return GetDescendants(this, false);
        }

        /// <summary>
        /// Gets all of the descendant elements of this element where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant elements by</typeparam>
        /// <returns>The descendants of type <typeparamref name="T"/> of this element</returns>
        public IEnumerable<T> GetDescendants<T>() where T : AbstractProgramElement {
            return GetDescendants(this, false).OfType<T>();
        }

        /// <summary>
        /// Gets all of the descendants of this element as well as the element itself.
        /// </summary>
        /// <returns>This element, followed by all of its descendants</returns>
        public IEnumerable<AbstractProgramElement> GetDescendantsAndSelf() {
            return GetDescendants(this, true);
        }

        /// <summary>
        /// Gets all of the descendants of this element as well as the element itself where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant elements by</typeparam>
        /// <returns>This element and its descendants where the type is <typeparamref name="T"/></returns>
        public IEnumerable<T> GetDescendantsAndSelf<T>() where T : AbstractProgramElement {
            return GetDescendants(this, true).OfType<T>();
        }

        /// <summary>
        /// Gets an element and all of its ancestors
        /// </summary>
        /// <param name="startingPoint">The first element to return</param>
        /// <returns>The <paramref name="startingPoint"/> and all of its ancestors</returns>
        protected static IEnumerable<AbstractProgramElement> GetAncestorsAndStartingPoint(AbstractProgramElement startingPoint) {
            var current = startingPoint;
            while(null != current) {
                yield return current;
                current = current.GetParent();
            }
        }

        public XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader) {
            reader.ReadStartElement();
            ReadAttributes(reader);

            while(XmlNodeType.Element == reader.NodeType) {
                ReadXmlChild(reader);
            }
            reader.ReadEndElement();
        }

        public virtual void WriteXml(XmlWriter writer) {
            WriteXmlAttributes(writer);
            WriteXmlContents(writer);
        }

        protected virtual void ReadAttributes(XmlReader reader) {
            ProgrammingLanguage = SrcMLElement.GetLanguageFromString(reader.GetAttribute(LanguageXmlName));
        }

        protected virtual void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString(LanguageXmlName, KsuAdapter.GetLanguage(ProgrammingLanguage));
        }

        protected abstract void ReadXmlChild(XmlReader reader);

        protected abstract void WriteXmlContents(XmlWriter writer);

        /// <summary>
        /// Gets the <paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and all of the descendants of the <paramref name="startingPoint"/>.
        /// </summary>
        /// <param name="startingPoint">The starting point</param>
        /// <param name="returnStartingPoint">If true, return the starting point first. Otherwise, just return  the descendants.</param>
        /// <returns><paramref name="startingPoint"/> (if <paramref name="returnStartingPoint"/> is true) and its descendants</returns>
        protected static IEnumerable<AbstractProgramElement> GetDescendants(AbstractProgramElement startingPoint, bool returnStartingPoint) {
            if(returnStartingPoint) {
                yield return startingPoint;
            }

            foreach(var element in startingPoint.GetChildren()) {
                foreach(var descendant in GetDescendants(element, true)) {
                    yield return descendant;
                }
            }
        }
    }
}
