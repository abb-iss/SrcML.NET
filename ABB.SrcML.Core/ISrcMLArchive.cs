using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML {
    public interface ISrcMLArchive : IArchive {

        IEnumerable<XElement> FileUnits { get; }

        string GetXmlPath(string sourcePath);

        XElement GetXElementForSourceFile(string sourceFilePath);

        bool IsValidFileExtension(string filePath);
    }
}
