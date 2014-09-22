/******************************************************************************
 * Copyright (c) 2014 ABB Group
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The data archive uses a <see cref="MonitoredArchive">SrcML Archive</see> to generate and store SrcML Data for each <see cref="SrcMLArchive.FileUnits">file unit</see>.
    /// </summary>
    public class DataArchive : GeneratorArchive<DataGenerator> {
        /// <summary>
        /// The default directory to store the data contents in
        /// </summary>
        public const string DEFAULT_ARCHIVE_DIRECTORY = "data";

        /// <summary>
        /// The srcML archive to monitor for changes
        /// </summary>
        public SrcMLArchive MonitoredArchive { get; set; }

        /// <summary>
        /// Creates a data archive in <see cref="DEFAULT_ARCHIVE_DIRECTORY"/> within <see cref="Environment.CurrentDirectory"/> with no <see cref="MonitoredArchive"/>.
        /// </summary>
        public DataArchive() : this(Environment.CurrentDirectory, null, true) { }

        /// <summary>
        /// Creates a new data archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// /// <param name="monitoredArchive">The srcML archive to monitor</param>
        /// <param name="useExistingData">If true, any existing data files in <see cref="AbstractArchive.ArchivePath"/> will be used.</param>
        
        public DataArchive(string baseDirectory, SrcMLArchive monitoredArchive, bool useExistingData)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, useExistingData, monitoredArchive, new DataGenerator(), new DataFileNameMapping(Path.Combine(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY)), TaskScheduler.Default) { }

        /// <summary>
        /// Creates a new data archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// <param name="monitoredArchive">The srcML archive to monitor</param>
        public DataArchive(string baseDirectory, SrcMLArchive monitoredArchive)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, true, monitoredArchive, new DataGenerator(), new DataFileNameMapping(Path.Combine(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY)), TaskScheduler.Default) { }

        /// <summary>
        /// Creates a new data archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// <param name="monitoredArchive">The srcML archive to monitor</param>
        /// <param name="scheduler">The task scheduler to use for asynchronous tasks</param>
        public DataArchive(string baseDirectory, SrcMLArchive monitoredArchive, TaskScheduler scheduler)
            : this(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY, true, monitoredArchive, new DataGenerator(), new DataFileNameMapping(Path.Combine(baseDirectory, DEFAULT_ARCHIVE_DIRECTORY)), scheduler) { }

        /// <summary>
        /// Creates a new data archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// <param name="dataDirectory">the directory to store the data files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingData">If true, any existing data files in <see cref="AbstractArchive.ArchivePath"/> will be used.</param>
        /// <param name="monitoredArchive">The srcML archive to monitor</param>
        public DataArchive(string baseDirectory, string dataDirectory, bool useExistingData, SrcMLArchive monitoredArchive)
            : this(baseDirectory, dataDirectory, useExistingData, monitoredArchive, new DataGenerator(), new DataFileNameMapping(Path.Combine(baseDirectory, dataDirectory)), TaskScheduler.Default) { }

        /// <summary>
        /// Creates a new data archive
        /// </summary>
        /// <param name="baseDirectory">The base directory</param>
        /// <param name="dataDirectory">the directory to store the data files in. This will be created as a subdirectory of <paramref name="baseDirectory"/></param>
        /// <param name="useExistingData">If true, any existing data files in <see cref="AbstractArchive.ArchivePath"/> will be used.</param>
        /// <param name="generator">The data generator to use for this archive</param>
        /// <param name="monitoredArchive">The srcML archive to monitor</param>
        /// <param name="mapping">the <see cref="AbstractFileNameMapping"/> to use to map source paths to data file paths</param>
        /// <param name="scheduler">The task scheduler to use for asynchronous tasks</param>
        public DataArchive(string baseDirectory, string dataDirectory, bool useExistingData, SrcMLArchive monitoredArchive, DataGenerator generator, AbstractFileNameMapping mapping, TaskScheduler scheduler)
            : base(baseDirectory, dataDirectory, useExistingData, generator, mapping, scheduler) {
            this.MonitoredArchive = monitoredArchive;
        }

        /// <summary>
        /// Gets the stored data for <paramref name="sourceFileName"/>
        /// </summary>
        /// <param name="sourceFileName">The source file to get data for</param>
        /// <returns>the namespace definition for <paramref name="sourceFileName"/></returns>
        public virtual NamespaceDefinition GetData(string sourceFileName) {
            if(!File.Exists(sourceFileName)) {
                return null;
            } else {
                string xmlPath = GetArchivePath(sourceFileName);

                return (File.Exists(xmlPath) ? XmlSerialization.Load(xmlPath) as NamespaceDefinition : null);
            }
        }

        /// <summary>
        /// Overrides <see cref="GeneratorArchive{TGenerator}.GetInputPath"/> in order to provide a srcML file to the
        /// <see cref="GeneratorArchive{TGenerator}.Generator"/>. It does this by calling <see cref="GeneratorArchive{TGenerator}.GetArchivePath"/>
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        protected override string GetInputPath(string sourcePath) {
            return MonitoredArchive.GetArchivePath(sourcePath);
        }
    }
}
