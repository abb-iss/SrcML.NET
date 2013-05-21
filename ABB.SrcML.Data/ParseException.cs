using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class ParseException : Exception {
        public string FileName { get; protected set; }
        public AbstractCodeParser Parser { get; protected set; }

        public ParseException(string fileName, AbstractCodeParser parser, string message, Exception innerException)
            : base(message, innerException) {
            this.FileName = fileName;
            this.Parser = parser;
        }

        public ParseException(string fileName, AbstractCodeParser parser, string message)
            : base(message) {
            this.FileName = fileName;
            this.Parser = parser;
        }

        public ParseException(string fileName, AbstractCodeParser parser)
            : base(String.Format("Error parsing {0} with the {1} parser", fileName, parser.ParserLanguage)) {
            this.FileName = fileName;
            this.Parser = parser;
        }
    }
}
