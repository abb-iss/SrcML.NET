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

namespace ABB.SrcML.Data {

    public interface IAlias {

        INamedScopeUse ImportedNamedScope { get; set; }

        INamedScopeUse ImportedNamespace { get; set; }

        bool IsNamespaceImport { get; }

        SrcMLLocation Location { get; set; }

        Language ProgrammingLanguage { get; set; }

        IEnumerable<INamespaceDefinition> FindMatchingNamespace(INamespaceDefinition rootScope);

        string GetFullName();

        string GetNamespaceName();

        bool IsAliasFor(INamedScope namedScope);

        bool IsAliasFor<DEFINITION>(IUse<DEFINITION> use) where DEFINITION : class;
    }
}