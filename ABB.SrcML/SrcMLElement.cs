/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ABB.SrcML.Utilities;

namespace ABB.SrcML {
    /// <summary>
    /// Contains static utility methods that act upon srcML XElements.
    /// </summary>
    public static class SrcMLElement {
        /// <summary>
        /// Gets the method signature from the method definition srcML element.
        /// </summary>
        /// <param name="methodElement">The srcML method element to extract the signature from.</param>
        /// <returns>The method signature</returns>
        public static string GetMethodSignature(XElement methodElement) {
            if(methodElement == null) {
                throw new ArgumentNullException("methodElement");
            }
            if(!(new[] { SRC.Function, SRC.Constructor, SRC.Destructor }).Contains(methodElement.Name)) {
                throw new ArgumentException(string.Format("Not a valid method element: {0}", methodElement.Name), "methodElement");
            }

            var sig = new StringBuilder();
            var paramListElement = methodElement.Element(SRC.ParameterList);
            //add all the text and whitespace prior to the parameter list
            foreach(var n in paramListElement.NodesBeforeSelf()) {
                if(n.NodeType == XmlNodeType.Element) {
                    sig.Append(((XElement)n).Value);
                } else if(n.NodeType == XmlNodeType.Text || n.NodeType == XmlNodeType.Whitespace || n.NodeType == XmlNodeType.SignificantWhitespace) {
                    sig.Append(((XText)n).Value);
                }
            }
            //add the parameter list
            sig.Append(paramListElement.Value);

            //convert whitespace chars to spaces and condense any consecutive whitespaces.
            return Regex.Replace(sig.ToString().Trim(), @"\s+", " ");
        }

        /// <summary>
        /// Gets the language for a unit element.
        /// 
        /// It throws an exception if the element is not a unit, has no language, or the language is invalid. <see cref="ABB.SrcML.Language"/>
        /// </summary>
        /// <param name="fileUnit">The file unit to get the language for</param>
        /// <returns>The language</returns>
        public static Language GetLanguageForUnit(XElement fileUnit) {
            if(fileUnit == null) {
                throw new ArgumentNullException("fileUnit");
            }
            if(fileUnit.Name != SRC.Unit) {
                throw new ArgumentException("Not a unit element", "fileUnit");
            }

            var languageAttribute = fileUnit.Attribute("language");

            if(null == languageAttribute) {
                throw new SrcMLException("unit contains no language attribute");
            }

            return KsuAdapter.GetLanguageFromString(languageAttribute.Value);
        }

        /// <summary>
        /// Returns the filename attribute in the given unit element.
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public static string GetFileNameForUnit(XElement fileUnit) {
            if(fileUnit == null) throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit) throw new ArgumentException("should be a SRC.Unit", "fileUnit");
            var fileNameAttribute = fileUnit.Attribute("filename");
            if(null != fileNameAttribute) {
                return fileNameAttribute.Value;
            }
            return null;
        }

        /// <summary>
        /// Loads an <see cref="System.Xml.Linq.XElement"/> from the file name with whitespae preserved and line info included
        /// </summary>
        /// <param name="xmlFileName">The srcml file name</param>
        /// <returns>An XElement</returns>
        public static XElement Load(string xmlFileName) {
            return XElement.Load(xmlFileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        }
    }
}
