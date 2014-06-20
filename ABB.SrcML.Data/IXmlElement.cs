/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ABB.SrcML.Data {
    /// <summary>
    /// IXmlElement is an internal interface used to aid in serialization. The <see cref="GetXmlName()"/> method is used by the
    /// serialization code to determine the XML element to surround the contents.
    /// 
    /// In order to support both serialization and deserialization, there is a pattern for implementing this method:
    /// 
    /// <example>
    /// This is how to implement <see cref="GetXmlName"/> in order to support serialization and deserialization.
    /// <code>
    /// public class MyClass : IXmlElement {
    ///     public const string XmlName = "MyClass";
    ///     
    ///     public string GetXmlName() { return MyClass.XmlName; }
    /// }
    /// </code>
    /// </example>
    /// 
    /// If your new class is a subclass of <see cref="Statement"/> or <see cref="Expression"/>, you should also add it to
    /// either <see cref="XmlSerialization.XmlStatementMap"/> or <see cref="XmlSerialization.XmlExpressionMap"/>.
    /// </summary>
    public interface IXmlElement : IXmlSerializable {
        /// <summary>
        /// Returns the default XML element name to use for this class.
        /// </summary>
        /// <returns>The XML element name</returns>
        string GetXmlName();
    }
}
