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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.ComponentModel;
using System.Xml;
using SDML.SrcMLVSAddin.SyntaticCategory;

namespace ABB.SrcML.VisualStudio.PreviewAddIn
{
    /// <summary>
    /// Summary description for TransformPreviewControl.
    /// </summary>
    public partial class TransformPreviewControl : UserControl
    {
        public static string GUID = "{C2FFB509-8A43-4bf6-B40C-AAF7648A7D28}";
        public EventHandler<SrcMLFileCreatedEventArgs> SrcMLFileCreatedEvent;
        public EventHandler<OpenFileEventArgs> OpenFileEvent;

        private SrcML srcml;
        private DirectoryInfo projectSrcmlFolder;
        private BindingList<DirectoryInfo> sourceDirectories;
        private Dictionary<DirectoryInfo, string> srcmlDict;

        private BindingList<DirectoryInfo> outputSourceDirectoryList;

        private bool generatingXML;
        private bool generatingSource;
        private bool queryingXML;

        private SrcMLFile selectedDoc;
        private List<DataCell> data;
        private SyntaticCategoryDataModel categories;
        private ITransform selectedTransform;

        public TransformPreviewControl()
        {
            InitializeComponent();

            dataGridView1.AutoGenerateColumns = false;

            try
            {
                srcml = new SrcML();
            }
            catch(FileNotFoundException e)
            {
                string msg = String.Format("{0}: {1}\n{2}", e.Source, e.Message, e.StackTrace);
                if (e.InnerException != null)
                    msg += String.Format("\n{0}", e.InnerException);
            }

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            projectSrcmlFolder = null;
            sourceDirectories = new BindingList<DirectoryInfo>();
            srcmlDict = new Dictionary<DirectoryInfo, string>(new DirectoryInfoComparer());
            outputSourceDirectoryList = new BindingList<DirectoryInfo>();

            inputFolderComboBox.DataSource = sourceDirectories;
            inputFolderComboBox.DisplayMember = "FullName";
            inputFolderComboBox.SelectedIndex = -1;

            outputFolderComboBox.DataSource = outputSourceDirectoryList;
            outputFolderComboBox.DisplayMember = "FullName";
            outputFolderComboBox.SelectedIndex = -1;

            LocationColumn.DataPropertyName = "Location";
            OriginalSourceColumn.DataPropertyName = "Text";
            TransformedSourceColumn.DataPropertyName = "TransformedText";
            EnabledColumn.DataPropertyName = "Enabled";
            setButtons();
        }

