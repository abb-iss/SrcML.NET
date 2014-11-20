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

        private static readonly HashSet<XName> NotImplementedStatements = new HashSet<XName>() {
            SRC.Typedef, SRC.Macro, SRC.Escape, SRC.Template,
            SRC.Synchronized, SRC.Attribute, SRC.Unchecked, SRC.Asm
        };
        private static readonly HashSet<XName> NotImplementedExpressions = new HashSet<XName>() {SRC.SizeOf, SRC.Macro, SRC.Escape};

        /// <summary>
        /// Creates a new abstract code parser object. Should only be called by child classes.
        /// </summary>
        protected AbstractCodeParser() {
            MethodElementNames = new HashSet<XName>(new XName[] { SRC.Function, SRC.Constructor, SRC.Destructor,
                                                                  SRC.FunctionDeclaration, SRC.ConstructorDeclaration, SRC.DestructorDeclaration });
            NamespaceElementNames = new HashSet<XName>(new XName[] { SRC.Namespace });
            VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement });
            ContainerReferenceElementNames = new HashSet<XName>(new XName[] { SRC.ClassDeclaration, SRC.StructDeclaration, SRC.UnionDeclaration,
                                                                              SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration });
            UnknownLog = null;
        }

        /// <summary> Returns the XName that represents an import or alias statement. </summary>
        public XName AliasElementName { get; protected set; }

        /// <summary> Returns the XNames that represent reference elements (such as function_decl and class_decl) </summary>
        public HashSet<XName> ContainerReferenceElementNames { get; protected set; }

        /// <summary> Returns the XNames that represent types for this language. </summary>
        public HashSet<XName> MethodElementNames { get; protected set; }

        /// <summary> Returns the XNames that represent namespaces for this language. </summary>
        public HashSet<XName> NamespaceElementNames { get; protected set; }

        /// <summary> Returns the XNames that represent types for this language. </summary>
        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary> Returns the XNames that represent variable declarations for this language. </summary>
        public HashSet<XName> VariableDeclarationElementNames { get; protected set; }

        /// <summary> Returns the Language that this parser supports. </summary>
        public abstract Language ParserLanguage { get; }

        /// <summary>
        /// Writer to log unknown elements to. If null no logging is done
        /// </summary>
        public TextWriter UnknownLog {
            get { return _synchronizedErrorLog; }
            set { _synchronizedErrorLog = (null == value ? null : TextWriter.Synchronized(value)); }
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

        /// <summary>
        /// Logs the given element as unknown. This will do nothing if <see cref="UnknownLog"/> is null.
        /// </summary>
        /// <param name="element">The unknown element</param>
        /// <param name="context">The parser context</param>
        protected void LogUnknown(XElement element, ParserContext context) {
            LogUnknown(element, context, null);
        }

        /// <summary>
        /// Logs the given element as unknown along with an optional message. This will do nothing if <see cref="UnknownLog"/> is null.
        /// </summary>
        /// <param name="element">The unknown element</param>
        /// <param name="context">The parser context</param>
        /// <param name="message">An optional message</param>
        protected void LogUnknown(XElement element, ParserContext context, string message) {
            if(null != UnknownLog) {
                UnknownLog.Write("{0}({1},{2}) Unexpected {3}", context.FileName, element.GetSrcLineNumber(), element.GetSrcLinePosition(), element.Name, message);
                if(!String.IsNullOrWhiteSpace(message)) {
                    UnknownLog.WriteLine(" ({0})", message);
                }
            }
        }


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
                } else if(element.Name == SRC.Lock) {
                    stmt = ParseLockElement(element, context);
                } else if(element.Name == SRC.Comment) {
                    // do nothing. we are ignoring comments
                } else if(element.Name == SRC.Package) {
                    //do nothing. This is already handled in JavaCodeParser.ParseUnitElement()
                } else if(element.Name.Namespace == CPP.NS) {
                    //do nothing. skip any cpp preprocessor macros
                } else if(NotImplementedStatements.Contains(element.Name)) {
                    //do nothing. These are known and we're skipping them for now.
                } else {
                    LogUnknown(element, context, "ParseStatement");
                }

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
        /// <see cref="NamespaceDefinition">namespace definition</see> object.
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

            var methodIsDestructor = (methodElement.Name == SRC.Destructor || methodElement.Name == SRC.DestructorDeclaration);
            var methodDefinition = new MethodDefinition() {
                Name = String.Format("{0}{1}", (methodIsDestructor ? "~" : String.Empty), GetNameForMethod(methodElement)),
                IsConstructor = (methodElement.Name == SRC.Constructor || methodElement.Name == SRC.ConstructorDeclaration),
                IsDestructor = methodIsDestructor,
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

            //add the constructor initializer list, if any
            var memberListElement = methodElement.Element(SRC.MemberList);
            if(memberListElement != null) {
                foreach(var callElement in memberListElement.Elements(SRC.Call)) {
                    var call = ParseCallElement(callElement, context) as MethodCall;
                    if(call != null) {
                        methodDefinition.AddInitializer(call);
                    }
                }
            }

            //Add the method's parameters
            var parameters = from paramElement in GetParametersFromMethodElement(methodElement)
                             select ParseParameterElement(paramElement, context);
            methodDefinition.AddMethodParameters(parameters.ToList());
            
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

            var declElement = paramElement.Elements().FirstOrDefault(e => e.Name == SRC.Declaration || e.Name == SRC.FunctionDeclaration);
            if(declElement == null) {
                return new VariableDeclaration() {
                    Name = string.Empty, 
                    Location = context.CreateLocation(paramElement),
                    ProgrammingLanguage = ParserLanguage
                };
            } else {
                return ParseDeclarationElement(declElement, context);
            }
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
        /// <param name="whileElement">The SRC.While element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="WhileStatement"/> corresponding to <paramref name="whileElement"/>.</returns>
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
                } else if(forChild.Name == SRC.Condition) {
                    //fill in condition
                    var expElement = GetFirstChildExpression(forChild);
                    if(expElement != null) {
                        forStmt.Condition = ParseExpression(expElement, context);
                    }
                } else if(forChild.Name == SRC.Increment) {
                    //fill in incrementer
                    var expElement = GetFirstChildExpression(forChild);
                    if(expElement != null) {
                        forStmt.Incrementer = ParseExpression(expElement, context);
                    }
                } else if(forChild.Name == SRC.Block) {
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

        /// <summary>
        /// Creates a ForeachStatement from the given element.
        /// </summary>
        /// <param name="foreachElement">The SRC.Foreach element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A ForeachStatement corresponding to foreachElement. </returns>
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
                } else if(child.Name == SRC.Block) {
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

        /// <summary>
        /// Creates a <see cref="DoWhileStatement"/> object for <paramref name="doElement"/>.
        /// </summary>
        /// <param name="doElement">The SRC.Do element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="DoWhileStatement"/> corresponding to <paramref name="doElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="SwitchStatement"/> object for <paramref name="switchElement"/>.
        /// </summary>
        /// <param name="switchElement">The SRC.Switch element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="SwitchStatement"/> corresponding to <paramref name="switchElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="CaseStatement"/> object for <paramref name="caseElement"/>.
        /// </summary>
        /// <param name="caseElement">The SRC.Case or SRC.Default element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="CaseStatement"/> corresponding to <paramref name="caseElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="BreakStatement"/> object for <paramref name="breakElement"/>.
        /// </summary>
        /// <param name="breakElement">The SRC.Break element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="BreakStatement"/> corresponding to <paramref name="breakElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="ContinueStatement"/> object for <paramref name="continueElement"/>.
        /// </summary>
        /// <param name="continueElement">The SRC.Continue element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="ContinueStatement"/> corresponding to <paramref name="continueElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="GotoStatement"/> object for <paramref name="gotoElement"/>.
        /// </summary>
        /// <param name="gotoElement">The SRC.Goto element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="GotoStatement"/> corresponding to <paramref name="gotoElement"/>.</returns>
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
                gotoStmt.Content = ParseExpression(gotoElement.Elements().First(), context);
            }

            return gotoStmt;
        }

        /// <summary>
        /// Creates a <see cref="LabelStatement"/> object for <paramref name="labelElement"/>.
        /// </summary>
        /// <param name="labelElement">The SRC.Label element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="LabelStatement"/> corresponding to <paramref name="labelElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="ReturnStatement"/> object for <paramref name="returnElement"/>.
        /// </summary>
        /// <param name="returnElement">The SRC.Return element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="ReturnStatement"/> corresponding to <paramref name="returnElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="ThrowStatement"/> object for <paramref name="throwElement"/>.
        /// </summary>
        /// <param name="throwElement">The SRC.Throw element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="ThrowStatement"/> corresponding to <paramref name="throwElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="TryStatement"/> object for <paramref name="tryElement"/>.
        /// </summary>
        /// <param name="tryElement">The SRC.Try element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="TryStatement"/> corresponding to <paramref name="tryElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="CatchStatement"/> object for <paramref name="catchElement"/>.
        /// </summary>
        /// <param name="catchElement">The SRC.Catch element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="CatchStatement"/> corresponding to <paramref name="catchElement"/>.</returns>
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
                if(catchChild.Name == SRC.ParameterList) {
                    //add the catch parameter
                    var paramElement = catchChild.Element(SRC.Parameter);
                    if(paramElement != null) {
                        catchStmt.Parameter = ParseParameterElement(paramElement, context);
                    }
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

        /// <summary>
        /// Creates a <see cref="Statement"/> object for <paramref name="stmtElement"/>.
        /// The expression contained within <paramref name="stmtElement"/> will be parsed and placed in 
        /// Statement.Content.
        /// </summary>
        /// <param name="stmtElement">The SRC.ExpressionStatement element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="Statement"/> corresponding to <paramref name="stmtElement"/>.</returns>
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

        /// <summary>
        /// Creates a <see cref="Statement"/> object for <paramref name="stmtElement"/>.
        /// The expression contained within <paramref name="stmtElement"/> will be parsed and placed in 
        /// Statement.Content.
        /// </summary>
        /// <param name="stmtElement">The SRC.DeclarationStatement element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="DeclarationStatement"/> corresponding to <paramref name="stmtElement"/>.
        /// The return type is <see cref="Statement"/> so that subclasses can return another type, as necessary. </returns>
        protected virtual Statement ParseDeclarationStatementElement(XElement stmtElement, ParserContext context) {
            if(stmtElement == null)
                throw new ArgumentNullException("stmtElement");
            if(stmtElement.Name != SRC.DeclarationStatement)
                throw new ArgumentException("Must be a SRC.DeclarationStatement element", "stmtElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var stmt = new DeclarationStatement() {
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

        /// <summary>
        /// Parses the given <paramref name="lockElement"/> and creates a <see cref="LockStatement"/> from it.
        /// </summary>
        /// <param name="lockElement">The SRC.Lock element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A LockStatement created from the given lockElement.</returns>
        protected virtual LockStatement ParseLockElement(XElement lockElement, ParserContext context) {
            if(lockElement == null)
                throw new ArgumentNullException("lockElement");
            if(lockElement.Name != SRC.Lock)
                throw new ArgumentException("Must be a SRC.Lock element", "lockElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var lockStmt = new LockStatement() {ProgrammingLanguage = ParserLanguage};
            lockStmt.AddLocation(context.CreateLocation(lockElement));

            foreach(var child in lockElement.Elements()) {
                if(child.Name == SRC.Expression) {
                    lockStmt.LockExpression = ParseExpression(child, context);
                } else if(child.Name == SRC.Block) {
                    var blockStatements = child.Elements().Select(e => ParseStatement(e, context));
                    lockStmt.AddChildStatements(blockStatements);
                } else {
                    lockStmt.AddChildStatement(ParseStatement(child, context));
                }
            }

            return lockStmt;
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
            return ParseExpression<NameUse>(element, context);
        }
        
        /// <summary>
        /// Creates an Expression from the given element.
        /// </summary>
        /// <typeparam name="T">The type of use to use when parsing name elements.</typeparam>
        /// <param name="element">The element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An Expression parsed from the element.</returns>
        protected virtual Expression ParseExpression<T>(XElement element, ParserContext context) where T : NameUse, new() {
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
                    exp = ParseNameUseElement<T>(element, context);
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
                } else if(element.Name == SRC.Class && ParserLanguage == Language.Java) {
                    //anonymous class, skip
                    //TODO: add parsing for anonymous classes in Java
                } else if(element.Name.Namespace == CPP.NS) {
                    //do nothing. skip any cpp preprocessor macros
                } else if(NotImplementedExpressions.Contains(element.Name)) {
                    //skip. These are known and we're skipping them for now.
                } else {
                    LogUnknown(element, context, "ParseExpression");
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
            return ParseExpression<NameUse>(elements, context);
        }

        /// <summary>
        /// Parses (possibly) multiple expression component elements, and combines them into an Expression. 
        /// All the elements must have the same parent.
        /// </summary>
        /// <typeparam name="T">The use type to use when parsing name elements.</typeparam>
        /// <param name="elements">The expression component elements to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An Expression with each of the parsed elements as its components. 
        /// If <paramref name="elements"/> contains only a single value, the result will be the same as if it were parsed directly..</returns>
        protected virtual Expression ParseExpression<T>(IEnumerable<XElement> elements, ParserContext context) where T : NameUse, new() {
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
                var exp = ParseExpression<T>(element, context);
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

        /// <summary>
        /// Creates an <see cref="Expression"/> object for <paramref name="expElement"/>.
        /// </summary>
        /// <param name="expElement">The SRC.Expression element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="Expression"/> corresponding to <paramref name="expElement"/>.</returns>
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
            if(!(declElement.Name == SRC.Declaration || declElement.Name == SRC.FunctionDeclaration))
                throw new ArgumentException("Must be a SRC.Declaration or SRC.FunctionDeclaration element", "declElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var varDecl = new VariableDeclaration {
                Location = context.CreateLocation(declElement), 
                ProgrammingLanguage = ParserLanguage
            };

            var nameElement = declElement.Elements(SRC.Name).LastOrDefault();
            if(nameElement != null) {
                varDecl.Name = NameHelper.GetLastName(nameElement);
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

            var rangeElement = declElement.Element(SRC.Range);
            if(rangeElement != null) {
                var expElement = GetFirstChildExpression(rangeElement);
                if(expElement != null) {
                    varDecl.Range = ParseExpression(rangeElement, context);
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
            return ParseNameUseElement<NameUse>(nameElement, context);
        }

        /// <summary>
        /// Creates a use object from the given name element.
        /// </summary>
        /// <typeparam name="T">The type of use to use for the name element. This must inherit from NameUse.</typeparam>
        /// <param name="nameElement">The SRC.Name element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A use corresponding to <paramref name="nameElement"/>.</returns>
        protected virtual Expression ParseNameUseElement<T>(XElement nameElement, ParserContext context) where T : NameUse, new() {
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
                return ParseExpression<T>(nameElement.Elements(), context);
            }

            //no children
            var nu = new T() {
                Location = context.CreateLocation(nameElement, true),
                ProgrammingLanguage = ParserLanguage,
                Name = NameHelper.GetLastName(nameElement),
            };

            return nu;
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
        /// Creates a <see cref="TypeUse"/> object for <paramref name="typeUseElement"/>.
        /// </summary>
        /// <param name="typeUseElement">The type use element to parse. This must be a SRC.Type or SRC.Name element.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>A <see cref="TypeUse"/> corresponding to <paramref name="typeUseElement"/>.</returns>
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
                prefix.AddComponent(ParseExpression<TypeContainerUse>(part, context));
            }
            return prefix;
        }

        /// <summary>
        /// Creates an <see cref="Expression"/> object for <paramref name="callElement"/>.
        /// </summary>
        /// <param name="callElement">The SRC.Call element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>An <see cref="Expression"/> corresponding to <paramref name="callElement"/>.</returns>
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
            if(mc.Name == "super" && mc.ProgrammingLanguage == Language.Java) {
                mc.IsConstructor = true;
            }

            //parse the arguments to the method call
            var argList = callElement.Element(SRC.ArgumentList);
            if(argList != null) {
                foreach(var argElement in argList.Elements(SRC.Argument)) {
                    var expElement = GetFirstChildExpression(argElement);
                    if(expElement != null) {
                        var exp = ParseExpression(expElement, context);
                        if(exp == null) {
                            //we still want to record the argument, even if we can't parse it properly
                            exp = new Expression() {
                                ProgrammingLanguage = ParserLanguage,
                                Location = context.CreateLocation(expElement)
                            };
                        }
                        mc.AddArgument(exp);
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
                Text = literalElement.Value,
                Kind = LiteralUse.GetLiteralKind(literalElement)
            };

            return litUse;
        }

        /// <summary>
        /// Creates an <see cref="Expression"/> object for <paramref name="nameElement"/>.
        /// This returns an Expression rather than a VariableUse because any calling expression nested within the
        /// nameElement will be parsed and added to an Expression along with a VariableUse.
        /// </summary>
        /// <param name="nameElement">The SRC.Name element to parse.</param>
        /// <param name="context">The context to use.</param>
        /// <returns>An <see cref="Expression"/> corresponding to <paramref name="nameElement"/>.</returns>
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
                    var expElement = GetFirstChildExpression(indexElement);
                    if(expElement != null) {
                        vu.Index = ParseExpression(expElement, context);
                    }
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
                if(callingExp != null) {
                    return MergeExpressions(callingExp, vu);
                }
            }
            
            return vu;
        }

        #endregion Parse expression elements

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

            return GetChildExpressions(element).FirstOrDefault();
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