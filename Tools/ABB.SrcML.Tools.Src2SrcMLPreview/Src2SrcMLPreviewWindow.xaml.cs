/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - Added support for SrcMLGenerator.
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
        private SrcMLGenerator _xmlGenerator;
        private string _xml;
        private Language _language;
        private Forms.FolderBrowserDialog directorySelector;
        private static Collection<string> _namespaceArguments = new Collection<string>() { LIT.ArgumentLabel, OP.ArgumentLabel, TYPE.ArgumentLabel };
        private static Collection<string> _namespaceArgumentsWithPosition = new Collection<string>() { LIT.ArgumentLabel, OP.ArgumentLabel, TYPE.ArgumentLabel, POS.ArgumentLabel };


        public SrcMLGenerator XmlGenerator {
            get { return this._xmlGenerator;  }
            set {
                this._xmlGenerator = value;
                UpdateSupportedLanguages();
            }
        }
        public Src2SrcMLPreviewWindow()
        {
            ToggleShowPositionCommand.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Alt));
            SelectSrcMLDirectoryCommand.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Alt));

            binDirIsValid = true;
            directorySelector = new System.Windows.Forms.FolderBrowserDialog();
            directorySelector.ShowNewFolderButton = false;
            directorySelector.SelectedPath = SrcMLHelper.GetSrcMLDefaultDirectory();
            
            _language = ABB.SrcML.Language.CPlusPlus;
            InitializeComponent();
            XmlGenerator = new SrcMLGenerator(directorySelector.SelectedPath, _namespaceArguments);
        }

        /// <summary>
        /// Static command to route "Alt-P" to the "show position" menu item
        /// </summary>
        public static RoutedCommand ToggleShowPositionCommand = new RoutedCommand();

        /// <summary>
        /// Static command to route "Alt-D" to the "Select SrcML Directory" menu item
        /// </summary>
        public static RoutedCommand SelectSrcMLDirectoryCommand = new RoutedCommand();

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
                        _xml = XmlGenerator.GenerateSrcMLFromString(sourceBox.Text, this._language);

                        languageLabel.Content = String.Format("(Code parsed as {0})", KsuAdapter.GetLanguage(this._language));
                        var doc = XDocument.Parse(_xml, LoadOptions.PreserveWhitespace);
                        this.xmlTree.DataContext = doc;

                        int startIndex = _xml.IndexOf('<', 1);
                        int endIndex = _xml.LastIndexOf('<');
                        srcmlBox.Text = _xml.Substring(startIndex, endIndex - startIndex);
                    }
                    catch (Win32Exception)
                    {
                        PrintErrorInSrcmlBox("The SrcML directory does not contain a valid copy of src2srcml.exe.\n\nSelect a valid directory from File->Select SrcML Directory...");
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

                if (null != menuItem && menuItem.IsChecked)
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

                    this._language = SrcMLElement.GetLanguageFromString(menuItem.Header.ToString());
                    sourceBox_TextChanged(sender, null);
                }
            }
            catch (SrcMLException error)
            {
                PrintErrorInSrcmlBox(error.Message);
            }
        }

        private void MenuItemSrcMLSelect_Click(object sender, RoutedEventArgs e)
        {
            var currentDirectory = this.directorySelector.SelectedPath;
            directorySelector.Description = String.Format("Select a directory that contains src2srcml.exe\nThe current directory is {0}", currentDirectory);
            if (directorySelector.ShowDialog() == Forms.DialogResult.OK)
            {
                this._xmlGenerator = new SrcMLGenerator(directorySelector.SelectedPath, _namespaceArguments);
                binDirIsValid = true;
                srcmlBox.Foreground = Brushes.Black;
                UpdateSupportedLanguages();
                if (sourceBox.Text.Length > 0)
                    sourceBox_TextChanged(sender, null);
            }
        }

        private void UpdateSupportedLanguages() {
            HashSet<Language> supportedLanguages = new HashSet<Language>(this.XmlGenerator.SupportedLanguages);
            foreach(var item in this.MenuItemLanguage.Items) {
                var menuItem = item as MenuItem;
                var language = SrcMLElement.GetLanguageFromString(menuItem.Header.ToString());
                menuItem.IsEnabled = supportedLanguages.Contains(language);
            }
        }

        private void PrintErrorInSrcmlBox(string errorMessage)
        {
            languageLabel.Content = "Error!";
            srcmlBox.Text = errorMessage;
            srcmlBox.Foreground = Brushes.Red;
        }
        
        private void MenuItemShowPosition_Checked(object sender, RoutedEventArgs e) {
            _xmlGenerator = new SrcMLGenerator(directorySelector.SelectedPath, _namespaceArgumentsWithPosition);
            if(sourceBox.Text.Length > 0) sourceBox_TextChanged(sender, null);
        }

        private void MenuItemShowPosition_Unchecked(object sender, RoutedEventArgs e) {
            _xmlGenerator = new SrcMLGenerator(directorySelector.SelectedPath, _namespaceArguments);
            if(sourceBox.Text.Length > 0) sourceBox_TextChanged(sender, null);
        }

        private void ToggleShowPosition(object sender, ExecutedRoutedEventArgs e) {
            var window = sender as Src2SrcMLPreviewWindow;
            if(null != window) {
                window.MenuItemShowPosition.IsChecked = !window.MenuItemShowPosition.IsChecked;
            }
        }

        private void SelectSrcMLDirectory(object sender, ExecutedRoutedEventArgs e) {
            var window = sender as Src2SrcMLPreviewWindow;
            if(null != window) {
                MenuItemSrcMLSelect_Click(sender, e);
            }
        }
    }
}
