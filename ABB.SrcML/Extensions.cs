/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// Extensions for working with SrcML documents
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the X path that uniquely identifies the given XElement relative to to the containing file unit.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>An XPath query that uniquely identifies <paramref name="element"/></returns>
        public static string GetXPath(this XElement element)
        {
            return GetXPath(element, true);
        }

        /// <summary>
        /// Gets an XPath query that uniquely identifies the given XElement
        /// </summary>
        /// <param name="element">The element to create an XPath query for</param>
        /// <param name="relativeToFileUnit">whether or not the XPath query is relative to the parent file unit or not</param>
        /// <returns>An XPath query that uniquely identifies <paramref name="element"/></returns>
        public static string GetXPath(this XElement element, bool relativeToFileUnit)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            StringBuilder xpathBuilder = new StringBuilder();

            XElement current = element;

            do
            {
                if (SRC.Unit == current.Name)
                {
                    if (relativeToFileUnit)
                    {
                        current = null;
                        continue;
                    }
                    var fileName = current.Attribute("filename");
                    if (fileName != null)
                    {
                        xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "[@filename=\"{0}\"]", fileName.Value));
                    }
                }
                else
                {
                    var count = current.ElementsBeforeSelf(current.Name).Count() + 1;
                    xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "[{0}]", count));
                }

                var prefix = SrcML.NamespaceManager.LookupPrefix(current.Name.NamespaceName);

                xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "{0}", current.Name.LocalName));
                if (!String.IsNullOrEmpty(prefix))
                {
                    xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "{0}:", prefix));
                }
                xpathBuilder.Insert(0, "/");
                current = (null == current.Parent ? null : current.Parent);
            } while (null != current);
            return xpathBuilder.ToString();
        }

        /// <summary>
        /// Returns the parent statement (either expr_stmt, or decl_stmt) of the given node.
        /// </summary>
        /// <param name="node">The node to search from.</param>
        /// <returns>the parent element for <paramref name="node"/>. It will be either <see cref="SRC.ExpressionStatement"/> or <see cref="SRC.DeclarationStatement"/></returns>
        public static XElement ParentStatement(this XNode node)
        {
            if (null == node)
                throw new ArgumentNullException("node");

            var ancestors = node.Ancestors().Where(a => a.Name.LocalName.EndsWith("_stmt", StringComparison.Ordinal));

            if (ancestors.Any())
                return ancestors.First();
            return null;
        }

        private static LineInfo GetLineInfo(XElement element)
        {
            IXmlLineInfo ie = (IXmlLineInfo)element;

            if (ie.HasLineInfo())
                return new LineInfo(ie.LineNumber, ie.LinePosition);
            else
                return element.Annotation<ABB.SrcML.LineInfo>();
        }
        /// <summary>
        /// Gets the line number for the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line number that the given element is found on; -1 if the data is not present</returns>
        public static int GetXmlLineNumber(this XElement element)
        {
            LineInfo li = GetLineInfo(element);
            if (null == li)
                return -1;
            return li.LineNumber;
        }

        /// <summary>
        /// Gets the line position for the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line number that the given element is found on; -1 if the data is not present</returns>
        public static int GetXmlLinePosition(this XElement element)
        {
            LineInfo li = GetLineInfo(element);
            if (null == li)
                return -1;
            return li.Position;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static XAttribute GetAttributeFromSelfOrDescendant(XElement element, XName name)
        {
            XAttribute attribute = element.Attribute(name);

            if (null == attribute)
            {
                attribute = (from child in element.Descendants()
                             let childAttribute = child.Attribute(name)
                             where childAttribute != null
                             select childAttribute).FirstOrDefault();
            }

            return attribute;
        }

        /// <summary>
        /// Gets the line of source code that contains the given element.
        /// <para>This differs from <see cref="GetXmlLineNumber"/> in that this is the number of lines relative
        /// to the current <see cref="SRC.Unit"/>; this matches to the line number in the original source file.</para>
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line of source code; -1 if that info is not found.</returns>
        public static int GetSrcLineNumber(this XElement element)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            int lineNumber = -1;

            var srcLineAttribute = GetAttributeFromSelfOrDescendant(element, POS.Line);
            if(null != srcLineAttribute && Int32.TryParse(srcLineAttribute.Value, out lineNumber)) {
                return lineNumber;
            }

            int xmlLineNum = element.GetXmlLineNumber();

            // if no line info is present, just return -1
            // we may want to look at calculating the line number based on the text in the file (see GetSrcLinePosition below)
            if (-1 == xmlLineNum)
                return -1;

            // if th element is a unit, just return 0: Source line number doesn't make sense for a file.
            if (SRC.Unit == element.Name)
                return 1;

            // get the xml line number of the file unit that contains this element
            var unit = element.Ancestors(SRC.Unit).First();
            int fileStart = unit.GetXmlLineNumber();

            // the line number is just the difference between the xml line number and the xml line number of the unit
            lineNumber = xmlLineNum - fileStart + 1;
            return lineNumber;
        }

        /// <summary>
        /// Gets the ending source line number.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>the last line number this element occupies</returns>
        public static int GetEndingSrcLineNumber(this XElement element)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            var descendants = element.DescendantsAndSelf();

            return descendants.Last().GetSrcLineNumber();
        }

        /// <summary>
        /// Gets the original source column number that the given element starts on.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The column number that this element starts on. This will return 0 if the element is a Unit and -1 if no line information is present.</returns>
        public static int GetSrcLinePosition(this XElement element)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            // if element is a unit, just return 0: Source line position does not make sense for a file.
            if(SRC.Unit == element.Name)
                return 0;

            var srcPositionAttribute = GetAttributeFromSelfOrDescendant(element, POS.Column);
            int columnNumber = -1;

            if(null != srcPositionAttribute && Int32.TryParse(srcPositionAttribute.Value, out columnNumber)) {
                return columnNumber;
            }

            return -1;
            // if no line info is present, just return -1
            // This isn't technically necessary: position is computed without relying on the xml position. However, the position will most likely be useless without
            // the line number, and the line number calculation does rely on having line information.
            //if (element.GetXmlLinePosition() == -1)
            //    return -1;



            //bool reachedNewLineOrBeginning = false;
            //XElement currentNode = element;
            //Stack<string> stack = new Stack<string>();
            //string text;

            //while (!reachedNewLineOrBeginning)
            //{
            //    // get the text in reverse order that is
            //    // 1. in the parent node
            //    // 2. before currentNode (if we don't do this, we get 
            //    var textNodes = (from node in currentNode.Parent.DescendantNodes()
            //                     where node.IsBefore(currentNode)
            //                     where XmlNodeType.Text == node.NodeType
            //                     select (node as XText).Value).Reverse();

            //    // take each node and put it on the stack
            //    foreach (var node in textNodes)
            //    {
            //        stack.Push(node);

            //        // We can stop searching once we find a newline because that indicates we've moved to the previous line.
            //        if (node.Contains('\n'))
            //        {
            //            reachedNewLineOrBeginning = true;
            //            break;
            //        }
            //    }

            //    // set currentNode to it's parent. Stop searching if:
            //    // 1. current node has no parent
            //    // 2. the parent is a file unit
            //    currentNode = currentNode.Parent;
            //    if (null == currentNode || SRC.Unit == currentNode.Name)
            //        reachedNewLineOrBeginning = true;
            //}

            //// assemble all of the text we currently have by popping them off of the stack
            //using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            //{
            //    while (stack.Count > 0)
            //        sw.Write(stack.Pop());
            //    text = sw.ToString();
            //}

            //// check if the assembled text has a newline.
            //// If it doesn't, then the source line position is the length of the assembled text + 1
            //// If it does, then the source line position is the length of the assembled text *after the last newline* + 1
            //int newLineIndex = text.LastIndexOf('\n');
            //int srcLinePosition;
            //if (-1 == newLineIndex)
            //{
            //    srcLinePosition = text.Length + 1;
            //}
            //else
            //{
            //    srcLinePosition = text.Substring(newLineIndex + 1).Length + 1;
            //}
            //return srcLinePosition;
        }

        /// <summary>
        /// Adds line info to the given XObject.
        /// </summary>
        /// <param name="xmlObject">the XObject</param>
        /// <param name="lineInfo">a LineInfo object. This object is added as an annotation to <paramref name="xmlObject"/></param>
        public static void SetLineInfo(this XObject xmlObject, ABB.SrcML.LineInfo lineInfo)
        {
            if (null == xmlObject)
                throw new ArgumentNullException("xmlObject");

            xmlObject.AddAnnotation(lineInfo);
        }

        /// <summary>
        /// Converts the tree rooted at the given element to source code.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The source code in a string</returns>
        public static string ToSource(this XElement element)
        {
            return ToSource(element, 0);
        }

        /// <summary>
        /// Converts the tree rooted at the given element to source code.
        /// <para>It optionally converts tab to spaces.</para>
        /// </summary>
        /// <param name="element">The element to convert.</param>
        /// <param name="spacesPerTab">The number of spaces to convert each tab to; if zero, no conversion is done.</param>
        /// <returns>The source code in a string.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static string ToSource(this XElement element, int spacesPerTab)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            XmlReader r = element.CreateReader();

            using (StringWriter sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                while (r.Read())
                {
                    switch (r.NodeType)
                    {
                        case XmlNodeType.Element:
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            sw.Write(r.Value);
                            break;
                        default:
                            break;
                    }
                }
                var source = sw.ToString();
                if (spacesPerTab > 0)
                {
                    using (StringWriter spaces = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        for (int i = 0; i < spacesPerTab; i++)
                            spaces.Write(" ");
                        source = source.Replace("\t", spaces.ToString());
                    }
                }
                return source.Replace("\n", Environment.NewLine);
            }
        }

        #region UNUSED
        /// <summary>
        /// Checks whether the given container contains a call to the specified function.
        /// </summary>
        /// <param name="container">The container to test.</param>
        /// <param name="functionName">The function name to look for.</param>
        /// <returns>True if the call exists, false if not.</returns>
        public static bool ContainsCallTo(this XContainer container, string functionName)
        {
            if (null == container)
                throw new ArgumentNullException("container");

            return container.Descendants(SRC.Call).Where(c => c.Element(SRC.Name).Value == functionName).Any();
        }

        /// <summary>
        /// Checks whether the element is a declaration statement for a variable of the specified type.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="typeName">The typename to look for.</param>
        /// <returns>True if this is a declaration for the given type; false if not.</returns>
        public static bool IsDeclOfType(this XElement element, string typeName)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            return element.Name == SRC.DeclarationStatement &&
                   (from decl in element.Descendants(SRC.Declaration)
                    where decl.Elements(SRC.Type).Any()
                    where decl.Element(SRC.Type).Value == typeName
                    select decl).Any();
        }

        /// <summary>
        /// Gets the local declaration corresponding to the given name.
        /// </summary>
        /// <param name="name">A <see cref="SRC"/> element.</param>
        /// <returns>The corresponding declaration, null if not found.</returns>
        public static XElement GetLocalDecl(this XElement name)
        {
            if (null == name)
                throw new ArgumentNullException("name");

            SrcMLHelper.ThrowExceptionOnInvalidName(name, SRC.Name);

            var decls = from d in name.Ancestors(SRC.Function).First().Descendants(SRC.Declaration)
                        where d.Elements(SRC.Name).Any()
                        where d.IsBefore(name) && d.Element(SRC.Name).Value == name.Value
                        select d;
            return decls.Last();
        }
        #endregion
    }
}
