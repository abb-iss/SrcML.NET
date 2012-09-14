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

namespace ABB.SrcML
{
    public class SrcMLArchive : ISourceFolder
    {
        private Src2SrcMLRunner _generator;
        private string _xmlDirectory;

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

            this._xmlDirectory = xmlDirectory;
            this._generator = generator;
            if (!Directory.Exists(this._xmlDirectory))
            {
                Directory.CreateDirectory(this._xmlDirectory);
            }
            this.SourceDirectory.SourceFileChanged += RespondToFileChangedEvent;
        }

        public ISourceFolder SourceDirectory
        {
            get;
            set;
        }

        public string XmlDirectory
        {
            get
            {
                return this._xmlDirectory;
            }
        }

        public Src2SrcMLRunner XmlGenerator
        {
            get
            {
                return this._generator;
            }
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

        #endregion

        public void RespondToFileChangedEvent(object sender, SourceEventArgs eventArgs)
        {
            var directoryName = Path.GetDirectoryName(Path.GetFullPath(eventArgs.SourceFilePath));
            var xmlFullPath = Path.GetFullPath(this.XmlDirectory);

            if (!directoryName.StartsWith(xmlFullPath, StringComparison.InvariantCultureIgnoreCase))
            {
                switch (eventArgs.EventType)
                {
                    case SourceEventType.Renamed:
                        File.Delete(eventArgs.OldSourceFilePath);
                        goto case SourceEventType.Changed;
                    case SourceEventType.Added:
                        goto case SourceEventType.Changed;
                    case SourceEventType.Changed:
                        GenerateXmlForSource(eventArgs.SourceFilePath);
                        break;
                    case SourceEventType.Deleted:
                        File.Delete(eventArgs.SourceFilePath);
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
            this._generator.GenerateSrcMLFromFile(sourcePath, xmlPath);
        }

        public string GetXmlPathForSourcePath(string sourcePath)
        {
            string fullPath = String.Empty;
            if (!Path.IsPathRooted(sourcePath))
            {
                fullPath = Path.GetFullPath(sourcePath);
            }

            if (!File.Exists(fullPath))
            {
                throw new IOException(String.Format("{0} does not refer to a file", sourcePath));
            }
            if (!fullPath.StartsWith(this.SourceDirectory.FullFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new IOException(String.Format("{0} is not rooted in {1}", sourcePath, this.SourceDirectory));
            }

            var dirLength = this.SourceDirectory.FullFolderPath.Length;
            if (Path.PathSeparator != this.SourceDirectory.FullFolderPath[dirLength - 1])
                dirLength++;

            string relativePath = fullPath.Substring(dirLength);
            string xmlPath = Path.Combine(this.XmlDirectory, relativePath);

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
