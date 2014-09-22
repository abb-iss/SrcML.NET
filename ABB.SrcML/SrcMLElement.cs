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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

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
            var lastSigElement = methodElement.Element(SRC.ParameterList);
            if(lastSigElement == null) {
                lastSigElement = methodElement.Element(SRC.Name);
            }
            if(lastSigElement != null) {
                //add all the text and whitespace prior to the last element
                foreach(var n in lastSigElement.NodesBeforeSelf()) {
                    if(n.NodeType == XmlNodeType.Element) {
                        sig.Append(((XElement)n).Value);
                    } else if(n.NodeType == XmlNodeType.Text || n.NodeType == XmlNodeType.Whitespace || n.NodeType == XmlNodeType.SignificantWhitespace) {
                        sig.Append(((XText)n).Value);
                    }
                }
                //add the last element
                sig.Append(lastSigElement.Value);
            } else {
                //no name or parameter list, anonymous method?
            }

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

            return GetLanguageFromString(languageAttribute.Value);
        }

        /// <summary>
        /// Helper method to get a Language value from a string. This is primarily used by obsolete SrcML.cs APIs to interface with newer code that does use the Language enumeration.
        /// </summary>
        /// <param name="language">a string to convert</param>
        /// <returns>the Language value that corresponds to language.</returns>
        public static Language GetLanguageFromString(string language) {
            if("Any" == language)
                return Language.Any;
            else if("C++" == language)
                return Language.CPlusPlus;
            else if("C" == language)
                return Language.C;
            else if("Java" == language)
                return Language.Java;
            else if("AspectJ" == language)
                return Language.AspectJ;
            else if("C#" == language) {
                return Language.CSharp;
            }
            throw new SrcMLException(String.Format(CultureInfo.CurrentCulture, "{0} is not a valid language.", language));
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
            if(File.Exists(xmlFileName)) {
                using(var f = File.Open(xmlFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    return XElement.Load(f, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                }
            }
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static XAttribute GetAttributeFromSelfOrDescendant(XElement element, XName name) {
            XAttribute attribute = element.Attribute(name);

            if(null == attribute) {
                attribute = (from child in element.Descendants()
                             let childAttribute = child.Attribute(name)
                             where childAttribute != null
                             select childAttribute).FirstOrDefault();
            }

            return attribute;
        }

        /// <summary>
        /// Gets the X path that uniquely identifies the given XElement relative to to the containing file unit.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>An XPath query that uniquely identifies <paramref name="element"/></returns>
        public static string GetXPath(this XElement element) {
            return GetXPath(element, true);
        }

        /// <summary>
        /// Gets an XPath query that uniquely identifies the given XElement
        /// </summary>
        /// <param name="element">The element to create an XPath query for</param>
        /// <param name="relativeToFileUnit">whether or not the XPath query is relative to the parent file unit or not</param>
        /// <returns>An XPath query that uniquely identifies <paramref name="element"/></returns>
        public static string GetXPath(this XElement element, bool relativeToFileUnit) {
            if(null == element)
                throw new ArgumentNullException("element");

            StringBuilder xpathBuilder = new StringBuilder();

            XElement current = element;

            do {
                if(SRC.Unit == current.Name) {
                    if(relativeToFileUnit) {
                        current = null;
                        continue;
                    }
                    var fileName = current.Attribute("filename");
                    if(fileName != null) {
                        xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "[@filename=\"{0}\"]", fileName.Value));
                    }
                } else {
                    var count = current.ElementsBeforeSelf(current.Name).Count() + 1;
                    xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "[{0}]", count));
                }

                var prefix = SrcMLNamespaces.LookupPrefix(current.Name.NamespaceName);
                

                xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "{0}", current.Name.LocalName));
                if(!String.IsNullOrEmpty(prefix)) {
                    xpathBuilder.Insert(0, String.Format(CultureInfo.InvariantCulture, "{0}:", prefix));
                }
                xpathBuilder.Insert(0, "/");
                current = (null == current.Parent ? null : current.Parent);
            } while(null != current);
            return xpathBuilder.ToString();
        }

        /// <summary>
        /// Gets the line number for the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line number that the given element is found on; -1 if the data is not present</returns>
        public static int GetXmlLineNumber(this XElement element) {
            LineInfo li = GetLineInfo(element);
            if(null == li)
                return -1;
            return li.LineNumber;
        }

        /// <summary>
        /// Gets the line position for the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line number that the given element is found on; -1 if the data is not present</returns>
        public static int GetXmlLinePosition(this XElement element) {
            LineInfo li = GetLineInfo(element);
            if(null == li)
                return -1;
            return li.Position;
        }

        /// <summary>
        /// Gets the line of source code that contains the given element.
        /// <para>This differs from <see cref="GetXmlLineNumber"/> in that this is the number of lines relative
        /// to the current <see cref="SRC.Unit"/>; this matches to the line number in the original source file.</para>
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The line of source code; -1 if that info is not found.</returns>
        public static int GetSrcLineNumber(this XElement element) {
            if(null == element)
                throw new ArgumentNullException("element");

            int lineNumber = -1;

            var srcLineAttribute = GetAttributeFromSelfOrDescendant(element, POS.Line);
            if(null != srcLineAttribute && Int32.TryParse(srcLineAttribute.Value, out lineNumber)) {
                return lineNumber;
            }

            int xmlLineNum = element.GetXmlLineNumber();

            // if no line info is present, just return -1
            // we may want to look at calculating the line number based on the text in the file (see GetSrcLinePosition below)
            if(-1 == xmlLineNum)
                return -1;

            // if th element is a unit, just return 0: Source line number doesn't make sense for a file.
            if(SRC.Unit == element.Name)
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
        public static int GetEndingSrcLineNumber(this XElement element) {
            if(null == element)
                throw new ArgumentNullException("element");

            var descendants = element.DescendantsAndSelf();

            return descendants.Last().GetSrcLineNumber();
        }

        /// <summary>
        /// Gets the original source column number that the given element starts on.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>The column number that this element starts on. This will return 0 if the element is a Unit and -1 if no line information is present.</returns>
        public static int GetSrcLinePosition(this XElement element) {
            if(null == element)
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
        }

        /// <summary>
        /// Adds line info to the given XObject.
        /// </summary>
        /// <param name="xmlObject">the XObject</param>
        /// <param name="lineInfo">a LineInfo object. This object is added as an annotation to <paramref name="xmlObject"/></param>
        public static void SetLineInfo(this XObject xmlObject, ABB.SrcML.LineInfo lineInfo) {
            if(null == xmlObject)
                throw new ArgumentNullException("xmlObject");

            xmlObject.AddAnnotation(lineInfo);
        }

        /// <summary>
        /// Returns the parent statement (either expr_stmt, or decl_stmt) of the given node.
        /// </summary>
        /// <param name="node">The node to search from.</param>
        /// <returns>the parent element for <paramref name="node"/>. It will be either <see cref="SRC.ExpressionStatement"/> or <see cref="SRC.DeclarationStatement"/></returns>
        public static XElement ParentStatement(this XNode node) {
            if(null == node)
                throw new ArgumentNullException("node");

            var ancestors = node.Ancestors().Where(a => a.Name.LocalName.EndsWith("_stmt", StringComparison.OrdinalIgnoreCase));

            if(ancestors.Any())
                return ancestors.First();
            return null;
        }

        /// <summary>
        /// Converts the tree rooted at the given element to source code.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The source code in a string</returns>
        public static string ToSource(this XElement element) {
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
        public static string ToSource(this XElement element, int spacesPerTab) {
            if(null == element)
                throw new ArgumentNullException("element");

            XmlReader r = element.CreateReader();

            using(StringWriter sw = new StringWriter(CultureInfo.InvariantCulture)) {
                while(r.Read()) {
                    switch(r.NodeType) {
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
                if(spacesPerTab > 0) {
                    using(StringWriter spaces = new StringWriter(CultureInfo.InvariantCulture)) {
                        for(int i = 0; i < spacesPerTab; i++)
                            spaces.Write(" ");
                        source = source.Replace("\t", spaces.ToString());
                    }
                }
                return source.Replace("\n", Environment.NewLine);
            }
        }

        private static LineInfo GetLineInfo(XElement element) {
            IXmlLineInfo ie = (IXmlLineInfo) element;

            if(ie.HasLineInfo())
                return new LineInfo(ie.LineNumber, ie.LinePosition);
            else
                return element.Annotation<ABB.SrcML.LineInfo>();
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Throws a SrcMLRequiredNameException if <paramref name="name"/> does not match <paramref name="requiredName"/>.</exception>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="requiredName">Name of the required.</param>
        public static void ThrowExceptionOnInvalidName(XName name, XName requiredName) {
            if(name != requiredName)
                throw new SrcMLRequiredNameException(requiredName);
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Throws a SrcMLRequiredNameException if <paramref name="name"/> is not in the list of <paramref name="validNames">valid names</paramref>.</exception>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="validNames">The valid names.</param>
        public static void ThrowExceptionOnInvalidName(XName name, IEnumerable<XName> validNames) {
            if(validNames.All(validName => validName != name))
                throw new SrcMLRequiredNameException(validNames.ToList());
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Thrown if the given <paramref name="element"/> does not have <paramref name="requiredName"/> as it's Name.</exception>
        /// </summary>
        /// <param name="element">The element to check the name for</param>
        /// <param name="requiredName">The name required</param>
        public static void ThrowExceptionOnInvalidName(XElement element, XName requiredName) {
            if(null == element)
                throw new ArgumentNullException("element");

            ThrowExceptionOnInvalidName(element.Name, requiredName);
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Thrown if the given <paramref name="element"/> does not have a Name from the list of <paramref name="validNames"/></exception>
        /// </summary>
        /// <param name="element">The element to check the name for</param>
        /// <param name="validNames">The collection of valid names</param>
        public static void ThrowExceptionOnInvalidName(XElement element, IEnumerable<XName> validNames) {
            if(null == element)
                throw new ArgumentNullException("element");

            ThrowExceptionOnInvalidName(element.Name, validNames);
        }
        /// <summary>
        /// <para>Gets the function name for the given method.</para>
        /// <para>If the function is an implementation of a class method, it has two parts: the class name and the method name. This function returns just the method name if both are present</para>
        /// </summary>
        /// <param name="method">The method to get the name for</param>
        /// <returns>The name of the method</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static XElement GetNameForMethod(XElement method) {
            if(null == method)
                throw new ArgumentNullException("method");

            ThrowExceptionOnInvalidName(method, new List<XName>() { SRC.Constructor, SRC.Destructor, SRC.Function,
                                                                    SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration,
                                                                    SRC.Call });

            var name = method.Element(SRC.Name);

            if(null == name) {
                return null;
            }
            if(name.Elements(SRC.Name).Any())
                return name.Elements(SRC.Name).Last();
            else
                return name;
        }

        /// <summary>
        /// Gets the class name for method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>the class name if found. Otherwise, null</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static XElement GetClassNameForMethod(XElement method) {
            if(null == method)
                throw new ArgumentNullException("method");

            ThrowExceptionOnInvalidName(method, new List<XName>() { SRC.Constructor, SRC.Destructor, SRC.Function,
                                                                    SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration,
                                                                    SRC.Call });

            var name = method.Element(SRC.Name);
            if(null == name) {
                return null;
            }

            var nameCount = name.Elements(SRC.Name).Count();

            if(nameCount > 1) {
                var className = name.Elements(SRC.Name).Skip(nameCount - 2).FirstOrDefault();
                return className;
            }

            return null;
        }
        /// <summary>
        /// <para>Gets all the calls contained in a function element.Function elements can either be of type <c>SRC.Function</c> or <c>SRC.Constructor</c>.</para>
        /// <exception cref="ABB.SrcML.SrcMLRequiredNameException">thrown if <c>function.Name</c> is not <c>SRC.Constructor</c> or <c>SRC.Function</c></exception>
        /// </summary>
        /// <param name="function">the function to find calls in</param>
        /// <returns>all method calls and constructor uses</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static IEnumerable<XElement> GetCallsFromFunction(XElement function) {
            if(null == function)
                throw new ArgumentNullException("function");

            ThrowExceptionOnInvalidName(function, new List<XName>() { SRC.Function, SRC.Constructor, SRC.Destructor });

            var calls = from call in function.Descendants(SRC.Call)
                        select call;
            var constructorCalls = from decl in function.Descendants(SRC.Declaration)
                                   where decl.Element(SRC.ArgumentList) != null
                                   select decl;
            var allCalls = calls.Concat(constructorCalls).InDocumentOrder();

            return allCalls;
        }
    }
}
