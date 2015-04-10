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
        /// <summary>
        /// Raised when a methoddef is added
        /// </summary>
        MethodAdded,

        /// <summary>
        /// Raised when a methoddef is changed (including a position change in the same methoddef)
        /// </summary>
        MethodChanged,

        /// <summary>
        /// Raised when a methoddef is deleted
        /// </summary>
        MethodDeleted,
    }

    /// <summary>
    /// Event data of methoddef events.
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
        /// <param name="eventType">add, change, or delete a methoddef</param>
        /// <param name="methodObj">methoddef name, type, file FilePath, start line number of the methoddef</param>
        /// <param name="curLine">current line number of the methoddef</param>
        /// <param name="curColumn">current column of the methoddef</param>
        public MethodEventRaisedArgs(MethodEventType eventType, Method methodObj, int curLine, int curColumn)            
        {
            this.EventType = eventType;
            this.method = methodObj;
            this.curLine = curLine;
            this.curColumn = curColumn;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodObj">methoddef name, type, file FilePath, start line number of the methoddef</param>
        /// <param name="curLine">current line number of the methoddef</param>
        /// <param name="curColumn">current column of the methoddef</param>
        public MethodEventRaisedArgs(Method methodObj, int curLine, int curColumn)
            : this(MethodEventType.MethodChanged, methodObj, curLine, curColumn)
        {      
        }

        /// <summary>
        /// Type of the methoddef event
        /// </summary>
        public MethodEventType EventType
        {
            get;
            set;
        }

        /// <summary>
        /// Method Object
        /// </summary>
        public Method method
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
