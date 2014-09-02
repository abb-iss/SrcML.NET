/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// This exception is thrown whenever a function recieves an XElement parameter with an invalid XName.
    /// <see cref="ABB.SrcML.SrcMLHelper.ThrowExceptionOnInvalidName(System.Xml.Linq.XName,System.Xml.Linq.XName)">for the typical way of checking for and throwing this exception.</see>
    /// </summary>
    [Serializable]
    public sealed class SrcMLRequiredNameException : SrcMLException
    {
        private ICollection<XName> validNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRequiredNameException"/> class.
        /// </summary>
        public SrcMLRequiredNameException()
            : base("An unknown name is required")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRequiredNameException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SrcMLRequiredNameException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRequiredNameException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SrcMLRequiredNameException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRequiredNameException"/> class.
        /// </summary>
        /// <param name="validNames">The valid names.</param>
        /// <param name="message">The message.</param>
        public SrcMLRequiredNameException(ICollection<XName> validNames, string message)
            : base(message)
        {
            this.validNames = validNames;
        }

        /// <summary>
        /// Creates the exception with the given XName and message.
        /// </summary>
        /// <param name="expectedName">The expected XName</param>
        /// <param name="message">A message describing what went wrong</param>
        public SrcMLRequiredNameException(XName expectedName, string message)
            : this(new Collection<XName>() { expectedName }, message)
        {
            
        }

        /// <summary>
        /// Creates the exception with the given collection of XNames and a default message
        /// </summary>
        /// <param name="validNames">collection of valid names</param>
        public SrcMLRequiredNameException(ICollection<XName> validNames)
            : this(validNames, String.Format(CultureInfo.CurrentCulture, "Name must be from the following list: {0}", ConvertCollectionToString(validNames)))
        {

        }

        /// <summary>
        /// Creates the exception with the given XName and a default message
        /// </summary>
        /// <param name="expectedName">The expected XName</param>
        public SrcMLRequiredNameException(XName expectedName)
            : this(expectedName , String.Format(CultureInfo.CurrentCulture, "Name is expected to be {0}", expectedName))
        {

        }

        private SrcMLRequiredNameException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {

        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic). </exception>
        ///   
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/>
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/>
        ///   </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (null == info)
                throw new ArgumentNullException("info");

            base.GetObjectData(info, context);
            info.AddValue("ExpectedNames", ExpectedNames);
        }
        /// <summary>
        /// The collection of required names
        /// </summary>
        public ICollection<XName> ExpectedNames
        {
            get
            {
                return this.validNames;
            }
        }

        private static string ConvertCollectionToString(ICollection<XName> names)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var name in names)
            {
                var ns = SrcMLNamespaces.LookupPrefix(name.Namespace.NamespaceName);
                builder.AppendFormat(", {0}:{1}", ns, name.LocalName);
            }
            builder.Remove(0, 2);
            return builder.ToString();
        }
    }
}
