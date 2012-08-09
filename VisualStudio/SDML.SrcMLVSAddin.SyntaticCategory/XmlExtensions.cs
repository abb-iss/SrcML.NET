/******************************************************************************
 * Copyright (c) 2011 Brian Bartman
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Brian Bartman (SDML) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - integration with ABB.SrcML Framework
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SDML.SrcMLVSAddin.SrcML.XMLExtensions
{

    public static class XmlExtensions
    {

        static XmlExtensions()
        {

        }

        /// <summary>
        /// Returns the xpath of a node.
        /// </summary>
        /// <param name="currentXObj">The xobject the function this is being called on.</param>
        /// <returns>String representation of of the xpath to this node.</returns>
        public static String GetXPath(this XObject currentXObj)
        {
            StringBuilder xPathBuilder = new StringBuilder();
            while (currentXObj != null)
            {
                switch (currentXObj.NodeType)
                {
                    // Handling XObjects.
                    case XmlNodeType.Attribute:
                        xPathBuilder.Insert(0, "/@" + ((XAttribute)currentXObj).Name);
                        currentXObj = currentXObj.Parent;
                        break;

                    // Handling XElements
                    case XmlNodeType.Element:
                        xPathBuilder.Insert(0, "/" + ((XElement)currentXObj).Name + "[" + ((XElement)currentXObj).IndexAsChild() + "]");
                        currentXObj = currentXObj.Parent;
                        break;
                    default:
                        throw new ArgumentException("Only elements and attributes are supported");
                }
            }
            return xPathBuilder.ToString();
        }

        /// <summary>
        /// Gets the index of an XElement as the child of it's parent.
        /// If the element doesn't have a parent then 1 is returned.
        /// </summary>
        /// <param name="node">The XElement to get the index of.</param>
        /// <returns>The index of the the XElement inside of it's parent's children.</returns>
        public static int IndexAsChild(this XElement node)
        {
            // if the node's parent is null then
            // this node is the root node.
            if (node.Parent == null)
            {
                return 1;
            }
            return node.ElementsBeforeSelf().Count() + 1;
        }

        /// <summary>
        /// Searches for a XText node with the supplied value and returns
        /// true if it finds the located value and false other wise.
        /// </summary>
        /// <param name="element">Extension element.</param>
        /// <param name="strValue">String to search for.</param>
        /// <returns>True if the located XText node with a given value.</returns>
        public static Boolean ContainsXTextNode(this XElement element, String strValue)
        {
            Func<XNode, bool> myFunc = (x) =>
            {
                if (!(x is XText))
                {
                    return false;
                }
                if (((XText)x).Value == strValue)
                    return true;
                return false;
            };
            var XTextNodes = from node in element.Nodes()
                             where myFunc(node)
                             select node;
            return XTextNodes.Count() > 0;
        }

        /// <summary>
        /// Searches for a XText node with the supplied regular expression
        /// and returns true of it located that node.
        /// </summary>
        /// <param name="element">Extension element.</param>
        /// <param name="strValue">String to search for.</param>
        /// <returns>True if the located XText node with a given value.</returns>
        public static Boolean ContainsXTextWith(this XElement element, Regex regex)
        {
            Func<XNode, bool> myFunc = (x) =>
            {
                if (!(x is XText))
                {
                    return false;
                }
                return regex.IsMatch(((XText)x).Value);
            };
            var XTextNodes = from node in element.Nodes()
                             where myFunc(node)
                             select node;
            return XTextNodes.Count() > 0;
        }

        /// <summary>
        /// Checks to see if an element contains a non-empty XText node.
        /// </summary>
        /// <param name="element">The element this function is being called on.</param>
        /// <returns>True if it locates an XText node with a value and other wise false.</returns>
        public static Boolean ContainsNonEmptyXText(this XElement element)
        {
            Regex tempRegex = new Regex(@"[^\s]*");
            Func<XNode, bool> myFunc = (x) =>
            {
                if (!(x is XText))
                {
                    return false;
                }
                return tempRegex.IsMatch(((XText)x).Value);
            };

            return element.Nodes().First(myFunc) != null;
        }

        /// <summary>
        /// Returns the name which can the qualified name of name of the element.
        /// </summary>
        /// <param name="element">XElement to get xpath name of.</param>
        /// <returns>String representation of the XPath name of the element.</returns>
        public static String GetXPathName(this XElement element)
        {
            if (element.Name.Namespace != "")
            {
                //XmlNamespaceManager namespaceManager = new XmlNamespaceManager(element.Document.CreateReader().NameTable);
                //namespaceManager.AddNamespace("src", "http://www.sdml.info/srcML/src");
                //namespaceManager.AddNamespace("cpp", "http://www.sdml.info/srcML/cpp");
                //namespaceManager.AddNamespace("lit", "http://www.sdml.info/srcML/literal");
                //namespaceManager.AddNamespace("op", "http://www.sdml.info/srcML/operator");
                //namespaceManager.AddNamespace("mod", "http://www.sdml.info/srcML/modifier");
                XmlNamespaceManager namespaceManager = ABB.SrcML.SrcML.NamespaceManager;
                return namespaceManager.LookupPrefix(element.Name.NamespaceName) + ":" + element.Name.LocalName.ToString();
            }
            return element.Name.LocalName;
        }

        /// <summary>
        /// Visitor delegate used when visiting each element
        /// in a depth first ordering.
        /// </summary>
        /// <param name="element"></param>
        public delegate void DepthFirstDelegate(XElement element);

        /// <summary>
        /// Depth First traversal function used to visit each of the
        /// differnt elements in a depth first ordering.
        /// </summary>
        /// <param name="element">Element which this function is being called on.</param>
        /// <param name="onVisit">A depth first visitor delegate. This is called the first time the node is reached.</param>
        /// <param name="onLeave">A depth first visitor delegate. This is called after the last child has been visited.</param>
        public static void DepthFirstVisit(this XElement element, DepthFirstDelegate onVisit, DepthFirstDelegate onLeave)
        {
            DFVImpl(element, onVisit, onLeave);
        }

        /// <summary>
        /// The depth first visit's implementation.
        /// </summary>
        /// <param name="onVisit">A depth first visitor delegate. This is called the first time the node is reached.</param>
        /// <param name="onLeave">A depth first visitor delegate. This is called after the last child has been visited.</param>
        private static void DFVImpl(XElement elem, DepthFirstDelegate onVisit, DepthFirstDelegate onLeave)
        {
            onVisit(elem);
            foreach (XElement childElement in elem.Elements())
            {
                DFVImpl(childElement, onVisit, onLeave);
            }
            onLeave(elem);
        }
    }
}