/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Collections.ObjectModel;

namespace ABB.SrcML
{
    /// <summary>
    /// This class represents a SrcMLFile. The underlying data is stored in an XML file, and can be accessed in a number of ways.
    /// </summary>
    public class SrcMLFile : AbstractArchive//AbstractDocument
    {
        private string _rootDirectory;   
        private XDocument _document;

        /// <summary>
        /// Instantiates a new SrcMLFile with the characteristics of another SrcMLFile.
        /// </summary>
        /// <param name="other">The SrcMLFile to copy.</param>
        public SrcMLFile(SrcMLFile other)
        {
            if (null == other)
                throw new ArgumentNullException("other");

            this._rootDirectory = other._rootDirectory;
        }

        /// <summary>
        /// Instantiates new SrcML file based on the given file.
        /// </summary>
        /// <param name="fileName">The file to read from.</param>
        public SrcMLFile(string fileName)
        {
            this.ArchivePath = fileName;
            this.SourceFolderPath = getCommonPath();
            this.NumberOfNestedFileUnits = XmlHelper.StreamElements(this.ArchivePath, SRC.Unit).Count();
        }

        public string FileName
        {
            get
            {
                return this.ArchivePath;
            }
        }
        /// <summary>
        /// Merges this SrcMLFile with another SrcMLFile. 
        /// </summary>
        /// <param name="other">The second SrcML File to merge with.</param>
        /// <param name="outputFileName">The path to write the resulting SrcMLFile to.</param>
        /// <returns>The newly merged SrcMLFile.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public SrcMLFile Merge(SrcMLFile other, string outputFileName)
        {
            if (null == other)
                throw new ArgumentNullException("other");

            using (XmlWriter xw = XmlWriter.Create(outputFileName, new XmlWriterSettings() { Indent = false }))
            {
                xw.WriteStartElement(SRC.Unit.LocalName, SRC.Unit.NamespaceName);
                WriteXmlnsAttributes(xw);

                foreach (var unit in this.FileUnits)
                {
                    unit.WriteTo(xw);
                }
                
                foreach (var unit in other.FileUnits)
                {
                    unit.WriteTo(xw);
                }

                xw.WriteEndElement();
            }
            return new SrcMLFile(outputFileName);
        }

        private static string getCommonPath(IEnumerable<string> filePaths)
        {
            bool shortestFound = false;
            string shortest = null;
            try
            {
                shortest = filePaths.First();
            }
            catch (InvalidOperationException)
            {

            }

            while (!shortestFound)
            {
                if (filePaths.All(f => f.StartsWith(shortest, StringComparison.Ordinal)))
                {
                    shortestFound = true;
                }
                else
                {
                    shortest = Path.GetDirectoryName(shortest);
                }
                if (null == shortest)
                    break;
            }
            return shortest;
        }

        private string getCommonPath()
        {
            XAttribute dir;
            string commonPath;
            if (RootAttributeDictionary.TryGetValue("dir", out dir))
            {
                commonPath = dir.Value;
            }
            else if (0 == this.NumberOfNestedFileUnits)
            {
                commonPath = Path.GetDirectoryName(GetPathForUnit(this.FileUnits.First()));
            }
            else
            {
                var folders = from unit in this.FileUnits
                              let path = Path.GetDirectoryName(GetPathForUnit(unit))
                              select path;
                commonPath = getCommonPath(folders);
            }
            
            return commonPath;
        }

        public int NumberOfNestedFileUnits
        {
            get;
            protected set;
        }
        /// <summary>
        /// The project rootUnit directory for this SrcMLFile.
        /// </summary>
        /// <value>The ProjectDirectory property gets &amp; sets the rootUnit directory for this SrcMLFile.</value>
        public string ProjectDirectory
        {
            get { return _rootDirectory; }
            set
            {
                var units = from unit in this.FileUnits
                              where unit.Attribute("filename") != null
                              select unit;
                
                var tempFileName = Path.GetTempFileName();
                using (XmlWriter xw = XmlWriter.Create(tempFileName, new XmlWriterSettings() { Indent = false }))
                {
                    xw.WriteStartElement(SRC.Unit.LocalName, SRC.Unit.NamespaceName);
                    WriteXmlnsAttributes(xw);
                    foreach (var kvp in this.RootAttributeDictionary)
                    {
                        xw.WriteAttributeString(kvp.Value.Name.LocalName, kvp.Value.Name.NamespaceName, ("dir" == kvp.Key ? value : kvp.Value.Value));
                    }

                    foreach (var unit in units)
                    {
                        var fileName = unit.Attribute("filename").Value;
                        fileName = fileName.Replace(_rootDirectory, value);
                        unit.SetAttributeValue("filename", fileName);

                        unit.WriteTo(xw);
                    }
                    xw.WriteEndElement();
                }
                File.Delete(this.ArchivePath);
                File.Move(tempFileName, this.ArchivePath);

                _rootDirectory = value;
                this.RootAttributeDictionary["dir"] = new XAttribute("dir", value);
            }
        }
        
