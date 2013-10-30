/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABB.SrcML.Data {

    /// <summary>
    /// The binary loader class provides a wrapper around the <see cref="BinaryFormatter"/> class.
    /// It also subscribes to the <see cref="AppDomain.AssemblyResolve"/> event in order to ensure
    /// that assemblies are resolved properly.
    /// </summary>
    internal class BinaryLoader : IDisposable {

        public AppDomain CurrentDomain { get; set; }

        public Dictionary<string, Assembly> LoadedAssemblies { get; set; }

        public BinaryLoader() {
            CurrentDomain = AppDomain.CurrentDomain;
            ReadLoadedAssemblies();
            CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public object Load(string fileName) {
            using(var f = File.OpenRead(fileName)) {
                return Load(f);
            }
        }

        public object Load(Stream serializationStream) {
            var formatter = new BinaryFormatter();
            var o = formatter.Deserialize(serializationStream);
            return o;
        }

        private void ReadLoadedAssemblies() {
            LoadedAssemblies = new Dictionary<string, Assembly>();
            foreach(var assembly in CurrentDomain.GetAssemblies()) {
                LoadedAssemblies[assembly.FullName] = assembly;
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            return LoadedAssemblies[args.Name];
        }

        public void Dispose() {
            CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            LoadedAssemblies.Clear();
        }
    }
}