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

        private Dictionary<Language, AbstractCodeParser> _parserMap = new Dictionary<Language, AbstractCodeParser>() {
            { Language.C, new CPlusPlusCodeParser() },
            { Language.CPlusPlus, new CPlusPlusCodeParser() },
            { Language.CSharp, new CSharpCodeParser() },
            { Language.Java, new JavaCodeParser() },
        };

        /// <summary>
        /// The SrcML Archive to generate data from
        /// </summary>
        public SrcMLArchive Archive;

        /// <summary>
        /// The data generator supports the same set of extensions as <see cref="Archive"/>
        /// </summary>
        public override ICollection<string> SupportedExtensions {
            get { return Archive.SupportedExtensions; }
        }

        /// <summary>
        /// Creates a new data generator with no <see cref="Archive"/>
        /// </summary>
        public DataGenerator() : this(null) { }

        /// <summary>
        /// Creates a new data generator object
        /// </summary>
        /// <param name="archive">The archive to use for loading srcML</param>
        public DataGenerator(SrcMLArchive archive) : base() { this.Archive = archive; }

        /// <summary>
        /// Parses a srcML file unit element
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public NamespaceDefinition Parse(XElement fileUnit) {
            var language = SrcMLElement.GetLanguageForUnit(fileUnit);
            return _parserMap[language].ParseFileUnit(fileUnit);
        }

        public XElement GetSrcMLUnit(string sourceFileName) {
            if(null == Archive) {
                var e = new InvalidOperationException("Archive is null");
                if(IsLoggingErrors) {
                    LogError(e);
                    return null;
                } else {
                    throw e;
                }
            }
            return Archive.GetXElementForSourceFile(sourceFileName);
        }

        protected override bool GenerateImpl(string inputFileName, string outputFileName) {
            var unit = Archive.GetXElementForSourceFile(inputFileName);
            var data = Parse(unit);
            XmlSerialization.WriteElement(data, outputFileName);
            return true;
        }
    }
}
