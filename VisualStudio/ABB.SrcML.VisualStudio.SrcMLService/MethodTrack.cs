/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Xiao Qu (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ABB.SrcML;
using ABB.SrcML.Data;
using ABB.SrcML.VisualStudio;

namespace ABB.VisualStudio
{
    /// <summary>
    /// current cursor position defined by (filename, line number, column number)
    /// </summary>
    public struct CursorPos
    {
        public string _file;
        public int _line, _col;

        /// <summary>
        /// constructing a cursor position 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="line"></param>
        /// <param name="col"></param>
        public CursorPos(string file, int line, int col)
        {
            _file = file;
            _line = line;
            _col = col;
        }

        /// <summary>
        /// set curosr position
        /// </summary>
        /// <param name="file"></param>
        /// <param name="line"></param>
        /// <param name="col"></param>
        public void SetCursurPos(string file, int line, int col)
        {
            _file = file;
            _line = line;
            _col = col;
        }
    }

    class MethodTrack: IMethodTrackService, SMethodTrackService{
        
        private IServiceProvider _serviceProvider;

        private ISrcMLDataService _dataService;

        private ISrcMLGlobalService _srcmlService;

        private ICursorMonitorService _cursorMonitor;

        public event EventHandler<MethodEventRaisedArgs> MethodUpdatedEvent;

        private CursorPos _currentCursor;
        private Method _currentMethod;
        
        private List<Method> _navigatedMethods; 
        
        public Method CurrentMethod
        {
            get { return _currentMethod; }
        }

        public int CurrentLineNumber
        {
            get { return _currentCursor._line; }
        }

        public int CurrentColumnNumber
        {
            get { return _currentCursor._col;  }
        }

        public List<Method> NavigatedMethods
        {
            get { return _navigatedMethods; }
        }

       public MethodTrack(IServiceProvider sp)
       {
           _serviceProvider = sp;
           _dataService = sp.GetService(typeof(SSrcMLDataService)) as ISrcMLDataService;
           _srcmlService = sp.GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
           _cursorMonitor = sp.GetService(typeof(SCursorMonitorService)) as ICursorMonitorService;
           _currentMethod = new Method();
           _navigatedMethods = new List<Method>();
           _currentCursor = new CursorPos("", 0, 0);
          
           if (null != _cursorMonitor)
               _cursorMonitor.PropertyChanged += OnCursorMoving;
           if(null != _srcmlService)
                _srcmlService.SourceFileChanged += OnFileChanged;
        }

     
       /// <summary>
       /// Get a method object from (file, line number, column number)
       /// </summary>
       /// <param name="fileName"></param>
       /// <param name="lineNum"></param>
       /// <param name="colNum"></param>
       /// <returns>if the cursor is in a method, return true; otherwise return false</returns>
       private bool GetMethod(string fileName, int lineNum, int colNum)
       {
           bool isInMethod = false;

           if (null == _dataService)
               return false;
             
           var data = _dataService.CurrentDataArchive.GetData(fileName);
           
           if (null != data)
           {
               var methods = data.GetDescendants<MethodDefinition>();
               foreach (var methoddef in methods)
               {
                   SourceLocation cursorPos = new SourceLocation(fileName, lineNum, colNum);
                   if (methoddef.ContainsLocation(cursorPos))
                   {
                       isInMethod = true;
                       _currentMethod = new Method(methoddef);
                       break;
                       //assert(outMethod.filePath.equals(fileName))
                   }
               }
           }

           return isInMethod;
       }

        /// <summary>
        /// Get all methods in a source file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="allMethods"></param>
        private void GetAllMethods(string fileName, out List<Method> allMethods)
        {
            allMethods = new List<Method>();

            if (null == _dataService)
                return;

            var data = _dataService.CurrentDataArchive.GetData(fileName);

            if (null != data)
            {
                var methods = data.GetDescendants<MethodDefinition>();
                allMethods.AddRange(methods.Select(methoddef => new Method(methoddef)));
            }
        }
        
       #region cursor moving
       internal void OnCursorMoving(object sender, PropertyChangedEventArgs e)
       {
           var cursorMonitor = (sender as CursorMonitor);
           if (null != cursorMonitor)
           {
               int curLine = cursorMonitor.CurrentLineNumber;
               int curColumn = cursorMonitor.CurrentColumnNumber;
               string fileName = cursorMonitor.CurrentFileName;

               _currentCursor.SetCursurPos(fileName, curLine, curColumn);

               //System.Diagnostics.Debugger.Launch(); //for integration test debugging only
               bool isMethod = GetMethod(fileName, curLine, curColumn);
               if (isMethod)
               {
                   AddMethodToHistoryList(CurrentMethod);
                   OnMethodUpdatedEvent(new MethodEventRaisedArgs(CurrentMethod, null, CurrentLineNumber, CurrentColumnNumber));
               }
           }
       }

        private void AddMethodToHistoryList(Method currentMethod)
        {
            if(! _navigatedMethods.Contains(_currentMethod))
                _navigatedMethods.Add(currentMethod);
        }

        #endregion


       #region file changed
       internal void OnFileChanged(object sender, FileEventRaisedArgs e)
       {
           FileEventType fileEvent = e.EventType;
           string oldFilePath = e.OldFilePath;
           string newFilePath = e.FilePath;
           //bool hasSrcML = e.HasSrcML;

           switch (fileEvent)
           {
               case FileEventType.FileAdded:
                   return;
               case FileEventType.FileChanged:
                   ChangeMethodsInChangedFile(oldFilePath);
                   break;
               case FileEventType.FileDeleted: 
                   DeleteMethodsInDeletedFile(oldFilePath);
                   break;
               case FileEventType.FileRenamed: 
                   ChangeMethodsInRenamedFile(oldFilePath, newFilePath);
                   break;
           }
       }

        private void ChangeMethodsInRenamedFile(string oldFilePath, string newFilePath)
        {
            if(oldFilePath == newFilePath)
                return;
            
            for (var i=0; i< _navigatedMethods.Count; i++)
            {  
                if (_navigatedMethods[i].FilePath == oldFilePath)
                {
                    var oldmethod = _navigatedMethods[i];
                    _navigatedMethods[i].FilePath = newFilePath;
                   
                    var method = _navigatedMethods[i];
                    OnMethodUpdatedEvent(new MethodEventRaisedArgs(MethodEventType.MethodChanged, method, oldmethod));
                    
                    if (_currentMethod.SignatureEquals(method))
                        _currentMethod = method;
                }
            }
        }

        private void ChangeMethodsInChangedFile(string oldFilePath)
        {   
            List<Method> allMethods;
            GetAllMethods(oldFilePath, out allMethods);
            
            for (int i=0; i < _navigatedMethods.Count; i++)
            {
                if (_navigatedMethods[i].FilePath == oldFilePath)
                {   
                    var method = _navigatedMethods[i];
                    Method newMethod;

                    var updateType = MethodUpdatedState(method, allMethods, out newMethod);
                    switch (updateType)
                    {
                        case 0: // unchanged
                            break;
                        case 1: // stays but changed
                            _navigatedMethods[i] = newMethod;
                            OnMethodUpdatedEvent(new MethodEventRaisedArgs(MethodEventType.MethodChanged, newMethod, method));
                            if (_currentMethod.Equals(method)) 
                                _currentMethod = newMethod;
                            break;
                        case 2: // deleted
                            _navigatedMethods.RemoveAt(i);
                            OnMethodUpdatedEvent(new MethodEventRaisedArgs(MethodEventType.MethodDeleted, null, method));
                            if (_currentMethod.Equals(method)) 
                                _currentMethod = new Method(); 
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Determine the state of a previously navigated method in the file which it was located at
        /// </summary>
        /// <param name="preNavigatedMethod">a previously navigated method</param>
        /// <param name="newAllMethods">collection of all methods after a file change (where the previously navigate method wass located)</param>
        /// <param name="newMethod">a new method object is also returned if the function returns 1</param>
        /// <returns>0 - unchanged, 1 - still there but changed, 2 - deleted</returns>
        private int MethodUpdatedState(Method preNavigatedMethod, List<Method> newAllMethods, out Method newMethod)
        {
            //method change: "name", "type" and "prameter type" remain the same but startline number changes
            //method deletion: no longer find a method with the same (type + name + parameter type)
            //todo: method rename is a special case might be handled later (it may need a comparision of the method body)

            newMethod = new Method();

            foreach (var method in newAllMethods)
            {
                if (method.SignatureEquals(preNavigatedMethod) && method.Type == preNavigatedMethod.Type)
                {
                    if (method.Equals(preNavigatedMethod)) //StartLineNumber comparison
                        return 0;
                    else
                    {
                        newMethod = method;
                        return 1;
                    }
                }
            }

            return 2;
        }

        private void DeleteMethodsInDeletedFile(string deletedFilePath)
        {
            if (_currentMethod.FilePath == deletedFilePath)
                _currentMethod = new Method();

            foreach (var method in _navigatedMethods)
            {
                if (method.FilePath == deletedFilePath)
                {
                    _navigatedMethods.Remove(method);
                    OnMethodUpdatedEvent(new MethodEventRaisedArgs(MethodEventType.MethodDeleted, null, method));
                }
            }
        }


       #endregion

       /// <summary>
       /// Raises the method updated event
       /// </summary>
       /// <param name="e">The event arguments</param>
       protected virtual void OnMethodUpdatedEvent(MethodEventRaisedArgs e)
       {
           EventHandler<MethodEventRaisedArgs> handler = MethodUpdatedEvent;
           if (null != handler)
           {
               handler(this, e);
           }
       }

    }
}