        protected virtual void OnSrcMLFileCreatedEvent(SrcMLFileCreatedEventArgs e)
        {
            EventHandler<SrcMLFileCreatedEventArgs> handler = SrcMLFileCreatedEvent;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnOpenFileEvent(OpenFileEventArgs e)
        {
            EventHandler<OpenFileEventArgs> handler = OpenFileEvent;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// This conducts assembly resolution for the processBuiltDlls function by checking the current domain
        /// for assembly. It is most likely used to resolve ABB.SrcML.dll which should be in the current domain
        /// in order for the TransformPreviewControl to load in the first place.
        /// 
        /// This problem was solved based on code from here:
        /// http://stackoverflow.com/a/2658326/6171
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="args">resolve event arguments</param>
        /// <returns>the requested assembly if it has been loaded in the current domain. null otherwise.</returns>
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var domain = (AppDomain)sender;

            foreach (var assembly in domain.GetAssemblies())
            {
                if (assembly.FullName == args.Name)
                    return assembly;
            }
            return null;
        }

        /// <summary> 
        /// Let this control process the mnemonics.
        /// </summary>
        [UIPermission(SecurityAction.LinkDemand, Window = UIPermissionWindow.AllWindows)]
        protected override bool ProcessDialogChar(char charCode)
        {
              // If we're the top-level form or control, we need to do the mnemonic handling
              if (charCode != ' ' && ProcessMnemonic(charCode))
              {
                    return true;
              }
              return base.ProcessDialogChar(charCode);
        }

        /// <summary>
        /// Enable the IME status handling for this control.
        /// </summary>
        protected override bool CanEnableIme
        {
            get
            {
                return true;
            }
        }

        private static List<DataCell> executeTransform(SrcMLFile doc, ITransform transform, ref SyntaticCategoryDataModel categories, BackgroundWorker worker)
        {
            IEnumerable<XElement> elements;
            List<DataCell> data = null;

            if (null != transform)
            {
                try
                {
                    elements = doc.QueryEachUnit(transform);

                    if (null != elements)
                    {
                        float numElements = (float)elements.Count();
                        int i = 0, percentComplete = 0;
                        data = new List<DataCell>();
                        foreach (var node in elements)
                        {
                            var occurrence = new SyntaticOccurance(categories, node);
                            categories.AddOccurance(occurrence);
                            data.Add(new DataCell(doc, node, transform, occurrence));
                            percentComplete = (int)((float)++i / numElements * 100);
                            worker.ReportProgress(percentComplete);
                        }
                        foreach (var d in data)
                            d.Enabled = true;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(String.Format("{0}: {1}\n{2}", e.Source, e.Message, e.StackTrace));
                }
            }
            return data;
        }

        private string getPathFor(DirectoryInfo sourceFolder)
        {
            var docPath = Path.GetTempFileName();

            string[] parts = sourceFolder.FullName.Split(Path.DirectorySeparatorChar);
            string filename = String.Join("-", parts, 1, parts.Length - 1);

            if (null != projectSrcmlFolder)
            {
                docPath = Path.Combine(projectSrcmlFolder.FullName, filename);
                docPath += ".xml";
            }

            return docPath;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            List<DataCell> data = dataGridView1.DataSource as List<DataCell>;
            int index= dataGridView1.CurrentRow.Index;
        }

        private void updateSelectedTransform(object sender, EventArgs e)
        {
            if (null != transformComboBox.SelectedItem)
                selectedTransform = transformComboBox.SelectedItem as ITransform;
            else
                selectedTransform = null;
            setButtons();
        }

        /// <summary>
        /// setButtons should be called in two places:
        /// 1. at the end of each button press. To enable this, _Click functions must reset any variables that are to be modified.
        /// 2. at the end of each action triggered by a button press -- for instance, when a button press causes a long-running job
        ///    to be triggered, that action must call setButtons() when completed.
        /// 
        /// setButtons only sets the enabled/disabled state of the buttons. It does not set the text or modify any other parts of the control
        /// </summary>
        private void setButtons()
        {
            browseInputFoldersButton.Enabled = (!generatingXML && !generatingSource);
            browseOutputFoldersButton.Enabled = (!generatingSource);

            runQueryButton.Enabled = (!generatingSource &&
                                      !queryingXML &&
                                      null != inputFolderComboBox.SelectedItem &&
                                      null != selectedTransform);
            
            runTransformButton.Enabled = (runQueryButton.Enabled &&
                                          null != dataGridView1.DataSource &&
                                          -1 < outputFolderComboBox.SelectedIndex);
        }

        public bool processBuiltDlls(List<string> dlls)
        {
            Assembly assembly;
            List<ITransform> transforms = new List<ITransform>();
            string msg = "";

            foreach (var dll in dlls)
            {
                try
                {
                    var bytes = File.ReadAllBytes(dll);
                    assembly = Assembly.Load(bytes);
                    var newTypes = assembly.GetTypes();

                    foreach (var type in newTypes)
                    {
                        try
                        {
                            if (null != type.GetInterface("ABB.SrcML.ITransform") &&
                                "ABB.SrcML" != type.Namespace)
                            {
                                ITransform test = new TransformObjectHarness(type);
                                transforms.Add(test);
                            }
                            else
                            {
                                foreach (var test in QueryHarness.CreateFromType(type))
                                {
                                    transforms.Add(test);
                                }
                            }
                        }
                        catch (MissingMethodException e)
                        {
                            messageLabel.Text = String.Format("Could not load {0}({1})", type.FullName, e.Message);
                        }

                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    msg = String.Format("{0}\n", e);
                    foreach (var le in e.LoaderExceptions)
                        msg += le.ToString();
                }
                catch (Exception e)
                {
                    msg = e.ToString();
                }
                finally
                {
                    if (0 < msg.Length)
                        MessageBox.Show(msg);
                }
            }
            int index = transformComboBox.SelectedIndex;
            transformComboBox.DataSource = transforms;
            transformComboBox.DisplayMember = "Name";

            if (index < transforms.Count)
                transformComboBox.SelectedIndex = index;
            else
                transformComboBox.SelectedIndex = transforms.Count - 1;

            messageLabel.Text = String.Format("Loaded {0} transforms.", transforms.Count);

            return true;
        }
 
        #region selecting folders
        private void browseInputFoldersButton_Click(object sender, EventArgs e)
        {
            projectFolderDialog.ShowNewFolderButton = false;
            projectFolderDialog.Description = "Select a Source Folder";
            if (projectFolderDialog.ShowDialog() == DialogResult.OK)
            {
                addNewSourceFolder(new DirectoryInfo(projectFolderDialog.SelectedPath));
                if (1 == sourceDirectories.Count)
                    updateSelectedInputFolder();
                // selectedDoc = null;
            }
            inputFolderComboBox.SelectedIndex = sourceDirectories.Count - 1;
            setButtons();
        }

        private void browseOutputFoldersButton_Click(object sender, EventArgs e)
        {
            projectFolderDialog.ShowNewFolderButton = true;
            projectFolderDialog.Description =  "Select an Output Folder";
            if (projectFolderDialog.ShowDialog() == DialogResult.OK)
            {
                var dir = new DirectoryInfo(projectFolderDialog.SelectedPath);
                if (!outputSourceDirectoryList.Contains(dir, srcmlDict.Comparer))
                    outputSourceDirectoryList.Add(dir);
            }
            outputFolderComboBox.SelectedIndex = outputSourceDirectoryList.Count - 1;
            setButtons();
        }

        private void addNewSourceFolder(DirectoryInfo sourceFolder)
        {   
            if(!sourceDirectories.Contains(sourceFolder, srcmlDict.Comparer))
                sourceDirectories.Add(sourceFolder);
        }

        private void updateSelectedInputFolder()
        {
            DirectoryInfo source = inputFolderComboBox.SelectedItem as DirectoryInfo;

            if (null != source)
            {
                if (!srcmlDict.ContainsKey(source))
                {
                    generatingXML = true;
                    browseInputFoldersButton.Enabled = false;
                    browseInputFoldersButton.Text = Resources.BrowseButtonLoading;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    messageLabel.Text = String.Format("Converting {0} to SrcML...", source.FullName);
                    srcmlGenWorker.RunWorkerAsync((object)source);
                }
            }
        }

        private void srcmlGenWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Object[] array = new Object[2];
            DirectoryInfo source = e.Argument as DirectoryInfo;
            
            var docPath = getPathFor(source);
            if (null == docPath)
                docPath = Path.GetTempFileName();
            
            array[0] = source as Object;
            
            try
            {
                var so = new SrcML();
                SrcMLFile doc = so.GenerateSrcMLFromDirectory(source.FullName, docPath);
                array[1] = (doc == null ? null : doc.FileName as Object);
            }
            catch (TargetInvocationException ex)
            {
                MessageBox.Show(String.Format("Could not load {0}: {1}", source.FullName, ex.Message));
            }
            catch (SrcMLException ex)
            {
                MessageBox.Show(String.Format("Could not load {0}: {1}", source.FullName, ex.Message));
            }

            e.Result = array;
        }

        private void srcmlGenWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Object[] results = e.Result as Object[];
            DirectoryInfo source = results[0] as DirectoryInfo;
            string doc = results[1] as string;// SrcMLFile;

            if (null != doc)
            {
                srcmlDict[source] = doc;
                OnSrcMLFileCreatedEvent(new SrcMLFileCreatedEventArgs(doc));
            }
            else
            {
                srcmlDict.Remove(source);
                sourceDirectories.Remove(source);
            }
            generatingXML = false;
            browseInputFoldersButton.Text = Resources.BrowseButton;
            browseInputFoldersButton.Enabled = true;
            progressBar.Style = ProgressBarStyle.Blocks;
            messageLabel.Text = (null != doc ? "Finished importing " : "Could not import ") + source;

            setButtons();
        }

        private void updateSelectedDocument(object sender, EventArgs e)
        {
            updateSelectedInputFolder();
        }
        #endregion

        private void progressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            progressLabel.Text = String.Format("{0}% done", e.ProgressPercentage);
        }

