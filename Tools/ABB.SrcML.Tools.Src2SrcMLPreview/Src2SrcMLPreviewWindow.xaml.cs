/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ABB.SrcML;
using System.Xml.Linq;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;
using Forms = System.Windows.Forms;
using System.ComponentModel;

namespace ABB.SrcML.Tools.Src2SrcMLPreview
{
    /// <summary>
    /// Interaction logic for Src2SrcMLPreviewWindow.xaml
    /// </summary>
    public partial class Src2SrcMLPreviewWindow : Window
    {
        private bool binDirIsValid;
        private Src2SrcMLRunner _srcmlObject;
        private string _xml;
        private Language _language;
        private Forms.FolderBrowserDialog directorySelector;

        public Src2SrcMLPreviewWindow()
        {
            binDirIsValid = true;
            directorySelector = new System.Windows.Forms.FolderBrowserDialog();
            directorySelector.ShowNewFolderButton = false;
            directorySelector.SelectedPath = SrcMLHelper.GetSrcMLDefaultDirectory();

            _srcmlObject = new Src2SrcMLRunner(directorySelector.SelectedPath, new Collection<string>() { LIT.ArgumentLabel, OP.ArgumentLabel, TYPE.ArgumentLabel });
            _language = ABB.SrcML.Language.CPlusPlus;
            InitializeComponent();
        }

        private void sourceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (0 == sourceBox.Text.Length)
                srcmlBox.Text = "";
            else
            {
                if (binDirIsValid)
                {
                    try
                    {
                        _xml = _srcmlObject.GenerateSrcMLFromString(sourceBox.Text, this._language);

                        languageLabel.Content = String.Format("(Code parsed as {0})", KsuAdapter.GetLanguage(this._language));
                        var doc = XDocument.Parse(_xml, LoadOptions.PreserveWhitespace);
                        this.xmlTree.DataContext = doc;

                        int startIndex = _xml.IndexOf('<', 1);
                        int endIndex = _xml.LastIndexOf('<');
                        srcmlBox.Text = _xml.Substring(startIndex, endIndex - startIndex);
                    }
                    catch (Win32Exception)
                    {
                        languageLabel.Content = "Error!";
                        srcmlBox.Text = "The SrcML directory does not contain a valid copy of src2srcml.exe.\n\nSelect a valid directory from File->Select SrcML Directory...";
                        srcmlBox.Foreground = Brushes.Red;
                        this.binDirIsValid = false;
                    }
                }
            }
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItemLanguage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = (sender as MenuItem);

                if (menuItem.IsChecked)
                {
                    menuItem.IsCheckable = false;
                    var otherMenuItems = from MenuItem item in MenuItemLanguage.Items.SourceCollection
                                         where item != menuItem
                                         select item;
                    foreach (var item in otherMenuItems)
                    {
                        item.IsChecked = false;
                        item.IsCheckable = true;
                    }

                    this._language = KsuAdapter.GetLanguageFromString(menuItem.Header.ToString());
                    sourceBox_TextChanged(sender, null);
                }
            }
            catch (SrcMLException)
            {

            }
        }

        private void MenuItemSrcMLSelect_Click(object sender, RoutedEventArgs e)
        {
            directorySelector.Description = String.Format("Select a directory that contains src2srcml.exe\nThe current directory is {0}", this._srcmlObject.ApplicationDirectory);
            if (directorySelector.ShowDialog() == Forms.DialogResult.OK)
            {
                this._srcmlObject.ApplicationDirectory = directorySelector.SelectedPath;
                binDirIsValid = true;
                srcmlBox.Foreground = Brushes.Black;
                if (sourceBox.Text.Length > 0)
                    sourceBox_TextChanged(sender, null);
            }
        }

        private void srcmlBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
