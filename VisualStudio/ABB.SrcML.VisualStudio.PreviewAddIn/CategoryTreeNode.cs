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
using System.Windows.Forms;
using System.Runtime.Serialization;

namespace ABB.SrcML.VisualStudio.PreviewAddIn
{
    class CategoryTreeNode : TreeNode
    {
        int _count;
        string _xpath;

        public CategoryTreeNode()
        {
            
        }
        public CategoryTreeNode(string text)
        {
            this.Name = text;
            if ("All" == text)
                this.Tag = String.Empty;
            else
                this.Tag = text;
            this._xpath = String.Empty;
            this.Count = 0;
        }

        public int Count
        {
            get
            {
                return this._count;
            }
            set
            {
                this._count = value;
                this.Update();
            }
        }

        public CategoryTreeNode Root
        {
            get;
            set;
        }

        public string XPath
        {
            get
            {
                return this._xpath;
            }
        }

        private void setXPath()
        {
            var node = this;
            StringBuilder xpath = new StringBuilder();
            while (null != node && String.Empty != (node.Tag as String))
            {
                xpath.Insert(0, "/" + node.Tag as string);
                node = node.Parent as CategoryTreeNode;
            }
            this._xpath = xpath.ToString();
        }

        public void AddCategory(string category, int count)
        {
            var node = this;

            node.Count += count;
            foreach (var part in category.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (node.Nodes.ContainsKey(part))
                {
                    node = node.Nodes[part] as CategoryTreeNode;
                }
                else
                {
                    var newNode = new CategoryTreeNode(part);
                    node.Nodes.Add(newNode);
                    node = newNode;
                    node.setXPath();
                }
                node.Root = this;
                node.Count += count;
            }

        }
        
        private void UpdateAll()
        {
            foreach (var child in this.Nodes)
            {
                (child as CategoryTreeNode).Update();
            }
        }
        private void Update()
        {
            var pluralString = (this.Count == 1 ? String.Empty : "s");
            string text = String.Format("{0} ({1} item{2})", this.Name, this.Count, pluralString);

            if(null != Parent && null != Root)
            {
                double percentage = (double)this.Count / this.Root.Count;
                if (percentage <= .01 && (this.Parent as CategoryTreeNode).Count > this.Count)
                {
                    text = String.Format("{0} (\u2264 1% - {1} item{2})", this.Name, this.Count, pluralString);
                }
                else if (percentage > .01)
                {
                    text = String.Format("{0} ({1:P0})", this.Name, percentage);
                }
            }
            this.Text = text;
            this.ToolTipText = String.Format("{0} item{1} - {2}", this.Count, pluralString, this.XPath);
            this.UpdateAll();
        }
    }
}
