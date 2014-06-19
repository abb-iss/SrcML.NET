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
using System.Xml;
using System.Xml.Serialization;

namespace ABB.SrcML.Data {
    internal delegate T XmlInitializer<T>(XmlReader reader) where T : IXmlElement, new();
    
    /// <summary>
    /// XmlSerialization provides helper methods that aid in serializing and deserializing different objects in SrcML.Data.
    /// </summary>
    internal class XmlSerialization {
        internal static Dictionary<string, XmlInitializer<SourceLocation>> XmlLocationMap = new Dictionary<string, XmlInitializer<SourceLocation>>() {
            { SourceLocation.XmlName, CreateFromReader<SourceLocation> },
            { SrcMLLocation.XmlName, CreateFromReader<SrcMLLocation> },
        };

        internal static Dictionary<string, XmlInitializer<Statement>> XmlStatementMap = new Dictionary<string, XmlInitializer<Statement>>() {
            { Statement.XmlName, CreateFromReader<Statement> },
            
            /* alias & import statements */
            { ImportStatement.XmlName, CreateFromReader<ImportStatement> },

            /* basic statements */
            { BreakStatement.XmlName, CreateFromReader<BreakStatement> },
            { CaseStatement.XmlName, CreateFromReader<CaseStatement> },
            { ContinueStatement.XmlName, CreateFromReader<ContinueStatement> },
            { ExternStatement.XmlName, CreateFromReader<ExternStatement> },
            { LabelStatement.XmlName, CreateFromReader<LabelStatement> },
            { ReturnStatement.XmlName, CreateFromReader<ReturnStatement> },
            { ThrowStatement.XmlName, CreateFromReader<ThrowStatement> },

            /* block statements */
            { BlockStatement.XmlName, CreateFromReader<BlockStatement> },
            { CatchStatement.XmlName, CreateFromReader<CatchStatement> },
            { ForStatement.XmlName, CreateFromReader<ForStatement> },
            { ForeachStatement.XmlName, CreateFromReader<ForeachStatement> },
            { IfStatement.XmlName, CreateFromReader<IfStatement> },
            { SwitchStatement.XmlName, CreateFromReader<SwitchStatement> },
            { TryStatement.XmlName, CreateFromReader<TryStatement> },
            { WhileStatement.XmlName, CreateFromReader<WhileStatement> },
            { DoWhileStatement.XmlName, CreateFromReader<DoWhileStatement> },
            { UsingBlockStatement.XmlName, CreateFromReader<UsingBlockStatement> },

            /* Named statements */
            { NamespaceDefinition.XmlName, CreateFromReader<NamespaceDefinition> },
            { TypeDefinition.XmlName, CreateFromReader<TypeDefinition> },
            { MethodDefinition.XmlName, CreateFromReader<MethodDefinition> },
            { PropertyDefinition.XmlName, CreateFromReader<PropertyDefinition> },
        };

        internal static Dictionary<string, XmlInitializer<Expression>> XmlExpressionMap = new Dictionary<string, XmlInitializer<Expression>>() {
            { Expression.XmlName, CreateFromReader<Expression> },
            { LiteralUse.XmlName, CreateFromReader<LiteralUse> },
            { MethodCall.XmlName, CreateFromReader<MethodCall> },
            { NameUse.XmlName, CreateFromReader<NameUse> },
            { NamePrefix.XmlName, CreateFromReader<NamePrefix> },
            { NamespaceUse.XmlName, CreateFromReader<NamespaceUse> },
            { OperatorUse.XmlName, CreateFromReader<OperatorUse> },
            { TypeUse.XmlName, CreateFromReader<TypeUse> },
            { VariableDeclaration.XmlName, CreateFromReader<VariableDeclaration> },
            { VariableUse.XmlName, CreateFromReader<VariableUse> },
        };

        internal static IXmlElement Load(string fileName) {
            using(var reader = new XmlTextReader(fileName)) {
                return DeserializeStatement(reader);
            }
        }