        /// <summary>
        /// Get the file path relative to <see cref="ProjectDirectory"/> for the unit containing the given node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The relative file path to that node.</returns>
        public string RelativePath(XNode node)
        {
            if (null == node)
                throw new ArgumentNullException("node");

            var unit = node.Ancestors(SRC.Unit).First();
            var path = GetPathForUnit(unit);

            int start = this.ProjectDirectory.Length;
            if (this.ProjectDirectory[start - 1] != Path.DirectorySeparatorChar)
                start++;
            path = path.Substring(start);

            return path;
        }

        /// <summary>
        /// Get this SrcML file as an XDocument. This should not be used on very large SrcML file as it
        /// loads the entire XML file into memory.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public XDocument GetXDocument()
        {
            if(null == _document)
                _document = XDocument.Load(this.ArchivePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            return this._document;
        }

        /// <summary>
        /// Works in conjunction with <see cref="AbstractDocument.FileUnits"/> to execute a query against each file in a SrcML document
        /// </summary>
        /// <param name="transform">The transform object with the <see cref="ITransform.Query"/></param>
        /// <returns>yields each node that matches the query in <paramref name="transform"/></returns>
        public IEnumerable<XElement> QueryEachUnit(ITransform transform)
        {
            foreach (var unit in this.FileUnits)
            {
                foreach (var result in transform.Query(unit))
                    yield return result;
            }
        }

        #region Save
        /// <summary>
        /// Writes this SrcML file to <paramref name="fileName"/> with changes stored in <paramref name="changedFiles"/>.
        /// Currently, this only handles changes to existing files. New files will be ignored.
        /// </summary>
        /// <param name="fileName">The file to write to. If it exists it will be deleted and replaced.</param>
        /// <param name="changedFiles">A list of units with changes. These will be substituted for the original units.</param>
        public void Save(string fileName, IEnumerable<XElement> changedFiles)
        {
            if (null == changedFiles)
                throw new ArgumentNullException("changedFiles");

            var tempFileName = Path.GetTempFileName();
            Dictionary<string, XElement> changedDict = new Dictionary<string, XElement>();

            foreach (var change in changedFiles)
            {
                var path = GetPathForUnit(change);
                changedDict[path] = change;
            }

            using (XmlWriter xw = XmlWriter.Create(tempFileName, new XmlWriterSettings() { Indent = false }))
            {
                xw.WriteStartElement(SRC.Unit.LocalName, SRC.Unit.NamespaceName);
                WriteXmlnsAttributes(xw);

                foreach (var unit in this.FileUnits)
                {
                    string path = GetPathForUnit(unit);

                    (changedDict.ContainsKey(path) ? changedDict[path] : unit).WriteTo(xw);
                }
                xw.WriteEndElement();
            }

            if (File.Exists(fileName))
                File.Delete(fileName);

            File.Move(tempFileName, fileName);
        }

        /// <summary>
        /// Writes changes to this SrcML file back to the current file (<see cref="AbstractDocument.FileName"/>).
        /// <seealso cref="Save(string, IEnumerable&lt;XElement&gt;)"/>
        /// </summary>
        /// <param name="changedFiles">a list of units with changes. These will be substituted for the original units</param>
        public void Save(IEnumerable<XElement> changedFiles)
        {
            Save(this.ArchivePath, changedFiles);
        }

        /// <summary>
        /// Writes this SrcML file to <paramref name="fileName"/> without making any changes.
        /// <para>This is identical to <c>srcmlDoc.Save(fileName, Enumerable.Empty&lt;XElement&gt;()</c></para>
        /// <seealso cref="Enumerable.Empty&lt;XElement&gt;" />
        /// </summary>
        /// <param name="fileName">the file to write to. If it exists it will be deleted and replaced.</param>
        public void Save(string fileName)
        {
            Save(fileName, Enumerable.Empty<XElement>());
        }

        /// <summary>
        /// Saves the document to the file. This uses <see cref="System.Xml.Linq.XDocument"/>, which is more memory intensive.
        /// <seealso cref="XDocument.Save(string)"/>
        /// </summary>
        /// <param name="fileName">The filename to write to.</param>
        public void Write(string fileName)
        {
            var doc = GetXDocument();
            var tmp = Path.GetTempFileName();

            doc.Save(tmp);

            if (File.Exists(fileName))
                File.Delete(fileName);
            File.Move(tmp, fileName);

            this.ArchivePath = fileName;
        }

        /// <summary>
        /// Writes the document back to the current file <see cref="AbstractDocument.FileName"/>.
        /// <seealso cref="Write(string)"/>
        /// </summary>
        public void Write()
        {
            Write(this.ArchivePath);
        }
        #endregion

        /// <summary>
        /// Gets the index number of a given filename from the SrcML document.
        /// If this is being passed to SrcML.ExtractSourceFile(), +1 must be added to it.
        /// </summary>
        /// <param name="fileName">The filename to get an index for.</param>
        /// <returns>the index of the file. -1 if not found.</returns>
        public int IndexOfUnit(string fileName)
        {
            IEnumerable<string> filenames = from attr in this.FileUnits.Attributes("filename")
                                            select (string)attr;
            return filenames.ToList<string>().IndexOf(Path.GetFileName(fileName));
        }

        #region AbstractArchive
        public override IEnumerable<XElement> FileUnits
        {
            get
            {
                if (0 == this.NumberOfNestedFileUnits)
                {
                    var shortList = new List<XElement>(1);
                    shortList.Add(XElement.Load(this.ArchivePath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo));
                    return shortList;
                }
                IEnumerable<XElement> units = from unit in XmlHelper.StreamElements(this.ArchivePath, SRC.Unit)
                                              where unit.Attribute("filename") != null
                                              select unit;
                return units;
            }
        }

        public override void AddUnits(IEnumerable<XElement> units)
        {
            var tmpFileName = Path.GetTempFileName();
            using (XmlWriter xw = XmlWriter.Create(tmpFileName, new XmlWriterSettings() { Indent = false }))
            {
                xw.WriteStartElement(SRC.Unit.LocalName, SRC.Unit.NamespaceName);
                WriteXmlnsAttributes(xw);

                foreach (var unit in this.FileUnits)
                {
                    unit.WriteTo(xw);
                }

                foreach (var unit in units)
                {
                    unit.WriteTo(xw);
                }

                xw.WriteEndElement();
            }
            File.Move(tmpFileName, this.ArchivePath);
        }

        public override void DeleteUnits(IEnumerable<XElement> units)
        {
            var tmpFileName = Path.GetTempFileName();
            using (XmlWriter xw = XmlWriter.Create(tmpFileName, new XmlWriterSettings() { Indent = false}))
            {
                xw.WriteStartElement(SRC.Unit.LocalName, SRC.Unit.NamespaceName);
                WriteXmlnsAttributes(xw);

                var remainingUnits = this.FileUnits;
                foreach (var unit in remainingUnits)
                {
                    unit.WriteTo(xw);
                }
                xw.WriteEndElement();
            }
        }
        public override void UpdateUnits(IEnumerable<XElement> units)
        {
            this.Save(units);
        }

        public override XElement GetUnitForPath(string pathToUnit)
        {
            throw new NotImplementedException();
        }
        #endregion


        /// <summary>
        /// Added by JZ on 12/3/2012
        /// Read the srcML file and generate a string of the content of this file.
        /// </summary>
        /// <returns>The string of the content of this file.</returns>
        public string GetXMLString()
        {
            string xml = "";

            try
            {
                using (StreamReader sr = new StreamReader(this.ArchivePath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        xml += line;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return xml;
        }
    }
}
