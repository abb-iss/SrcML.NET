using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML
{
    public interface IArchive
    {
        string ArchivePath
        {
            get;
        }

        string SourceFolderPath
        {
            get;
        }

        IEnumerable<XElement> FileUnits
        {
            get;
        }

        Dictionary<XName, XAttribute> RootAttributeDictionary
        {
            get;
        }

        // Functions for adding, deleting, and updating file units
        void AddUnits(IEnumerable<XElement> units);
        void DeleteUnits(IEnumerable<XElement> units);
        void UpdateUnits(IEnumerable<XElement> units);

        // functions for finding particular file units
        string GetPathForUnit(XElement unit);
        XElement GetUnitForPath(string pathToUnit);

        void ExportSource();
    }
}
