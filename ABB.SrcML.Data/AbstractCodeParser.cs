/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, and documentation
 *****************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// <para>AbstractCodeParser is used to parse SrcML files and extract useful info from the
    /// elements. Implementations of this class provide language-specific functions to extract
    /// useful data from the class.</para> <para>The entry point for this class is the
    /// <see cref="ParseFileUnit(XElement)"/> method.</para>
    /// </summary>
    public abstract class AbstractCodeParser {
        private TextWriter _synchronizedErrorLog;

        /// <summary>
        /// Creates a new abstract code parser object. Should only be called by child classes.
        /// </summary>
        protected AbstractCodeParser() {
            ContainerElementNames = new HashSet<XName>(new XName[] {
                SRC.Block, SRC.Catch, SRC.Class, SRC.Constructor, SRC.ConstructorDeclaration, SRC.Destructor,  SRC.DestructorDeclaration, SRC.Do,
                SRC.Else, SRC.Enum, SRC.Extern, SRC.For, SRC.Function, SRC.FunctionDeclaration, SRC.If, SRC.Namespace, SRC.Struct, SRC.Switch,
                SRC.Template, SRC.Then, SRC.Try, SRC.Typedef, SRC.Union, SRC.Unit, SRC.While,
            });
            MethodElementNames = new HashSet<XName>(new XName[] { SRC.Function, SRC.Constructor, SRC.Destructor,
                                                                  SRC.FunctionDeclaration, SRC.ConstructorDeclaration, SRC.DestructorDeclaration });
            NamespaceElementNames = new HashSet<XName>(new XName[] { SRC.Namespace });
            VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement });
            ContainerReferenceElementNames = new HashSet<XName>(new XName[] { SRC.ClassDeclaration, SRC.StructDeclaration, SRC.UnionDeclaration,
                                                                                SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration });
            UnknownLog = null;
        }

        /// <summary>
        /// Returns the XName that represents an import statement
        /// </summary>
        public XName AliasElementName { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent containers for this language
        /// </summary>
        public HashSet<XName> ContainerElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent reference elements (such as function_decl and
        /// class_decl)
        /// </summary>
        public HashSet<XName> ContainerReferenceElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> MethodElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent namespaces for this language
        /// </summary>
        public HashSet<XName> NamespaceElementNames { get; protected set; }

        /// <summary>
        /// Returns the Language that this parser supports
        /// </summary>
        public abstract Language ParserLanguage { get; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent variable declarations for this language
        /// </summary>
        public HashSet<XName> VariableDeclarationElementNames { get; protected set; }

        /// <summary>
        /// writer to log unknown elements to. If null no logging is done
        /// </summary>
        public TextWriter UnknownLog {
            get { return _synchronizedErrorLog; }
            set { _synchronizedErrorLog = (null == value ? null : TextWriter.Synchronized(value)); }
        }

        /// <summary>
        /// Creates a resolvable use from an expression
        /// </summary>
        /// <param name="element">The element to parse</param>
        /// <param name="context">The parser context</param>
        /// <returns>A resolvable use object</returns>
        // TODO make this fit in with the rest of the parse methods (rename to parse)
        protected virtual IResolvesToType CreateResolvableUse(XElement element, ParserContext context) {
            throw new NotImplementedException();

            //XElement expression = null;
            //if(element.Name == SRC.Expression) {
            //    expression = element;
            //} else if(element.Name == SRC.Argument || element.Name == SRC.ExpressionStatement) {
            //    expression = element.Elements(SRC.Expression).FirstOrDefault();
            //}

            //var use = new VariableUse() {
            //    Location = context.CreateLocation(element, true),
            //    ParentScope = context.CurrentStatement,
            //    ProgrammingLanguage = ParserLanguage,
            //};

            //if(expression != null) {
            //    if(expression.Elements(SRC.Name).Count() == 1) {
            //        use.Name = expression.Element(SRC.Name).Value;
            //    }
            //}

            //return use;
        }

        /// <summary>
        /// Creates a variable use from the given element. Must be a
        /// <see cref="ABB.SrcML.SRC.Expression"/>, <see cref="ABB.SrcML.SRC.Name"/>, or
        /// <see cref="ABB.SrcML.SRC.ExpressionStatement"/>
        /// </summary>
        /// <param name="element">The element to parse</param>
        /// <param name="context">The parser context</param>
        /// <returns>A variable use object</returns>
        // TODO make this fit in with the rest of the parse methods
        protected virtual VariableUse CreateVariableUse(XElement element, ParserContext context) {
            throw new NotImplementedException();

            //XElement nameElement;
            //if(element.Name == SRC.Name) {
            //    nameElement = element;
            //} else if(element.Name == SRC.Expression) {
            //    nameElement = element.Element(SRC.Name);
            //} else if(element.Name == SRC.ExpressionStatement || element.Name == SRC.Argument) {
            //    nameElement = element.Element(SRC.Expression).Element(SRC.Name);
            //} else {
            //    throw new ArgumentException("element should be an expression, expression statement, argument, or name", "element");
            //}

            //var lastNameElement = NameHelper.GetLastNameElement(nameElement);

            //IVariableUse variableUse = new VariableUse() {
            //    Location = context.CreateLocation(lastNameElement, true),
            //    Name = lastNameElement.Value,
            //    ParentScope = context.CurrentStatement,
            //    ProgrammingLanguage = ParserLanguage,
            //};
            //return variableUse;
        }

        /// <summary>
        /// Gets the alias elements for this file. This only returns the aliases at the root of the
        /// file
        /// </summary>
        /// <param name="fileUnit">The file unit to get the aliases from</param>
        /// <returns>The alias elements</returns>
        // TODO handle alias elements in other parts of the file
        protected virtual IEnumerable<XElement> GetAliasElementsForFile(XElement fileUnit) {
            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(SRC.Unit != fileUnit.Name)
                throw new ArgumentException("must be a unit element", "fileUnit");

            return fileUnit.Elements(AliasElementName);
        }

        /// <summary>
        /// Gets all of the parameters for this method. It finds the variable declarations in
        /// parameter list.
        /// </summary>
        /// <param name="methodElement">The method container</param>
        /// <returns>An enumerable of all the param XElements.</returns>
        protected virtual IEnumerable<XElement> GetParametersFromMethodElement(XElement methodElement) {
            var paramList = methodElement.Element(SRC.ParameterList);
            if(paramList != null) {
                return paramList.Elements(SRC.Parameter);
            }
            //property getters/setters don't have a parameter list
            return Enumerable.Empty<XElement>();
        }

        /// <summary>
        /// Gets the type use elements from a <see cref="TypeElementNames">type definition
        /// element</see>
        /// </summary>
        /// <param name="typeElement">The type element. Must belong to see
        /// cref="TypeElementNames"/></param>
        /// <returns>An enumerable of type uses that represent parent types</returns>
        protected abstract IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement);

        protected void LogUnknown(XElement element, ParserContext context) {
            if(null != UnknownLog) {
                UnknownLog.WriteLine("{0}({1},{2}) Unexpected {3}", context.FileName, element.GetSrcLineNumber(), element.GetSrcLinePosition(), element.Name);
            }
        }

        ///// <summary>
        ///// Creates a <see cref="IScope"/> object for
        ///// <paramref name="element"/>and pushes it onto
        ///// <paramref name="context"/></summary>
        ///// <param name="element">The element to parse</param>
        ///// <param name="context">the context to place the resulting scope on</param>
        //public virtual void ParseContainerElement(XElement element, ParserContext context) {
        //    var scope = new Scope();
        //    context.Push(scope);
        //}


        #region Parse statement elements
        /// <summary>
        /// Creates a <see cref="Statement"/> object from the given <paramref name="element"/>.
        /// This method simply dispatches to the appropriate element parsing method based on the name of the element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A Statement corresponding to <paramref name="element"/>. 
        /// If an unknown element type is passed in, this method will return null if LogUnknownElements is true, or throw an exception if LogUnknownElements is false.</returns>
        protected virtual Statement ParseStatement(XElement element, ParserContext context) {
            try {
                Statement stmt = null;
                if(TypeElementNames.Contains(element.Name)) {
                    stmt = ParseTypeElement(element, context);
                } else if(NamespaceElementNames.Contains(element.Name)) {
                    stmt = ParseNamespaceElement(element, context);
                } else if(element.Name == AliasElementName) {
                    stmt = ParseAliasElement(element, context);
                } else if(MethodElementNames.Contains(element.Name)) {
                    stmt = ParseMethodElement(element, context);
                } else if(element.Name == SRC.If) {
                    stmt = ParseIfElement(element, context);
                } else if(element.Name == SRC.While) {
                    stmt = ParseWhileElement(element, context);
                } else if(element.Name == SRC.Do) {
                    stmt = ParseDoElement(element, context);
                } else if(element.Name == SRC.For) {
                    stmt = ParseForElement(element, context);
                } else if(element.Name == SRC.Foreach) {
                    stmt = ParseForeachElement(element, context);
                } else if(element.Name == SRC.Switch) {
                    stmt = ParseSwitchElement(element, context);
                } else if(element.Name == SRC.Case || element.Name == SRC.Default) {
                    stmt = ParseCaseElement(element, context);
                } else if(element.Name == SRC.Continue) {
                    stmt = ParseContinueElement(element, context);
                } else if(element.Name == SRC.Break) {
                    stmt = ParseBreakElement(element, context);
                } else if(element.Name == SRC.Return) {
                    stmt = ParseReturnElement(element, context);
                } else if(element.Name == SRC.Goto) {
                    stmt = ParseGotoElement(element, context);
                } else if(element.Name == SRC.Label) {
                    stmt = ParseLabelElement(element, context);
                } else if(element.Name == SRC.Throw) {
                    stmt = ParseThrowElement(element, context);
                } else if(element.Name == SRC.Try) {
                    stmt = ParseTryElement(element, context);
                } else if(element.Name == SRC.ExpressionStatement) {
                    stmt = ParseExpressionStatementElement(element, context);
                } else if(element.Name == SRC.DeclarationStatement) {
                    stmt = ParseDeclarationStatementElement(element, context);
                } else if(element.Name == SRC.Block) {
                    stmt = ParseBlockElement(element, context);
                } else if(element.Name == SRC.Extern) {
                    stmt = ParseExternElement(element, context);
                } else if(element.Name == SRC.EmptyStatement) {
                    stmt = ParseEmptyStatementElement(element, context);
                } else if(element.Name == SRC.Comment) {
                    // do nothing. we are ignoring comments
                } else if(element.Name == SRC.Package) {
                    //do nothing. This is already handled in JavaCodeParser.ParseUnitElement()
                } else {
                    LogUnknown(element, context);
                }
                //TODO: parse using blocks
                //TODO: handle other CPP elements
                

                return stmt;
            } catch(ParseException) {
                throw;
            } catch(Exception e) {
                int lineNumber = element.GetSrcLineNumber();
                int columnNumber = element.GetSrcLinePosition();
                throw new ParseException(context.FileName, lineNumber, columnNumber, this, e.Message, e);
            }
        }

        /// <summary>
        /// Parses a file unit and returns a <see cref="NamespaceDefinition.IsGlobal">global</see>
        /// <see cref="NamespaceDefinition">namespace definition</see> object
        /// </summary>
        /// <param name="unitElement">The file unit to parse. Must be a SRC.Unit element.</param>
        /// <returns>A global namespace definition for <paramref name="unitElement"/>.</returns>
        public virtual NamespaceDefinition ParseFileUnit(XElement unitElement) {
            if(null == unitElement)
                throw new ArgumentNullException("unitElement");
            if(SRC.Unit != unitElement.Name)
                throw new ArgumentException("should be a SRC.Unit", "unitElement");

            var globalScope = ParseUnitElement(unitElement, new ParserContext());
            return globalScope;
        }

        /// <summary>
        /// Creates a <see cref="MethodDefinition"/> object for <paramref name="methodElement"/>.
        /// </summary>
        /// <param name="methodElement">The element to parse</param>
        /// <param name="context">The context to use</param>
        protected virtual MethodDefinition ParseMethodElement(XElement methodElement, ParserContext context) {
            if(null == methodElement)
                throw new ArgumentNullException("methodElement");
            if(!MethodElementNames.Contains(methodElement.Name))
                throw new ArgumentException("must be a method element", "methodElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var methodDefinition = new MethodDefinition() {
                Name = GetNameForMethod(methodElement),
                IsConstructor = (methodElement.Name == SRC.Constructor || methodElement.Name == SRC.ConstructorDeclaration),
                IsDestructor = (methodElement.Name == SRC.Destructor || methodElement.Name == SRC.DestructorDeclaration),
                Accessibility = GetAccessModifierForMethod(methodElement),
                ProgrammingLanguage = ParserLanguage
            };
            methodDefinition.AddLocation(context.CreateLocation(methodElement, ContainerIsReference(methodElement)));

            // get the return type for the method
            var returnTypeElement = methodElement.Element(SRC.Type);
            if(returnTypeElement != null) {
                // construct the return type. however, if the Name of the return type is "void",
                // don't use it because it means the return type is void
                var returnTypeUse = ParseTypeUseElement(returnTypeElement, context);
                if(returnTypeUse != null && returnTypeUse.Name != "void") {
                    methodDefinition.AddReturnType(ParseTypeUseElement(returnTypeElement, context));
                }
            }
            //Add the method's parameters
            var parameters = from paramElement in GetParametersFromMethodElement(methodElement)
                             select ParseParameterElement(paramElement, context);
            methodDefinition.AddMethodParameters(parameters.ToList());
            //methodDefinition.AddMethodParameters(parameters);
            
            //Add the method body statements as children
            var methodBlock = methodElement.Element(SRC.Block);
            if(methodBlock != null) {
                foreach(var child in methodBlock.Elements()) {
                    methodDefinition.AddChildStatement(ParseStatement(child, context));
                }
            }

            return methodDefinition;
        }

        /// <summary>
        /// Generates a parameter declaration for the given parameter element
        /// </summary>
        /// <param name="paramElement">A <see cref="ABB.SrcML.SRC.Parameter"/> XElement</param>
        /// <param name="context">the parser context</param>
        /// <returns>A parameter declaration object</returns>
        protected virtual VariableDeclaration ParseParameterElement(XElement paramElement, ParserContext context) {
            if(paramElement == null)
                throw new ArgumentNullException("paramElement");
            if(paramElement.Name != SRC.Parameter)
                throw new ArgumentException("must be a SRC.Parameter", "paramElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var declElement = paramElement.Elements().First(e => e.Name == SRC.Declaration || e.Name == SRC.FunctionDeclaration);

            var varDeclaration = ParseDeclarationElement(declElement, context);
            return varDeclaration;
        }

        /// <summary>
        /// Creates an <see cref="IfStatement"/> object for <paramref name="ifElement"/>.
        /// </summary>
        /// <param name="ifElement">The element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>An IfStatement corresponding to ifElement.</returns>
        protected virtual IfStatement ParseIfElement(XElement ifElement, ParserContext context) {
            if(ifElement == null) 
                throw new ArgumentNullException("ifElement");
            if(ifElement.Name != SRC.If) 
                throw new ArgumentException("must be a SRC.If element", "ifElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var ifStmt = new IfStatement() {ProgrammingLanguage = ParserLanguage};
            ifStmt.AddLocation(context.CreateLocation(ifElement));

            foreach(var ifChild in ifElement.Elements()) {
                if(ifChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(ifChild);
                    if(expElement != null) {
                        ifStmt.Condition = ParseExpression(expElement, context);
                    }
                } else if(ifChild.Name == SRC.Then) {
                    //add the then statements
                    foreach(var thenChild in ifChild.Elements()) {
                        if(thenChild.Name == SRC.Block) {
                            var blockStatements = thenChild.Elements().Select(e => ParseStatement(e, context));
                            ifStmt.AddChildStatements(blockStatements);
                        } else {
                            ifStmt.AddChildStatement(ParseStatement(thenChild, context));
                        }
                    }
                } else if(ifChild.Name == SRC.Else) {
                    //add the else statements
                    foreach(var elseChild in ifChild.Elements()) {
                        if(elseChild.Name == SRC.Block) {
                            var blockStatements = elseChild.Elements().Select(e => ParseStatement(e, context));
                            ifStmt.AddElseStatements(blockStatements);
                        } else {
                            ifStmt.AddElseStatement(ParseStatement(elseChild, context));
                        }
                    }
                } else {
                    //Add as a child statement (i.e. a then statement)
                    ifStmt.AddChildStatement(ParseStatement(ifChild, context));
                }
            }

            return ifStmt;
        }

        /// <summary>
        /// Creates a <see cref="WhileStatement"/> object for <paramref name="whileElement"/>.
        /// </summary>
        /// <param name="whileElement">The element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A WhileStatement corresponding to whileElement.</returns>
        protected virtual WhileStatement ParseWhileElement(XElement whileElement, ParserContext context) {
            if(whileElement == null)
                throw new ArgumentNullException("whileElement");
            if(whileElement.Name != SRC.While)
                throw new ArgumentException("Must be a SRC.While element", "whileElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var whileStmt = new WhileStatement() {ProgrammingLanguage = ParserLanguage};
            whileStmt.AddLocation(context.CreateLocation(whileElement));

            foreach(var whileChild in whileElement.Elements()) {
                if(whileChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(whileChild);
                    if(expElement != null) {
                        whileStmt.Condition = ParseExpression(expElement, context);
                    }
                } else if(whileChild.Name == SRC.Block) {
                    //has a block, add children
                    var blockStatements = whileChild.Elements().Select(e => ParseStatement(e, context));
                    whileStmt.AddChildStatements(blockStatements);
                } else {
                    //child outside of block
                    whileStmt.AddChildStatement(ParseStatement(whileChild, context));
                }
            }

            return whileStmt;
        }

        /// <summary>
        /// Creates a ForStatement from the given element.
        /// </summary>
        /// <param name="forElement">The SRC.For element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A ForStatement corresponding to forElement. 
        /// The return type is ConditionBlockStatement so that the Java parser can also return a ForeachStatement when necessary.</returns>
        protected virtual ConditionBlockStatement ParseForElement(XElement forElement, ParserContext context) {
            if(forElement == null)
                throw new ArgumentNullException("forElement");
            if(forElement.Name != SRC.For)
                throw new ArgumentException("Must be a SRC.For element", "forElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var forStmt = new ForStatement() {ProgrammingLanguage = ParserLanguage};
            forStmt.AddLocation(context.CreateLocation(forElement));

            foreach(var forChild in forElement.Elements()) {
                if(forChild.Name == SRC.Init) {
                    //fill in initializer
                    var expElement = GetFirstChildExpression(forChild);
                    if(expElement != null) {
                        forStmt.Initializer = ParseExpression(expElement, context);
                    }
                }
                else if(forChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(forChild);
                    if(expElement != null) {
                        forStmt.Condition = ParseExpression(expElement, context);
                    }
                }
                else if(forChild.Name == SRC.Increment) {
                    //fill in incrementer
                    var expElement = GetFirstChildExpression(forChild);
                    if(expElement != null) {
                        forStmt.Incrementer = ParseExpression(expElement, context);
                    }
                }
                else if(forChild.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = forChild.Elements().Select(e => ParseStatement(e, context));
                    forStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    forStmt.AddChildStatement(ParseStatement(forChild, context));
                }
            }

            return forStmt;
        }

        protected virtual ForeachStatement ParseForeachElement(XElement foreachElement, ParserContext context) {
            if(foreachElement == null)
                throw new ArgumentNullException("foreachElement");
            if(foreachElement.Name != SRC.Foreach)
                throw new ArgumentException("Must be a SRC.Foreach element", "foreachElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var foreachStmt = new ForeachStatement() {ProgrammingLanguage = ParserLanguage};
            foreachStmt.AddLocation(context.CreateLocation(foreachElement));

            foreach(var child in foreachElement.Elements()) {
                if(child.Name == SRC.Init) {
                    //fill in condition/initializer
                    var expElement = GetFirstChildExpression(child);
                    if(expElement != null) {
                        foreachStmt.Condition = ParseExpression(expElement, context);
                    }
                }
                else if(child.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = child.Elements().Select(e => ParseStatement(e, context));
                    foreachStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    foreachStmt.AddChildStatement(ParseStatement(child, context));
                }
            }

            return foreachStmt;
        }

        protected virtual DoWhileStatement ParseDoElement(XElement doElement, ParserContext context) {
            if(doElement == null)
                throw new ArgumentNullException("doElement");
            if(doElement.Name != SRC.Do)
                throw new ArgumentException("Must be a SRC.Do element", "doElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var doStmt = new DoWhileStatement() {ProgrammingLanguage = ParserLanguage};
            doStmt.AddLocation(context.CreateLocation(doElement));

            foreach(var doChild in doElement.Elements()) {
                if(doChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(doChild);
                    if(expElement != null) {
                        doStmt.Condition = ParseExpression(expElement, context);
                    }
                } else if(doChild.Name == SRC.Block) {
                    //has a block, add children
                    var blockStatements = doChild.Elements().Select(e => ParseStatement(e, context));
                    doStmt.AddChildStatements(blockStatements);
                } else {
                    //child outside of block
                    doStmt.AddChildStatement(ParseStatement(doChild, context));
                }
            }

            return doStmt;
        }

        protected virtual SwitchStatement ParseSwitchElement(XElement switchElement, ParserContext context) {
            if(switchElement == null)
                throw new ArgumentNullException("switchElement");
            if(switchElement.Name != SRC.Switch)
                throw new ArgumentException("Must be a SRC.Switch element", "switchElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var switchStmt = new SwitchStatement() {ProgrammingLanguage = ParserLanguage};
            switchStmt.AddLocation(context.CreateLocation(switchElement));

            foreach(var switchChild in switchElement.Elements()) {
                if(switchChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(switchChild);
                    if(expElement != null) {
                        switchStmt.Condition = ParseExpression(expElement, context);
                    }
                } else if(switchChild.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = switchChild.Elements().Select(e => ParseStatement(e, context));
                    switchStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    switchStmt.AddChildStatement(ParseStatement(switchChild, context));
                }
            }

            return switchStmt;
        }

        protected virtual CaseStatement ParseCaseElement(XElement caseElement, ParserContext context) {
            if(caseElement == null)
                throw new ArgumentNullException("caseElement");
            if(!(caseElement.Name == SRC.Case || caseElement.Name == SRC.Default))
                throw new ArgumentException("Must be a SRC.Case or SRC.Default element", "caseElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var caseStmt = new CaseStatement() {
                ProgrammingLanguage = ParserLanguage,
                IsDefault = caseElement.Name == SRC.Default
            };
            caseStmt.AddLocation(context.CreateLocation(caseElement));

            foreach(var caseChild in caseElement.Elements()) {
                if(caseChild.Name == SRC.Expression && caseStmt.Condition == null) {
                    //this is the first expression we've seen, add as the case label
                    caseStmt.Condition = ParseExpressionElement(caseChild, context);
                }
                else if(caseChild.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = caseChild.Elements().Select(e => ParseStatement(e, context));
                    caseStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    caseStmt.AddChildStatement(ParseStatement(caseChild, context));
                }
            }

            return caseStmt;
        }

        protected virtual BreakStatement ParseBreakElement(XElement breakElement, ParserContext context) {
            if(breakElement == null)
                throw new ArgumentNullException("breakElement");
            if(breakElement.Name != SRC.Break)
                throw new ArgumentException("Must be a SRC.Break element", "breakElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var breakStmt = new BreakStatement() {ProgrammingLanguage = ParserLanguage};
            breakStmt.AddLocation(context.CreateLocation(breakElement));

            return breakStmt;
        }

        protected virtual ContinueStatement ParseContinueElement(XElement continueElement, ParserContext context) {
            if(continueElement == null)
                throw new ArgumentNullException("continueElement");
            if(continueElement.Name != SRC.Continue)
                throw new ArgumentException("Must be a SRC.Continue element", "continueElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var continueStmt = new ContinueStatement() {ProgrammingLanguage = ParserLanguage};
            continueStmt.AddLocation(context.CreateLocation(continueElement));

            return continueStmt;
        }

        protected virtual GotoStatement ParseGotoElement(XElement gotoElement, ParserContext context) {
            if(gotoElement == null)
                throw new ArgumentNullException("gotoElement");
            if(gotoElement.Name != SRC.Goto)
                throw new ArgumentException("Must be a SRC.Goto element", "gotoElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var gotoStmt = new GotoStatement() {ProgrammingLanguage = ParserLanguage};
            gotoStmt.AddLocation(context.CreateLocation(gotoElement));

            if(gotoElement.HasElements) {
                throw new NotImplementedException();
                //gotoStmt.Content = ParseExpression(gotoElement.Elements().First(), context);
                //TODO: we know that this will be a name element corresponding to a label. Should we just create the NameUse object here
                //instead of calling ParseExpression?
            }
            //TODO: in C#, you can write "goto case 3;" within a switch statement. SrcML does not mark up the case 3 as anything.
            //<goto>goto case 3;</goto>
            //Mike collard to fix "in next release".

            return gotoStmt;
        }

        protected virtual LabelStatement ParseLabelElement(XElement labelElement, ParserContext context) {
            if(labelElement == null)
                throw new ArgumentNullException("labelElement");
            if(labelElement.Name != SRC.Label)
                throw new ArgumentException("Must be a SRC.Label element", "labelElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var labelStmt = new LabelStatement() {ProgrammingLanguage = ParserLanguage};
            labelStmt.AddLocation(context.CreateLocation(labelElement));

            var name = labelElement.Element(SRC.Name);
            if(name != null) {
                labelStmt.Name = name.Value;
            }

            return labelStmt;
        }

        protected virtual ReturnStatement ParseReturnElement(XElement returnElement, ParserContext context) {
            if(returnElement == null)
                throw new ArgumentNullException("returnElement");
            if(returnElement.Name != SRC.Return)
                throw new ArgumentException("Must be a SRC.Return element", "returnElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var returnStmt = new ReturnStatement() {ProgrammingLanguage = ParserLanguage};
            returnStmt.AddLocation(context.CreateLocation(returnElement));

            var expElement = GetFirstChildExpression(returnElement);
            if(expElement != null) {
                returnStmt.Content = ParseExpression(expElement, context);
            }

            return returnStmt;
        }

        protected virtual ThrowStatement ParseThrowElement(XElement throwElement, ParserContext context) {
            if(throwElement == null)
                throw new ArgumentNullException("throwElement");
            if(throwElement.Name != SRC.Throw)
                throw new ArgumentException("Must be a SRC.Throw element", "throwElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var throwStmt = new ThrowStatement() {ProgrammingLanguage = ParserLanguage};
            throwStmt.AddLocation(context.CreateLocation(throwElement));

            var expElement = GetFirstChildExpression(throwElement);
            if(expElement != null) {
                throwStmt.Content = ParseExpression(expElement, context);
            }

            return throwStmt;
        }

        protected virtual TryStatement ParseTryElement(XElement tryElement, ParserContext context) {
            if(tryElement == null)
                throw new ArgumentNullException("tryElement");
            if(tryElement.Name != SRC.Try)
                throw new ArgumentException("Must be a SRC.Try element", "tryElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var tryStmt = new TryStatement() {ProgrammingLanguage = ParserLanguage};
            tryStmt.AddLocation(context.CreateLocation(tryElement));

            foreach(var tryChild in tryElement.Elements()) {
                if(tryChild.Name == SRC.Catch) {
                    //add catch statement
                    tryStmt.AddCatchStatement(ParseCatchElement(tryChild, context));
                } else if(tryChild.Name == SRC.Finally) {
                    //add finally children
                    foreach(var finallyChild in tryChild.Elements()) {
                        if(finallyChild.Name == SRC.Block) {
                            var blockStatements = finallyChild.Elements().Select(e => ParseStatement(e, context));
                            tryStmt.AddFinallyStatements(blockStatements);
                        } else {
                            tryStmt.AddFinallyStatement(ParseStatement(finallyChild, context));
                        }
                    }
                } else if(tryChild.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = tryChild.Elements().Select(e => ParseStatement(e, context));
                    tryStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    tryStmt.AddChildStatement(ParseStatement(tryChild, context));
                }
            }

            return tryStmt;
        }

        protected virtual CatchStatement ParseCatchElement(XElement catchElement, ParserContext context) {
            if(catchElement == null)
                throw new ArgumentNullException("catchElement");
            if(catchElement.Name != SRC.Catch)
                throw new ArgumentException("Must be a SRC.Catch element", "catchElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var catchStmt = new CatchStatement {ProgrammingLanguage = ParserLanguage};
            catchStmt.AddLocation(context.CreateLocation(catchElement));

            foreach(var catchChild in catchElement.Elements()) {
                if(catchChild.Name == SRC.Parameter) {
                    //add the catch parameter
                    catchStmt.Parameter = ParseParameterElement(catchChild, context);
                } else if(catchChild.Name == SRC.Block) {
                    //add children of the block
                    var blockStatements = catchChild.Elements().Select(e => ParseStatement(e, context));
                    catchStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    catchStmt.AddChildStatement(ParseStatement(catchChild, context));
                }
            }

            return catchStmt;
        }

        protected virtual Statement ParseExpressionStatementElement(XElement stmtElement, ParserContext context) {
            if(stmtElement == null)
                throw new ArgumentNullException("stmtElement");
            if(stmtElement.Name != SRC.ExpressionStatement)
                throw new ArgumentException("Must be a SRC.ExpressionStatement element", "stmtElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var stmt = new Statement() {ProgrammingLanguage = ParserLanguage};
            stmt.AddLocation(context.CreateLocation(stmtElement));

            foreach(var child in stmtElement.Elements()) {
                if(child.Name == SRC.Expression) {
                    stmt.Content = ParseExpressionElement(child, context);
                } else {
                    //This should probably only be comments?
                    stmt.AddChildStatement(ParseStatement(child, context));
                }
            }

            return stmt;
        }

        protected virtual Statement ParseDeclarationStatementElement(XElement stmtElement, ParserContext context) {
            if(stmtElement == null)
                throw new ArgumentNullException("stmtElement");
            if(stmtElement.Name != SRC.DeclarationStatement)
                throw new ArgumentException("Must be a SRC.DeclarationStatement element", "stmtElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var stmt = new Statement() {
                ProgrammingLanguage = ParserLanguage,
                Content = ParseExpression(GetChildExpressions(stmtElement), context)
            };
            stmt.AddLocation(context.CreateLocation(stmtElement));

            return stmt;
        }


        /// <summary>
        /// Creates a <see cref="NamespaceDefinition"/> object for <paramref name="namespaceElement"/>
        /// </summary>
        /// <param name="namespaceElement">The element to parse.</param>
        /// <param name="context">The context to use.</param>
        protected abstract NamespaceDefinition ParseNamespaceElement(XElement namespaceElement, ParserContext context);

        /// <summary>
        /// Parses the given <paramref name="aliasElement"/> and creates an ImportStatement or AliasStatement from it.
        /// </summary>
        /// <param name="aliasElement">The alias element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An ImportStatement if the element is an import, or an AliasStatement if it is an alias.</returns>
        protected abstract Statement ParseAliasElement(XElement aliasElement, ParserContext context);


        ///// <summary>
        ///// Creates an <see cref="Alias"/> object from a using import (such as using in C++ and C#
        ///// and import in Java).
        ///// </summary>
        ///// <param name="aliasStatement">The statement to parse. Should be of type see
        ///// cref="AliasElementName"/></param>
        ///// <param name="context">The context to place the resulting alias in</param>
        ///// <returns>a new alias object that represents this alias statement</returns>
        //protected Alias ParseAliasElement(XElement aliasStatement, ParserContext context) {
        //    if(null == aliasStatement)
        //        throw new ArgumentNullException("aliasStatement");
        //    if(aliasStatement.Name != AliasElementName)
        //        throw new ArgumentException(String.Format("must be a {0} statement", AliasElementName), "usingStatement");
        //    if(context == null)
        //        throw new ArgumentNullException("context");

        //    var alias = new Alias() {
        //        Location = context.CreateLocation(aliasStatement, true),
        //        ProgrammingLanguage = ParserLanguage,
        //    };

        //    IEnumerable<XElement> namespaceNames = GetNamesFromAlias(aliasStatement);

        //    if(!AliasIsNamespaceImport(aliasStatement)) {
        //        var lastNameElement = namespaceNames.LastOrDefault();
        //        namespaceNames = from name in namespaceNames
        //                         where name.IsBefore(lastNameElement)
        //                         select name;

        //        alias.ImportedNamedScope = new NamedScopeUse() {
        //            Name = lastNameElement.Value,
        //            Location = context.CreateLocation(lastNameElement),
        //            ProgrammingLanguage = ParserLanguage,
        //        };
        //    }

        //    NamespaceUse current = null;
        //    foreach(var namespaceName in namespaceNames) {
        //        var use = new NamespaceUse() {
        //            Name = namespaceName.Value,
        //            Location = context.CreateLocation(namespaceName),
        //            ProgrammingLanguage = ParserLanguage,
        //        };

        //        if(alias.ImportedNamespace == null) {
        //            alias.ImportedNamespace = use;
        //            current = use;
        //        } else {
        //            current.ChildScopeUse = use;
        //            current = use;
        //        }
        //    }

        //    return alias;
        //}

        /// <summary>
        /// Parses an element corresponding to a type definition and creates a TypeDefinition object 
        /// </summary>
        /// <param name="typeElement">The type element to parse. This must be one of the elements contained in TypeElementNames.</param>
        /// <param name="context">The parser context</param>
        /// <returns>A TypeDefinition parsed from the element</returns>
        protected virtual TypeDefinition ParseTypeElement(XElement typeElement, ParserContext context) {
            if(null == typeElement)
                throw new ArgumentNullException("typeElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var typeDefinition = new TypeDefinition() {
                Accessibility = GetAccessModifierForType(typeElement),
                Kind = XNameMaps.GetKindForXElement(typeElement),
                Name = GetNameForType(typeElement),
                ProgrammingLanguage = ParserLanguage
            };
            typeDefinition.AddLocation(context.CreateLocation(typeElement, ContainerIsReference(typeElement)));

            foreach(var parentTypeElement in GetParentTypeUseElements(typeElement)) {
                var parentTypeUse = ParseTypeUseElement(parentTypeElement, context);
                typeDefinition.AddParentType(parentTypeUse);
            }
            //get the block containing the type members, and add them as children
            var typeBlock = typeElement.Element(SRC.Block);
            if(typeBlock != null) {
                foreach(var child in typeBlock.Elements()) {
                    typeDefinition.AddChildStatement(ParseStatement(child, context));
                }
            }

            return typeDefinition;
        }

        

        /// <summary>
        /// Creates a global <see cref="NamespaceDefinition"/> object for <paramref name="unitElement"/>.
        /// </summary>
        /// <param name="unitElement">The SRC.Unit element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A NamespaceDefinition corresponding to <paramref name="unitElement"/>.</returns>
        protected virtual NamespaceDefinition ParseUnitElement(XElement unitElement, ParserContext context) {
            if(null == unitElement)
                throw new ArgumentNullException("unitElement");
            if(SRC.Unit != unitElement.Name)
                throw new ArgumentException("should be a SRC.Unit", "unitElement");
            if(context == null)
                throw new ArgumentNullException("context");
            context.FileUnit = unitElement;
            //var aliases = from aliasStatement in GetAliasElementsForFile(unitElement)
            //              select ParseAliasElement(aliasStatement, context);

            //context.Aliases = new Collection<Alias>(aliases.ToList());

            //create a global namespace for the file unit
            var namespaceForUnit = new NamespaceDefinition() {ProgrammingLanguage = ParserLanguage};
            namespaceForUnit.AddLocation(context.CreateLocation(unitElement));

            foreach(var child in unitElement.Elements()) {
                namespaceForUnit.AddChildStatement(ParseStatement(child, context));
            }
            return namespaceForUnit;
        }

        /// <summary>
        /// Creates a BlockStatement from the given block element. 
        /// This method is only for parsing free-standing blocks, which are very rare. 
        /// Most blocks are parsed by the construct they are attached to, e.g. an if-statement or class definition.
        /// </summary>
        /// <param name="blockElement">The SRC.Block element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A BlockStatement corresponding to blockElement.</returns>
        protected virtual BlockStatement ParseBlockElement(XElement blockElement, ParserContext context) {
            if(blockElement == null)
                throw new ArgumentNullException("blockElement");
            if(blockElement.Name != SRC.Block)
                throw new ArgumentException("must be a SRC.Block element", "blockElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var bs = new BlockStatement() {ProgrammingLanguage = ParserLanguage};
            bs.AddLocation(context.CreateLocation(blockElement));

            foreach(var child in blockElement.Elements()) {
                bs.AddChildStatement(ParseStatement(child, context));
            }

            return bs;
        }

        /// <summary>
        /// Creates an ExternStatement from the given extern element.
        /// Note that only extern statements with a linkage specifier, e.g. "extern "C" int foo();", are marked up with SRC.Extern.
        /// </summary>
        /// <param name="externElement">The SRC.Extern element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An ExternStatement corresponding to externElement.</returns>
        protected virtual ExternStatement ParseExternElement(XElement externElement, ParserContext context) {
            if(externElement == null)
                throw new ArgumentNullException("externElement");
            if(externElement.Name != SRC.Extern)
                throw new ArgumentException("must be a SRC.Extern element", "externElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var es = new ExternStatement() {ProgrammingLanguage = ParserLanguage};
            es.AddLocation(context.CreateLocation(externElement));

            foreach(var exChild in externElement.Elements()) {
                if(exChild.Name == LIT.Literal) {
                    es.LinkageType = exChild.Value;
                } else if(exChild.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = exChild.Elements().Select(e => ParseStatement(e, context));
                    es.AddChildStatements(blockStatements);
                } else {
                    es.AddChildStatement(ParseStatement(exChild, context));
                }
            }

            return es;
        }

        /// <summary>
        /// Creates an empty Statement object from the given SRC.EmptyStatement element.
        /// </summary>
        /// <param name="emptyElement">A SRC.EmptyStatement element.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A Statement corresponding to <paramref name="emptyElement"/>.</returns>
        protected virtual Statement ParseEmptyStatementElement(XElement emptyElement, ParserContext context) {
            if(emptyElement == null)
                throw new ArgumentNullException("emptyElement");
            if(emptyElement.Name != SRC.EmptyStatement)
                throw new ArgumentException("must be a SRC.EmptyStatement element", "emptyElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var stmt = new Statement() {ProgrammingLanguage = ParserLanguage};
            stmt.AddLocation(context.CreateLocation(emptyElement));
            return stmt;
        }

        #endregion Parse statement elements

        #region Parse expression elements
        /// <summary>
        /// Creates an Expression from the given element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An Expression parsed from the element.</returns>
        protected virtual Expression ParseExpression(XElement element, ParserContext context) {
            if(element == null)
                throw new ArgumentNullException("element");
            if(context == null)
                throw new ArgumentNullException("context");

            try {
                Expression exp = null;
                if(element.Name == SRC.Expression) {
                    exp = ParseExpressionElement(element, context);
                } else if(element.Name == SRC.Declaration) {
                    exp = ParseDeclarationElement(element, context);
                } else if(element.Name == SRC.Name) {
                    exp = ParseNameUseElement(element, context);
                } else if(element.Name == SRC.Type) {
                    exp = ParseTypeUseElement(element, context);
                } else if(element.Name == OP.Operator) {
                    exp = ParseOperatorElement(element, context);
                } else if(element.Name == SRC.Call) {
                    exp = ParseCallElement(element, context);
                } else if(element.Name == LIT.Literal) {
                    exp = ParseLiteralElement(element, context);
                } else if(element.Name == SRC.Comment) {
                    //skip
                } else {
                    LogUnknown(element, context);
                }

                //TODO: how do we handle a function_declaration that's put in an expression-type place?
                //For example, a function pointer declared in an if condition

                return exp;
            } catch(ParseException) {
                throw;
            } catch(Exception e) {
                int lineNumber = element.GetSrcLineNumber();
                int columnNumber = element.GetSrcLinePosition();
                throw new ParseException(context.FileName, lineNumber, columnNumber, this, e.Message, e);
            }
        }

        /// <summary>
        /// Parses (possibly) multiple expression component elements, and combines them into an Expression. 
        /// All the elements must have the same parent.
        /// </summary>
        /// <param name="elements">The expression component elements to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An Expression with each of the parsed elements as its components. 
        /// If <paramref name="elements"/> contains only a single value, the result will be the same as if it were parsed directly..</returns>
        protected virtual Expression ParseExpression(IEnumerable<XElement> elements, ParserContext context) {
            if(elements == null)
                throw new ArgumentNullException("elements");
            if(context == null)
                throw new ArgumentNullException("context");

            var expElements = elements.ToList();

            if(expElements.Count == 0) {
                return null;
            }
            if(expElements.Count == 1) {
                return ParseExpression(expElements.First(), context);
            }

            var expressionStack = new Stack<Expression>();
            expressionStack.Push(new Expression() {
                ProgrammingLanguage = ParserLanguage,
                Location = context.CreateLocation(expElements.First().Parent)
            });

            //parse each of the components in the expression
            var declList = new List<VariableDeclaration>();
            foreach(var element in expElements) {
                var exp = ParseExpression(element, context);
                var varDecl = exp as VariableDeclaration;
                if(varDecl != null) {
                    if(varDecl.VariableType == null && declList.Any()) {
                        //type will be null in cases of multiple declarations, e.g. int a, b;
                        varDecl.VariableType = declList.First().VariableType;
                        varDecl.Accessibility = declList.First().Accessibility;
                    }
                    declList.Add(varDecl);
                }

                //handle sub-expressions
                var opUse = exp as OperatorUse;
                if(opUse != null && opUse.Text == "(") {
                    //this is the start of a sub-expression
                    expressionStack.Push(new Expression() {
                        ProgrammingLanguage = ParserLanguage,
                        Location = context.CreateLocation(element.Parent)
                    });
                } else if(opUse != null && opUse.Text == ")") {
                    //this is the end of a sub-expression
                    var subExp = expressionStack.Pop();
                    expressionStack.Peek().AddComponent(subExp);
                } else {
                    expressionStack.Peek().AddComponent(exp);
                }
            }

            while(expressionStack.Count > 1) {
                //we saw more lparens than rparens, just combine the expression fragments
                var exp = expressionStack.Pop();
                expressionStack.Peek().AddComponent(exp);
            }

            return expressionStack.Pop();
        }

        protected virtual Expression ParseExpressionElement(XElement expElement, ParserContext context) {
            if(expElement == null)
                throw new ArgumentNullException("expElement");
            if(expElement.Name != SRC.Expression)
                throw new ArgumentException("Must be a SRC.Expression element", "expElement");
            if(context == null)
                throw new ArgumentNullException("context");

            return ParseExpression(expElement.Elements(), context);
        }
        
        /// <summary>
        /// Creates a variable declaration object from the given declaration element
        /// </summary>
        /// <param name="declElement">The SRC.Declaration element to parse.</param>
        /// <param name="context">The parser context.</param>
        /// <returns>A VariableDeclaration object corresponding to the given element.</returns>
        protected virtual VariableDeclaration ParseDeclarationElement(XElement declElement, ParserContext context) {
            //TODO: can/should this handle function_decls as well as decls? ParseParameterElement may pass in a function_decl

            if(declElement == null)
                throw new ArgumentNullException("declElement");
            if(declElement.Name != SRC.Declaration)
                throw new ArgumentException("Must be a SRC.Declaration element", "declElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var varDecl = new VariableDeclaration {
                Location = context.CreateLocation(declElement), 
                ProgrammingLanguage = ParserLanguage
            };

            var nameElement = declElement.Element(SRC.Name);
            if(nameElement != null) {
                varDecl.Name = nameElement.Value;
            }

            var typeElement = declElement.Element(SRC.Type);
            if(typeElement != null && typeElement.Attribute("ref") == null) {
                varDecl.VariableType = ParseTypeUseElement(typeElement, context);
                varDecl.Accessibility = GetAccessModifierFromTypeUseElement(typeElement);
            }

            var initElement = declElement.Element(SRC.Init);
            if(initElement != null) {
                var expElement = GetFirstChildExpression(initElement);
                if(expElement != null) {
                    varDecl.Initializer = ParseExpression(expElement, context);
                }
            }
            //TODO: need to also handle C++ case of calling constructor in the declaration, e.g. "Foo bar(27);"

            return varDecl;
        }

        

        /// <summary>
        /// Creates a NameUse object from the given name element.
        /// </summary>
        /// <param name="nameElement">The SRC.Name element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A NameUse corresponding to <paramref name="nameElement"/>.</returns>
        protected virtual Expression ParseNameUseElement(XElement nameElement, ParserContext context) {
            if(nameElement == null)
                throw new ArgumentNullException("nameElement");
            if(nameElement.Name != SRC.Name)
                throw new ArgumentException("should be a SRC.Name", "nameElement");
            if(context == null)
                throw new ArgumentNullException("context");

            //check if we can be sure this is a variable use
            if(nameElement.Elements(SRC.Index).Any()) {
                return ParseVariableUse(nameElement, context);
            }

            if(nameElement.HasElements) {
                return ParseExpression(nameElement.Elements(), context);
            }

            //no children
            var nu = new NameUse() {
                Location = context.CreateLocation(nameElement, true),
                ProgrammingLanguage = ParserLanguage,
                Name = NameHelper.GetLastName(nameElement),
                //Prefix = ParseNamePrefix(nameElement,context)
            };

            return nu;
        }

        /// <summary>
        /// Creates a NamespaceUse object from the given name element.
        /// </summary>
        /// <param name="nameElement">The SRC.Name element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A NamespaceUse corresponding to <paramref name="nameElement"/>.</returns>
        protected virtual Expression ParseNamespaceUse(XElement nameElement, ParserContext context) {
            if(nameElement == null)
                throw new ArgumentNullException("nameElement");
            if(nameElement.Name != SRC.Name)
                throw new ArgumentException("should be a SRC.Name", "nameElement");
            if(context == null)
                throw new ArgumentNullException("context");

            Expression result;

            if(nameElement.HasElements) {
                result = new Expression() {
                    ProgrammingLanguage = ParserLanguage,
                    Location = context.CreateLocation(nameElement)
                };
                foreach(var child in nameElement.Elements()) {
                    if(child.Name == SRC.Name) {
                        result.AddComponent(ParseNamespaceUse(child, context));
                    } else {
                        result.AddComponent(ParseExpression(child, context));
                    }
                }
            } else {
                result = new NamespaceUse() {
                    Location = context.CreateLocation(nameElement, true),
                    ProgrammingLanguage = ParserLanguage,
                    Name = NameHelper.GetLastName(nameElement),
                    //Prefix = ParseNamePrefix(nameElement,context)
                };
            }

            return result;
        }

        /// <summary>
        /// Creates an OperatorUse object from the given operator element.
        /// </summary>
        /// <param name="operatorElement">The OP.Operator element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An OperatorUse corresponding to <paramref name="operatorElement"/>.</returns>
        protected virtual OperatorUse ParseOperatorElement(XElement operatorElement, ParserContext context) {
            if(operatorElement == null)
                throw new ArgumentNullException("operatorElement");
            if(operatorElement.Name != OP.Operator)
                throw new ArgumentException("should be an OP.Operator", "operatorElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var op = new OperatorUse() {
                Location = context.CreateLocation(operatorElement),
                ProgrammingLanguage = ParserLanguage,
                Text = operatorElement.Value
            };

            return op;
        }

        /// <summary>
        /// Creates a type use element
        /// </summary>
        /// <param name="typeUseElement">the element to parse. Must be of a
        /// <see cref="ABB.SrcML.SRC.Type"/> or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="context">the parser context</param>
        /// <returns>A Type Use object</returns>
        protected virtual TypeUse ParseTypeUseElement(XElement typeUseElement, ParserContext context) {
            if(typeUseElement == null)
                throw new ArgumentNullException("typeUseElement");
            if(!(typeUseElement.Name == SRC.Type || typeUseElement.Name == SRC.Name))
                throw new ArgumentException("Must be a SRC.Type or SRC.Name element", "typeUseElement");
            if(context == null)
                throw new ArgumentNullException("context");

            XElement typeNameElement;
            // locate the name element for the type
            if(typeUseElement.Name == SRC.Type) {
                typeNameElement = typeUseElement.Elements(SRC.Name).LastOrDefault();
            } else {
                //typeUseElement is a SRC.Name
                typeNameElement = typeUseElement;
            } 

            XElement lastNameElement = null;                   // this is the name element that identifies the type being used
            NamePrefix prefix = null;                          // This is the prefix (in A::B::C, this would be the chain A::B)
            XElement argumentListElement = null;               // the argument list element holds the parameters for generic type uses
            var typeArguments = Enumerable.Empty<TypeUse>();   // enumerable for the actual generic arguments

            // get the last name element and the prefix
            if(typeNameElement != null) {
                lastNameElement = NameHelper.GetLastNameElement(typeNameElement);
                prefix = ParseNamePrefix(typeNameElement, context);
            }

            // if the last name element exists, then this *may* be a generic type use 
            // go look for the argument list element
            if(lastNameElement != null) {
                if(prefix == null) { 
                    //if there is no prefix, then the argument list element will be the first sibling of lastNameElement
                    argumentListElement = lastNameElement.ElementsAfterSelf(SRC.ArgumentList).FirstOrDefault();
                } else {             
                    //otherwise, it will be the first *child* of lastNameElement
                    argumentListElement = lastNameElement.Elements(SRC.ArgumentList).FirstOrDefault();
                }
            }

            if(argumentListElement != null) {
                typeArguments = from argument in argumentListElement.Elements(SRC.Argument)
                                where argument.Elements(SRC.Name).Any()
                                select ParseTypeUseElement(argument.Element(SRC.Name), context);
                // if this is a generic type use and there is a prefix (A::B::C) then the last name
                // element will actually be the first child of lastNameElement
                if(prefix != null) {
                    lastNameElement = lastNameElement.Element(SRC.Name);
                }
            }

            // construct the type use
            var typeUse = new TypeUse() {
                Name = (lastNameElement != null ? lastNameElement.Value : string.Empty),
                Location = context.CreateLocation(lastNameElement != null ? lastNameElement : typeUseElement),
                Prefix = prefix,
                ProgrammingLanguage = ParserLanguage
            };
            typeUse.AddTypeParameters(typeArguments);
            
            //TODO: handle aliases
            //typeUse.AddAliases(context.Aliases);

            return typeUse;
        }

        /// <summary>
        /// Parses the prefix out of the given name element, if it contains one.
        /// In a name usage like System.IO.File, File is the name and System.IO is the prefix.
        /// </summary>
        /// <param name="nameElement">The SRC.Name element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A NamePrefix object, or null if <paramref name="nameElement"/> contains no prefix.</returns>
        protected virtual NamePrefix ParseNamePrefix(XElement nameElement, ParserContext context) {
            if(nameElement == null)
                throw new ArgumentNullException("nameElement");
            if(nameElement.Name != SRC.Name)
                throw new ArgumentException("must be a SRC.Name element", "nameElement");
            if(context == null)
                throw new ArgumentNullException("context");
            
            if(!nameElement.HasElements || nameElement.Elements(SRC.Name).Count() <= 1) {
                //this name doesn't have a prefix
                return null;
            }

            var methodName = nameElement.Elements(SRC.Name).Last();
            var prefixParts = methodName.ElementsBeforeSelf();

            var prefix = new NamePrefix();
            foreach(var part in prefixParts) {
                prefix.AddComponent(ParseExpression(part, context));
            }
            return prefix;
        }

        /// <summary>
        /// Creates a method call object.
        /// </summary>
        /// <param name="callElement">The XML element to parse</param>
        /// <param name="context">The parser context</param>
        /// <returns>A method call for <paramref name="callElement"/>.</returns>
        protected virtual Expression ParseCallElement(XElement callElement, ParserContext context) {
            if(callElement == null)
                throw new ArgumentNullException("callElement");
            if(callElement.Name != SRC.Call)
                throw new ArgumentException("must be a SRC.Call element", "callElement");
            if(context == null)
                throw new ArgumentNullException("context");
            
            var mc = new MethodCall() {
                Location = context.CreateLocation(callElement),
                ProgrammingLanguage = ParserLanguage
            };

            XElement methodNameElement = null;
            Expression callingExpression = null;

            //parse the name element for the call
            var nameElement = callElement.Element(SRC.Name);
            if(nameElement != null) {
                if(!nameElement.HasElements) {
                    methodNameElement = nameElement;
                } else {
                    methodNameElement = nameElement.Elements(SRC.Name).Last();
                    callingExpression = ParseExpression(methodNameElement.ElementsBeforeSelf(), context);
                }
            }
            if(methodNameElement != null) {
                var argListElement = methodNameElement.Element(SRC.ArgumentList);
                if(argListElement != null) {
                    //this is a method call with type arguments
                    mc.Name = methodNameElement.Element(SRC.Name).Value;
                    foreach(var argElement in argListElement.Elements(SRC.Argument)) {
                        var typeName = argElement.Descendants(SRC.Name).First();
                        if(typeName != null) {
                            mc.AddTypeArgument(ParseTypeUseElement(typeName, context));
                        }
                    }
                } else {
                    mc.Name = methodNameElement.Value;
                }
            }

            //check if this is a call to a constructor
            if(callElement.ElementsBeforeSelf().Any(e => e.Name == OP.Operator && e.Value == "new")) {
                mc.IsConstructor = true;
            }
            var parentElement = callElement.Parent;
            if(parentElement != null && parentElement.Name == SRC.MemberList) {
                var container = parentElement.Parent;
                if(container != null && container.Name == SRC.Constructor) {
                    mc.IsConstructor = true;
                }
            }

            //parse the arguments to the method call
            var argList = callElement.Element(SRC.ArgumentList);
            if(argList != null) {
                foreach(var argElement in argList.Elements(SRC.Argument)) {
                    var exp = GetFirstChildExpression(argElement);
                    if(exp != null) {
                        mc.AddArgument(ParseExpression(exp, context));
                    }
                }
            }

            return callingExpression != null ? MergeExpressions(callingExpression, mc) : mc;
        }

        /// <summary>
        /// Creates a LiteralUse object from the given element
        /// </summary>
        /// <param name="literalElement">The element to parse. Must be a <see cref="ABB.SrcML.LIT.Literal"/> element.</param>
        /// <param name="context">the parser context</param>
        /// <returns>A LiteralUse corresponding to <paramref name="literalElement"/>.</returns>
        protected virtual LiteralUse ParseLiteralElement(XElement literalElement, ParserContext context) {
            if(literalElement == null)
                throw new ArgumentNullException("literalElement");
            if(literalElement.Name != LIT.Literal)
                throw new ArgumentException("Must be a LIT.Literal element", "literalElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var litUse = new LiteralUse() {
                Location = context.CreateLocation(literalElement),
                ProgrammingLanguage = ParserLanguage,
                Value = literalElement.Value,
                Kind = LiteralUse.GetLiteralKind(literalElement)
            };

            return litUse;
        }


        protected virtual Expression ParseVariableUse(XElement nameElement, ParserContext context) {
            if(nameElement == null)
                throw new ArgumentNullException("nameElement");
            if(nameElement.Name != SRC.Name)
                throw new ArgumentException("Must be a SRC.Name element", "nameElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var vu = new VariableUse() {ProgrammingLanguage = ParserLanguage};

            var childElements = nameElement.Elements().ToList();
            if(childElements.Count == 0) {
                vu.Name = nameElement.Value;
                vu.Location = context.CreateLocation(nameElement);
            } else {
                //parse the index, if there is one
                var indexElement = nameElement.Element(SRC.Index);
                if(indexElement != null) {
                    vu.Index = ParseExpression(GetFirstChildExpression(indexElement), context);
                }
                //get the name for variable being used
                var lastName = nameElement.Elements(SRC.Name).LastOrDefault();
                if(lastName != null) {
                    vu.Name = lastName.Value;
                    vu.Location = context.CreateLocation(lastName);
                }
                //parse the calling object expression
                var callingObjects = lastName != null ? lastName.ElementsBeforeSelf() : nameElement.Elements();
                var callingExp = ParseExpression(callingObjects, context);
                return MergeExpressions(callingExp, vu);
            }
            
            return vu;
        }

        #endregion Parse expression elements

        #region aliases

        /// <summary>
        /// Checks if this alias statement is a namespace import or something more specific (such as
        /// a type or method)
        /// </summary>
        /// <param name="aliasStatement">The alias statement to check. Must be of type see
        /// cref="AliasElementName"/></param>
        /// <returns>True if this is a namespace import; false otherwise</returns>
        protected abstract bool AliasIsNamespaceImport(XElement aliasStatement);

        /// <summary>
        /// Gets all of the names for this alias
        /// </summary>
        /// <param name="aliasStatement">The alias statement. Must be of type see
        /// cref="AliasElementName"/></param>
        /// <returns>An enumerable of all the <see cref="ABB.SrcML.SRC.Name">name elements</see> for
        /// this statement</returns>
        protected virtual IEnumerable<XElement> GetNamesFromAlias(XElement aliasStatement) {
            if(null == aliasStatement)
                throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName)
                throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

            var nameElement = aliasStatement.Element(SRC.Name);
            if(null != nameElement)
                return NameHelper.GetNameElementsFromName(nameElement);
            return Enumerable.Empty<XElement>();
        }

        #endregion aliases

        //#region get child containers from scope

        ///// <summary>
        ///// Gets all of the child containers for the given container
        ///// </summary>
        ///// <param name="container">The container</param>
        ///// <returns>An enumerable of all the children</returns>
        //protected virtual IEnumerable<XElement> GetChildContainers(XElement container) {
        //    if(null == container)
        //        return Enumerable.Empty<XElement>();
        //    IEnumerable<XElement> children;

        //    if(TypeElementNames.Contains(container.Name)) {
        //        children = GetChildContainersFromType(container);
        //    } else if(MethodElementNames.Contains(container.Name)) {
        //        children = GetChildContainersFromMethod(container);
        //    } else if(NamespaceElementNames.Contains(container.Name)) {
        //        children = GetChildContainersFromNamespace(container);
        //    } else {
        //        children = from child in container.Elements()
        //                   where ContainerElementNames.Contains(child.Name)
        //                   select child;
        //    }
        //    return children;
        //}

        ///// <summary>
        ///// Gets all of the child containers for a method. It calls
        ///// <see cref="GetChildContainers(XElement)"/> on the child block.
        ///// </summary>
        ///// <param name="container">The method container</param>
        ///// <returns>All of the child containers</returns>
        //protected virtual IEnumerable<XElement> GetChildContainersFromMethod(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    return GetChildContainers(block);
        //}

        ///// <summary>
        ///// Gets all of the child containers for a namespace. It calls
        ///// <see cref="GetChildContainers(XElement)"/> on the child block.
        ///// </summary>
        ///// <param name="container">The namespace container</param>
        ///// <returns>All of the child containers</returns>
        //protected virtual IEnumerable<XElement> GetChildContainersFromNamespace(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    return GetChildContainers(block);
        //}

        ///// <summary>
        ///// Gets all of the child containers for a type. It calls
        ///// <see cref="GetChildContainers(XElement)"/> on the child block.
        ///// </summary>
        ///// <param name="container">The namespace type</param>
        ///// <returns>All of the child containers</returns>
        //protected virtual IEnumerable<XElement> GetChildContainersFromType(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    return GetChildContainers(block);
        //}

        //#endregion get child containers from scope

        //#region get method calls from scope

        ///// <summary>
        ///// Gets the method calls from an element
        ///// </summary>
        ///// <param name="element">The element to search</param>
        ///// <returns>All of the call elements from the element</returns>
        //protected virtual IEnumerable<XElement> GetMethodCallsFromElement(XElement element) {
        //    if(SRC.Constructor == element.Name) {
        //        return GetCallsFromConstructorElement(element);
        //    } else if(MethodElementNames.Contains(element.Name) ||
        //       NamespaceElementNames.Contains(element.Name) ||
        //       TypeElementNames.Contains(element.Name)) {
        //        return GetCallsFromBlockParent(element);
        //    }
        //    return GetMethodCallsFromBlockElement(element);
        //}

        //private IEnumerable<XElement> GetCallsFromBlockParent(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    if(null == block)
        //        return Enumerable.Empty<XElement>();
        //    return GetMethodCallsFromBlockElement(block);
        //}

        //private IEnumerable<XElement> GetCallsFromConstructorElement(XElement element) {
        //    var blockCalls = GetCallsFromBlockParent(element);
        //    if(element.Element(SRC.MemberList) != null) {
        //        var memberListCalls = from call in element.Element(SRC.MemberList).Elements(SRC.Call)
        //                              select call;
        //        return memberListCalls.Concat(blockCalls);
        //    }
        //    return blockCalls;
        //}

        //private IEnumerable<XElement> GetMethodCallsFromBlockElement(XElement container) {
        //    var methodCalls = from child in container.Elements()
        //                      where !ContainerElementNames.Contains(child.Name)
        //                      from call in child.Descendants(SRC.Call)
        //                      select call;
        //    return methodCalls;
        //}

        //#endregion get method calls from scope

        //#region get declarations from scope

        ///// <summary>
        ///// Gets all of the variable declarations for this block.
        ///// </summary>
        ///// <param name="container">The type container</param>
        ///// <returns>An enumerable of all the declaration XElements.</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromBlockElement(XElement container) {
        //    if(null == container)
        //        return Enumerable.Empty<XElement>();
        //    var declarations = from stmtElement in container.Elements(SRC.DeclarationStatement)
        //                       let declElement = stmtElement.Element(SRC.Declaration)
        //                       select declElement;
        //    return declarations;
        //}

        ///// <summary>
        ///// Gets all of the variable declarations for this catch block. It finds the variable
        ///// declarations in <see cref="ABB.SrcML.SRC.ParameterList"/>.
        ///// </summary>
        ///// <param name="container">The catch container</param>
        ///// <returns>An enumerable of all the declaration XElements.</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromCatchElement(XElement container) {
        //    var declarations = from parameter in container.Elements(SRC.Parameter)
        //                       let declElement = parameter.Element(SRC.Declaration)
        //                       let typeElement = declElement.Element(SRC.Type)
        //                       where typeElement != null
        //                       where !typeElement.Elements(TYPE.Modifier).Any()
        //                       select declElement;
        //    return declarations;
        //}

        ///// <summary>
        ///// Gets the declaration elements from an element
        ///// </summary>
        ///// <param name="element">The element to search</param>
        ///// <returns>All of the declaration elements from an element</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromElement(XElement element) {
        //    if(null == element)
        //        return Enumerable.Empty<XElement>();

        //    IEnumerable<XElement> declarationElements;

        //    if(SRC.Block == element.Name || SRC.Unit == element.Name) {
        //        declarationElements = GetDeclarationsFromBlockElement(element);
        //    } else if(SRC.Catch == element.Name) {
        //        declarationElements = GetDeclarationsFromCatchElement(element);
        //    } else if(SRC.For == element.Name) {
        //        declarationElements = GetDeclarationsFromForElement(element);
        //    } else if(MethodElementNames.Contains(element.Name)) {
        //        declarationElements = GetDeclarationsFromMethodElement(element);
        //    } else if(TypeElementNames.Contains(element.Name)) {
        //        declarationElements = GetDeclarationsFromTypeElement(element);
        //    } else {
        //        declarationElements = Enumerable.Empty<XElement>();
        //    }

        //    return declarationElements;
        //}

        ///// <summary>
        ///// Gets all of the variable declarations for this for loop. It finds the variable
        ///// declaration in the <see cref="ABB.SrcML.SRC.Init"/> statement.
        ///// </summary>
        ///// <param name="container">The type container</param>
        ///// <returns>An enumerable of all the declaration XElements.</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromForElement(XElement container) {
        //    var declarations = from declElement in container.Element(SRC.Init).Elements(SRC.Declaration)
        //                       select declElement;
        //    return declarations;
        //}

        ///// <summary>
        ///// Gets all of the variable declarations for this method. It finds the variable
        ///// declarations in the child block.
        ///// </summary>
        ///// <param name="container">The method container</param>
        ///// <returns>An enumerable of all the declaration XElements.</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromMethodElement(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    return GetDeclarationsFromBlockElement(block);
        //}

        ///// <summary>
        ///// Gets all of the variable declarations for this type. It finds the variable declarations
        ///// in the child block.
        ///// </summary>
        ///// <param name="container">The type container</param>
        ///// <returns>An enumerable of all the declaration XElements.</returns>
        //protected virtual IEnumerable<XElement> GetDeclarationsFromTypeElement(XElement container) {
        //    var block = container.Element(SRC.Block);
        //    foreach(var declElement in GetDeclarationsFromBlockElement(block)) {
        //        yield return declElement;
        //    }
        //}

        //#endregion get declarations from scope

        #region access modifiers

        /// <summary>
        /// Gets the access modifier for this method. For Java and C#, a "specifier" tag is placed
        /// in either the methodElement, or the typeElement in the method.
        /// </summary>
        /// <param name="methodElement">The methodElement</param>
        /// <returns>The first specifier encountered. If none, it returns see
        /// cref="AccessModifier.None"/></returns>
        protected virtual AccessModifier GetAccessModifierForMethod(XElement methodElement) {
            if(methodElement == null)
                throw new ArgumentNullException("methodElement");
            if(!MethodElementNames.Contains(methodElement.Name))
                throw new ArgumentException(string.Format("Not a valid methodElement: {0}", methodElement.Name), "methodElement");

            var specifierContainer = methodElement.Element(SRC.Type);
            if(null == specifierContainer) {
                specifierContainer = methodElement;
            }
            return GetAccessModifier(specifierContainer);
        }

        /// <summary>
        /// Gets the access modifier for the given type
        /// </summary>
        /// <param name="typeElement">The type XElement</param>
        /// <returns>The access modifier for the type.</returns>
        protected virtual AccessModifier GetAccessModifierForType(XElement typeElement) {
            if(typeElement == null)
                throw new ArgumentNullException("typeElement");
            if(!TypeElementNames.Contains(typeElement.Name))
                throw new ArgumentException(string.Format("Not a valid typeElement: {0}", typeElement.Name), "typeElement");

            return GetAccessModifier(typeElement);
        }

        /// <summary>
        /// Determines the access modifier used within a type use element, i.e. SRC.Type
        /// </summary>
        /// <param name="typeElement">A SRC.Type element</param>
        /// <returns>An AccessModifier based on the specifier elements in the type use.</returns>
        protected virtual AccessModifier GetAccessModifierFromTypeUseElement(XElement typeElement) {
            if(typeElement == null)
                throw new ArgumentNullException("typeElement");
            if(typeElement.Name != SRC.Type)
                throw new ArgumentException("Must be a SRC.Type element", "typeElement");

            return GetAccessModifier(typeElement);
        }

        /// <summary>
        /// Determines the access modifier used within the given element. This element must have SRC.Specifier element(s) as its children.
        /// </summary>
        /// <param name="element">An element that may contain children of type SRC.Specifer.</param>
        /// <returns>The access modifier used.</returns>
        protected virtual AccessModifier GetAccessModifier(XElement element) {
            if(element == null)
                throw new ArgumentNullException("element");

            var accessModifierMap = new Dictionary<string, AccessModifier>()
                                    {
                                        {"public", AccessModifier.Public},
                                        {"private", AccessModifier.Private},
                                        {"protected", AccessModifier.Protected},
                                        {"internal", AccessModifier.Internal}
                                    };

            //specifiers might include non-access keywords like "partial" or "static"
            //get only specifiers that are in the accessModiferMap
            var accessSpecifiers = element.Elements(SRC.Specifier).Select(e => e.Value).Where(s => accessModifierMap.ContainsKey(s)).ToList();
            AccessModifier result;
            if(!accessSpecifiers.Any()) {
                result = AccessModifier.None;
            } else if(accessSpecifiers.Count == 2 && accessSpecifiers.Contains("protected") && accessSpecifiers.Contains("internal")) {
                result = AccessModifier.ProtectedInternal;
            } else {
                result = accessModifierMap[accessSpecifiers.First()];
            }
            return result;
        }

        #endregion access modifiers

        #region parse literal types

        /// <summary>
        /// Gets the type for a boolean literal
        /// </summary>
        /// <param name="literalValue">The literal value to parse</param>
        /// <returns>The type name</returns>
        protected abstract string GetTypeForBooleanLiteral(string literalValue);

        /// <summary>
        /// Gets the type for a character literal
        /// </summary>
        /// <param name="literalValue">the literal value to parse</param>
        /// <returns>The type name</returns>
        protected abstract string GetTypeForCharacterLiteral(string literalValue);

        /// <summary>
        /// Gets the type of the literal element
        /// </summary>
        /// <param name="kind">The literal kind</param>
        /// <param name="literalValue">The value</param>
        /// <returns>The name of this type</returns>
        protected virtual string GetTypeForLiteralValue(LiteralKind kind, string literalValue) {
            switch(kind) {
                case LiteralKind.Boolean:
                    return GetTypeForBooleanLiteral(literalValue);

                case LiteralKind.Character:
                    return GetTypeForCharacterLiteral(literalValue);

                case LiteralKind.Number:
                    return GetTypeForNumberLiteral(literalValue);

                case LiteralKind.String:
                    return GetTypeForStringLiteral(literalValue);
            }
            return String.Empty;
        }

        /// <summary>
        /// Gets the type for a number literal
        /// </summary>
        /// <param name="literalValue">The literal value to parse</param>
        /// <returns>The type name</returns>
        protected abstract string GetTypeForNumberLiteral(string literalValue);

        /// <summary>
        /// Gets the type for a string literal
        /// </summary>
        /// <param name="literalValue">The literal value to parse</param>
        /// <returns>The type name</returns>
        protected abstract string GetTypeForStringLiteral(string literalValue);

        ///// <summary>
        ///// Parses a literal use element
        ///// </summary>
        ///// <param name="literalElement">The literal element to parse</param>
        ///// <param name="context">The parser context</param>
        ///// <returns>A literal use object</returns>
        //protected virtual LiteralUse ParseLiteralElement(XElement literalElement, ParserContext context) {
        //    if(literalElement == null)
        //        throw new ArgumentNullException("literalElement");
        //    if(literalElement.Name != LIT.Literal)
        //        throw new ArgumentException("should be a literal", "literalElement");

        //    var kind = LiteralUse.GetLiteralKind(literalElement);
        //    string typeName = string.Empty;

        //    var use = new LiteralUse() {
        //        Kind = kind,
        //        Location = context.CreateLocation(literalElement),
        //        Name = GetTypeForLiteralValue(kind, literalElement.Value),
        //        ParentScope = context.CurrentStatement,
        //    };

        //    return use;
        //}

        #endregion parse literal types

        #region utilities

        /// <summary>
        /// Checks to see if this callElement is a reference container
        /// </summary>
        /// <param name="element">The callElement to check</param>
        /// <returns>True if this is a reference container; false otherwise</returns>
        protected virtual bool ContainerIsReference(XElement element) {
            return (element != null && ContainerReferenceElementNames.Contains(element.Name));
        }

        /// <summary>
        /// Gets the filename for the given file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit. <c>fileUnit.Name</c> must be /c></param>
        /// <returns>The file path represented by this
        /// <paramref name="fileUnit"/></returns>
        protected virtual string GetFileNameForUnit(XElement fileUnit) {
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("element must be a unit", "fileUnit");

            var fileNameAttribute = fileUnit.Attribute("filename");

            if(null != fileNameAttribute)
                return fileNameAttribute.Value;
            return String.Empty;
        }

        /// <summary>
        /// Gets the name for the method callElement
        /// </summary>
        /// <param name="methodElement">the method callElement to get the name for</param>
        /// <returns>The name of the method</returns>
        protected virtual string GetNameForMethod(XElement methodElement) {
            var nameElement = methodElement.Element(SRC.Name);

            if(null == nameElement)
                return string.Empty;
            return NameHelper.GetLastName(nameElement);
        }

        /// <summary>
        /// Gets the name for the type element
        /// </summary>
        /// <param name="typeElement">The type element to get the name for</param>
        /// <returns>The name of the type</returns>
        protected virtual string GetNameForType(XElement typeElement) {
            var name = typeElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return NameHelper.GetLastName(name);
        }

        /// <summary>
        /// Gets all of the text nodes that are children of the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>An enumerable of the XText elements for
        /// <paramref name="element"/></returns>
        protected IEnumerable<XText> GetTextNodes(XElement element) {
            var textNodes = from node in element.Nodes()
                            where node.NodeType == XmlNodeType.Text
                            let text = node as XText
                            select text;
            return textNodes;
        }

        /// <summary>
        /// Get the children of <paramref name="element"/> that are expressions.
        /// These may be elements of type SRC.Expression, SRC.Declaration or SRC.FunctionDeclaration.
        /// </summary>
        /// <param name="element">The parent element from which to find the child expressions.</param>
        /// <returns>An enumerable of the expression elements, or an empty enumerable if none is found.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="element"/> is null.</exception>
        protected virtual IEnumerable<XElement> GetChildExpressions(XElement element) {
            if(element == null)
                throw new ArgumentNullException("element");

            return element.Elements().Where(e => e.Name == SRC.Expression || e.Name == SRC.Declaration || e.Name == SRC.FunctionDeclaration);
        }

        /// <summary>
        /// Get the first child of <paramref name="element"/> that is an expression.
        /// This might be an element of type SRC.Expression, SRC.Declaration or SRC.FunctionDeclaration.
        /// </summary>
        /// <param name="element">The parent element from which to find the child expression.</param>
        /// <returns>The first expression element, or null if none is found.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="element"/> is null.</exception>
        protected virtual XElement GetFirstChildExpression(XElement element) {
            if(element == null)
                throw new ArgumentNullException("element");

            return GetChildExpressions(element).First();
        }

        /// <summary>
        /// Creates a new expression containing the components of expression <paramref name="a"/> and the components of expression <paramref name="b"/>.
        /// If either expression does not have components, the root expression itself will be included instead.
        /// The expressions must be adjacent to each other in the original srcml.
        /// </summary>
        protected virtual Expression MergeExpressions(Expression a, Expression b) {
            if(a == null) { throw new ArgumentNullException("a"); }
            if(b == null) { throw new ArgumentNullException("b"); }

            var aIsContainer = a.GetType() == typeof(Expression);
            var bIsContainer = b.GetType() == typeof(Expression);

            var newExpression = new Expression() {ProgrammingLanguage = ParserLanguage};
            if(aIsContainer) {
                newExpression.Location = a.Location;
            } else if(bIsContainer) {
                newExpression.Location = b.Location;
            } else {
                //both are not containers
                //set location to the first, although this won't be entirely accurate
                newExpression.Location = a.Location;
            }

            if(aIsContainer) {
                newExpression.AddComponents(a.Components);
            } else {
                newExpression.AddComponent(a);
            }
            if(bIsContainer) {
                newExpression.AddComponents(b.Components);
            } else {
                newExpression.AddComponent(b);
            }

            return newExpression;
        }
        #endregion utilities
    }
}