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
    [Serializable]
    public class MethodCall : NameUse {
        private List<Expression> argumentList;

        /// <summary>
        /// The XML name for MethodCall
        /// </summary>
        public new const string XmlName = "call";

        /// <summary>
        /// XML Name for <see cref="Arguments" />
        /// </summary>
        public const string XmlArgumentsName = "Arguments";

        /// <summary>
        /// XML Name for <see cref="IsConstructor" />
        /// </summary>
        public const string XmlIsConstructorName = "IsConstructor";

        /// <summary>
        /// XML Name for <see cref="IsDestructor" />
        /// </summary>
        public const string XmlIsDestructorName = "IsDestructor";

        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            argumentList = new List<Expression>();
            Arguments = new ReadOnlyCollection<Expression>(argumentList);
            IsConstructor = false;
            IsDestructor = false;
        }

        /// <summary>
        /// The arguments to this call
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments { get; private set;}

        ///// <summary>
        ///// The calling object for a use is used when you have <c>a.Foo()</c> -- this method call
        ///// would refer to <c>Foo()</c> and the calling object would be <c>a</c>.
        ///// </summary>
        //public Expression CallingObject { get; set; }

        /// <summary>
        /// True if this is a call to a constructor
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a call to a destructor
        /// </summary>
        public bool IsDestructor { get; set; }

        /// <summary>
        /// Adds the given argument to the Arguments collection.
        /// </summary>
        /// <param name="arg">The argument to add.</param>
        public void AddArgument(Expression arg) {
            if(arg == null) { throw new ArgumentNullException("arg"); }
            arg.ParentExpression = this;
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

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}{1}({2})", Prefix, Name, string.Join(",",argumentList));
            return sb.ToString();
        }

        ///// <summary>
        ///// The parent scope for this method call. When you update the parent scope, the object also
        ///// updates the parent scope of <see cref="CallingObject"/> and all of the
        ///// <see cref="Arguments"/>
        ///// </summary>
        //public override Scope ParentScope {
        //    get {
        //        return base.ParentScope;
        //    }
        //    set {
        //        base.ParentScope = value;
        //        if(this.CallingObject != null) {
        //            this.CallingObject.ParentScope = this.ParentScope;
        //        }
        //        foreach(var argument in this.Arguments) {
        //            argument.ParentScope = this.ParentScope;
        //        }
        //    }
        //}

        /// <summary>
        /// Gets the first type definition that matches the return type for this method
        /// </summary>
        /// <returns>The first matching type definition</returns>
        public TypeDefinition FindFirstMatchingType() {
            return FindMatchingTypes().FirstOrDefault();
        }

        /// <summary>
        /// Finds matching <see cref="IMethodDefinition">method definitions</see> from the
        /// <see cref="IScope.GetParentScopes()"/> of this usage. Because method calls can also be
        /// to constructors and destructors, this will also search for matching types and then
        /// constructors within those types
        /// </summary>
        /// <returns>An enumerable of method definitions that match this method call</returns>
        public IEnumerable<MethodDefinition> FindMatches() {
            //TODO: review this method and update it for changes in TypeUse structure
            throw new NotImplementedException();
            //IEnumerable<MethodDefinition> matchingMethods = Enumerable.Empty<MethodDefinition>();

            //if(IsConstructor || IsDestructor) {
            //    IEnumerable<TypeDefinition> typeDefinitions;
            //    if(this.Name == "this" || (this.Name == "base" && this.ProgrammingLanguage == Language.CSharp)) {
            //        typeDefinitions = TypeDefinition.GetTypeForKeyword(this);
            //    } else {
            //        TypeUse tempTypeUse = new TypeUse() {
            //            Name = this.Name,
            //            ParentScope = this.ParentScope,
            //        };
            //        tempTypeUse.AddAliases(this.Aliases);
            //        typeDefinitions = tempTypeUse.FindMatches();
            //    }

            //    matchingMethods = from typeDefinition in typeDefinitions
            //                      from method in typeDefinition.GetChildScopesWithId<MethodDefinition>(typeDefinition.Name)
            //                      where Matches(method)
            //                      select method;
            //} else if(CallingObject != null) {
            //    matchingMethods = from matchingType in CallingObject.FindMatchingTypes()
            //                      from typeDefinition in matchingType.GetParentTypesAndSelf()
            //                      from method in typeDefinition.GetChildScopesWithId<IMethodDefinition>(this.Name)
            //                      where Matches(method)
            //                      select method;
            //} else {
            //    var matches = base.FindMatches();
            //    var matchingTypeMethods = from containingType in ParentScope.GetParentScopesAndSelf<TypeDefinition>()
            //                              from typeDefinition in containingType.GetParentTypes()
            //                              from method in typeDefinition.GetChildScopesWithId<IMethodDefinition>(this.Name)
            //                              where Matches(method)
            //                              select method;
            //    matchingMethods = matches.Concat(matchingTypeMethods);
            //}
            //foreach(var method in matchingMethods) {
            //    yield return method;
            //}
        }

        /// <summary>
        /// Finds all of the matching type definitions for the return type of this method definition
        /// </summary>
        /// <returns>An enumerable of the matching type definitions for this method</returns>
        public IEnumerable<TypeDefinition> FindMatchingTypes() {
            //TODO: review this method and update it for changes in TypeUse structure
            throw new NotImplementedException();
            //foreach(var methodDefinition in FindMatches()) {
            //    IEnumerable<TypeDefinition> matchingTypes = Enumerable.Empty<TypeDefinition>();

            //    if(methodDefinition.ReturnType != null) {
            //        matchingTypes = methodDefinition.ReturnType.FindMatches();
            //    } else if(methodDefinition.IsConstructor) {
            //        matchingTypes = from type in methodDefinition.GetParentScopes<TypeDefinition>()
            //                        where type.Name == methodDefinition.Name
            //                        select type;
            //    }
            //    foreach(var result in matchingTypes) {
            //        yield return result;
            //    }
            //}
        }

        //public IEnumerable<string> GetPossibleNames() {
        //    if(this.Name == "this") {
        //        foreach(var containingType in ParentScopes.OfType<TypeDefinition>().Take(1)) {
        //            yield return containingType.Name;
        //        }
        //    } else if(this.Name == "base" && ProgrammingLanguage == Language.CSharp) {
        //        var typeDefinitions = from containingType in ParentScopes.OfType<TypeDefinition>()
        //                              from parentTypeReference in containingType.ParentTypes
        //                              from parentType in parentTypeReference.FindMatchingTypes()
        //                              select parentType;
        //        foreach(var baseType in typeDefinitions) {
        //            yield return baseType.Name;
        //        }
        //    } else {
        //        yield return this.Name;
        //    }
        //}

        /// <summary>
        /// Instance method for getting <see cref="MethodCall.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for MethodCall</returns>
        public override string GetXmlName() { return MethodCall.XmlName; }

        /// <summary>
        /// Tests if the provided method definition matches this method call
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <returns>True if this method call matches the provided method definition</returns>
        public bool Matches(MethodDefinition definition) {
            //TODO: review this method and update it for changes in TypeUse structure
            throw new NotImplementedException();
            //if(null == definition)
            //    return false;

            ////var argumentsMatchParameters = Enumerable.Zip(this.Arguments, definition.Parameters,
            ////                                              (a,p) => ArgumentMatchesDefinition(a,p));
            //var numberOfMethodParameters = definition.Parameters.Count;
            //var numberOfMethodParametersWithDefault = definition.Parameters.Where(p => p.HasDefaultValue).Count();

            //return this.IsConstructor == definition.IsConstructor &&
            //       this.IsDestructor == definition.IsDestructor &&
            //       GetPossibleNames().Any(n => n == definition.Name) &&
            //       this.Arguments.Count >= numberOfMethodParameters - numberOfMethodParametersWithDefault &&
            //       this.Arguments.Count <= definition.Parameters.Count;// &&
            //                                                           //argumentsMatchParameters.All(a => a);
        }

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

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlArgumentsName == reader.Name) {
                AddArguments(XmlSerialization.ReadChildExpressions(reader));
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlAttributes(XmlWriter writer) {
            if(IsConstructor) {
                writer.WriteAttributeString(XmlIsConstructorName, XmlConvert.ToString(IsConstructor));
            }

            if(IsDestructor) {
                writer.WriteAttributeString(XmlIsDestructorName, XmlConvert.ToString(IsDestructor));
            }

            base.WriteXmlAttributes(writer);
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            base.WriteXmlContents(writer);
            if(Arguments.Count > 0) {
                XmlSerialization.WriteCollection<Expression>(writer, XmlArgumentsName, Arguments);
            }
        }

        /// <summary>
        /// Computes the intersection of the matching types for
        /// <paramref name="argument"/>and
        /// <paramref name="parameter"/>. It returns true if the intersection has any elements in
        /// it.
        /// </summary>
        /// <param name="argument">an argument from see cref="Arguments"/></param>
        /// <param name="parameter">a parameter from see
        /// cref="MethodDefinition.Parameters"/></param>
        /// <returns>true if the argument and the parameter have a matching type in common; false
        /// otherwise</returns>
        private bool ArgumentMatchesDefinition(IResolvesToType argument, VariableDeclaration parameter) {
            var possibleArgumentTypes = argument.FindMatchingTypes();
            var possibleParameterTypes = parameter.VariableType.FindMatchingTypes();

            return possibleArgumentTypes.Intersect(possibleParameterTypes).Any();
        }
    }
}