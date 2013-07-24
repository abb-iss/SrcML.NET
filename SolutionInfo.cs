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
[assembly: AssemblyCompany("ABB")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyVersion("2.0.48.0")]