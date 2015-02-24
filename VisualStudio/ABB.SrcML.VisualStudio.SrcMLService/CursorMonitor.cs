/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using IServiceProvider = System.IServiceProvider;

namespace ABB.VisualStudio {
    class CursorMonitor : ICursorMonitorService, SCursorMonitorService {
        private static Guid textViewGuid = new Guid("E1965DA9-E791-49E2-9F9D-ED766D885967");

        private IServiceProvider serviceProvider;
        private DTE vsEnvironment;
        private WindowEvents windowEvents;

        private Dictionary<Window, Tuple<uint,IConnectionPoint>> activeWindows;
        
        private string _currentFileName;
        private int _currentLineNumber;
        private int _currentColumnNumber;

        public event PropertyChangedEventHandler PropertyChanged;

        public CursorMonitor(IServiceProvider provider) {

            this.serviceProvider = provider;
            this.vsEnvironment = serviceProvider.GetService(typeof(DTE)) as DTE;

            activeWindows = new Dictionary<Window, Tuple<uint, IConnectionPoint>>();
            Setup();
        }

        public string CurrentFileName {
            get { return _currentFileName; }
            private set {
                if(SetField(ref _currentFileName, value, "CurrentFileName", StringComparer.CurrentCultureIgnoreCase)) {
                    LastUpdated = DateTime.Now;
                }
            }
        }

        public int CurrentColumnNumber {
            get { return _currentColumnNumber; }
            private set {
                if(SetField(ref _currentColumnNumber, value, "CurrentColumnNumber")) {
                    LastUpdated = DateTime.Now;
                }
            }
        }

        public int CurrentLineNumber {
            get { return _currentLineNumber; }
            private set {
                if(SetField(ref _currentLineNumber, value, "CurrentLineNumber")) {
                    LastUpdated = DateTime.Now;
                }
            }
        }

        public DateTime LastUpdated { get; private set; }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            var handler = PropertyChanged;
            if(null != handler) {
                handler(this, e);
            }
        }

        private IConnectionPoint GetConnectionPoint(string path) {
            if(string.IsNullOrEmpty(path)) { return null; }

            IVsUIHierarchy uiHierarchy;
            uint itemId;
            IVsWindowFrame windowFrame;

            if(VsShellUtilities.IsDocumentOpen(this.serviceProvider, path, Guid.Empty, out uiHierarchy, out itemId, out windowFrame)) {
                var view = VsShellUtilities.GetTextView(windowFrame);
                var container = view as IConnectionPointContainer;
                if(null != container) {
                    IConnectionPoint connectionPoint;
                    container.FindConnectionPoint(ref textViewGuid, out connectionPoint);
                    return connectionPoint;
                }
            }

            return null;
        }

        private string GetWindowPath(Window window) {
            try {
                if(null == window || null == window.Document) { return null; }
                return window.Document.FullName;
            } catch(Exception) {
                return String.Empty;
            }
        }

        private bool SetField<T>(ref T field, T value, string propertyName) {
            return SetField<T>(ref field, value, propertyName, EqualityComparer<T>.Default);
        }

        private bool SetField<T>(ref T field, T value, string propertyName, IEqualityComparer<T> comparer) {
            if(comparer.Equals(field, value)) { return false; }
            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            return true;
        }

        private void Setup() {
            if(null == vsEnvironment) { throw new InvalidOperationException("no DTE"); }

            vsEnvironment.Events.DTEEvents.OnBeginShutdown += Teardown;
            windowEvents = vsEnvironment.Events.WindowEvents;

            if(null != this.windowEvents) {
                this.windowEvents.WindowActivated += windowEvents_WindowActivated;
                this.windowEvents.WindowClosing += windowEvents_WindowClosing;
            }
        }

        void Teardown() {
            if(null != this.windowEvents) {
                this.windowEvents.WindowActivated -= windowEvents_WindowActivated;
                this.windowEvents.WindowClosing -= windowEvents_WindowClosing;
            }
        }

        void windowEvents_WindowActivated(Window GotFocus, Window LostFocus) {
            if(null == GotFocus || activeWindows.ContainsKey(GotFocus)) { return; }

            var filePath = GetWindowPath(GotFocus);
            var connectionPoint = GetConnectionPoint(filePath);
            if(null == connectionPoint) { return; }
            

            var listener = new TextViewListener(this, filePath);
            uint cookie;
            connectionPoint.Advise(listener, out cookie);
            activeWindows.Add(GotFocus, Tuple.Create(cookie, connectionPoint));
        }

        void windowEvents_WindowClosing(Window Window) {
            if(!activeWindows.ContainsKey(Window)) { return; }

            activeWindows[Window].Item2.Unadvise(activeWindows[Window].Item1);
            activeWindows.Remove(Window);
        }

        private class TextViewListener : IVsTextViewEvents {
            private CursorMonitor monitor;
            private string filePath;

            public TextViewListener(CursorMonitor monitor, string filePath) {
                this.monitor = monitor;
                this.filePath = filePath;
            }
            public void OnChangeCaretLine(IVsTextView pView, int iNewLine, int iOldLine) {
                OnSetFocus(pView);
            }

            public void OnSetFocus(IVsTextView pView) {
                if(null == pView) { return; }
                var position = GetPosition(pView);

                monitor.CurrentFileName = this.filePath;
                monitor.CurrentLineNumber = position.Item1;
                monitor.CurrentColumnNumber = position.Item2;
            }

            private Tuple<int, int> GetPosition(IVsTextView view) {
                int line, column;
                if(VSConstants.S_OK == view.GetCaretPos(out line, out column)) {
                    return Tuple.Create<int, int>(line + 1, column + 1);
                }
                return null;
            }

            #region not implemented
            public void OnChangeScrollInfo(IVsTextView pView, int iBar, int iMinUnit, int iMaxUnits, int iVisibleUnits, int iFirstVisibleUnit) {
                // not implemented
            }

            public void OnKillFocus(IVsTextView pView) {
                // not implemented
            }

            public void OnSetBuffer(IVsTextView pView, IVsTextLines pBuffer) {
                // not implemented
            }
            #endregion not implemented
        }
    }
}
