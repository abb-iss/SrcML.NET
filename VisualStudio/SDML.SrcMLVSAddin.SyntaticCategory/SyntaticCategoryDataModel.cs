/******************************************************************************
 * Copyright (c) 2011 Brian Bartman
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Brian Bartman (SDML) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SDML.SrcMLVSAddin.SyntaticCategory
{
    /// <summary>
    /// This class is the data model created when a the occurances of 
    /// a generalized syntatic expression are generated, by preformming a
    /// search with the generalized syntatic expression.
    /// This data structure is used to present all of the different syntatic
    /// category occurances within a given srcml archive or msvc project.
    /// </summary>
    /// <remarks>
    /// TODO/Think-About: this class may need a private constructor and could
    /// be produced inside of a single function which would be used
    /// to do all of the necessary searching and loading of this
    /// data structure.
    /// </remarks>
    public class SyntaticCategoryDataModel
    {
        #region Constructor/s.

        /// <summary>
        /// Simple Default constuctor.
        /// </summary>
        public SyntaticCategoryDataModel()
        {
            SyntaticCategoryGenerationType = SyntaticCategoryPathTypes.OuterStatmentCategory;
            SyntaticCategories = new Dictionary<String, List<SyntaticOccurance>>();
        }

        #endregion

        #region Properties.

        /// <summary>
        /// Gets the syntatic category used to display the
        /// the multiple categories.
        /// </summary>
        public SyntaticCategoryPathTypes SyntaticCategoryGenerationType
        {
            get;
            set;
        }

        /// <summary>
        /// A dictionary of syntatic categories indexed by it's
        /// syntatic category and containing a list of the syntatic occurances
        /// of that category.
        /// </summary>
        public Dictionary<String, List<SyntaticOccurance>> SyntaticCategories
        {
            get;
            set;
        }

        /// <summary>
        /// This string represents the search pattern to obtain the current
        /// search context. This is the XPath string.
        /// </summary>
        public String XPathSearchString
        {
            get;
            set;
        }

        /// <summary>
        /// The code string used to create the xpath search pattern.
        /// </summary>
        public String GeneralizedCodeString
        {
            get;
            set;
        }

        /// <summary>
        /// The document which all of the of the crc contexts
        /// come from.
        /// </summary>
        public XDocument Document
        {
            get;
            set;
        }

        /// <summary>
        /// An xpath expression document which was used to create the generalized
        /// xpath expression and then used to gather the syntatic context.
        /// </summary>
        public XDocument XPathExpressionDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the first element associated with the 
        /// generalized xpath expression.
        /// </summary>
        public XElement XPathExpressionFirstNode
        {
            get;
            set;
        }
           
        #endregion

        #region Public Member Functions.

        /// <summary>
        /// Updates all of the different occurances and all of the 
        /// SyntaticCategories dictionary.
        /// </summary>
        public void UpdateCategories()
        {
            SyntaticCategories.Clear();
            foreach (SyntaticOccurance occurance in mSyntaticOccurances)
            {
                occurance.UpdateCategory();
                AddToSyntaticCategory(occurance);
            }
        }

        /// <summary>
        /// Adds an instance of an occurance into the syntatic category list.
        /// </summary>
        /// <param name="occurance">An instance of a syntatic occurance.</param>
        public void AddOccurance(SyntaticOccurance occurance)
        {
            mSyntaticOccurances.Add(occurance);
            AddToSyntaticCategory(occurance);
        }

        /// <summary>
        /// Creates the syntatic occurance and adds it into the syntatic categories
        /// dictionary.
        /// </summary>
        /// <param name="searchInstance">An element represents a syntatic occurance of a pattern.</param>
        public void AddOccurance(XElement searchInstance)
        {
            SyntaticOccurance occurance = new SyntaticOccurance(this, searchInstance);
            AddOccurance(occurance);
        }

        /// <summary>
        /// Clears all occurances and syntatic categories.
        /// </summary>
        public void Clear()
        {
            SyntaticCategories.Clear();
            mSyntaticOccurances.Clear();
        }
        #endregion

        #region Private Utility Functions.

        /// <summary>
        /// Takes an occurance and adds it into the syntatic categories 
        /// dictionary.
        /// </summary>
        /// <param name="occurance"></param>
        private void AddToSyntaticCategory(SyntaticOccurance occurance)
        {
            if (!SyntaticCategories.ContainsKey(occurance.Category))
            {
                SyntaticCategories.Add(occurance.Category,new List<SyntaticOccurance>());
            }
            SyntaticCategories[occurance.Category].Add(occurance);
        }
        #endregion

        private List<SyntaticOccurance> mSyntaticOccurances = new List<SyntaticOccurance>();
    }
}