        #region transform
        // Transform button functions
        private void runTransformButton_Click(object sender, EventArgs e)
        {
            generatingSource = true;

            Object[] args = { dataGridView1.DataSource as List<DataCell>,
                              selectedDoc,
                              outputFolderComboBox.SelectedItem as DirectoryInfo };

            messageLabel.Text = "Transforming with " + selectedTransform.GetType().FullName;
            runTransformButton.Text = Resources.ExecuteButtonRunning;
            runTransformButton.Enabled = false;
            exportSourceWorker.RunWorkerAsync(args);
            setButtons();
        }

        private void exportSourceWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Object[] args = e.Argument as Object[];
            Object[] results = new Object[3];

            List<DataCell> queryResults = args[0] as List<DataCell>;
            SrcMLFile doc = args[1] as SrcMLFile;
            DirectoryInfo target = args[2] as DirectoryInfo;
            SrcMLFile output;

            Dictionary<String, Exception> errors = new Dictionary<string, Exception>();

            var outputDoc = getPathFor(target);

            if (null != queryResults)
            {
                // get the files that contain the changes the user wants to make
                // put the files in a list -- this takes more memory, but will be stable
                //var units = (from node in queryResults
                //             select (node.Enabled ? node.TransformedXml : node.Xml).Ancestors(SRC.Unit).First()).Distinct().ToList();

                var units = (from node in queryResults
                             where node.Enabled
                             where node.TransformedXml != null
                             where node.TransformedXml.Ancestors(SRC.Unit).Any()
                             select node.TransformedXml.Ancestors(SRC.Unit).First()).Distinct().ToList();

                doc.Save(outputDoc, units);

                output = new SrcMLFile(outputDoc);
                output.ProjectDirectory = target.FullName;

                float numUnits = output.FileUnits.Count();
                int unitsProcessed = 0;
                int percentComplete = 0;

                foreach (var unit in output.FileUnits)
                {
                    var filename = unit.Attribute("filename").Value;

                    FileInfo fileInfo = new FileInfo(filename);
                    
                    try
                    {
                        Directory.CreateDirectory(fileInfo.DirectoryName);
                        File.WriteAllText(filename, unit.ToSource(), Encoding.UTF8);
                        percentComplete = (++unitsProcessed / (int)numUnits);
                        exportSourceWorker.ReportProgress(percentComplete);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        errors[filename] = uae;
                    }
                }

                results[0] = target as Object;
                results[1] = output as Object;
                results[2] = errors as Object;
            }

