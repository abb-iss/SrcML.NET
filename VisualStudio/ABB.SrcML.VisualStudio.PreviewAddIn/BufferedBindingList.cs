/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.ComponentModel;

namespace ABB.SrcML.VisualStudio.PreviewAddIn
{
    class BufferedBindingList<T> : BindingList<T>
    {
        public int BufferCount
        {
            get;
            set;
        }

        

        public BufferedBindingList(IList<T> list, int buffer) : base(list)
        {
            this.BufferCount = buffer;
        }

        public BufferedBindingList(IList<T> list) : this(list, 1)
        {
        }

        public BufferedBindingList(int buffer)
            : base()
        {
            this.BufferCount = buffer;
        }

        public BufferedBindingList() : this(1)
        {
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {

            base.OnListChanged(e);
        }
    }
}
