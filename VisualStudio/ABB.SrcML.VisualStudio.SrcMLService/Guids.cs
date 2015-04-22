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

namespace ABB.VisualStudio {
    /// <summary>
    /// All the service GUIDs available in this package
    /// </summary>
    public static class GuidList {
        /// <summary>SrcML service package GUID</summary>
        public const string SrcMLServicePackageId = "8b448a37-2665-4b23-a2f9-cad4510f1337";

        /// <summary><see cref="ABB.SrcML.VisualStudio.SrcMLServicePackage"/> GUID</summary>
        public static readonly Guid SrcMLServicePackageGuid = new Guid(SrcMLServicePackageId);
        
        /// <summary>SrcML service package command ID</summary>
        public const string guidSrcMLServiceCmdSetString = "a92a902c-213b-4b54-9580-afacc7240bec";

        /// <summary>SrcML service package command GUID</summary>
        public static readonly Guid guidSrcMLServiceCmdSet = new Guid(guidSrcMLServiceCmdSetString);

        /// <summary><see cref="ABB.SrcML.VisualStudio.IMethodTrackService"/> ID</summary>
        public const string IMethodTrackServiceId = "49069910-2125-4DA1-920B-5DCACCF2D105";
        
        /// <summary><see cref="ABB.SrcML.VisualStudio.SMethodTrackService"/> ID</summary>
        public const string SMethodTrackServiceId = "C368B627-B76F-410C-8E11-E9243E47F562";
        
        /// <summary><see cref="ABB.SrcML.VisualStudio.ICursorMonitorService"/> ID</summary>
        public const string ICursorMonitorServiceId = "243E0BCC-563C-4E31-A360-49DB56F825BB";

        /// <summary><see cref="ABB.SrcML.VisualStudio.SCursorMonitorService"/> ID</summary>
        public const string SCursorMonitorServiceId = "36BEEBB8-613C-4764-A0ED-D2090CCBA023";
        
        /// <summary><see cref="ABB.SrcML.VisualStudio.ISrcMLGlobalService"/> ID</summary>
        public const string ISrcMLGlobalServiceId = "ba9fe7a3-e216-424e-87f9-dee001228d04";

        /// <summary><see cref="ABB.SrcML.VisualStudio.SSrcMLGlobalService"/> ID</summary>
        public const string SSrcMLGlobalServiceId = "fafafdfb-60f3-47e4-b38c-1bae05b44241";

        /// <summary><see cref="ABB.SrcML.VisualStudio.ISrcMLDataService"/> ID</summary>
        public const string ISrcMLDataServiceId = "3331EA7E-2877-45F5-9E14-31FF0F5B761A";

        /// <summary><see cref="ABB.SrcML.VisualStudio.SSrcMLDataService"/> ID</summary>
        public const string SSrcMLDataServiceId = "4F09B16E-9048-40BA-89FA-31F692C5D8E0";

        /// <summary><see cref="ITaskManagerService"/> ID</summary>
        public const string ITaskManagerServiceId = "11875F19-54B4-4C88-BA8D-E2B4A702D115";

        /// <summary><see cref="STaskManagerService"/> ID</summary>
        public const string STaskManagerServiceId = "4B917BC0-C42E-447C-B732-6A675EDF4EB9";

        /// <summary><see cref="ABB.SrcML.VisualStudio.IWorkingSetRegistrarService"/> ID</summary>
        public const string IWorkingSetRegistrarServiceId = "6550A558-65FF-45A9-AD44-00F49FC6F2A3";

        /// <summary><see cref="ABB.SrcML.VisualStudio.SWorkingSetRegistrarService"/> ID</summary>
        public const string SWorkingSetRegistrarServiceId = "07C20FC2-3AF4-4194-89E4-6B2226C4497B";


        /// <summary><see cref="ABB.SrcML.VisualStudio.IMethodTrackService"/> GUID</summary>
        public static readonly Guid IMethodTrackServiceGuid = new Guid(IMethodTrackServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.SMethodTrackService"/> GUID</summary>
        public static readonly Guid SMethodTrackServiceGuid = new Guid(SMethodTrackServiceId);
        
        /// <summary><see cref="ABB.SrcML.VisualStudio.ICursorMonitorService"/> GUID</summary>
        public static readonly Guid ICursorMonitorServiceGuid = new Guid(ICursorMonitorServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.SCursorMonitorService"/> GUID</summary>
        public static readonly Guid SCursorMonitorServiceGuid = new Guid(SCursorMonitorServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.ISrcMLGlobalService"/> GUID</summary>
        public static readonly Guid ISrcMLGlobalServiceGuid = new Guid(ISrcMLGlobalServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.SSrcMLGlobalService"/> GUID</summary>
        public static readonly Guid SSrcMLGlobalServiceGuid = new Guid(SSrcMLGlobalServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.ISrcMLDataService"/> GUID</summary>
        public static readonly Guid ISrcMLDataServiceGuid = new Guid(ISrcMLDataServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.SSrcMLDataService"/> GUID</summary>
        public static readonly Guid SSrcMLDataServiceGuid = new Guid(SSrcMLDataServiceId);

        /// <summary><see cref="ITaskManagerService"/> GUID</summary>
        public static readonly Guid ITaskManagerServiceGuid = new Guid(ITaskManagerServiceId);

        /// <summary><see cref="STaskManagerService"/> GUID</summary>
        public static readonly Guid STaskManagerServiceGuid = new Guid(STaskManagerServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.IWorkingSetRegistrarService"/> GUID</summary>
        public static readonly Guid IWorkingSetRegistrarServiceGuid = new Guid(IWorkingSetRegistrarServiceId);

        /// <summary><see cref="ABB.SrcML.VisualStudio.SWorkingSetRegistrarService"/> GUID</summary>
        public static readonly Guid SWorkingSetRegistrarServiceGuid = new Guid(SWorkingSetRegistrarServiceId);
    };
}