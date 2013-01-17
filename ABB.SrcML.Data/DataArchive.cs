/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class DataArchive {
        public SrcMLArchive Archive { get; set; }

        /// <summary>
        /// Create a data archive for the given srcML archive. It will subscribe to the <see cref="SrcMLArchive.SourceFileChanged"/> event.
        /// </summary>
        /// <param name="archive">The archive to monitor for changes.</param>
        public DataArchive(SrcMLArchive archive) {
            this.Archive = archive;
            this.Archive.SourceFileChanged += Archive_SourceFileChanged;
        }

        public TypeDefinition ResolveType(XElement variableDeclarationElement) {
            //var typeUse = new TypeUse(variableDeclarationElement);
            //return ResolveType(typeUse);
            throw new NotImplementedException();
        }

        public TypeDefinition ResolveType(TypeUse typeUse) {
            throw new NotImplementedException();
        }

        void Archive_SourceFileChanged(object sender, SourceEventArgs e) {
            switch(e.EventType) {
                case SourceEventType.Changed:
                    // Treat a change source file as deleted then added
                    RespondToSourceFileDeletion(e.SourceFilePath);
                    goto case SourceEventType.Added;
                case SourceEventType.Added:
                    RespondToSourceFileAddition(e.SourceFilePath);
                    break;
                case SourceEventType.Deleted:
                    RespondToSourceFileDeletion(e.SourceFilePath);
                    break;
                case SourceEventType.Renamed:
                    // TODO remove the file from the data archive
                    // TODO do we need the rename case? or just handle delete + add?
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// This function is a container function for all of the different "Load" functions
        /// It is responsible for adding data for new files. It is *not* responsible for deleting existing data (<see cref="RespondToSourceFileDeletion(string)"/>
        /// </summary>
        /// <param name="pathToNewFile">the path to the source file that changed</param>
        private void RespondToSourceFileAddition(string pathToNewFile) {
            var updatedFileUnit = GetXElementForSourceFile(pathToNewFile);

            LoadTypesFromFile(updatedFileUnit);
        }

        /// <summary>
        /// This function is a container function for all of the different "Remove" functions
        /// It is responsible for removing existing data for changed or deleted files.
        /// </summary>
        /// <param name="pathToDeletedFile"></param>
        private void RespondToSourceFileDeletion(string pathToDeletedFile) {
            RemoveTypesForFile(pathToDeletedFile);
        }

        #region Type Inventory
        /// <summary>
        /// Load types from the given source file and add them to the type inventory
        /// </summary>
        /// <param name="updatedFileUnit">The file unit to get types for</param>
        private void LoadTypesFromFile(XElement updatedFileUnit) {
            throw new NotImplementedException();
        }

        private void RemoveTypesForFile(string pathToSourceFile) {
            // TODO find all types defined in the given source file and remove them
            throw new NotImplementedException();
        }
        #endregion
        
        /// <summary>
        /// Get the XElement corresponding to a source file.
        /// </summary>
        /// <param name="pathToSourceFile">The path to the source file on disk</param>
        /// <returns>The file unit element for the source file</returns>
        private XElement GetXElementForSourceFile(string pathToSourceFile) {
            var pathToXmlFile = this.Archive.GetXmlPathForSourcePath(pathToSourceFile);

            var fileUnit = XElement.Load(pathToXmlFile);
            return fileUnit;
        }

    }
}
