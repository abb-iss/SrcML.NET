using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Xml;

namespace ABB.SrcML
{
    public abstract class AbstractArchive : IArchive
    {
        private string _archivePath;
        private string _projectPath;
        private Dictionary<XName, XAttribute> _rootAttributeDictionary;

        protected AbstractArchive(string projectPath, string archivePath)
            : this()
        {
            this.SourceFolderPath = projectPath;
            this.ArchivePath = archivePath;
        }

        protected AbstractArchive()
        {
            this._rootAttributeDictionary = new Dictionary<XName, XAttribute>();
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

        #region IArchive Members

        public string GetPathForUnit(XElement unit)
        {
            if (null == unit)
                throw new ArgumentNullException("unit");
            try
            {
                SrcMLHelper.ThrowExceptionOnInvalidName(unit, SRC.Unit);
            }
            catch (SrcMLRequiredNameException e)
            {
                throw new ArgumentException(e.Message, "unit", e);
            }

            var fileName = unit.Attribute("filename");

            if (null != fileName)
                return fileName.Value;

            return null;
            //return fileName.Value;
        }

        public abstract XElement GetUnitForPath(string pathToUnit);
        public string ArchivePath
        {
            get
            {
                return this._archivePath;
            }
            protected set
            {
                this._archivePath = value;
            }
        }

        public string SourceFolderPath
        {
            get
            {
                return this._projectPath;
            }
            protected set
            {
                this._projectPath = value;
            }
        }

        public abstract IEnumerable<XElement> FileUnits
        {
            get;
        }

        public Dictionary<XName, XAttribute> RootAttributeDictionary
        {
            get
            {
                return this._rootAttributeDictionary;
            }
            protected set
            {
                this._rootAttributeDictionary = value;
            }
        }

        public abstract void AddUnits(IEnumerable<XElement> units);

        public abstract void DeleteUnits(IEnumerable<XElement> units);

        public abstract void UpdateUnits(IEnumerable<XElement> units);

        public void ExportSource()
        {
            foreach (var unit in this.FileUnits)
            {
                var path = GetPathForUnit(unit);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, unit.ToSource(), Encoding.UTF8);
                }
                catch (UnauthorizedAccessException)
                {
                    throw;
                }
            }
        }

        #endregion
    }
}
