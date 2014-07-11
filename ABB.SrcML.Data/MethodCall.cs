/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a method call
    /// </summary>
    public class MethodCall : NameUse {
        private List<Expression> argumentList;
        private List<TypeUse> typeArgumentList;

        /// <summary> The XML name for MethodCall </summary>
        public new const string XmlName = "call";
        
        /// <summary> XML Name for <see cref="Arguments" /> </summary>
        public const string XmlArgumentsName = "Arguments";

        /// <summary> XML name for <see cref="TypeArguments"/> </summary>
        public const string XmlTypeArgumentsName = "TypeArguments";
        
        /// <summary> XML Name for <see cref="IsConstructor" /> </summary>
        public const string XmlIsConstructorName = "IsConstructor";
        
        /// <summary> XML Name for <see cref="IsDestructor" /> </summary>
        public const string XmlIsDestructorName = "IsDestructor";

        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            argumentList = new List<Expression>();
            Arguments = new ReadOnlyCollection<Expression>(argumentList);
            typeArgumentList = new List<TypeUse>();
            TypeArguments = new ReadOnlyCollection<TypeUse>(typeArgumentList);
            IsConstructor = false;
            IsDestructor = false;
        }

        /// <summary>
        /// The arguments to this call.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments { get; private set;}

        /// <summary>
        /// The type arguments to this method call. 
        /// For example, in "Foo&lt;int&gt;(17)", int is a type argument.
        /// </summary>
        public ReadOnlyCollection<TypeUse> TypeArguments { get; private set; }

        /// <summary> True if this is a call to a constructor </summary>
        public bool IsConstructor { get; set; }

        /// <summary> True if this is a call to a destructor </summary>
        public bool IsDestructor { get; set; }

        /// <summary> The statement containing this expression. </summary>
        public override Statement ParentStatement {
            get { return base.ParentStatement; }
            set {
                base.ParentStatement = value;
                foreach(var arg in Arguments) { arg.ParentStatement = value; }
                foreach(var typeArg in TypeArguments) { typeArg.ParentStatement = value; }
            }
        }

        /// <summary>
        /// Adds the given argument to the Arguments collection.
        /// </summary>
        /// <param name="arg">The argument to add.</param>
        public void AddArgument(Expression arg) {
            if(arg == null) { throw new ArgumentNullException("arg"); }
            arg.ParentExpression = this;
            arg.ParentStatement = this.ParentStatement;
            argumentList.Add(arg);
        }

        /// <summary>
        /// Adds the given arguments to the Arguments collection.
        /// </summary>
        /// <param name="args">The arguments to add.</param>
        public void AddArguments(IEnumerable<Expression> args) {
            foreach(var arg in args) {
                AddArgument(arg);
            }
        }

        /// <summary>
        /// Adds the given type argument to the TypeArguments collection.
        /// </summary>
        /// <param name="arg">The type argument to add.</param>
        public void AddTypeArgument(TypeUse arg) {
            if(arg == null) { throw new ArgumentNullException("arg"); }
            arg.ParentExpression = this;
            arg.ParentStatement = this.ParentStatement;
            typeArgumentList.Add(arg);
        }

        /// <summary>
        /// Adds the given type arguments to the TypeArguments collection.
        /// </summary>
        /// <param name="args">The type arguments to add.</param>
        public void AddTypeArguments(IEnumerable<TypeUse> args) {
            foreach(var arg in args) {
                AddTypeArgument(arg);
            }
        }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}{1}({2})", Prefix, Name, string.Join(",",argumentList));
            return sb.ToString();
        }

        /// <summary>
        /// Gets the first type definition that matches the return type for this method
        /// </summary>
        /// <returns>The first matching type definition</returns>
        public TypeDefinition FindFirstMatchingType() {
            return ResolveType().FirstOrDefault();
        }

        /// <summary>
        /// Finds matching <see cref="MethodDefinition">method definitions</see> for this method call.
        /// This method searches for matches in the ancestor scopes of the call. Because method calls can also be
        /// to constructors and destructors, this will also search for matching types and then
        /// constructors within those types
        /// </summary>
        /// <returns>An enumerable of method definitions that match this method call</returns>
        public override IEnumerable<INamedEntity> FindMatches() {
            if(ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }            

            if(IsConstructor || IsDestructor) {
                IEnumerable<TypeDefinition> typeDefinitions;
                if(this.Name == "this" ||
                   (this.Name == "base" && this.ProgrammingLanguage == Language.CSharp) ||
                   (this.Name == "super" && this.ProgrammingLanguage == Language.Java)) {
                    typeDefinitions = TypeDefinition.GetTypeForKeyword(this);
                } else {
                    //var tempTypeUse = new TypeUse() {
                    //    Name = this.Name,
                    //    ParentScope = this.ParentScope,
                    //};
                    //tempTypeUse.AddAliases(this.Aliases);
                    //typeDefinitions = tempTypeUse.FindMatches();
                    throw new NotImplementedException();
                }
                
                //TODO: handle case of C++ constructor initialization lists. 
                //These will be marked as constructor calls. They can be used to initialize fields, though, in which case the call name will be the field name,
                //rather than a type name.

                //matchingMethods = from typeDefinition in typeDefinitions
                //                  from method in typeDefinition.GetChildScopesWithId<MethodDefinition>(typeDefinition.Name)
                //                  where Matches(method)
                //                  select method;
                throw new NotImplementedException();
            }

            //If there's a calling expression, resolve and search under the results
            var siblings = GetSiblingsBeforeSelf().ToList();
            var priorOp = siblings.LastOrDefault() as OperatorUse;
            if(priorOp != null && NameInclusionOperators.Contains(priorOp.Text)) {
                var callingExp = siblings[siblings.Count - 2]; //second-to-last sibling
                IEnumerable<INamedEntity> parents;
                if(callingExp is NameUse) {
                    parents = ((NameUse)callingExp).FindMatches();
                    //TODO: fix this, we actually need to get the type if this resolves to a method or property. Fix in other resolution methods too.
                } else {
                    parents = callingExp.ResolveType();
                }
                return parents.SelectMany(p => p.GetNamedChildren<MethodDefinition>(this.Name)).Where(Matches);
            }

            

            //TODO: look for matches starting from the global scope?
            //var matches = base.FindMatches();

            //if the method call occurs within a class, search that class (and its parents) for a matching method
            var matchingTypeMethods = from containingType in ParentStatement.GetAncestorsAndSelf<TypeDefinition>()
                                      from typeDefinition in containingType.GetParentTypesAndSelf(true)
                                      from method in typeDefinition.GetNamedChildren<MethodDefinition>(this.Name)
                                      where Matches(method)
                                      select method;

            //matchingMethods = matches.Concat(matchingTypeMethods);
            return matchingTypeMethods;

        }

        /// <summary>
        /// Finds all of the matching type definitions for the return type of this method definition
        /// </summary>
        /// <returns>An enumerable of the matching type definitions for this method</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            foreach(var methodDefinition in FindMatches().OfType<MethodDefinition>()) {
                var matchingTypes = Enumerable.Empty<TypeDefinition>();

                if(methodDefinition.ReturnType != null) {
                    matchingTypes = methodDefinition.ReturnType.ResolveType();
                } else if(methodDefinition.IsConstructor) {
                    var methodName = methodDefinition.Name; //define local var because of Resharper warning about accessing foreach var in closure
                    matchingTypes = methodDefinition.GetAncestors<TypeDefinition>().Where(td => td.Name == methodName);
                }
                foreach(var result in matchingTypes) {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Returns the possible names for this method call. 
        /// This is used to translate keywords like 'this' or 'base' to actual method names.
        /// </summary>
        public IEnumerable<string> GetPossibleNames() {
            if(this.Name == "this") {
                foreach(var containingType in GetAncestors<TypeDefinition>().Take(1)) {
                    yield return containingType.Name;
                }
            } else if(this.Name == "base" && ProgrammingLanguage == Language.CSharp) {
                var typeDefinitions = from containingType in GetAncestors<TypeDefinition>()
                                      from parentTypeReference in containingType.ParentTypeNames
                                      from parentType in parentTypeReference.ResolveType()
                                      select parentType;
                foreach(var baseType in typeDefinitions) {
                    yield return baseType.Name;
                }
            } else {
                yield return this.Name;
            }
        }

        /// <summary>
        /// Tests if the provided method definition matches this method call
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <returns>True if this method call matches the provided method definition</returns>
        public bool Matches(MethodDefinition definition) {
            if(null == definition) {
                return false;
            }

            //var argumentsMatchParameters = Enumerable.Zip(this.Arguments, definition.Parameters,
            //                                              (a,p) => ArgumentMatchesDefinition(a,p));
            var numberOfMethodParameters = definition.Parameters.Count;
            var numberOfMethodParametersWithDefault = definition.Parameters.Count(p => p.Initializer != null);

            return this.IsConstructor == definition.IsConstructor &&
                   this.IsDestructor == definition.IsDestructor &&
                   GetPossibleNames().Any(n => n == definition.Name) &&
                   this.Arguments.Count >= numberOfMethodParameters - numberOfMethodParametersWithDefault &&
                   this.Arguments.Count <= numberOfMethodParameters;// &&
                                                                       //argumentsMatchParameters.All(a => a);
        }

        /// <summary>
        /// Instance method for getting <see cref="MethodCall.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for MethodCall</returns>
        public override string GetXmlName() { return MethodCall.XmlName; }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            string attribute = reader.GetAttribute(XmlIsConstructorName);
            if(!String.IsNullOrEmpty(attribute)) {
                IsConstructor = XmlConvert.ToBoolean(attribute);
            }
            attribute = reader.GetAttribute(XmlIsDestructorName);
            if(!String.IsNullOrEmpty(attribute)) {
                IsDestructor = XmlConvert.ToBoolean(attribute);
            }
            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlArgumentsName == reader.Name) {
                AddArguments(XmlSerialization.ReadChildExpressions(reader));
            } else if(XmlTypeArgumentsName == reader.Name) {
                AddTypeArguments(XmlSerialization.ReadChildExpressions(reader).Cast<TypeUse>());
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

            base.WriteXmlAttributes(writer);
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            base.WriteXmlContents(writer);
            if(Arguments.Count > 0) {
                XmlSerialization.WriteCollection<Expression>(writer, XmlArgumentsName, Arguments);
            }
            if(TypeArguments.Count > 0) {
                XmlSerialization.WriteCollection<TypeUse>(writer, XmlTypeArgumentsName, TypeArguments);
            }
        }

        ///// <summary>
        ///// Computes the intersection of the matching types for
        ///// <paramref name="argument"/>and
        ///// <paramref name="parameter"/>. It returns true if the intersection has any elements in
        ///// it.
        ///// </summary>
        ///// <param name="argument">an argument from see cref="Arguments"/></param>
        ///// <param name="parameter">a parameter from see
        ///// cref="MethodDefinition.Parameters"/></param>
        ///// <returns>true if the argument and the parameter have a matching type in common; false
        ///// otherwise</returns>
        //private bool ArgumentMatchesDefinition(IResolvesToType argument, VariableDeclaration parameter) {
        //    var possibleArgumentTypes = argument.FindMatchingTypes();
        //    var possibleParameterTypes = parameter.VariableType.ResolveType();

        //    return possibleArgumentTypes.Intersect(possibleParameterTypes).Any();
        //}
    }
}