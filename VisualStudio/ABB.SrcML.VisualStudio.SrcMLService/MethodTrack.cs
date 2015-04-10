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

namespace ABB.VisualStudio
{
    /// <summary>
    /// current cursor position defined by (filename, line number, column number)
    /// </summary>
    public struct CursorPos
    {
        public string _file;
        public int _line, _col;

        public CursorPos(string file, int line, int col)
        {
            _file = file;
            _line = line;
            _col = col;
        }

        public void SetCursurPos(string file, int line, int col)
        {
            _file = file;
            _line = line;
            _col = col;
        }
    }

    class MethodTrack: IMethodTrackService, SMethodTrackService{
        private IServiceProvider _serviceProvider;

        private VsDataService _dataService;

        private ISrcMLGlobalService _srcmlService;

        private ICursorMonitorService _cursorMonitor;

        public event EventHandler<MethodEventRaisedArgs> MethodEvent;

        private CursorPos lastCursor;

       public MethodTrack(IServiceProvider sp)
       {
           _serviceProvider = sp;
           _dataService = Package.GetGlobalService(typeof(VsDataService)) as VsDataService;
           _srcmlService = Package.GetGlobalService(typeof(SSrcMLDataService)) as ISrcMLGlobalService;
           _cursorMonitor = Package.GetGlobalService(typeof(SCursorMonitorService)) as ICursorMonitorService;
           if (null != _cursorMonitor)
             _cursorMonitor.PropertyChanged += OnCursorMoving;
           if(null != _srcmlService)
            _srcmlService.SourceFileChanged += onFileChanged;

           lastCursor._file = "";
           lastCursor._line = 0;
           lastCursor._col = 0;            
        }

       /// <summary>
       /// Get a methoddef object from file, line number and column number
       /// </summary>
       /// <param name="fileName"></param>
       /// <param name="lineNum"></param>
       /// <param name="colNum"></param>
       /// <returns>if the cursor is in a methoddef, return true; otherwise return false</returns>
       internal bool GetMethod(string fileName, int lineNum, int colNum, out Method outMethod)
       {
           bool isInMethod = false;
           outMethod = null;

           var data = _dataService.CurrentDataArchive.GetData(fileName);
           //do we deal with the case that one source file contains multiple namespaces?
           if(null != data)
           {
               var methods = data.GetDescendants<MethodDefinition>();
               foreach(var methoddef in methods)
               {
                   SourceLocation cursorPos = new SourceLocation(fileName, lineNum, colNum);
                   if(methoddef.ContainsLocation(cursorPos))
                   {
                       isInMethod = true;
                       outMethod = new Method(methoddef);
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
               DateTime timeStamp = cursorMonitor.LastUpdated;
               lastCursor.SetCursurPos(fileName,curLine,curColumn);

               Method curMethod;
               bool isMethod = GetMethod(fileName, curLine, curColumn, out curMethod);
               if(isMethod)
                   onMethodEvent(new MethodEventRaisedArgs(curMethod, curLine, curColumn));
           }

       }
       #endregion


       #region file changed
       internal void onFileChanged(object sender, FileEventRaisedArgs e)
       {
            
       }

       #endregion

       /// <summary>
       /// Raises the methoddef added event
       /// </summary>
       /// <param name="e">The event arguments</param>
       protected virtual void onMethodEvent(MethodEventRaisedArgs e)
       {
           EventHandler<MethodEventRaisedArgs> handler = MethodEvent;
           if (null != handler)
           {
               handler(this, e);
           }
       }

    }
}
