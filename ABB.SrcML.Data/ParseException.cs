using System;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents an error from an <see cref="AbstractCodeParser"/>. The various parser functions
    /// are caught by <see cref="AbstractCodeParser.ParseFileUnit(System.Xml.Linq.XElement)"/> and
    /// rethrown as a ParseException.
    /// </summary>
    public class ParseException : Exception {

        /// <summary>
        /// Constructs an object
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="parser">The parser object</param>
        /// <param name="message">Description of the exception</param>
        /// <param name="innerException">The exception being rethrown</param>
        public ParseException(string fileName, AbstractCodeParser parser, string message, Exception innerException)
            : base(message, innerException) {
            this.FileName = fileName;
            this.Parser = parser;
        }

        /// <summary>
        /// Constructs an exception
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="parser">The parser object</param>
        /// <param name="message">Description fo the exception</param>
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
        /// The file name that caused the exception
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// The parser object that threw the exception
        /// </summary>
        public AbstractCodeParser Parser { get; protected set; }
    }
}