using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TreeCMS.Models;
using TreeCMS.Services;

namespace TreeCMS.Forms
{
    public partial class MainForm : Form
    {
        private readonly DataManager _dataManager;
        private readonly SearchService _searchService;
        private List<NodeData> _allNodes;
        private NodeData _selectedNode;
        private DescriptionItem _selectedDescription;

        public MainForm()
        {
            InitializeComponent();
            _dataManager = new DataManager();
            _searchService = new SearchService(_dataManager);
            _allNodes = new List<NodeData>();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadTreeFromStorage();
        }

        #region Tree Loading & Binding

        /// <summary>
        /// Reads tree.txt via DataManager and builds the TreeView hierarchy.
        /// </summary>
        private void LoadTreeFromStorage()
        {
            try
            {
                treeViewCategories.BeginUpdate();
                treeViewCategories.Nodes.Clear();

                _allNodes = _dataManager.LoadAllNodes();

                // Map nodes by Id
                Dictionary<string, TreeNode> nodeMap = new Dictionary<string, TreeNode>();

                // First pass: create all TreeNodes
                foreach (NodeData data in _allNodes)
                {
                    TreeNode tn = new TreeNode(data.Name);
                    tn.Tag = data;
                    nodeMap[data.Id] = tn;
                }

                // Second pass: attach child nodes to parents or root
                foreach (NodeData data in _allNodes)
                {
                    TreeNode tn = nodeMap[data.Id];
                    if (!string.IsNullOrEmpty(data.ParentId) && nodeMap.ContainsKey(data.ParentId))
                    {
                        nodeMap[data.ParentId].Nodes.Add(tn);
                    }
                    else
                    {
                        treeViewCategories.Nodes.Add(tn);
                    }
                }

                treeViewCategories.ExpandAll();
                treeViewCategories.EndUpdate();

                statusNodeCount.Text = "Nodes: " + _allNodes.Count;

                // Select first node if available
                if (treeViewCategories.Nodes.Count > 0)
                {
                    treeViewCategories.SelectedNode = treeViewCategories.Nodes[0];
                }
                else
                {
                    ClearNodeDetails();
                }

                SetStatus("Loaded " + _allNodes.Count + " nodes from storage.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load tree: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TreeViewCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Tag == null)
            {
                ClearNodeDetails();
                return;
            }

            _selectedNode = e.Node.Tag as NodeData;
            DisplaySelectedNodeDetails();
        }

        private void DisplaySelectedNodeDetails()
        {
            if (_selectedNode == null)
            {
                ClearNodeDetails();
                return;
            }

            // Update header & breadcrumbs
            txtRenameNode.Text = _selectedNode.Name;
            lblBreadcrumb.Text = "Path: " + GetCurrentNodeBreadcrumb(treeViewCategories.SelectedNode);

            // 1. Hyperlinks
            RefreshHyperlinksList();

            // 2. Uploaded Files
            RefreshFilesList();

            // 3. Descriptions
            RefreshDescriptionsList();

            SetStatus("Active node: " + _selectedNode.Name);
        }

        private void ClearNodeDetails()
        {
            _selectedNode = null;
            _selectedDescription = null;
            txtRenameNode.Text = string.Empty;
            lblBreadcrumb.Text = "Path: (No node selected)";
            lvHyperlinks.Items.Clear();
            txtLinkTitle.Text = string.Empty;
            txtLinkUrl.Text = string.Empty;
            lvFiles.Items.Clear();
            lbDescriptions.Items.Clear();
            txtDescTitle.Text = string.Empty;
            rtbDescriptionContent.Clear();
            lblDescStatus.Text = string.Empty;
        }

        private string GetCurrentNodeBreadcrumb(TreeNode node)
        {
            if (node == null) return string.Empty;
            List<string> parts = new List<string>();
            TreeNode curr = node;
            while (curr != null)
            {
                parts.Insert(0, curr.Text);
                curr = curr.Parent;
            }
            return string.Join(" > ", parts.ToArray());
        }

        #endregion

        #region Node CRUD Operations

        private void BtnAddRoot_Click(object sender, EventArgs e)
        {
            string name = PromptInput("Enter Root Node Name:", "Add Root Node");
            if (string.IsNullOrWhiteSpace(name))
            {
                return; // User canceled or entered empty
            }

            try
            {
                string newId = GenerateNextNodeId();
                string folder = _dataManager.GenerateFolderName(name, newId);

                NodeData newNode = new NodeData(newId, string.Empty, name.Trim(), folder);
                _dataManager.CreateNodeStorage(newNode);

                _allNodes.Add(newNode);
                _dataManager.SaveTree(_allNodes);

                TreeNode tn = new TreeNode(newNode.Name);
                tn.Tag = newNode;
                treeViewCategories.Nodes.Add(tn);
                treeViewCategories.SelectedNode = tn;

                statusNodeCount.Text = "Nodes: " + _allNodes.Count;
                SetStatus("Root node '" + name + "' created successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding root node: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddChild_Click(object sender, EventArgs e)
        {
            if (treeViewCategories.SelectedNode == null || _selectedNode == null)
            {
                MessageBox.Show("Please select a parent node first.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = PromptInput("Enter Child Node Name for '" + _selectedNode.Name + "':", "Add Child Node");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            try
            {
                string newId = GenerateNextNodeId();
                string folder = _dataManager.GenerateFolderName(name, newId);

                NodeData newNode = new NodeData(newId, _selectedNode.Id, name.Trim(), folder);
                _dataManager.CreateNodeStorage(newNode);

                _allNodes.Add(newNode);
                _dataManager.SaveTree(_allNodes);

                TreeNode parentTn = treeViewCategories.SelectedNode;
                TreeNode childTn = new TreeNode(newNode.Name);
                childTn.Tag = newNode;
                parentTn.Nodes.Add(childTn);
                parentTn.Expand();
                treeViewCategories.SelectedNode = childTn;

                statusNodeCount.Text = "Nodes: " + _allNodes.Count;
                SetStatus("Child node '" + name + "' added under '" + _selectedNode.Name + "'.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding child node: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateNodeName_Click(object sender, EventArgs e)
        {
            if (treeViewCategories.SelectedNode == null || _selectedNode == null)
            {
                MessageBox.Show("Please select a node to update.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newName = txtRenameNode.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Node name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRenameNode.Focus();
                return;
            }

            try
            {
                _selectedNode.Name = newName;
                treeViewCategories.SelectedNode.Text = newName;

                _dataManager.UpdateNodeName(_selectedNode);
                _dataManager.SaveTree(_allNodes);

                lblBreadcrumb.Text = "Path: " + GetCurrentNodeBreadcrumb(treeViewCategories.SelectedNode);
                SetStatus("Node renamed to '" + newName + "'.");
                MessageBox.Show("Node name updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating node: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteNode_Click(object sender, EventArgs e)
        {
            if (treeViewCategories.SelectedNode == null || _selectedNode == null)
            {
                MessageBox.Show("Please select a node to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to delete node '" + _selectedNode.Name + "' and ALL its sub-nodes, hyperlinks, descriptions, and uploaded files?\n\nThis operation cannot be undone.",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // Collect node and all recursive descendant nodes to delete
                List<NodeData> nodesToDelete = new List<NodeData>();
                CollectDescendants(_selectedNode.Id, nodesToDelete);
                nodesToDelete.Add(_selectedNode);

                foreach (NodeData n in nodesToDelete)
                {
                    _dataManager.DeleteNodeStorage(n);
                    _allNodes.Remove(n);
                }

                _dataManager.SaveTree(_allNodes);

                TreeNode selectedTn = treeViewCategories.SelectedNode;
                TreeNode parentTn = selectedTn.Parent;
                selectedTn.Remove();

                if (parentTn != null)
                {
                    treeViewCategories.SelectedNode = parentTn;
                }
                else if (treeViewCategories.Nodes.Count > 0)
                {
                    treeViewCategories.SelectedNode = treeViewCategories.Nodes[0];
                }
                else
                {
                    ClearNodeDetails();
                }

                statusNodeCount.Text = "Nodes: " + _allNodes.Count;
                SetStatus("Node and associated files deleted.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting node: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CollectDescendants(string parentId, List<NodeData> result)
        {
            foreach (NodeData node in _allNodes)
            {
                if (node.ParentId == parentId)
                {
                    result.Add(node);
                    CollectDescendants(node.Id, result);
                }
            }
        }

        private string GenerateNextNodeId()
        {
            int maxId = 0;
            foreach (NodeData n in _allNodes)
            {
                int idVal;
                if (int.TryParse(n.Id, out idVal))
                {
                    if (idVal > maxId) maxId = idVal;
                }
            }
            return (maxId + 1).ToString();
        }

        private void BtnExpandAll_Click(object sender, EventArgs e)
        {
            treeViewCategories.ExpandAll();
        }

        private void BtnCollapseAll_Click(object sender, EventArgs e)
        {
            treeViewCategories.CollapseAll();
        }

        #endregion

        #region Hyperlinks Operations

        private void RefreshHyperlinksList()
        {
            lvHyperlinks.Items.Clear();
            if (_selectedNode == null || _selectedNode.Hyperlinks == null) return;

            foreach (HyperlinkItem link in _selectedNode.Hyperlinks)
            {
                ListViewItem lvi = new ListViewItem(link.Title);
                lvi.SubItems.Add(link.Url);
                lvi.Tag = link;
                lvHyperlinks.Items.Add(lvi);
            }
        }

        private void LvHyperlinks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvHyperlinks.SelectedItems.Count > 0)
            {
                HyperlinkItem item = lvHyperlinks.SelectedItems[0].Tag as HyperlinkItem;
                if (item != null)
                {
                    txtLinkTitle.Text = item.Title;
                    txtLinkUrl.Text = item.Url;
                }
            }
        }

        private void BtnAddLink_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null)
            {
                MessageBox.Show("Please select a category node first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string title = txtLinkTitle.Text.Trim();
            string url = txtLinkUrl.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please provide both Link Title and URL.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            try
            {
                HyperlinkItem link = new HyperlinkItem(title, url);
                _selectedNode.Hyperlinks.Add(link);
                _dataManager.SaveHyperlinks(_selectedNode);

                RefreshHyperlinksList();
                txtLinkTitle.Clear();
                txtLinkUrl.Clear();
                SetStatus("Hyperlink '" + title + "' added.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving hyperlink: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEditLink_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null || lvHyperlinks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a hyperlink from the list to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HyperlinkItem selectedLink = lvHyperlinks.SelectedItems[0].Tag as HyperlinkItem;
            if (selectedLink == null) return;

            string title = txtLinkTitle.Text.Trim();
            string url = txtLinkUrl.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Link Title and URL cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            try
            {
                selectedLink.Title = title;
                selectedLink.Url = url;
                _dataManager.SaveHyperlinks(_selectedNode);

                RefreshHyperlinksList();
                SetStatus("Hyperlink updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating hyperlink: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteLink_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null || lvHyperlinks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a hyperlink from the list to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HyperlinkItem selectedLink = lvHyperlinks.SelectedItems[0].Tag as HyperlinkItem;
            if (selectedLink == null) return;

            DialogResult confirm = MessageBox.Show(
                "Delete hyperlink '" + selectedLink.Title + "'?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    _selectedNode.Hyperlinks.Remove(selectedLink);
                    _dataManager.SaveHyperlinks(_selectedNode);

                    RefreshHyperlinksList();
                    txtLinkTitle.Clear();
                    txtLinkUrl.Clear();
                    SetStatus("Hyperlink removed.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting hyperlink: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnOpenLink_Click(object sender, EventArgs e)
        {
            OpenSelectedHyperlink();
        }

        private void LvHyperlinks_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedHyperlink();
        }

        private void OpenSelectedHyperlink()
        {
            if (lvHyperlinks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a hyperlink to open.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HyperlinkItem link = lvHyperlinks.SelectedItems[0].Tag as HyperlinkItem;
            if (link != null && !string.IsNullOrEmpty(link.Url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(link.Url) { UseShellExecute = true });
                    SetStatus("Opening URL: " + link.Url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region File Upload Operations

        private void RefreshFilesList()
        {
            lvFiles.Items.Clear();
            if (_selectedNode == null) return;

            try
            {
                FileInfo[] files = _dataManager.GetUploadedFiles(_selectedNode);
                foreach (FileInfo fi in files)
                {
                    ListViewItem lvi = new ListViewItem(fi.Name);
                    lvi.SubItems.Add(fi.Extension.ToUpperInvariant());
                    lvi.SubItems.Add(string.Format("{0:N0}", fi.Length));
                    lvi.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                    lvi.Tag = fi;
                    lvFiles.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading uploaded files: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUploadFile_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null)
            {
                MessageBox.Show("Please select a category node first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select Files to Upload to " + _selectedNode.Name;
                ofd.Multiselect = true;
                ofd.Filter = "All Files (*.*)|*.*|PDF Documents (*.pdf)|*.pdf|Word Documents (*.docx;*.doc)|*.docx;*.doc|Text Files (*.txt)|*.txt|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    int uploadedCount = 0;
                    foreach (string sourceFile in ofd.FileNames)
                    {
                        try
                        {
                            _dataManager.UploadFile(_selectedNode, sourceFile, overwrite: true);
                            uploadedCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not upload " + Path.GetFileName(sourceFile) + ": " + ex.Message, "Upload Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    RefreshFilesList();
                    SetStatus("Uploaded " + uploadedCount + " file(s) into " + _selectedNode.Name);
                }
            }
        }

        private void BtnOpenFile_Click(object sender, EventArgs e)
        {
            OpenSelectedFile();
        }

        private void LvFiles_DoubleClick(object sender, EventArgs e)
        {
            OpenSelectedFile();
        }

        private void OpenSelectedFile()
        {
            if (lvFiles.SelectedItems.Count == 0 || _selectedNode == null)
            {
                MessageBox.Show("Please select a file to open.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileInfo fi = lvFiles.SelectedItems[0].Tag as FileInfo;
            if (fi != null && File.Exists(fi.FullName))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(fi.FullName) { UseShellExecute = true });
                    SetStatus("Opened file: " + fi.Name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not launch file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDeleteFile_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 0 || _selectedNode == null)
            {
                MessageBox.Show("Please select a file to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FileInfo fi = lvFiles.SelectedItems[0].Tag as FileInfo;
            if (fi == null) return;

            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to delete file '" + fi.Name + "'?",
                "Confirm File Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    _dataManager.DeleteUploadedFile(_selectedNode, fi.Name);
                    RefreshFilesList();
                    SetStatus("Deleted file: " + fi.Name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnOpenNodeFolder_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null) return;
            string folder = _dataManager.GetNodeFilesPath(_selectedNode);
            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            }
        }

        #endregion

        #region Rich Text Descriptions Operations

        private void RefreshDescriptionsList()
        {
            lbDescriptions.Items.Clear();
            _selectedDescription = null;
            txtDescTitle.Text = string.Empty;
            rtbDescriptionContent.Clear();
            lblDescStatus.Text = string.Empty;

            if (_selectedNode == null || _selectedNode.Descriptions == null) return;

            foreach (DescriptionItem desc in _selectedNode.Descriptions)
            {
                lbDescriptions.Items.Add(desc);
            }

            if (lbDescriptions.Items.Count > 0)
            {
                lbDescriptions.SelectedIndex = 0;
            }
        }

        private void LbDescriptions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbDescriptions.SelectedItem == null)
            {
                _selectedDescription = null;
                txtDescTitle.Text = string.Empty;
                rtbDescriptionContent.Clear();
                return;
            }

            _selectedDescription = lbDescriptions.SelectedItem as DescriptionItem;
            if (_selectedDescription != null && _selectedNode != null)
            {
                txtDescTitle.Text = _selectedDescription.Title;
                string raw = _dataManager.ReadDescriptionContent(_selectedNode, _selectedDescription);

                try
                {
                    if (raw.StartsWith("{\\rtf"))
                    {
                        rtbDescriptionContent.Rtf = raw;
                    }
                    else
                    {
                        rtbDescriptionContent.Text = raw;
                    }
                }
                catch
                {
                    rtbDescriptionContent.Text = raw;
                }

                lblDescStatus.Text = "Loaded: " + _selectedDescription.FileName;
            }
        }

        private void BtnAddDescription_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null)
            {
                MessageBox.Show("Please select a category node first.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string title = PromptInput("Enter Description Title:", "Add Description");
            if (string.IsNullOrWhiteSpace(title)) return;

            try
            {
                // Generate unique filename e.g. desc_3.rtf
                int maxNum = 0;
                foreach (DescriptionItem item in _selectedNode.Descriptions)
                {
                    string f = item.FileName.Replace("desc_", string.Empty).Replace(".rtf", string.Empty).Replace(".txt", string.Empty);
                    int n;
                    if (int.TryParse(f, out n))
                    {
                        if (n > maxNum) maxNum = n;
                    }
                }
                string newFileName = string.Format("desc_{0}.rtf", maxNum + 1);

                DescriptionItem newItem = new DescriptionItem(newFileName, title.Trim());
                _selectedNode.Descriptions.Add(newItem);

                // Initial basic content
                _dataManager.WriteDescriptionContent(_selectedNode, newItem, "");
                _dataManager.SaveDescriptionsIndex(_selectedNode);

                RefreshDescriptionsList();
                lbDescriptions.SelectedItem = newItem;
                SetStatus("New description created.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding description: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteDescription_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null || _selectedDescription == null)
            {
                MessageBox.Show("Please select a description to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Delete description '" + _selectedDescription.Title + "'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    _dataManager.DeleteDescription(_selectedNode, _selectedDescription);
                    RefreshDescriptionsList();
                    SetStatus("Description deleted.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting description: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnSaveDescription_Click(object sender, EventArgs e)
        {
            if (_selectedNode == null || _selectedDescription == null)
            {
                MessageBox.Show("Please select a description to save.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string title = txtDescTitle.Text.Trim();
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Description title cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _selectedDescription.Title = title;
                string rtfText = rtbDescriptionContent.Rtf;

                _dataManager.WriteDescriptionContent(_selectedNode, _selectedDescription, rtfText);
                _dataManager.SaveDescriptionsIndex(_selectedNode);

                // Refresh ListBox item text without resetting cursor
                int idx = lbDescriptions.SelectedIndex;
                lbDescriptions.Items[idx] = _selectedDescription;

                lblDescStatus.Text = "Saved at " + DateTime.Now.ToString("HH:mm:ss");
                SetStatus("Description '" + title + "' saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving description: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region RichText Formatting Toolbar

        private void TsbBold_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Bold);
        }

        private void TsbItalic_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Italic);
        }

        private void TsbUnderline_Click(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Underline);
        }

        private void ToggleFontStyle(FontStyle style)
        {
            if (rtbDescriptionContent.SelectionFont == null) return;

            FontStyle currentStyle = rtbDescriptionContent.SelectionFont.Style;
            FontStyle newStyle = currentStyle ^ style; // Toggle bitwise

            rtbDescriptionContent.SelectionFont = new Font(
                rtbDescriptionContent.SelectionFont.FontFamily,
                rtbDescriptionContent.SelectionFont.Size,
                newStyle
            );
        }

        private void TscbFontFamily_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tscbFontFamily.SelectedItem == null || rtbDescriptionContent.SelectionFont == null) return;
            string familyName = tscbFontFamily.SelectedItem.ToString();

            try
            {
                rtbDescriptionContent.SelectionFont = new Font(
                    familyName,
                    rtbDescriptionContent.SelectionFont.Size,
                    rtbDescriptionContent.SelectionFont.Style
                );
            }
            catch { }
        }

        private void TscbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tscbFontSize.SelectedItem == null || rtbDescriptionContent.SelectionFont == null) return;
            float size;
            if (float.TryParse(tscbFontSize.SelectedItem.ToString(), out size))
            {
                try
                {
                    rtbDescriptionContent.SelectionFont = new Font(
                        rtbDescriptionContent.SelectionFont.FontFamily,
                        size,
                        rtbDescriptionContent.SelectionFont.Style
                    );
                }
                catch { }
            }
        }

        private void TsbColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                cd.Color = rtbDescriptionContent.SelectionColor;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    rtbDescriptionContent.SelectionColor = cd.Color;
                }
            }
        }

        private void TsbBullet_Click(object sender, EventArgs e)
        {
            rtbDescriptionContent.SelectionBullet = !rtbDescriptionContent.SelectionBullet;
        }

        #endregion

        #endregion

        #region Keyword Search Operations

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            string keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Please enter a keyword to search.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                List<SearchResultItem> results = _searchService.Search(_allNodes, keyword);
                lvSearchResults.Items.Clear();

                if (results.Count == 0)
                {
                    panelSearchResults.Visible = true;
                    lblSearchResultsHeader.Text = "No results found for '" + keyword + "'.";
                    SetStatus("Search completed. 0 matches found.");
                    return;
                }

                foreach (SearchResultItem item in results)
                {
                    ListViewItem lvi = new ListViewItem(item.NodeName);
                    lvi.SubItems.Add(item.Category.ToString());
                    lvi.SubItems.Add(item.MatchedText + " (" + item.Detail + ")");
                    lvi.SubItems.Add(item.NodePath);
                    lvi.Tag = item;
                    lvSearchResults.Items.Add(lvi);
                }

                panelSearchResults.Visible = true;
                lblSearchResultsHeader.Text = string.Format("Search Results for '{0}' ({1} match{2} found - Double-click to navigate):",
                    keyword, results.Count, results.Count == 1 ? "" : "es");

                SetStatus("Search found " + results.Count + " result(s).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            lvSearchResults.Items.Clear();
            panelSearchResults.Visible = false;
            SetStatus("Search cleared.");
        }

        private void BtnCloseSearchResults_Click(object sender, EventArgs e)
        {
            panelSearchResults.Visible = false;
        }

        private void LvSearchResults_DoubleClick(object sender, EventArgs e)
        {
            if (lvSearchResults.SelectedItems.Count == 0) return;

            SearchResultItem item = lvSearchResults.SelectedItems[0].Tag as SearchResultItem;
            if (item == null) return;

            // Find and select the corresponding TreeNode in TreeView
            TreeNode target = FindTreeNodeByNodeId(treeViewCategories.Nodes, item.NodeId);
            if (target != null)
            {
                treeViewCategories.SelectedNode = target;
                target.EnsureVisible();
                treeViewCategories.Focus();

                // Automatically switch to the matched tab
                if (item.Category == MatchCategory.Hyperlink)
                {
                    tabControlContent.SelectedTab = tabHyperlinks;
                }
                else if (item.Category == MatchCategory.UploadedFile)
                {
                    tabControlContent.SelectedTab = tabFiles;
                }
                else if (item.Category == MatchCategory.Description)
                {
                    tabControlContent.SelectedTab = tabDescriptions;
                }

                SetStatus("Navigated to: " + item.NodePath);
            }
        }

        private TreeNode FindTreeNodeByNodeId(TreeNodeCollection collection, string nodeId)
        {
            foreach (TreeNode tn in collection)
            {
                NodeData data = tn.Tag as NodeData;
                if (data != null && data.Id == nodeId)
                {
                    return tn;
                }

                TreeNode childMatch = FindTreeNodeByNodeId(tn.Nodes, nodeId);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }
            return null;
        }

        #endregion

        #region Helper Dialogs & Menu Actions

        private string PromptInput(string prompt, string title)
        {
            using (Form promptForm = new Form())
            {
                promptForm.Width = 420;
                promptForm.Height = 170;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = title;
                promptForm.StartPosition = FormStartPosition.CenterParent;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 18, Text = prompt, AutoSize = true };
                TextBox textBox = new TextBox() { Left = 20, Top = 42, Width = 360, Font = new Font("Segoe UI", 9.5F) };
                Button confirmation = new Button() { Text = "OK", Left = 220, Width = 75, Top = 82, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button cancel = new Button() { Text = "Cancel", Left = 305, Width = 75, Top = 82, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };

                confirmation.Click += (sender, e) => { promptForm.Close(); };
                promptForm.Controls.Add(textLabel);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(confirmation);
                promptForm.Controls.Add(cancel);
                promptForm.AcceptButton = confirmation;
                promptForm.CancelButton = cancel;

                return promptForm.ShowDialog(this) == DialogResult.OK ? textBox.Text : string.Empty;
            }
        }

        private void MenuOpenStorage_Click(object sender, EventArgs e)
        {
            try
            {
                string path = _dataManager.BaseDataPath;
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open data folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Tree-Based Content Management System\n\n" +
                "Developed for: Coding Skills 2 - Desktop Application Project\n" +
                "Architecture: C# Windows Forms with Text-File (.txt/RTF) Backend\n" +
                "Features:\n" +
                "- Hierarchical Category/Subcategory TreeView\n" +
                "- Full CRUD for Nodes, Hyperlinks, Files & Descriptions\n" +
                "- Rich-Text Description Editor with RTF formatting\n" +
                "- Case-insensitive keyword search across all content\n" +
                "- Pure File System backend (tree.txt, node.txt, hyperlinks.txt, descriptions.txt, Files/)",
                "About TreeCMS",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void SetStatus(string message)
        {
            statusLabel.Text = message;
        }

        #endregion
    }
}
