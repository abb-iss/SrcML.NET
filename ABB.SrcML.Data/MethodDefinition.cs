/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a method definition in a program.
    /// </summary>
    public class MethodDefinition : NamedScope {
        private List<VariableDeclaration> parameterList;
        private List<MethodCall> initializerList;
        private Dictionary<string, TypeUse> _returnTypeMap;
        private Dictionary<string, List<VariableDeclaration>> _parameterMap;

        /// <summary> The XML name for MethodDefinition </summary>
        public new const string XmlName = "Method";

        /// <summary> XML Name for <see cref="ConstructorInitializers"/></summary>
        public const string XmlConstructorInitializersName = "ConstructorInitializers";

        /// <summary> XML Name for <see cref="IsConstructor" /> </summary>
        public const string XmlIsConstructorName = "IsConstructor";

        /// <summary> XML Name for <see cref="IsDestructor" /> </summary>
        public const string XmlIsDestructorName = "IsDestructor";

        /// <summary> XML Name for <see cref="IsPartial" /> </summary>
        public const string XmlIsPartialName = "IsPartial";

        /// <summary> XML Name for <see cref="Parameters" /> </summary>
        public const string XmlParametersName = "Parameters";

        /// <summary> XML Name for <see cref="ReturnType" /> </summary>
        public const string XmlReturnTypeName = "ReturnType";

        /// <summary>
        /// Creates a new method definition object
        /// </summary>
        public MethodDefinition()
            : base() {
            _returnTypeMap = new Dictionary<string, TypeUse>(StringComparer.OrdinalIgnoreCase);
            _parameterMap = new Dictionary<string, List<VariableDeclaration>>(StringComparer.OrdinalIgnoreCase);

            parameterList = new List<VariableDeclaration>();
            Parameters = new ReadOnlyCollection<VariableDeclaration>(parameterList);

            initializerList = new List<MethodCall>();
            ConstructorInitializers = new ReadOnlyCollection<MethodCall>(initializerList);
        }

        /// <summary> Indicates whether this method is a constructor. </summary>
        public bool IsConstructor { get; set; }
        
        /// <summary> Indicates whether this method is a destructor. </summary>
        public bool IsDestructor { get; set; }
        
        /// <summary> Indicates whether this is a partial method. </summary>
        public bool IsPartial { get; set; }

        /// <summary> The parameters to the method. </summary>
        public ReadOnlyCollection<VariableDeclaration> Parameters { get; private set; }

        /// <summary> The list of initialization calls appearing in a constructor. This is only applicable to C++ and C#. </summary>
        public ReadOnlyCollection<MethodCall> ConstructorInitializers { get; private set; }

        /// <summary> The return type of the method. </summary>
        public TypeUse ReturnType {
            get {
                if(_returnTypeMap.Count > 0) {
                    return _returnTypeMap.First().Value;
                }
                return null;
            }
        }

        //TODO: record other keywords besides access modifiers? for example, static

        /// <summary>
        /// Adds set of method parameters to this method. If the <paramref name="parameters"/> have a different set of
        /// type name values than <see cref="Parameters"/>, then the current list is cleared
        /// and <paramref name="parameters"/> is used. If the variable type names match, then <paramref name="parameters"/>
        /// only matches if it has extra information (such as variable names or initializers).
        /// </summary>
        /// <param name="parameters">The collection of method parameters to add</param>
        public void AddMethodParameters(List<VariableDeclaration> parameters) {
            if(parameters == null) { throw new ArgumentNullException("parameters"); }

            if(parameters.Count > 0) {
                if(parameterList.Count > 0 && GetParameterFingerprint(parameterList) != GetParameterFingerprint(parameters)) {
                    _parameterMap.Clear();
                }

                foreach(var param in parameters) {
                    if(param != null) {
                        param.ParentStatement = this;
                    }
                }
                _parameterMap[parameters[0].Location.ToString()] = parameters;

                if(parameterList.Count == 0 || ComputeParameterInfoScore(parameterList) < ComputeParameterInfoScore(parameters)) {
                    parameterList.Clear();
                    parameterList.AddRange(parameters);
                }
            }
        }

        /// <summary>
        /// Adds a return type to the internal return type collection. If the <paramref name="returnType"/> has a different
        /// type name than this object, then the map is cleared and <paramref name="returnType" /> is the
        /// sole return type for this method.
        /// </summary>
        /// <param name="returnType">The return type object to add</param>
        public void AddReturnType(TypeUse returnType) {
            if(returnType == null) { throw new ArgumentNullException("returnType"); }

            if(null != ReturnType && this.ReturnType.Name != returnType.Name) {
                _returnTypeMap.Clear();
            }
            returnType.ParentStatement = this;
            _returnTypeMap[returnType.Location.ToString()] = returnType;
        }

        /// <summary>
        /// Adds the given initializer call to the ConstructorInitializers collection.
        /// </summary>
        /// <param name="initializerCall">The initializer to add.</param>
        public void AddInitializer(MethodCall initializerCall) {
            if(initializerCall == null) { throw new ArgumentNullException("initializerCall"); }
            initializerCall.ParentStatement = this;
            initializerCall.IsConstructorInitializer = true;
            initializerList.Add(initializerCall);
        }

        /// <summary>
        /// Adds the given initializer calls to the ConstructorInitializers collection.
        /// </summary>
        /// <param name="initializerCalls">The initializers to add.</param>
        public void AddInitializers(IEnumerable<MethodCall> initializerCalls) {
            foreach(var call in initializerCalls) {
                AddInitializer(call);
            }
        }

        //TODO: create method ResolveReturnType that will match the return type, and handle determining the constructor return type

        /// <summary>
        /// Instance method for getting <see cref="MethodDefinition.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for MethodDefinition</returns>
        public override string GetXmlName() { return MethodDefinition.XmlName; }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions()
        {
            if(ReturnType != null) {
                yield return ReturnType;
            }
            if(Prefix != null) {
                yield return Prefix;
            }
            //TODO: add type parameters, once they exist
            foreach(var param in Parameters) {
                yield return param;
            }
            foreach(var init in ConstructorInitializers) {
                yield return init;
            }
        }

        public override Statement Merge(Statement otherStatement) {
            return this.Merge(otherStatement as MethodDefinition);
        }

        public MethodDefinition Merge(MethodDefinition otherMethod) {
            if(null == otherMethod) {
                throw new ArgumentNullException("otherMethod");
            }

            MethodDefinition combinedMethod = Merge<MethodDefinition>(this, otherMethod);
            
            combinedMethod.IsPartial = this.IsPartial;
            combinedMethod.IsConstructor = this.IsConstructor;
            combinedMethod.IsDestructor = this.IsDestructor;

            foreach(var returnType in this._returnTypeMap.Values.Concat(otherMethod._returnTypeMap.Values)) {
                combinedMethod.AddReturnType(returnType);
            }
            foreach(var parameterList in this._parameterMap.Values.Concat(otherMethod._parameterMap.Values)) {
                combinedMethod.AddMethodParameters(parameterList);
            }
            
            return combinedMethod;
        }

        public override void RemoveFile(string fileName) {
            var returnTypeLocations = (from key in _returnTypeMap.Keys
                                       where key.StartsWith(fileName, StringComparison.OrdinalIgnoreCase)
                                       select key).ToList();
            foreach(var key in returnTypeLocations) {
                _returnTypeMap.Remove(key);
            }

            var parameterListLocations = (from key in _parameterMap.Keys
                                          where key.StartsWith(fileName, StringComparison.OrdinalIgnoreCase)
                                          select key).ToList();

            foreach(var key in parameterListLocations) {
                _parameterMap.Remove(key);
            }
            parameterList.Clear();
            if(_parameterMap.Count > 0) {
                var bestParameterList = (from plist in _parameterMap.Values
                                         orderby ComputeParameterInfoScore(plist) descending
                                         select plist).First();
                parameterList.AddRange(bestParameterList);
            }
            
            base.RemoveFile(fileName);
        }
        protected override string ComputeMergeId() {
            if(!PrefixIsResolved || Language.Java == ProgrammingLanguage || Language.CSharp == ProgrammingLanguage && !IsPartial) {
                return base.ComputeMergeId();
            }
            char methodType = 'M';
            if(IsConstructor) {
                methodType = 'C';
            } else if(IsDestructor) {
                methodType = 'D';
            }

            var parameterTypes = from parameter in Parameters
                                 select parameter.VariableType;
            string id = String.Format("{0}:M{1}:{2}:{3}", KsuAdapter.GetLanguage(ProgrammingLanguage), methodType, this.Name, String.Join(",", parameterTypes));

            return id;
        }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            string attribute = reader.GetAttribute(XmlIsConstructorName);
            if(null != attribute) {
                IsConstructor = XmlConvert.ToBoolean(attribute);
            }
            attribute = reader.GetAttribute(XmlIsDestructorName);
            if(null != attribute) {
                IsDestructor = XmlConvert.ToBoolean(attribute);
            }
            attribute = reader.GetAttribute(XmlIsPartialName);
            if(null != attribute) {
                IsPartial = XmlConvert.ToBoolean(attribute);
            }
            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlParametersName == reader.Name) {
                AddMethodParameters(XmlSerialization.ReadChildExpressions(reader).Cast<VariableDeclaration>().ToList());
            } else if(XmlReturnTypeName == reader.Name) {
                AddReturnType(XmlSerialization.ReadChildExpression(reader) as TypeUse);
            } else if(XmlConstructorInitializersName == reader.Name) {
                AddInitializers(XmlSerialization.ReadChildExpressions(reader).Cast<MethodCall>().ToList());
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes XML attributes from this object to the XML writer
        /// </summary>
        /// <param name="writer">The XML writer</param>
        protected override void WriteXmlAttributes(XmlWriter writer) {
            if(IsConstructor) {
                writer.WriteAttributeString(XmlIsConstructorName, XmlConvert.ToString(IsConstructor));
            }
            if(IsDestructor) {
                writer.WriteAttributeString(XmlIsDestructorName, XmlConvert.ToString(IsDestructor));
            }
            if(IsPartial) {
                writer.WriteAttributeString(XmlIsPartialName, XmlConvert.ToString(IsPartial));
            }
            base.WriteXmlAttributes(writer);
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Parameters) {
                XmlSerialization.WriteCollection<VariableDeclaration>(writer, XmlParametersName, Parameters);
            }
            if(null != ConstructorInitializers) {
                XmlSerialization.WriteCollection<MethodCall>(writer, XmlConstructorInitializersName, ConstructorInitializers);
            }
            if(null != ReturnType) {
                XmlSerialization.WriteElement(writer, ReturnType, XmlReturnTypeName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns the children of this MethodDefinition that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// If the <paramref name="use"/> occurs within this MethodDefinition, only the children that occur prior to that use will be returned.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public override IEnumerable<T> GetNamedChildren<T>(NameUse use, bool searchDeclarations) {
            //location comparison is only valid if the use occurs within this method (or its children)
            var filterLocation = PrimaryLocation.Contains(use.Location);
            if(filterLocation) {
                var scopes = GetChildren().OfType<T>().Where(ns => ns.Name == use.Name && PositionComparer.CompareLocation(PrimaryLocation, use.Location) < 0);
                if(!searchDeclarations) { return scopes; }

                //this will return the var decls in document order
                var decls = from declStmt in GetChildren().OfType<DeclarationStatement>()
                            where PositionComparer.CompareLocation(declStmt.PrimaryLocation, use.Location) < 0
                            from decl in declStmt.GetDeclarations().OfType<T>()
                            where decl.Name == use.Name
                            select decl;
                return scopes.Concat(decls);
            } else {
                return GetNamedChildren<T>(use.Name, searchDeclarations);
            }
        }

        /// <summary>
        /// Finds the method calls that resolve to this MethodDefinition.
        /// </summary>
        public IEnumerable<MethodCall> GetCallsToSelf() {
            var globalScope = GetAncestors<NamespaceDefinition>().FirstOrDefault(n => n.IsGlobal);
            if(null == globalScope) {
                throw new StatementDetachedException(this);
            }

            return GetCallsToSelf(globalScope);
        }

        /// <summary>
        /// Finds the method calls that resolve to this MethodDefintion.
        /// </summary>
        /// <param name="rootScope">The Statement to search below for method calls.</param>
        /// <returns>An enumerable of MethodCalls located at/below <paramref name="rootScope"/> that resolve to this MethodDefinition.</returns>
        public IEnumerable<MethodCall> GetCallsToSelf(Statement rootScope) {
            if(null == rootScope) { throw new ArgumentNullException("rootScope"); }
            
            return rootScope.GetCallsTo(this, true);
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            var signature = new StringBuilder();
            if(Accessibility != AccessModifier.None) { signature.AppendFormat("{0} ", Accessibility.ToKeywordString()); }
            if(IsPartial) { signature.Append("partial "); }
            if(ReturnType != null) { signature.AppendFormat("{0} ", ReturnType); }
            if(IsDestructor) { signature.Append("~"); }
            signature.Append(Name);
            var paramsString = string.Join(", ", Parameters);
            signature.AppendFormat("({0})", paramsString);
            var initString = string.Join(", ", ConstructorInitializers);
            if(!string.IsNullOrEmpty(initString)) {
                signature.AppendFormat(" : {0}", initString);
            }
            return signature.ToString();
        }

        #region Private Methods
        private static string GetParameterFingerprint(ICollection<VariableDeclaration> parameters) {
            var parameterTypes = from p in parameters select p.VariableType.Name;
            return String.Join(",", parameterTypes);
        }

        private static int ComputeParameterInfoScore(ICollection<VariableDeclaration> parameters) {
            int score = 0;
            if(parameters.All(p => !String.IsNullOrEmpty(p.Name))) {
                ++score;
            }
            if(parameters.Any(p => null != p.Initializer)) {
                ++score;
            }

            return score;
        }
        #endregion Private Methods
    }

    

    //    internal class MethodDebugView {
    //        private IMethodDefinition method;

    //        public MethodDebugView(IMethodDefinition method) {
    //            this.method = method;
    //        }

    //        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    //        public IScope[] ChildScopes {
    //            get { return this.method.ChildScopes.ToArray(); }
    //        }

    //        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    //        public IMethodCall[] MethodCalls { get { return this.method.MethodCalls.ToArray(); } }

    //        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    //        public IParameterDeclaration[] Parameters { get { return method.Parameters.ToArray(); } }

    //        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    //        public IVariableDeclaration[] Variables { get { return this.method.DeclaredVariables.ToArray(); } }

    //        public override string ToString() {
    //            return method.ToString();
    //        }
    //    }
    //}
}