        /// <summary>
        /// Writes <paramref name="element"/> to <paramref name="fileName"/>.
        /// </summary>
        /// <param name="element">The element to serializer</param>
        /// <param name="fileName">The file name to write <paramref name="element"/> to</param>
        internal static void WriteElement(IXmlElement element, string fileName) {
            using(var writer = new XmlTextWriter(fileName, null)) {
                writer.WriteStartDocument();
                WriteElement(writer, element);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Deserializes the <see cref="Expression"/> at the current <paramref name="reader"/> position. This automatically looks up the correct
        /// object to instantiate based on <see cref="XmlReader.Name"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>A new expression of the appropriate type</returns>
        internal static Expression DeserializeExpression(XmlReader reader) {
            return XmlExpressionMap[reader.Name](reader);
        }

        /// <summary>
        /// Deserializes the <see cref="SourceLocation"/> at the current <paramref name="reader"/> position. This automatically looks up the correct
        /// object to instantiate based on <see cref="XmlReader.Name"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>A new location of the appropriate type</returns>
        internal static SourceLocation DeserializeLocation(XmlReader reader) {
            return XmlLocationMap[reader.Name](reader);
        }

        internal static SrcMLLocation DeserializeSrcMLLocation(XmlReader reader) {
            return CreateFromReader<SrcMLLocation>(reader);
        }

        /// <summary>
        /// Deserializes the <see cref="Statement"/> at the current <paramref name="reader"/> position. This automatically looks up the correct
        /// object to instantiate based on <see cref="XmlReader.Name"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>A new statement object of the appropriate type</returns>
        internal static Statement DeserializeStatement(XmlReader reader) {
            return XmlStatementMap[reader.Name](reader);
        }

        /// <summary>
        /// Deserializes a collection of <see cref="SourceLocation"/> objects from <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>An enumerable of <see cref="SourceLocation"/> objects</returns>
        internal static IEnumerable<SourceLocation> ReadChildLocations(XmlReader reader) { return ReadChildCollection<SourceLocation>(reader, DeserializeLocation); }

        internal static IEnumerable<SrcMLLocation> ReadChildSrcMLLocations(XmlReader reader) { return ReadChildCollection<SrcMLLocation>(reader, DeserializeSrcMLLocation); }

        internal static Expression ReadChildExpression(XmlReader reader) { return ReadChildElement<Expression>(reader, DeserializeExpression); }

        /// <summary>
        /// Deserializes a collection of <see cref="Expression"/> objects from <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>An enumerable of <see cref="Expression"/> objects</returns>
        internal static IEnumerable<Expression> ReadChildExpressions(XmlReader reader) { return ReadChildCollection<Expression>(reader, DeserializeExpression); }

        internal static Statement ReadChildStatement(XmlReader reader) { return ReadChildElement<Statement>(reader, DeserializeStatement); }

        /// <summary>
        /// Deserializes a collection of <see cref="Statement"/> objects from <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        /// <returns>An enumerable of <see cref="Statement"/> objects</returns>
        internal static IEnumerable<Statement> ReadChildStatements(XmlReader reader) { return ReadChildCollection<Statement>(reader, DeserializeStatement); }

        /// <summary>
        /// Writes the <paramref name="element"/> with <paramref name="writer"/>. The element name is taken from <see cref="IXmlElement.GetXmlName()"/>.
        /// </summary>
        /// <param name="writer">The XML writer</param>
        /// <param name="element">The object to write</param>
        /// <param name="parentElementName">the parent element. If not null, <paramref name="element"/> is wrapped in an element with this name</param>
        internal static void WriteElement(XmlWriter writer, IXmlElement element, string parentElementName = null) {
            if(!String.IsNullOrEmpty(parentElementName)) {
                writer.WriteStartElement(parentElementName);
            }
            
            writer.WriteStartElement(element.GetXmlName());
            element.WriteXml(writer);
            writer.WriteEndElement();

            if(!String.IsNullOrEmpty(parentElementName)) {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Writes the given <paramref name="collection"/> to the <paramref name="writer"/> with the given <paramref name="collectionName">collection name</paramref>
        /// </summary>
        /// <typeparam name="T">the collection element type</typeparam>
        /// <param name="writer">The XML writer</param>
        /// <param name="collectionName">The element name to enclose the collection in</param>
        /// <param name="collection">The collection to serialize</param>
        internal static void WriteCollection<T>(XmlWriter writer, string collectionName, ICollection<T> collection) where T : IXmlElement {
            if(collection.Count > 0) {
                writer.WriteStartElement(collectionName);
                foreach(var item in collection) {
                    WriteElement(writer, item);
                }
                writer.WriteEndElement();
            }
        }

        private static T ReadChildElement<T>(XmlReader reader, XmlInitializer<T> initializer) where T : IXmlElement, new() {
            T element = default(T);
            reader.ReadStartElement();
            if(!reader.IsEmptyElement) {
                element = initializer(reader);
                reader.ReadEndElement();
            }
            return element;
        }

        private static IEnumerable<T> ReadChildCollection<T>(XmlReader reader, XmlInitializer<T> initializer) where T : IXmlElement, new() {
            reader.ReadStartElement();
            if(!reader.IsEmptyElement) {
                while(XmlNodeType.Element != reader.NodeType) {
                    yield return initializer(reader);
                }
                reader.ReadEndElement();
            }
        }

        private static T CreateFromReader<T>(XmlReader reader) where T : IXmlElement, new() {
            T tObj = new T();
            tObj.ReadXml(reader);
            return tObj;
        }
    }
}
