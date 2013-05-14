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
    /// Provides base functionality for various srcML documents.
    /// </summary>
    public class AbstractDocument
    {
        private string _fileName;
        private int _numNestedUnits;
        private Dictionary<XName, XAttribute> _rootAttributeDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDocument"/> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        protected AbstractDocument(string fileName)
        {
            this._fileName = fileName;
            this._rootAttributeDictionary = new Dictionary<XName, XAttribute>(getRootAttributes(fileName).ToDictionary(x => x.Name));
            this._numNestedUnits = XmlHelper.StreamElements(this.FileName, SRC.Unit).Count();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractDocument"/> class based on <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other <see cref="AbstractDocument"/> object to copy</param>
        protected AbstractDocument(AbstractDocument other)
        {
            if (null == other)
                throw new ArgumentNullException("other");

            this._fileName = other._fileName;
            this._rootAttributeDictionary = other._rootAttributeDictionary;

        }

        /// <summary>
        /// Gets the filename underlying this SrcMLFile.
        /// </summary>
        public string FileName
        {
            get { return this._fileName; }
            protected set { this._fileName = value; }
        }

        /// <summary>
        /// Returns the attribute dictionary 
        /// </summary>
        public Dictionary<XName, XAttribute> RootAttributeDictionary
        {
            get
            {
                return this._rootAttributeDictionary;
            }
        }

        /// <summary>
        /// Gets the number of nested file units.
        /// </summary>
        protected int NumberOfNestedFileUnits
        {
            get
            {
                return this._numNestedUnits;
            }
        }

        /// <summary>
        /// Get all units that have the "filename" attribute. This uses the <see cref="XmlHelper.StreamElements"/> function for low memory overhead,
        /// unless the document is not compound (and the root unit is the only file unit). In that case, it uses <see cref="SrcMLElement.Load(string)"/>.
        /// </summary>
        public IEnumerable<XElement> FileUnits
        {
            get
            {
                if (0 == this._numNestedUnits)
                {
                    var shortList = new List<XElement>(1);
                    shortList.Add(SrcMLElement.Load(this.FileName));
                    return shortList;
                }
                IEnumerable<XElement> units = from unit in XmlHelper.StreamElements(this.FileName, SRC.Unit)
                                              where unit.Attribute("filename") != null
                                              select unit;
                return units;
            }
        }

        /// <summary>
        /// Write attribute strings for each SrcML namespace to the given XmlWriter. This should be called immediately after XmlWriter.WriteStartElement.
        /// </summary>
        /// <param name="writer">Instance of XmlWriter to write to.</param>
        public static void WriteXmlnsAttributes(XmlWriter writer)
        {
            if (null == writer)
                throw new ArgumentNullException("writer");

            writer.WriteAttributeString("xmlns", CPP.Prefix, null, CPP.NS.NamespaceName);
            writer.WriteAttributeString("xmlns", LIT.Prefix, null, LIT.NS.NamespaceName);
            writer.WriteAttributeString("xmlns", OP.Prefix, null, OP.NS.NamespaceName);
            writer.WriteAttributeString("xmlns", POS.Prefix, null, POS.NS.NamespaceName);
            writer.WriteAttributeString("xmlns", TYPE.Prefix, null, TYPE.NS.NamespaceName);
        }

        /// <summary>
        /// Gets the root attributes.
        /// </summary>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <returns>the attributes attached to the root element.</returns>
        private static IEnumerable<XAttribute> getRootAttributes(string xmlFilePath)
        {
            using (var reader = XmlReader.Create(xmlFilePath))
            {
                reader.MoveToContent();
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        if ("http://www.w3.org/2000/xmlns/" != reader.NamespaceURI)
                        {
                            var attribute = new XAttribute(XName.Get(reader.LocalName, reader.NamespaceURI), reader.Value);
                            yield return attribute;
                        }
                    } while (reader.MoveToNextAttribute());
                }
            }
        }
    }
}
