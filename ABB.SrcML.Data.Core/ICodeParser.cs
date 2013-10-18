using System;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    public interface ICodeParser {

        Language ParserLanguage { get; }

        INamespaceDefinition ParseFileUnit(XElement fileUnit);
    }
}