            e.Result = results as Object;
        }

        private void exportSourceWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Object[] results = e.Result as Object[];
            DirectoryInfo outputDir = results[0] as DirectoryInfo;
            SrcMLFile outputDoc = results[1] as SrcMLFile;
            Dictionary<String, Exception> errors = results[2] as Dictionary<String, Exception>;

            addNewSourceFolder(outputDir);

            generatingSource = false;
            runTransformButton.Text = Resources.ExecuteButton;
            progressBar.Value = 0;
            if (0 == errors.Count)
                messageLabel.Text = "Wrote source to " + outputDir;
            else if(errors.Count < outputDoc.FileUnits.Count())
                messageLabel.Text = String.Format("Wrote Source to {0}. Could not write {1} / {2} files",
                                                    outputDir, errors.Count, outputDoc.FileUnits.Count());
            else
                messageLabel.Text = "Could not write any files to " + outputDir;
            progressLabel.Text = "";

            OnSrcMLFileCreatedEvent(new SrcMLFileCreatedEventArgs(outputDoc.FileName));
            setButtons();
        }
        #endregion

        #region testing srcml transforms
        private void runQueryButton_Click(object sender, EventArgs e)
        {
            queryingXML = true;
            string filename = srcmlDict[inputFolderComboBox.SelectedItem as DirectoryInfo];
            Object[] args = { filename, selectedTransform };
            runQueryButton.Text = Resources.TestButtonRunning;
            
            messageLabel.Text = "Querying " + Path.GetFileName(filename);
            queryWorker.RunWorkerAsync(args as Object);
            setButtons();
        }

        private void queryWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Object[] args = e.Argument as Object[];

            SrcMLFile doc = new SrcMLFile(args[0] as string);
            ITransform transform = args[1] as ITransform;

            SyntaticCategoryDataModel categories = new SyntaticCategoryDataModel();
            List<DataCell> data = executeTransform(doc, transform, ref categories, sender as BackgroundWorker);
            Object[] results = { doc, data, categories };
            e.Result = results;
        }

        private void queryWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Object[] results = e.Result as Object[];

            this.selectedDoc = results[0] as SrcMLFile;
            this.data = results[1] as List<DataCell>;
            this.categories = results[2] as SyntaticCategoryDataModel;

            int count = 0;

            if (null != this.data)
            {
                dataGridView1.DataSource = this.data;
                count = this.data.Count;
            }

            if (null != this.categories)
            {
                CategoryTreeNode root = new CategoryTreeNode("All");
                
                categoryTreeView.BeginUpdate();
                categoryTreeView.Nodes.Clear();
                categoryTreeView.Nodes.Add(root);
                foreach (var category in categories.SyntaticCategories.Keys)
                {
                    var xpath = categories.SyntaticCategories[category].First().CategoryAsXPath;
                    var categoryCount = categories.SyntaticCategories[category].Count;
                    root.AddCategory(xpath, categoryCount);
                }
                categoryTreeView.SelectedNode = root;
                categoryTreeView.EndUpdate();
            }

            queryingXML = false;
            runQueryButton.Text = Resources.TestButton;
            progressBar.Value = 0;
            progressLabel.Text = "";
            messageLabel.Text = String.Format("Found {0} items.", count);
            setButtons();
        }
        #endregion

        #region load SrcML Files
        public void LoadSrcMLFiles(DirectoryInfo folder)
        {
            List<FileInfo> srcmldocs = new List<FileInfo>(folder.GetFiles("*.xml"));
            
            projectSrcmlFolder = folder;

            if (srcmldocs.Count > 0)
            {
                messageLabel.Text = "Loading SrcML Files";
                srcmlLoadWorker.RunWorkerAsync(srcmldocs as Object);
                generatingXML = true;
                browseInputFoldersButton.Text = Resources.BrowseButtonLoading;
            }
            setButtons();
        }

        private void srcmlLoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<FileInfo> srcmldocs = e.Argument as List<FileInfo>;
            Dictionary<DirectoryInfo, string> results = new Dictionary<DirectoryInfo, string>();

            int i = 0;
            float count = (float) srcmldocs.Count;

            foreach (var fi in srcmldocs)
            {
                var path = fi.FullName;
                if (File.Exists(path))
                {
                    try
                    {
                        var srcml = new SrcMLFile(path);
                        if (null != srcml.ProjectDirectory)
                        {
                            var sourceFolder = new DirectoryInfo(srcml.ProjectDirectory);
                            results[sourceFolder] = path;
                        }
                    }
                    catch (XmlException ex)
                    {
                        MessageBox.Show(String.Format("Error Loading {0}: {1}", path, ex.Message));
                    }
                    catch (OutOfMemoryException ex)
                    {
                        MessageBox.Show(String.Format("Error Loading {0}: {1}", path, ex.Message));
                    }
                    catch (SrcMLException ex)
                    {
                        MessageBox.Show(String.Format("Error Loading {0}: {1}", path, ex.Message));
                    }
                }
                srcmlLoadWorker.ReportProgress((int)(100 * (float)++i / count));
            }

            e.Result = results as Object;
        }

        private void srcmlLoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dictionary<DirectoryInfo, string> results = e.Result as Dictionary<DirectoryInfo, string>;
            int startingIndex = -1;
            foreach (var sourceFolder in results.Keys)
            {
                srcmlDict[sourceFolder] = results[sourceFolder];
                addNewSourceFolder(sourceFolder);
                ++startingIndex;
                OnSrcMLFileCreatedEvent(new SrcMLFileCreatedEventArgs(results[sourceFolder]));
            }
            messageLabel.Text = "Ready";
            progressLabel.Text = "";
            progressBar.Value = 0;
            browseInputFoldersButton.Text = Resources.BrowseButton;
            generatingXML = false;
            inputFolderComboBox.SelectedIndex = startingIndex;
            updateSelectedInputFolder();
            setButtons();
        }
        #endregion

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            List<DataCell> data = dataGridView1.DataSource as List<DataCell>;
            if (null != data && 0 <= e.RowIndex)
            {
                var cell = data[e.RowIndex];
                var path = Path.Combine(selectedDoc.ProjectDirectory, cell.FilePath);
                OnOpenFileEvent(new OpenFileEventArgs(path, cell.LineNumber, cell.EndLineNumber));
            }
        }

        private void categoryTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var treeNode = e.Node as CategoryTreeNode;
            var label = (String.Empty == treeNode.XPath ? "All" : treeNode.XPath);

            if (String.Empty == treeNode.XPath)
            {
                dataGridView1.DataSource = this.data;
            }
            else
            {
                dataGridView1.DataSource = (from cell in this.data
                                            where cell.Category.StartsWith(treeNode.XPath)
                                            select cell).ToList();
            }
                                       
            messageLabel.Text = String.Format("Selected {0} results ({1})", dataGridView1.RowCount, label);
        }
    }

    public class SrcMLFileCreatedEventArgs : EventArgs
    {
        public SrcMLFileCreatedEventArgs(string path)
        {
            Path = path;
        }

        public string Path
        {
            get;
            set;
        }
    }

    public class OpenFileEventArgs : EventArgs
    {
        public OpenFileEventArgs(string filePath, int lineNumber, int endLineNumber)
        {
            Path = filePath;
            LineNumber = lineNumber;
            EndLineNumber = endLineNumber;
        }

        public string Path
        {
            get;
            set;
        }

        public int LineNumber
        {
            get;
            set;
        }

        public int EndLineNumber
        {
            get;
            set;
        }
    }
}
