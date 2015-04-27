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
using System.Linq;
using System.Text;
using ABB.SrcML;
using ABB.SrcML.VisualStudio;
using Microsoft.VisualStudio.Shell;
using ABB.SrcML.Data;
using System.Runtime.CompilerServices;

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
        private CursorPos _lastCursor;
        private Method _currentMethod;
        
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

       public MethodTrack(IServiceProvider sp)
       {
           _serviceProvider = sp;
           _dataService = sp.GetService(typeof(SSrcMLDataService)) as ISrcMLDataService;
           _srcmlService = sp.GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
           _cursorMonitor = sp.GetService(typeof(SCursorMonitorService)) as ICursorMonitorService;
           _currentMethod = new Method();
           _currentCursor.SetCursurPos("", 0, 0);
           _lastCursor.SetCursurPos("", 0, 0);

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
       internal bool GetMethod(string fileName, int lineNum, int colNum)
       {
           bool isInMethod = false;
           
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

        
       #region cursor moving
       internal void OnCursorMoving(object sender, System.ComponentModel.PropertyChangedEventArgs e)
       {
           var cursorMonitor = (sender as CursorMonitor);
           if (null != cursorMonitor)
           {
               int curLine = cursorMonitor.CurrentLineNumber;
               int curColumn = cursorMonitor.CurrentColumnNumber;
               string fileName = cursorMonitor.CurrentFileName;
               
               _lastCursor = _currentCursor;
               _currentCursor.SetCursurPos(fileName,curLine,curColumn);

               //System.Diagnostics.Debugger.Launch(); //for integration test debugging only
               bool isMethod = GetMethod(fileName, curLine, curColumn);
               if(isMethod)
                   OnMethodUpdatedEvent(new MethodEventRaisedArgs(CurrentMethod, CurrentLineNumber, CurrentColumnNumber));
              }

       }
       #endregion


       #region file changed
       internal void OnFileChanged(object sender, FileEventRaisedArgs e)
       {
            
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
