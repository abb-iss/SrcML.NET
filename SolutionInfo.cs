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

using System.Reflection;
using System.Resources;

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Revision
//      Build Number

[assembly: AssemblyCompany("ABB")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyVersion("99.99.0.0")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyInformationalVersion("0.0.0.0-Debug-dev")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyInformationalVersion("0.0.0.0-Release-dev")]
#endif
