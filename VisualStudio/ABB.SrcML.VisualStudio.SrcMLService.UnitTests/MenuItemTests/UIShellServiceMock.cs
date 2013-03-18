using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VsSDK.UnitTestLibrary;

namespace ABB.SrcML.VisualStudio.SrcMLService.UnitTests {
    static class UIShellServiceMock {
        private static GenericMockFactory uiShellFactory;

        #region UiShell Getters
        /// <summary>
        /// Returns an IVsUiShell that does not implement any methods
        /// </summary>
        /// <returns></returns>
        internal static BaseMock GetUiShellInstance() {
            if(uiShellFactory == null) {
                uiShellFactory = new GenericMockFactory("UiShell", new Type[] { typeof(IVsUIShell), typeof(IVsUIShellOpenDocument) });
            }
            BaseMock uiShell = uiShellFactory.GetInstance();
            return uiShell;
        }

        /// <summary>
        /// Get an IVsUiShell that implements SetWaitCursor, SaveDocDataToFile, ShowMessageBox
        /// </summary>
        /// <returns>uishell mock</returns>
        internal static BaseMock GetUiShellInstance0() {
            BaseMock uiShell = GetUiShellInstance();
            string name = string.Format("{0}.{1}", typeof(IVsUIShell).FullName, "SetWaitCursor");
            uiShell.AddMethodCallback(name, new EventHandler<CallbackArgs>(SetWaitCursorCallBack));

            name = string.Format("{0}.{1}", typeof(IVsUIShell).FullName, "SaveDocDataToFile");
            uiShell.AddMethodCallback(name, new EventHandler<CallbackArgs>(SaveDocDataToFileCallBack));

            name = string.Format("{0}.{1}", typeof(IVsUIShell).FullName, "ShowMessageBox");
            uiShell.AddMethodCallback(name, new EventHandler<CallbackArgs>(ShowMessageBoxCallBack));
            return uiShell;
        }
        #endregion

        #region Callbacks
        private static void SetWaitCursorCallBack(object caller, CallbackArgs arguments) {
            arguments.ReturnValue = VSConstants.S_OK;
        }

        private static void SaveDocDataToFileCallBack(object caller, CallbackArgs arguments) {
            arguments.ReturnValue = VSConstants.S_OK;
        }

        private static void ShowMessageBoxCallBack(object caller, CallbackArgs arguments) {
            arguments.ReturnValue = VSConstants.S_OK;
            arguments.SetParameter(10, (int)System.Windows.Forms.DialogResult.Yes);
        }

        #endregion
    }
}