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
        
        /// <summary> XML Name for <see cref="IsConstructorInitializer"/></summary>
        public const string XmlIsConstructorInitializerName = "IsConstructorInitializer";

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

        /// <summary> True if this call appears in a constructor's initializer list. </summary>
        public bool IsConstructorInitializer { get; set; }

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

        /// <summary>
        /// Returns the child expressions, including the TypeArguments and Arguments.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return TypeArguments.Concat(Arguments).Concat(base.GetChildren());
        }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            if(TypeArguments.Any()) {
                return string.Format("{0}{1}<{2}>({3})", Prefix, Name, string.Join(",", TypeArguments), string.Join(",", Arguments));
            }
            return string.Format("{0}{1}({2})", Prefix, Name, string.Join(",", Arguments));
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
                List<TypeDefinition> typeDefinitions;
                if(this.Name == "this" ||
                   (this.Name == "base" && this.ProgrammingLanguage == Language.CSharp) ||
                   (this.Name == "super" && this.ProgrammingLanguage == Language.Java)) {
                    typeDefinitions = TypeDefinition.GetTypeForKeyword(this).ToList();
                } else {
                    var tempTypeUse = new TypeUse() {
                        Name = this.Name,
                        ParentStatement = this.ParentStatement,
                        Location = this.Location
                    };
                    typeDefinitions = tempTypeUse.ResolveType().ToList();
                }
                
                //Handle case of C++ constructor initialization lists. 
                //These will be marked as constructor calls. They can be used to initialize fields, though, in which case the call name will be the field name,
                //rather than a type name.
                if(!typeDefinitions.Any() && IsConstructorInitializer && ProgrammingLanguage == Language.CPlusPlus) {
                    var containingType = ParentStatement.GetAncestorsAndSelf<TypeDefinition>().FirstOrDefault();
                    if(containingType != null) {
                        //search this type and its parents for a field matching the name of the call
                        var matchingField = containingType.GetParentTypesAndSelf(true).SelectMany(t => t.GetNamedChildren<VariableDeclaration>(this.Name)).FirstOrDefault();
                        if(matchingField != null) {
                            typeDefinitions = matchingField.VariableType.ResolveType().ToList();
                        }
                    }
                }

                var matchingMethods = from typeDefinition in typeDefinitions
                                      from method in typeDefinition.GetNamedChildren<MethodDefinition>(typeDefinition.Name)
                                      where SignatureMatches(typeDefinition.Name, method)
                                      select method;
                return matchingMethods;
            }

            //If there's a calling expression, resolve and search under the results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                IEnumerable<INamedEntity> matches = Enumerable.Empty<INamedEntity>();
                foreach(var scope in callingScopes) {
                    var localMatches = scope.GetNamedChildren<MethodDefinition>(this.Name).Where(SignatureMatches).ToList();
                    var callingType = scope as TypeDefinition;
                    if(!localMatches.Any() && callingType != null) {
                        //also search under the base types of the calling scope
                        matches = matches.Concat(callingType.SearchParentTypes<MethodDefinition>(this.Name, SignatureMatches));
                    } else {
                        matches = matches.Concat(localMatches);
                    }
                }
                return matches;
            }
            
            //search enclosing scopes and base types for the method
            foreach(var scope in ParentStatement.GetAncestors()) {
                var matches = scope.GetNamedChildren<MethodDefinition>(this).Where(SignatureMatches).ToList();
                if(matches.Any()) {
                    return matches;
                }
                var typeDef = scope as TypeDefinition;
                if(typeDef != null) {
                    //search the base types
                    var baseTypeMatches = typeDef.SearchParentTypes<MethodDefinition>(this.Name, SignatureMatches).ToList();
                    if(baseTypeMatches.Any()) {
                        return baseTypeMatches;
                    }
                }
            }

            //we didn't find it locally, search under imported namespaces
            return (from import in GetImports()
                    from match in import.ImportedNamespace.GetDescendantsAndSelf<NameUse>().Last().FindMatches().OfType<NamedScope>()
                    from child in match.GetNamedChildren<MethodDefinition>(this.Name)
                    where SignatureMatches(child)
                    select child);

        }

        /// <summary>
        /// Finds all of the matching type definitions for the return type of this method definition
        /// </summary>
        /// <returns>An enumerable of the matching type definitions for this method</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            var matchingMethods = FindMatches().OfType<MethodDefinition>().ToList();
            if(matchingMethods.Any()) {
                foreach(var methodDefinition in matchingMethods) {
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
            } else {
                //no matches
                //handle case of calls to default (implicit) constructors
                if(IsConstructor && Arguments.Count == 0) {
                    var tempType = new TypeUse() {
                        Name = this.Name, 
                        Location = this.Location,
                        ParentStatement = this.ParentStatement, 
                        ProgrammingLanguage = this.ProgrammingLanguage
                    };
                    foreach(var result in tempType.ResolveType()) {
                        yield return result;
                    }
                }
            }


        }


        /// <summary>
        /// Tests if the signature of the provided method definition matches this method call
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <returns>True if this method call matches the signature of the provided method definition, False otherwise.</returns>
        public bool SignatureMatches(MethodDefinition definition) {
            return SignatureMatches(this.Name, definition);
        }

        /// <summary>
        /// Tests if the signature of the provided method definition matches this method call. The parameter <paramref name="callName"/>
        /// specifies the name to use for this method call. This is useful for cases where the call is a
        /// keyword, like "base", "this" or "super". The caller can first translate the keyword to the
        /// actual method name to match against.
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <param name="callName">The name to use for the method call.</param>
        /// <returns>True if this method call matches the signature of the provided method definition, False otherwise.</returns>
        public bool SignatureMatches(string callName, MethodDefinition definition) {
            if(null == definition) {
                return false;
            }

            //var argumentsMatchParameters = Enumerable.Zip(this.Arguments, definition.Parameters,
            //                                              (a,p) => ArgumentMatchesDefinition(a,p));
            var numberOfMethodParameters = definition.Parameters.Count;
            var numberOfMethodParametersWithDefault = definition.Parameters.Count(p => p.Initializer != null);

            return this.IsConstructor == definition.IsConstructor &&
                   this.IsDestructor == definition.IsDestructor &&
                   callName == definition.Name &&
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
            attribute = reader.GetAttribute(XmlIsConstructorInitializerName);
            if(!String.IsNullOrEmpty(attribute)) {
                IsConstructorInitializer = XmlConvert.ToBoolean(attribute);
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
            if(IsConstructorInitializer) {
                writer.WriteAttributeString(XmlIsConstructorInitializerName, XmlConvert.ToString(IsConstructorInitializer));
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