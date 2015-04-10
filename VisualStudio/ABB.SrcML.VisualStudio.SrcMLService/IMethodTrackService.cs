/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Xiao Qu (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ABB.SrcML.Data;
using ABB.SrcML;

namespace ABB.VisualStudio
{
    /// <summary>
    /// The IMethodTrackService interface represents an object that tracks the methoddef change (add, delete) 
    /// </summary>
    [Guid(GuidList.IMethodTrackService), ComVisible(true)]
    public interface IMethodTrackService 
    {
        //list of visited methods

        /// <summary>
        /// Raised when a methoddef is change or a cusor is moving
        /// </summary>
        event EventHandler<MethodEventRaisedArgs> MethodEvent; //Method Add (FileChanged, FileAdded?)
        
    }

    /// <summary>
    /// Service interface for getting an instance of <see cref="IMethodTrackService"/>
    /// </summary>
    [Guid(GuidList.SMethodTrackService)]
    public interface SMethodTrackService { }
}
