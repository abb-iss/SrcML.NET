/******************************************************************************
 * Copyright (c) 2011 Brian Bartman
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Brian Bartman (SDML) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - integration with ABB.SrcML Framework
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SDML.SrcMLVSAddin.SrcML.XMLExtensions;
using Diagnostics = System.Diagnostics;
#region unused imports
//using EnvDTE;
//using System.Xml.XPath;
//using SDML.SrcMLVSAddin.SrcML;
//using System.Diagnostics;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.ComponentModel.Design;
//using Microsoft.Win32;
//using Microsoft.VisualStudio;
//using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.OLE.Interop;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.VCProjectEngine;
//using SDML.SrcMLVSAddin.UI.Controls;
#endregion


namespace SDML.SrcMLVSAddin.SyntaticCategory
{
    /// <summary>
    /// This class represents the finding of a syntatic category occurance
    /// within a srcml archive or msvc project.
    /// </summary>
    /// <remarks>
    /// It may be benificial to use the msvc code model so that if the user
    /// edits something I can go and where it's been moved to. This would be
    /// something which I would only do in the future and only if I have time.
    /// </remarks>
    public class SyntaticOccurance
        //:ITreeListViewHeaderInterface
    {

        #region Constructor/s.
        
        /// <summary>
        /// Constructor which is used to create an instance of this class
        /// however this constructor requiers that some specific properties
        /// be met. The dataModel is simply the class which owns this one
        /// and that should only ever be the class which creates an instance
        /// of this type. The element is the xml element from inside of a 
        /// srcml archive, which represents an instance of a match matched 
        /// by a search pattern. The VCFile is the file which contains 
        /// the matched pattern and is used so that it can be easily located 
        /// and opened if necessary.
        /// 
        /// TODO: in the future: make this into an interface
        /// and implement that interface inside of a different class which is
        /// private to this namespace.
        /// 
        /// Not sure if it's benificial but it may be necessary to use something
        /// other then VCFile but I'm not 100% sure what.
        /// </summary>
        /// <param name="dataModel">The data model which is owns this class.</param>
        /// <param name="node">The element which is used to </param>
        public SyntaticOccurance(SyntaticCategoryDataModel dataModel, XElement element)
        {
            Element = element;
            // SyntaticCategoryGenerationType = SyntaticCategoryPathTypes.OuterBlockCategory;
            mDataModel = dataModel;
            XElement translationUnitElem = GetTranslationUnit();
            mTranslationUnitName = translationUnitElem.Attributes().Where(
                new Func<XAttribute, bool>(
                    (x) => x.Name == "filename"
                )
            ).First().Value;
            UpdateCategory();
        }

        #endregion

        #region Public Properties.

        /// <summary>
        /// The xml element associated a syntatic occurance.
        /// </summary>
        public XElement Element
        {
            get;
            set;
        }

        /// <summary>
        /// The data model which owns this object.
        /// </summary>
        public SyntaticCategoryDataModel DataModel
        {
            get
            {
                return mDataModel;
            }
        }

        /// <summary>
        /// Returns the file path and name associated with
        /// the translation unit of this
        /// </summary>
        public String TranslationUnitName
        {
            get
            {
                return mTranslationUnitName;
            }
        }

        /// <summary>
        /// Returns the string category for each of the different
        /// categories.
        /// </summary>
        public String Category
        {
            get
            {
                return mCategory;
            }
        }

        /// <summary>
        /// Returns the category as an xpath expression.
        /// </summary>
        public String CategoryAsXPath
        {
            get
            {
                return mCategoryAsXPath;
            }
        }
        #endregion

        #region ITreeListViewInterface Properties.

        ///// <summary>
        ///// The context or occurance column.
        ///// </summary>
        //public String ContextOrOccuranceColumn
        //{
        //    get
        //    {
        //        return Element.Value;
        //    }
        //}

        ///// <summary>
        ///// The Text for the file path column of the grid view.
        ///// </summary>
        //public String FilePathColumn
        //{
        //    get
        //    {
        //        return mTranslationUnitName;
        //    }
        //}
        #endregion

        #region Public Member functions.

        /// <summary>
        /// Attempts to locate the translation unit associated with
        /// the element. Throws an exception when the Element property
        /// is null.
        /// </summary>
        /// <returns>The translation unit associated with the element.</returns>
        public XElement GetTranslationUnit()
        {
            XElement searchElement = Element;
            while (searchElement.Name.LocalName != "unit")
            {
                searchElement = (XElement)searchElement.Parent;
            }
            return searchElement;
        }

        /// <summary>
        /// Returns the line associated with the syntatic occurance
        /// within it's translation unit.
        /// </summary>
        /// <returns>The line number of at which this syntatic occurance occurs.</returns>
        public int GetLineInTranslationUnit()
        {
            XElement surroundingTanslationUnit = GetTranslationUnit();
            return ((IXmlLineInfo)Element).LineNumber - ((IXmlLineInfo)surroundingTanslationUnit).LineNumber;
        }

        /// <summary>
        /// Updates the syntatic category of the of this occurance.
        /// </summary>
        public void UpdateCategory()
        {
            StringBuilder categoryBuilder = new StringBuilder();
            StringBuilder categoryXPathBuilder = new StringBuilder();
            // mCategoryAsXPath
            // OuterStatmentCategory
            if (DataModel.SyntaticCategoryGenerationType == SyntaticCategoryPathTypes.OuterStatmentCategory)
            {
                const String exprStmt = "expr_stmt";
                const String declStmt = "decl_stmt";
                XElement currentElement = Element.Parent;
                while (null != currentElement && currentElement.Name.LocalName != declStmt && currentElement.Name.LocalName != exprStmt)
                {
                    categoryBuilder.Insert(0, "/");
                    categoryBuilder.Insert(0, currentElement.Name.LocalName);

                    // Adding the name first then the slash for the xpath representation.
                    categoryXPathBuilder.Insert(0, currentElement.GetXPathName());
                    categoryXPathBuilder.Insert(0, "/");

                    if (currentElement == null)
                    {
                        mCategory = categoryBuilder.ToString();
                        mCategoryAsXPath = categoryXPathBuilder.ToString();
                        return;
                    }
                    currentElement = currentElement.Parent;
                }

                // if the current element doesn't equal null then get the last
                // element and prepend it into the string builder.
                if (currentElement != null)
                {
                    categoryBuilder.Insert(0, currentElement.Name.LocalName + "/");

                    categoryXPathBuilder.Insert(0, currentElement.GetXPathName());
                    categoryXPathBuilder.Insert(0, "/");
                }
                mCategoryAsXPath = categoryXPathBuilder.ToString();
                mCategory = categoryBuilder.ToString();
                return;
            }

            // OuterMostBlockCategory
            if (DataModel.SyntaticCategoryGenerationType == SyntaticCategoryPathTypes.OuterMostBlockCategory)
            {
                return;
            }

            // OuterBlockCategory
            if (DataModel.SyntaticCategoryGenerationType == SyntaticCategoryPathTypes.OuterBlockCategory)
            {
                XElement currentElement = Element.Parent;
                while (currentElement.Name.LocalName != "unit" && currentElement.Name.LocalName != "block")
                {
                    categoryBuilder.Insert(0, "/");
                    categoryBuilder.Insert(0, currentElement.Name.LocalName);
                    categoryXPathBuilder.Insert(0, currentElement.GetXPathName());
                    categoryXPathBuilder.Insert(0, "/");

                    if (currentElement == null)
                    {
                        mCategoryAsXPath = categoryXPathBuilder.ToString();
                        mCategory = categoryBuilder.ToString();
                        return;
                    }
                    currentElement = currentElement.Parent;
                }

                categoryBuilder.Insert(0, "/");
                categoryBuilder.Insert(0, currentElement.Name.LocalName);
                
                categoryXPathBuilder.Insert(0, currentElement.GetXPathName());
                categoryXPathBuilder.Insert(0, "/");

                mCategoryAsXPath = categoryXPathBuilder.ToString();
                mCategory = categoryBuilder.ToString();
                return;
            }
        }
        #endregion

        #region Member Variables.

        private String mTranslationUnitName;
        private SyntaticCategoryDataModel mDataModel;
        private String mCategory;
        private String mCategoryAsXPath;

        #endregion
    }
}
