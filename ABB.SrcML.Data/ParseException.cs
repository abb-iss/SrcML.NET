/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents an error from an <see cref="AbstractCodeParser"/>. The various parser functions are
    /// caught by <see cref="AbstractCodeParser.ParseFileUnit(System.Xml.Linq.XElement)"/> and rethrown as
    /// a ParseException.
    /// </summary>
    public class ParseException : Exception {

        /// <summary>
        /// Constructs a new ParseException
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="lineNumber">The line number</param>
        /// <param name="columnNumber">The column number</param>
        /// <param name="parser">The parser object</param>
        /// <param name="message">Description of the exception</param>
        /// <param name="innerException">The exception being rethrown</param>
        public ParseException(string fileName, int lineNumber, int columnNumber, AbstractCodeParser parser, string message, Exception innerException)
            : base(message, innerException) {
            this.FileName = fileName;
            this.LineNumber = lineNumber;
            this.ColumnNumber = columnNumber;
            this.Parser = parser;
        }

        /// <summary>
        /// Constructs a new ParseException
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="parser">The parser object</param>
        /// <param name="message">Description of the exception</param>
        public ParseException(string fileName, AbstractCodeParser parser, string message)
            : base(message) {
            this.FileName = fileName;
            this.Parser = parser;
        }

        /// <summary>
        /// Constructs an exception object with a default message.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="parser">The parser object</param>
        public ParseException(string fileName, AbstractCodeParser parser)
            : base(String.Format("Error parsing {0} with the {1} parser", fileName, parser.ParserLanguage)) {
            this.FileName = fileName;
            this.Parser = parser;
        }

        /// <summary>
        /// The column number that caused the exception
        /// </summary>
        public int ColumnNumber { get; protected set; }

        /// <summary>
        /// The file name that caused the exception
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// The line number that caused the exception
        /// </summary>
        public int LineNumber { get; protected set; }

        /// <summary>
        /// The parser object that threw the exception
        /// </summary>
        public AbstractCodeParser Parser { get; protected set; }

        /// <summary>
        /// A string representation of the exception. Includes the file name, line and column numbers, and the exception message.
        /// </summary>
        public override string ToString() {
            return string.Format("Error parsing {0} ({1},{2}):{3}{4}", FileName, LineNumber, ColumnNumber, Environment.NewLine, base.ToString());
        }
    }
}