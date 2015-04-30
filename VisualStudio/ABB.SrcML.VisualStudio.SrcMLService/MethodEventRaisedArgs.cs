/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *   Xiao Qu (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.VisualStudio
{
    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum MethodEventType
    {
        ///// <summary>
        ///// Raised when a method is added
        ///// </summary>
        //MethodAdded,

        /// <summary>
        /// Raised when a method is changed 
        /// </summary>
        MethodChanged,

        /// <summary>
        /// Raised when a method is deleted
        /// </summary>
        MethodDeleted,

        /// <summary>
        /// Raised when a cursor position is changed (regardless in the same or different method) 
        /// </summary>
        PositionChanged,
    }

    /// <summary>
    /// Event data of method events.
    /// </summary>
    public class MethodEventRaisedArgs : System.EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected MethodEventRaisedArgs()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eventType">position change, or add/change/delete a method</param>
        /// <param name="methodObj">method name, type, file FilePath, start line number of the method</param>
        /// <param name="curLine">current line number of the method</param>
        /// <param name="curColumn">current column of the method</param>
        public MethodEventRaisedArgs(MethodEventType eventType, Method methodObj, Method oldMethod, int curLine, int curColumn)            
        {
            this.EventType = eventType;
            this.method = methodObj;
            this.oldMethod = oldMethod;
            this.curLine = curLine;
            this.curColumn = curColumn;
        }

        public MethodEventRaisedArgs(Method methodObj, Method oldMethod, int curLine, int curColumn)
            : this(MethodEventType.PositionChanged, methodObj, oldMethod, curLine, curColumn)
        {      
        }

        public MethodEventRaisedArgs(MethodEventType eventType, Method methodObj, Method oldMethod)
            : this(eventType, methodObj, oldMethod, 0, 0)
        {   
        }

        /// <summary>
        /// Type of the method event
        /// </summary>
        public MethodEventType EventType
        {
            get;
            set;
        }

        /// <summary>
        /// Method Object (three cases)
        /// 1. a method where the current cursor is at: MethodEventType.PositionChanged
        /// 2. a new method after method change:  MethodEventType.MethodChanged
        /// 3. NULL when a method is deleted: MethodEventType.MethodDeleted
        /// </summary>
        public Method method
        {
            get;
            set;
        }

        /// <summary>
        /// Old Method Object (before the method is changed/deleted)
        /// </summary>
        public Method oldMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Current line number
        /// </summary>
        public int curLine
        {
            get;
            set;
        }

        /// <summary>
        /// Current cloumn number
        /// </summary>
        public int curColumn
        {
            get;
            set;
        }
    }
}
