/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Linq;
using System.IO;
using System.Xml.Linq;

namespace ABB.SrcML
{
    public class SrcMLArchive : AbstractArchive, ISourceFolder
    {
        public SrcMLArchive(ISourceFolder sourceDirectory)
            : this(sourceDirectory, Path.Combine(sourceDirectory.FullFolderPath, ".srcml"), new Src2SrcMLRunner())
        {

        }

        public SrcMLArchive(ISourceFolder sourceDirectory, string xmlDirectory)
            : this(sourceDirectory, xmlDirectory, new Src2SrcMLRunner())
        {

        }

        public SrcMLArchive(ISourceFolder sourceDirectory, string xmlDirectory, Src2SrcMLRunner generator)
        {
            this.SourceDirectory = sourceDirectory;
            this.ArchivePath = xmlDirectory;

            this.XmlGenerator = generator;
            
            if (!Directory.Exists(this.ArchivePath))
            {
                Directory.CreateDirectory(this.ArchivePath);
            }
            this.SourceDirectory.SourceFileChanged += RespondToFileChangedEvent;
        }

        public ISourceFolder SourceDirectory
        {
            get;
            set;
        }

        public Src2SrcMLRunner XmlGenerator
        {
            get;
            set;
        }

        #region ISourceFolder Members

        public event EventHandler<SourceEventArgs> SourceFileChanged;

        public string FullFolderPath
        {
            get
            {
                return this.SourceDirectory.FullFolderPath;
            }
            set
            {
                this.SourceDirectory.FullFolderPath = value;
            }
        }

        public void StartWatching()
        {
            this.SourceDirectory.StartWatching();
        }

        public void StopWatching()
        {
            this.SourceDirectory.StopWatching();
        }

        #endregion

        #region AbstractArchive Members

        public override IEnumerable<XElement> FileUnits
        {
            get
            {
                var xmlFiles = Directory.EnumerateFiles(this.ArchivePath, "*.xml", SearchOption.AllDirectories);
                foreach (var xmlFileName in xmlFiles)
                {
                    yield return XElement.Load(xmlFileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                }
            }
        }

        public override void AddUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override void DeleteUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                DeleteXmlForSourceFile(path);
            }
        }

        public override void UpdateUnits(IEnumerable<XElement> units)
        {
            foreach (var unit in units)
            {
                var path = this.GetPathForUnit(unit);
                var xmlPath = this.GetXmlPathForSourcePath(path);
                unit.Save(xmlPath, SaveOptions.DisableFormatting);
            }
        }

        public override XElement GetUnitForPath(string pathToUnit)
        {
            throw new NotImplementedException();
        }
        #endregion
        
        public void RespondToFileChangedEvent(object sender, SourceEventArgs eventArgs)
        {
            var directoryName = Path.GetDirectoryName(Path.GetFullPath(eventArgs.SourceFilePath));
            var xmlFullPath = Path.GetFullPath(this.ArchivePath);

            
            if (!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                switch (eventArgs.EventType)
                {
                    case SourceEventType.Renamed:
                        DeleteXmlForSourceFile(eventArgs.OldSourceFilePath);
                        goto case SourceEventType.Changed;
                    case SourceEventType.Added:
                        goto case SourceEventType.Changed;
                    case SourceEventType.Changed:
                        GenerateXmlForSource(eventArgs.SourceFilePath);
                        break;
                    case SourceEventType.Deleted:
                        DeleteXmlForSourceFile(eventArgs.SourceFilePath);
                        break;

                }
                OnSourceFileChanged(eventArgs);
            }
        }

        public void GenerateXmlForSource(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            this.XmlGenerator.GenerateSrcMLFromFile(sourcePath, xmlPath);
        }

        public void DeleteXmlForSourceFile(string sourcePath)
        {
            var xmlPath = GetXmlPathForSourcePath(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            if (File.Exists(xmlPath))
            {
                File.Delete(xmlPath);
            }

            if (!Directory.Exists(sourceDirectory))
            {
                var xmlDirectory = Path.GetDirectoryName(xmlPath);
                Directory.Delete(xmlDirectory);
            }
        }

        public string GetXmlPathForSourcePath(string sourcePath)
        {
            string fullPath = String.Empty;
            if (Path.IsPathRooted(sourcePath))
            {
                fullPath = sourcePath;
            }
            else
            {
                fullPath = Path.GetFullPath(sourcePath);
            }

            if (!fullPath.StartsWith(this.SourceDirectory.FullFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", sourcePath, this.SourceDirectory));
            }

            var dirLength = this.SourceDirectory.FullFolderPath.Length;
            if (Path.PathSeparator != this.SourceDirectory.FullFolderPath[dirLength - 1])
                dirLength++;

            string relativePath = fullPath.Substring(dirLength);
            string xmlPath = Path.Combine(this.ArchivePath, relativePath);

            xmlPath = xmlPath + ".xml";

            return xmlPath;
        }

        protected virtual void OnSourceFileChanged(SourceEventArgs e)
        {
            EventHandler<SourceEventArgs> handler = SourceFileChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
