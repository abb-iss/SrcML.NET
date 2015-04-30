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
    /// The IMethodTrackService interface represents an object that tracks the method change (position change, add, delete) 
    /// </summary>
    [Guid(GuidList.IMethodTrackServiceId), ComVisible(true)]
    public interface IMethodTrackService 
    {
        //list of visited methods

        /// <summary>
        /// Raised when a method is change (position change, method add/delete)
        /// </summary>
        event EventHandler<MethodEventRaisedArgs> MethodUpdatedEvent;

        /// <summary>
        /// The current method that the cursor is on
        /// </summary>
        Method CurrentMethod { get; }

        /// <summary>The current line number in <see cref="CurrentMethod"/> that the cursor is on</summary>
        int CurrentLineNumber { get; }

        /// <summary>The current column number in <see cref="CurrentMethod"/> that the cursor is at</summary>
        int CurrentColumnNumber { get; }

        /// <summary>
        /// A list of methods that have been navigated (collected based on cursor movements, 
        /// incorporating various types of method update such as change and deletion)
        /// NOTE: there is no duplication or order of the methods 
        /// (i.e., method being navigated more than once only stores once and remains at the same location of the list)
        /// </summary>
        /// <returns></returns>
        List<Method> NavigatedMethods { get; }
    }

    /// <summary>
    /// Service interface for getting an instance of <see cref="IMethodTrackService"/>
    /// </summary>
    [Guid(GuidList.SMethodTrackServiceId)]
    public interface SMethodTrackService { }
}
