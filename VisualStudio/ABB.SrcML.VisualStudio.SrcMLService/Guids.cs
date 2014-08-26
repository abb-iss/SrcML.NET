/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;

namespace ABB.SrcML.VisualStudio {
    public static class GuidList {
        public const string guidSrcMLServicePkgString = "8b448a37-2665-4b23-a2f9-cad4510f1337";
        public const string guidSrcMLServiceCmdSetString = "a92a902c-213b-4b54-9580-afacc7240bec";

        public static readonly Guid guidSrcMLServiceCmdSet = new Guid(guidSrcMLServiceCmdSetString);
    };
}