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
            if(null != ReturnType) {
                XmlSerialization.WriteElement(writer, ReturnType, XmlReturnTypeName);
            }
            base.WriteXmlContents(writer);
        }

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
    }

    ///// <summary>
    ///// A method definition object.
    ///// </summary>
    //[DebuggerTypeProxy(typeof(MethodDebugView))]
    //[Serializable]
    //public class MethodDefinition : NamedScope, IMethodDefinition {
    //    private List<IParameterDeclaration> _parameters;

    //    /// <summary>
    //    /// Creates a new method definition object
    //    /// </summary>
    //    public MethodDefinition()
    //        : base() {
    //        _parameters = new List<IParameterDeclaration>();
    //        Parameters = new ReadOnlyCollection<IParameterDeclaration>(_parameters);
    //    }

    //    /// <summary>
    //    /// Copy constructor
    //    /// </summary>
    //    /// <param name="otherDefinition">The scope to copy from</param>
    //    public MethodDefinition(MethodDefinition otherDefinition)
    //        : base(otherDefinition) {
    //        IsConstructor = otherDefinition.IsConstructor;
    //        IsDestructor = otherDefinition.IsDestructor;
    //        _parameters = new List<IParameterDeclaration>();
    //        Parameters = new ReadOnlyCollection<IParameterDeclaration>(_parameters);

    //        AddMethodParameters(otherDefinition.Parameters);
    //    }

    //    /// <summary>
    //    /// True if this is a constructor; false otherwise
    //    /// </summary>
    //    public bool IsConstructor { get; set; }

    //    /// <summary>
    //    /// True if this is a destructor; false otherwise
    //    /// </summary>
    //    public bool IsDestructor { get; set; }

    //    /// <summary>
    //    /// The parameters for this method.
    //    /// </summary>
    //    public ReadOnlyCollection<IParameterDeclaration> Parameters { get; private set; }

    //    /// <summary>
    //    /// The return type for this method
    //    /// </summary>
    //    public ITypeUse ReturnType { get; set; }

    //    /// <summary>
    //    /// The AddFrom function adds all of the declarations and children from
    //    /// <paramref name="otherScope"/>to this scope
    //    /// </summary>
    //    /// <param name="otherScope">The scope to add data from</param>
    //    /// <returns>the new scope</returns>
    //    public override IScope AddFrom(IScope otherScope) {
    //        var otherMethod = otherScope as MethodDefinition;
    //        if(otherMethod != null) {
    //            var parameters = Parameters.ToList();
    //            var otherParameters = otherMethod.Parameters.ToList();
    //            if(parameters.Count == otherParameters.Count) {
    //                for(int i = 0; i < parameters.Count; i++) {
    //                    var param = parameters[i];
    //                    var otherParam = otherParameters[i];
    //                    if(param.VariableType.Name == otherParam.VariableType.Name) {
    //                        foreach(var otherLoc in otherParam.Locations) {
    //                            param.Locations.Add(otherLoc);
    //                        }
    //                        if(string.IsNullOrWhiteSpace(param.Name)) {
    //                            param.Name = otherParam.Name;
    //                        }
    //                    } else {
    //                        Debug.WriteLine("MethodDefinition.AddFrom: conflicting parameter types at position {0}: {1} and {2}", i, param.VariableType.Name, otherParam.VariableType.Name);
    //                    }
    //                }
    //            } else {
    //                Debug.WriteLine("MethodDefinition.AddFrom: adding from method with different number of parameters!");
    //            }
    //        }
    //        return base.AddFrom(otherScope);
    //    }

    //    /// <summary>
    //    /// Adds a method parameter to this method
    //    /// </summary>
    //    /// <param name="parameter">The parameter to add</param>
    //    public void AddMethodParameter(IParameterDeclaration parameter) {
    //        parameter.ParentScope = this;
    //        _parameters.Add(parameter);
    //    }

    //    /// <summary>
    //    /// Adds an enumerable of method parameters to this method.
    //    /// </summary>
    //    /// <param name="parameters">The parameters to add</param>
    //    public void AddMethodParameters(IEnumerable<IParameterDeclaration> parameters) {
    //        foreach(var parameter in parameters) {
    //            AddMethodParameter(parameter);
    //        }
    //    }

    //    /// <summary>
    //    /// Returns true if both this and
    //    /// <paramref name="otherScope"/>have the same name.
    //    /// </summary>
    //    /// <param name="otherScope">The scope to test</param>
    //    /// <returns>true if they are the same method; false otherwise.</returns> TODO implement
    //    /// better method merging
    //    public virtual bool CanBeMergedInto(MethodDefinition otherScope) {
    //        if(otherScope != null && this.Parameters.Count == otherScope.Parameters.Count &&
    //           this.IsConstructor == otherScope.IsConstructor && this.IsDestructor == otherScope.IsDestructor) {
    //            var parameterComparisons = Enumerable.Zip(this.Parameters, otherScope.Parameters, (t, o) => t.VariableType.Name == o.VariableType.Name);
    //            return base.CanBeMergedInto(otherScope) && parameterComparisons.All(x => x);
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Casts
    //    /// <paramref name="otherScope"/>to a <see cref="MethodDefinition"/> and calls
    //    /// <see cref="CanBeMergedInto(MethodDefinition)"/>
    //    /// </summary>
    //    /// <param name="otherScope">The scope to test</param>
    //    /// <returns>true if <see cref="CanBeMergedInto(MethodDefinition)"/> evaluates to
    //    /// true.</returns>
    //    public override bool CanBeMergedInto(NamedScope otherScope) {
    //        return this.CanBeMergedInto(otherScope as MethodDefinition);
    //    }

    //    /// <summary>
    //    /// Checks to see if this method contains a call to
    //    /// <paramref name="callee"/>.
    //    /// </summary>
    //    /// <param name="callee">The method to look for calls to</param>
    //    /// <returns>True if this method contains any <see cref="GetCallsTo">calls to</see>
    //    /// <paramref name="callee"/></returns>.
    //    public bool ContainsCallTo(IMethodDefinition callee) {
    //        if(null == callee)
    //            throw new ArgumentNullException("callee");

    //        return GetCallsTo(callee).Any();
    //    }

    //    /// <summary>
    //    /// Gets all the method calls in this method to
    //    /// <paramref name="callee"/>. This method searches this method and all of its
    //    /// <see cref="IScope.ChildScopes"/>.
    //    /// </summary>
    //    /// <param name="callee">The method to find calls for.</param>
    //    /// <returns>All of the method calls to
    //    /// <paramref name="callee"/>in this method.</returns>
    //    public IEnumerable<IMethodCall> GetCallsTo(IMethodDefinition callee) {
    //        if(null == callee)
    //            throw new ArgumentNullException("callee");

    //        var callsToMethod = from scope in this.GetDescendantScopesAndSelf()
    //                            from call in scope.MethodCalls
    //                            where call.Matches(callee)
    //                            select call;
    //        return callsToMethod;
    //    }

    //    public IEnumerable<IMethodCall> GetCallsToSelf() {
    //        var globalScope = GetParentScopesAndSelf<INamespaceDefinition>().Where(n => n.IsGlobal).FirstOrDefault();
    //        if(null == globalScope)
    //            throw new ScopeDetachedException(this);

    //        return GetCallsToSelf(globalScope);
    //    }

    //    public IEnumerable<IMethodCall> GetCallsToSelf(IScope rootScope) {
    //        if(null == rootScope)
    //            throw new ArgumentNullException("scope");
    //        var calls = from scope in rootScope.GetDescendantScopes()
    //                    from call in scope.MethodCalls
    //                    where call.Name == this.Name
    //                    where call.Matches(this)
    //                    select call;
    //        return calls;
    //    }

    //    /// <summary>
    //    /// Merges this method definition with
    //    /// <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c>
    //    /// evaluates to true.
    //    /// </summary>
    //    /// <param name="otherScope">the scope to merge with</param>
    //    /// <returns>a new method definition from this and otherScope, or null if they couldn't be
    //    /// merged.</returns>
    //    public override INamedScope Merge(INamedScope otherScope) {
    //        MethodDefinition mergedScope = null;
    //        if(otherScope != null) {
    //            if(otherScope.CanBeMergedInto(this)) {
    //                mergedScope = new MethodDefinition(this);
    //                mergedScope.AddFrom(otherScope);
    //                if(mergedScope.Accessibility == AccessModifier.None) {
    //                    mergedScope.Accessibility = otherScope.Accessibility;
    //                }
    //            }
    //        }
    //        return mergedScope;
    //    }

    //    /// <summary>
    //    /// Removes any program elements defined in the given file. If the scope is defined entirely
    //    /// within the given file, then it removes itself from its parent.
    //    /// </summary>
    //    /// <param name="fileName">The file to remove.</param>
    //    /// <returns>A collection of any unresolved scopes that result from removing the file. The
    //    /// caller is responsible for re-resolving these as appropriate.</returns>
    //    public override Collection<IScope> RemoveFile(string fileName) {
    //        if(LocationDictionary.ContainsKey(fileName)) {
    //            if(LocationDictionary.Count == 1) {
    //                //this scope exists solely in the file to be deleted
    //                if(ParentScope != null) {
    //                    ParentScope.RemoveChild(this);
    //                    ParentScope = null;
    //                }
    //            } else {
    //                //Method is defined in more than one file, delete the stuff defined in the given file
    //                //Remove the file from the children
    //                var unresolvedChildScopes = new List<IScope>();
    //                foreach(var child in ChildScopes.ToList()) {
    //                    var result = child.RemoveFile(fileName);
    //                    if(result != null) {
    //                        unresolvedChildScopes.AddRange(result);
    //                    }
    //                }
    //                if(unresolvedChildScopes.Count > 0) {
    //                    foreach(var child in unresolvedChildScopes) {
    //                        AddChildScope(child);
    //                    }
    //                }
    //                //remove method calls
    //                var callsInFile = MethodCallCollection.Where(call => call.Location.SourceFileName == fileName).ToList();
    //                foreach(var call in callsInFile) {
    //                    MethodCallCollection.Remove(call);
    //                }
    //                //remove declared variables
    //                var declsInFile = DeclaredVariablesDictionary.Where(kvp => kvp.Value.Location.SourceFileName == fileName).ToList();
    //                foreach(var kvp in declsInFile) {
    //                    DeclaredVariablesDictionary.Remove(kvp.Key);
    //                }
    //                //remove parameter locations
    //                foreach(var param in Parameters) {
    //                    var locationsInFile = param.Locations.Where(loc => loc.SourceFileName == fileName).ToList();
    //                    foreach(var loc in locationsInFile) {
    //                        param.Locations.Remove(loc);
    //                    }
    //                    if(param.Locations.Count == 0) {
    //                        Debug.WriteLine("MethodDefinition.RemoveFile: Found a method parameter with fewer locations than the rest of the method!");
    //                    }
    //                }
    //                //remove parent scope candidates
    //                var candidatesInFile = ParentScopeCandidates.Where(psc => psc.Location.SourceFileName == fileName).ToList();
    //                foreach(var candidate in candidatesInFile) {
    //                    ParentScopeCandidates.Remove(candidate);
    //                }
    //                //update locations
    //                LocationDictionary.Remove(fileName);
    //                //TODO: update access modifiers based on which definitions/declarations we've deleted
    //            }
    //        }
    //        return null;
    //    }

    //    /// <summary>
    //    /// Returns a string representation of this object.
    //    /// </summary>
    //    public override string ToString() {
    //        var sig = new StringBuilder();
    //        if(IsConstructor) {
    //            sig.Append("Constructor: ");
    //        } else if(IsDestructor) {
    //            sig.Append("Destructor: ");
    //        } else {
    //            sig.Append("Method: ");
    //        }
    //        if(Accessibility != AccessModifier.None) {
    //            sig.Append(Accessibility.ToKeywordString() + " ");
    //        }
    //        if(ReturnType != null) {
    //            var retString = ReturnType.ToString();
    //            if(!string.IsNullOrWhiteSpace(retString)) {
    //                sig.Append(retString + " ");
    //            }
    //        } else if(!(IsConstructor || IsDestructor)) {
    //            sig.Append("void ");
    //        }
    //        if(!string.IsNullOrWhiteSpace(Name)) {
    //            sig.Append(Name);
    //        }
    //        string paramString = Parameters != null ? string.Join(", ", Parameters) : string.Empty;
    //        sig.AppendFormat("({0})", paramString);

    //        return sig.ToString();
    //    }

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