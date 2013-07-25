/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Added SrcMLNamespaces static class
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ABB.SrcML {
    public class SrcMLNamespaces {
        private readonly static XmlNamespaceManager nsManager = new XmlNamespaceManager(new NameTable());

        static SrcMLNamespaces() {
            nsManager.AddNamespace(CPP.Prefix, CPP.NS.NamespaceName);
            nsManager.AddNamespace(LIT.Prefix, LIT.NS.NamespaceName);
            nsManager.AddNamespace(OP.Prefix, OP.NS.NamespaceName);
            nsManager.AddNamespace(POS.Prefix, POS.NS.NamespaceName);
            nsManager.AddNamespace(SRC.Prefix, SRC.NS.NamespaceName);
            nsManager.AddNamespace(TYPE.Prefix, TYPE.NS.NamespaceName);
            nsManager.AddNamespace(DIFF.Prefix, DIFF.NS.NamespaceName);
        }

        public static XmlNamespaceManager Manager { get { return nsManager; } }

        public static string LookupPrefix(string uri) {
            return Manager.LookupPrefix(uri);
        }

        public static string LookupNamespace(string prefix) {
            return Manager.LookupNamespace(prefix);
        }
    }
}
