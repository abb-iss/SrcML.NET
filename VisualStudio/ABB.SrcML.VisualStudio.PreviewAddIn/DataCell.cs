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
using System.Linq;
using System.Xml.Linq;
using SDML.SrcMLVSAddin.SyntaticCategory;

namespace ABB.SrcML.VisualStudio.PreviewAddIn
{
    class DataEnabledAnnotation
    {
        public bool Enabled { get; set; }

        public DataEnabledAnnotation()
            : this(false)
        {

        }
        public DataEnabledAnnotation(bool startingValue)
        {
            this.Enabled = startingValue;
        }
    }

    class DataTransformAnnotation
    {
        public XElement TransformedXml { get; set; }
        
        public DataTransformAnnotation(XElement transformedXml)
        {
            TransformedXml = transformedXml;
        }
    }
    class DataCell
    {
        private SrcMLFile doc;
        private XElement node;

        private string text;
        private string transformedText;

        private string location;
        private int lineNumber;
        private int endLineNumber;

        private ITransform transform;
        private string error;
        private SyntaticOccurance occurrence;

        public ITransform Transform
        {
            get { return this.transform; }
        }

        public Boolean Enabled
        {
            get
            {
                var annotation = Xml.Annotation<DataEnabledAnnotation>();
                if (null == annotation)
                    return false;
                return annotation.Enabled;
            }
            set
            {
                var annotation = Xml.Annotation<DataEnabledAnnotation>();
                if (null != annotation)
                {
                    if (annotation.Enabled && !value)
                        Revert();
                    else if(!annotation.Enabled && value)
                        ExecuteTransform();
                    annotation.Enabled = value;
                }
            }
        }

        public XElement Xml
        {
            get { return this.node; }
        }

        public XElement TransformedXml
        {
            get
            {
                var annotation = Xml.Annotation<DataTransformAnnotation>();
                if (null != annotation)
                    return annotation.TransformedXml;
                return null;
            }
        }

        public string Location
        {
            get 
            {
                if(lineNumber < 0)
                    return this.FilePath;
                return String.Format("{0}:{1}", this.location, this.lineNumber);
            }
        }

        public int LineNumber
        {
            get
            {
                return this.lineNumber;
            }
        }

        public int EndLineNumber
        {
            get
            {
                return this.endLineNumber;
            }
        }

        public string FilePath
        {
            get
            {
                return this.location;
            }
        }

        public string Text
        {
            get { return text; }
        }

        public string TransformedText
        {
            get
            {
                if (null == TransformedXml)
                    return error;
                return transformedText;
            }
        }

        public string Category
        {
            get
            {
                return this.occurrence.CategoryAsXPath;
            }
        }
        public DataCell(SrcMLFile doc, XElement xe, ITransform transform, SyntaticOccurance occurrence)
        {
            this.doc = doc;
            this.node = xe;
            this.transform = transform;
            this.occurrence = occurrence;

            this.Xml.AddAnnotation(new DataEnabledAnnotation());

            this.location = doc.RelativePath(node);
            this.lineNumber = xe.GetSrcLineNumber();
            if (xe.Descendants().Any())
            {
                this.endLineNumber = xe.Descendants().Last().GetSrcLineNumber();
            }
            else
            {
                this.endLineNumber = this.lineNumber;
            }

            if (null != this.Xml && null != this.Transform)
            {
                try
                {
                    this.text = Xml.ToSource(4);
                    var nodeCopy = new XElement(this.node);
                    var transformedNode = this.transform.Transform(nodeCopy);
                    this.Xml.AddAnnotation(new DataTransformAnnotation(transformedNode));
                    transformedText = transformedNode.ToSource(4);
                }
                catch (Exception e)
                {
                    error = String.Format("{0}: {1} ({2})", e.Source, e.Message, e.GetType().ToString());
                    this.Enabled = false;
                }
            }
        }

        public SrcMLFile Document
        {
            get { return this.doc; }
        }

        public void ExecuteTransform()
        {
            this.Xml.ReplaceWith(this.TransformedXml);
        }

        public void Revert()
        {
            this.TransformedXml.ReplaceWith(this.Xml);
        }
    }
}
