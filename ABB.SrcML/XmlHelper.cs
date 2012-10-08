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

namespace ABB.SrcML
{
    /// <summary>
    /// Collection of static functions for working with XML.
    /// </summary>
    internal static class XmlHelper
    {
        /// <summary>
        /// Stream elements that have the given name.
        /// This uses the technique defined http://msdn.microsoft.com/en-us/library/bb387008(v=vs.90).aspx
        /// with modifications from here: http://social.msdn.microsoft.com/Forums/en/xmlandnetfx/thread/da366348-f209-433f-bb3b-8b5615409fe0
        /// </summary>
        /// <param name="fileName">the filename to stream elements from</param>
        /// <param name="name">The XName to find in the XML document</param>
        /// <param name="minimumDepth">The minimum depth to find elements at in the DOM tree</param>
        /// <returns>elements from this XML document with name <paramref name="name"/></returns>
        public static IEnumerable<XElement> StreamElements(string fileName, XName name, int minimumDepth = 1)
        {
            using (XmlReader reader = XmlReader.Create(fileName))
            {
                IXmlLineInfo xmlLineInfo = reader as IXmlLineInfo;

                if (null == xmlLineInfo)
                    throw new XmlException("reader could not be cast to an IXmlLineInfo object");

                reader.MoveToContent();
                XElement node = null;

                while (!reader.EOF)
                {
                    if (reader.Depth >= minimumDepth &&
                        reader.NodeType == XmlNodeType.Element &&
                        reader.NamespaceURI == name.Namespace &&
                        reader.Name == name.LocalName)
                    {
                        //node = XElement.ReadFrom(reader) as XElement;
                        node = ReadWithLineInfo(reader, xmlLineInfo) as XElement;
                        if (node != null)
                            yield return node;
                        reader.Read();
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
        }

        /// <summary>
        /// Read an XNode from the given XmlReader and LineInfo object. If available, line info will be added to XElement.
        /// This technique is adapted from here: http://blogs.msdn.com/b/mikechampion/archive/2006/09/10/748408.aspx
        /// </summary>
        /// <param name="reader">The XmlReader to read from</param>
        /// <param name="lineInfo">This should be <paramref name="reader"/> cast as an <see cref="IXmlLineInfo"/></param>
        /// <returns>an XNode with line information if present</returns>
        /// <seealso cref="XNode.ReadFrom">This function replaces XNode.ReadFrom</seealso>
        public static XNode ReadWithLineInfo(XmlReader reader, IXmlLineInfo lineInfo)
        {
            XNode node = null;
            XElement parent = null;

            do
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        // create a new element with the given name
                        XElement element = new XElement(XName.Get(reader.LocalName, reader.NamespaceURI));

                        // add attributes to the element
                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                // Compound documents created with older versions of ABB.SrcML left out some namespaces
                                // this causes an "xmlns" attribute to be added to any names not in the default (SRC) namespace
                                // to avoid an ArgumentException thrown by element.Add in this case, just don't add these
                                if (!(reader.LocalName == "xmlns" && reader.NamespaceURI == "http://www.w3.org/2000/xmlns/"))
                                    element.Add(new XAttribute(XName.Get(reader.LocalName, reader.NamespaceURI), reader.Value));
                            } while (reader.MoveToNextAttribute());
                            reader.MoveToElement();
                        }
                        // add a ABB.SrcML.LineInfo annotation to the element if line information is present.
                        if (lineInfo.HasLineInfo())
                        {
                            element.SetLineInfo(new LineInfo(lineInfo.LineNumber, lineInfo.LinePosition));
                        }

                        // if the reader is not empty, we have to go and get all of the children and add them.
                        // otherwise, we can jsut set this to node.
                        if (!reader.IsEmptyElement)
                        {
                            if (null != parent)
                            {
                                parent.Add(element);
                            }
                            parent = element;
                            continue;
                        }
                        else
                        {
                            node = element;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        // process the EndElement
                        if (null == parent)
                            return null;
                        if (parent.IsEmpty)
                        {
                            parent.Add(string.Empty);
                        }
                        if (parent.Parent == null)
                            return parent;
                        parent = parent.Parent;
                        continue;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        node = new XText(reader.Value);
                        break;
                    case XmlNodeType.CDATA:
                        node = new XCData(reader.Value);
                        break;
                    case XmlNodeType.Comment:
                        node = new XComment(reader.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        node = new XProcessingInstruction(reader.Name, reader.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        node = new XDocumentType(reader.LocalName, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        reader.ResolveEntity();
                        continue;
                    case XmlNodeType.XmlDeclaration:
                    case XmlNodeType.EndEntity:
                        continue;
                    default:
                        throw new InvalidOperationException();
                }

                if (null == parent)
                    return node;
                parent.Add(node);
            } while (reader.Read());
            return null;
        }
    }
}
