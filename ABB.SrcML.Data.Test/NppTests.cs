/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABB.SrcML.Data.Test
{
    [TestClass]
    public class NppTests
    {
        public static string NppXmlPath = @"..\..\..\ABB.SrcML.Data.Test\XmlInput\npp-5.9.4.xml";

        [ClassInitialize()]
        public static void Init(TestContext context)
        {
            DbHelper.AddArchiveToDb(NppXmlPath);
        }

        [TestMethod]
        public void PrintNppStatsTest()
        {

            DbHelper.GetStatsFromDb(NppXmlPath);
        }

        [TestMethod]
        public void NppTypeTest()
        {
            List<string> testTypeNames = new List<string>() { "sessionInfo", "GRPICONDIRENTRY", "Notepad_plus" };
            TypeTester.TestTypeUse(NppXmlPath, testTypeNames);
        }

        [TestMethod]
        public void NppGlobalDeclarationTest()
        {
            List<string> useNames = new List<string>() { "TAB_DRAWTOPBAR", "TAB_DRAGNDROP", "USERMSG" };
            DeclarationTester.TestGlobalVariables(NppXmlPath, useNames);
        }

        [TestMethod]
        public void NppLocalDeclarationTest()
        {
            List<string> useNames = new List<string>() { "nbUserCommand", "hMenu", "_mainEditView" };
            DeclarationTester.TestLocalVariables(NppXmlPath, useNames);
        }

        [TestMethod]
        public void NppCallTest()
        {
            List<string> testCalls = new List<string>() { "SetSavePoint", "DeleteMarkFromHandle", "DeleteAllMarks", "LineFromHandle", "LineEnd" };
            MethodCallTester.TestMethodCalls(NppXmlPath, testCalls);
        }

        [TestMethod]
        public void NppCallGraphTest()
        {
            var testData = new List<Tuple<string, string, bool>>() {
                new Tuple<string,string,bool>("FunctionSignature(EncodingMapper::getEncodingFromString,1):129", "FunctionSignature(isInListA,2):77", true),
                new Tuple<string,string,bool>("FunctionSignature(StaticDialog::display,1):36", "FunctionSignature(Window::display,1):35", true),
                new Tuple<string,string,bool>("FunctionSignature(StaticDialog::display,1):36", "FunctionSignature(StaticDialog::display,1):36", false),
                new Tuple<string,string,bool>("FunctionSignature(ColourPopup::create,1):33", "FunctionSignature(Window::getClientRect,1):57", true),
                new Tuple<string,string,bool>("FunctionSignature(FileManager::moveFile,2):575", "FunctionSignature(Buffer::setFileName,2):92", true),
                new Tuple<string,string,bool>("FunctionSignature(FileManager::saveBuffer,3):589", "FunctionSignature(Buffer::setFileName,2):92", true),
                new Tuple<string,string,bool>("FunctionSignature(AutoCompletion::showAutoComplete,0):31", "FunctionSignature(ScintillaEditView::getCurrentLineNumber,0):394", true),
            };
            
            CallGraphTester.TestCallGraph(NppXmlPath, testData);
        }
    }
}
