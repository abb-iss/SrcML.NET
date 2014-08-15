using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    internal class DataHelpers {

        public static XElement GetElement(SrcMLArchive archive, SrcMLLocation location) {
            string fileName = location.SourceFileName;
            string query = location.XPath;

            var unit = archive.GetXElementForSourceFile(fileName);

            var startingLength = fileName.Length + 23;
            var path = query.Substring(startingLength);
            var element = unit.XPathSelectElement(path, SrcMLNamespaces.Manager);

            return element;
        }

        public static string GetLocation(SourceLocation location) {
            return string.Format("{0}:{1}:{2}", location.SourceFileName, location.StartingLineNumber, location.StartingColumnNumber);
        }
    }
}