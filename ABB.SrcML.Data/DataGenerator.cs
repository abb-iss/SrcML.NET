/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// 
    /// </summary>
    public class DataGenerator : AbstractGenerator {
        private static List<string> _supportedExtensions = new List<string>() { ".xml" };
        private TextWriter _unknownLog;

        private Dictionary<Language, AbstractCodeParser> _parserMap = new Dictionary<Language, AbstractCodeParser>() {
            { Language.C, new CPlusPlusCodeParser() },
            { Language.CPlusPlus, new CPlusPlusCodeParser() },
            { Language.CSharp, new CSharpCodeParser() },
            { Language.Java, new JavaCodeParser() },
        };

        /// <summary>
        /// The data generator supports the same set of extensions ".xml" as its extension
        /// </summary>
        public override ICollection<string> SupportedExtensions {
            get { return _supportedExtensions; }
        }

        /// <summary>
        /// Sets the <see cref="AbstractCodeParser.UnknownLog"/> property for each <see cref="AbstractCodeParser"/>.
        /// </summary>
        public TextWriter UnknownLog {
            get { return _unknownLog; }
            set { 
                TextWriter writer = (value != null ? TextWriter.Synchronized(value) : null);
                _unknownLog = writer;
                foreach(var parser in _parserMap.Values) {
                    parser.UnknownLog = writer;
                }
            }
        }
        /// <summary>
        /// Creates a new data generator with no unknown logger
        /// </summary>
        public DataGenerator() : base() {
            UnknownLog = null;
        }

        /// <summary>
        /// Parses a srcML file and returns a <see cref="NamespaceDefinition"/>
        /// </summary>
        /// <param name="srcMLFileName">The path to the srcML file</param>
        /// <returns>The namespace definition for <paramref name="srcMLFileName"/></returns>
        public NamespaceDefinition Parse(string srcMLFileName) {
            var unit = SrcMLElement.Load(srcMLFileName);
            return Parse(unit);
        }
        /// <summary>
        /// Parses a srcML file unit element
        /// </summary>
        /// <param name="fileUnit">The srcML file unit element</param>
        /// <returns>The namespace definition for <paramref name="fileUnit"/></returns>
        public NamespaceDefinition Parse(XElement fileUnit) {
            var language = SrcMLElement.GetLanguageForUnit(fileUnit);
            return _parserMap[language].ParseFileUnit(fileUnit);
        }

        /// <summary>
        /// Generates <paramref name="outputFileName"/> from the srcML file designated by <paramref name="inputFileName"/>.
        /// This works by calling <see cref="Parse(string)"/>.
        /// </summary>
        /// <param name="inputFileName">The path to a srcML file</param>
        /// <param name="outputFileName">the path to store the resulting namespace definition in.</param>
        /// <returns>true if <paramref name="outputFileName"/> was created; false otherwise.</returns>
        protected override bool GenerateImpl(string inputFileName, string outputFileName) {
            try {
                var data = Parse(inputFileName);
                XmlSerialization.WriteElement(data, outputFileName);
                return true;
            } catch(XmlException) {
                return false;
            } catch(FileNotFoundException) {
                return false;
            } catch(ArgumentNullException) {
                return false;
            }
        }
    }
}
