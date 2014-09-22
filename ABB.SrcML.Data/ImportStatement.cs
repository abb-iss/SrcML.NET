/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a general import statement in a program. 
    /// (Depending on the programming language, this may use a keyword other than import.)
    /// For example:
    /// Java: import java.lang.*;
    /// C#:   using System.IO;
    /// C++:  using namespace std;
    /// </summary>
    public class ImportStatement : Statement {
        private Expression importExpression;
        
        /// <summary> The XML name for ImportStatement </summary>
        public new const string XmlName = "Import";

        /// <summary> XML Name for <see cref="ImportedNamespace" /> </summary>
        public const string XmlImportedNamespaceName = "ImportedNamespace";

        /// <summary> The namespace being imported. </summary>
        public Expression ImportedNamespace {
            get { return importExpression; }
            set {
                importExpression = value;
                if(importExpression != null) {
                    importExpression.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// Instance method for getting <see cref="ImportStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ImportStatement</returns>
        public override string GetXmlName() { return ImportStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlImportedNamespaceName == reader.Name) {
                ImportedNamespace = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != ImportedNamespace) {
                XmlSerialization.WriteElement(writer, ImportedNamespace, XmlImportedNamespaceName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(ImportedNamespace != null) {
                yield return ImportedNamespace;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            switch(ProgrammingLanguage) {
                case Language.CSharp:
                    return string.Format("using {0};", ImportedNamespace);
                case Language.CPlusPlus:
                    return string.Format("using namespace {0};", ImportedNamespace);
                case Language.Java:
                    return string.Format("import {0}.*;", ImportedNamespace);
                default:
                    goto case Language.CSharp;
            }
        }
    }
}
