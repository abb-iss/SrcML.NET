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

namespace ABB.SrcML.DataIU {
    public class DataArchive {
        public SrcMLArchive Archive {
            get;
            set;
        }

        public DataArchive(SrcMLArchive archive) {
            this.Archive = archive;
        }

        public TypeDefinition ResolveType(XElement variableDeclarationElement) {
            var typeUse = new TypeUse(variableDeclarationElement);
            return ResolveType(typeUse);
        }

        public TypeDefinition ResolveType(TypeUse typeUse) {
            return null;
        }

    }
}
