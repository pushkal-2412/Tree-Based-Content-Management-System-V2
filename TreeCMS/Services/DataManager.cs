using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TreeCMS.Models;

namespace TreeCMS.Services
{
    /// <summary>
    /// Service responsible for managing all file-based operations.
    /// Uses simple text files (.txt) and local directories as the storage backend:
    /// - Data/tree.txt
    /// - Data/Nodes/{FolderName}/node.txt
    /// - Data/Nodes/{FolderName}/hyperlinks.txt
    /// - Data/Nodes/{FolderName}/descriptions.txt
    /// - Data/Nodes/{FolderName}/desc_*.rtf
    /// - Data/Nodes/{FolderName}/Files/
    /// </summary>
    public class DataManager
    {
        private readonly string _baseDataPath;
        private readonly string _nodesPath;
        private readonly string _treeFilePath;

        public string BaseDataPath { get { return _baseDataPath; } }
        public string NodesPath { get { return _nodesPath; } }

        public DataManager()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // When run from Windows Forms, Application.StartupPath points directly to the exe location
            try
            {
                if (!string.IsNullOrEmpty(System.Windows.Forms.Application.StartupPath) &&
                    !System.Windows.Forms.Application.StartupPath.Contains("System32"))
                {
                    baseDir = System.Windows.Forms.Application.StartupPath;
                }
            }
            catch { }

            _baseDataPath = Path.Combine(baseDir, "Data");
            _nodesPath = Path.Combine(_baseDataPath, "Nodes");
            _treeFilePath = Path.Combine(_baseDataPath, "tree.txt");

            EnsureStorageInitialized();
        }

        public DataManager(string customBasePath)
        {
            _baseDataPath = customBasePath;
            _nodesPath = Path.Combine(_baseDataPath, "Nodes");
            _treeFilePath = Path.Combine(_baseDataPath, "tree.txt");

            EnsureStorageInitialized();
        }

        /// <summary>
        /// Ensures Data and Data/Nodes directories exist. If tree.txt does not exist,
        /// creates it and populates sample educational content for viva demonstration.
        /// </summary>
        public void EnsureStorageInitialized()
        {
            if (!Directory.Exists(_baseDataPath))
            {
                Directory.CreateDirectory(_baseDataPath);
            }

            if (!Directory.Exists(_nodesPath))
            {
                Directory.CreateDirectory(_nodesPath);
            }

            if (!File.Exists(_treeFilePath))
            {
                SeedDefaultData();
            }
        }

        #region Tree & Node Operations

        /// <summary>
        /// Reads all nodes from Data/tree.txt and loads their associated hyperlinks and descriptions.
        /// </summary>
        public List<NodeData> LoadAllNodes()
        {
            List<NodeData> nodes = new List<NodeData>();

            if (!File.Exists(_treeFilePath))
            {
                return nodes;
            }

            string[] lines = File.ReadAllLines(_treeFilePath, Encoding.UTF8);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                {
                    continue; // Skip comments and blank lines
                }

                // Format: Id|ParentId|Name|FolderName
                string[] parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    string id = parts[0].Trim();
                    string parentId = parts[1].Trim();
                    string name = parts[2].Trim();
                    string folderName = parts[3].Trim();

                    NodeData node = new NodeData(id, parentId, name, folderName);

                    // Load node-specific metadata
                    LoadNodeDetails(node);

                    nodes.Add(node);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Saves the list of nodes back to Data/tree.txt.
        /// </summary>
        public void SaveTree(List<NodeData> nodes)
        {
            List<string> lines = new List<string>();
            lines.Add("# Tree-Based CMS Hierarchy File");
            lines.Add("# Format: NodeId|ParentId|NodeName|FolderName");

            foreach (NodeData node in nodes)
            {
                lines.Add(string.Format("{0}|{1}|{2}|{3}",
                    node.Id,
                    node.ParentId ?? string.Empty,
                    node.Name,
                    node.FolderName));
            }

            File.WriteAllLines(_treeFilePath, lines.ToArray(), Encoding.UTF8);
        }

        /// <summary>
        /// Creates a new node folder, node.txt, hyperlinks.txt, descriptions.txt, and Files/ subfolder.
        /// </summary>
        public void CreateNodeStorage(NodeData node)
        {
            string folderPath = GetNodeFolderPath(node);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filesPath = GetNodeFilesPath(node);
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }

            // Write node.txt
            SaveNodeMetadata(node);

            // Write empty hyperlinks.txt and descriptions.txt if they do not exist
            string hyperlinksPath = Path.Combine(folderPath, "hyperlinks.txt");
            if (!File.Exists(hyperlinksPath))
            {
                File.WriteAllText(hyperlinksPath, string.Empty, Encoding.UTF8);
            }

            string descriptionsPath = Path.Combine(folderPath, "descriptions.txt");
            if (!File.Exists(descriptionsPath))
            {
                File.WriteAllText(descriptionsPath, string.Empty, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Updates the node's name in node.txt and keeps folder in sync.
        /// </summary>
        public void UpdateNodeName(NodeData node)
        {
            SaveNodeMetadata(node);
        }

        /// <summary>
        /// Deletes the node's storage folder and all files inside it.
        /// </summary>
        public void DeleteNodeStorage(NodeData node)
        {
            string folderPath = GetNodeFolderPath(node);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
        }

        /// <summary>
        /// Generates a safe folder name from node name and ID.
        /// </summary>
        public string GenerateFolderName(string name, string id)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalidChars, c) < 0 && c != ' ')
                {
                    sb.Append(c);
                }
                else if (c == ' ')
                {
                    sb.Append('_');
                }
            }

            string clean = sb.ToString();
            if (string.IsNullOrEmpty(clean))
            {
                clean = "Node";
            }

            return string.Format("{0}_{1}", clean, id);
        }

        private void SaveNodeMetadata(NodeData node)
        {
            string folderPath = GetNodeFolderPath(node);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string nodeTxtPath = Path.Combine(folderPath, "node.txt");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ID: " + node.Id);
            sb.AppendLine("Name: " + node.Name);
            sb.AppendLine("ParentID: " + (node.ParentId ?? "0"));
            sb.AppendLine("Created: " + node.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));

            File.WriteAllText(nodeTxtPath, sb.ToString(), Encoding.UTF8);
        }

        private void LoadNodeDetails(NodeData node)
        {
            node.Hyperlinks = LoadHyperlinks(node);
            node.Descriptions = LoadDescriptions(node);

            // Read created date from node.txt if available
            string nodeTxtPath = Path.Combine(GetNodeFolderPath(node), "node.txt");
            if (File.Exists(nodeTxtPath))
            {
                string[] lines = File.ReadAllLines(nodeTxtPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Created: "))
                    {
                        DateTime dt;
                        if (DateTime.TryParse(line.Substring("Created: ".Length).Trim(), out dt))
                        {
                            node.CreatedDate = dt;
                        }
                    }
                }
            }
        }

        #endregion

        #region Hyperlinks Operations

        /// <summary>
        /// Loads hyperlinks from Data/Nodes/{FolderName}/hyperlinks.txt
        /// Format: Title|URL
        /// </summary>
        public List<HyperlinkItem> LoadHyperlinks(NodeData node)
        {
            List<HyperlinkItem> links = new List<HyperlinkItem>();
            string filePath = Path.Combine(GetNodeFolderPath(node), "hyperlinks.txt");

            if (!File.Exists(filePath))
            {
                return links;
            }

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    links.Add(new HyperlinkItem(parts[0].Trim(), parts[1].Trim()));
                }
            }

            return links;
        }

        /// <summary>
        /// Saves hyperlinks to Data/Nodes/{FolderName}/hyperlinks.txt
        /// </summary>
        public void SaveHyperlinks(NodeData node)
        {
            string filePath = Path.Combine(GetNodeFolderPath(node), "hyperlinks.txt");
            List<string> lines = new List<string>();

            if (node.Hyperlinks != null)
            {
                foreach (HyperlinkItem item in node.Hyperlinks)
                {
                    lines.Add(string.Format("{0}|{1}", item.Title, item.Url));
                }
            }

            File.WriteAllLines(filePath, lines.ToArray(), Encoding.UTF8);
        }

        #endregion

        #region Descriptions Operations

        /// <summary>
        /// Loads the descriptions index from Data/Nodes/{FolderName}/descriptions.txt
        /// Format: FileName|Title
        /// </summary>
        public List<DescriptionItem> LoadDescriptions(NodeData node)
        {
            List<DescriptionItem> list = new List<DescriptionItem>();
            string indexPath = Path.Combine(GetNodeFolderPath(node), "descriptions.txt");

            if (!File.Exists(indexPath))
            {
                return list;
            }

            string[] lines = File.ReadAllLines(indexPath, Encoding.UTF8);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split('|');
                if (parts.Length >= 2)
                {
                    list.Add(new DescriptionItem(parts[0].Trim(), parts[1].Trim()));
                }
            }

            return list;
        }

        /// <summary>
        /// Saves the list of descriptions metadata into Data/Nodes/{FolderName}/descriptions.txt
        /// </summary>
        public void SaveDescriptionsIndex(NodeData node)
        {
            string indexPath = Path.Combine(GetNodeFolderPath(node), "descriptions.txt");
            List<string> lines = new List<string>();

            if (node.Descriptions != null)
            {
                foreach (DescriptionItem item in node.Descriptions)
                {
                    lines.Add(string.Format("{0}|{1}", item.FileName, item.Title));
                }
            }

            File.WriteAllLines(indexPath, lines.ToArray(), Encoding.UTF8);
        }

        /// <summary>
        /// Reads RTF/text content from the description file.
        /// </summary>
        public string ReadDescriptionContent(NodeData node, DescriptionItem desc)
        {
            string filePath = Path.Combine(GetNodeFolderPath(node), desc.FileName);
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Writes RTF/text content to the description file.
        /// </summary>
        public void WriteDescriptionContent(NodeData node, DescriptionItem desc, string content)
        {
            string filePath = Path.Combine(GetNodeFolderPath(node), desc.FileName);
            File.WriteAllText(filePath, content ?? string.Empty, Encoding.UTF8);
        }

        /// <summary>
        /// Deletes an individual description file and updates descriptions.txt index.
        /// </summary>
        public void DeleteDescription(NodeData node, DescriptionItem desc)
        {
            string filePath = Path.Combine(GetNodeFolderPath(node), desc.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (node.Descriptions != null)
            {
                node.Descriptions.Remove(desc);
            }

            SaveDescriptionsIndex(node);
        }

        #endregion

        #region File Upload Operations

        /// <summary>
        /// Returns all files currently uploaded inside Data/Nodes/{FolderName}/Files/
        /// </summary>
        public FileInfo[] GetUploadedFiles(NodeData node)
        {
            string filesDir = GetNodeFilesPath(node);
            if (!Directory.Exists(filesDir))
            {
                Directory.CreateDirectory(filesDir);
            }

            DirectoryInfo di = new DirectoryInfo(filesDir);
            return di.GetFiles();
        }

        /// <summary>
        /// Copies a file from sourceFilePath into Data/Nodes/{FolderName}/Files/
        /// </summary>
        public string UploadFile(NodeData node, string sourceFilePath, bool overwrite)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source file does not exist: " + sourceFilePath);
            }

            string filesDir = GetNodeFilesPath(node);
            if (!Directory.Exists(filesDir))
            {
                Directory.CreateDirectory(filesDir);
            }

            string fileName = Path.GetFileName(sourceFilePath);
            string targetPath = Path.Combine(filesDir, fileName);

            if (File.Exists(targetPath) && !overwrite)
            {
                throw new IOException("A file named '" + fileName + "' already exists in this node.");
            }

            File.Copy(sourceFilePath, targetPath, true);
            return targetPath;
        }

        /// <summary>
        /// Deletes an uploaded file from Data/Nodes/{FolderName}/Files/
        /// </summary>
        public void DeleteUploadedFile(NodeData node, string fileName)
        {
            string filesDir = GetNodeFilesPath(node);
            string targetPath = Path.Combine(filesDir, fileName);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }

        /// <summary>
        /// Gets the full path of an uploaded file.
        /// </summary>
        public string GetUploadedFilePath(NodeData node, string fileName)
        {
            return Path.Combine(GetNodeFilesPath(node), fileName);
        }

        #endregion

        #region Helper Paths

        public string GetNodeFolderPath(NodeData node)
        {
            return Path.Combine(_nodesPath, node.FolderName);
        }

        public string GetNodeFilesPath(NodeData node)
        {
            return Path.Combine(GetNodeFolderPath(node), "Files");
        }

        #endregion

        #region Seed Default Sample Data

        /// <summary>
        /// Seeds the default categories hierarchy matching the project specification:
        /// Categories
        /// ├── Programming
        /// │   ├── Java
        /// │   ├── Python
        /// │   └── C Programming
        /// ├── Database
        /// │   ├── SQL
        /// │   └── MongoDB
        /// └── Operating Systems
        /// </summary>
        private void SeedDefaultData()
        {
            List<NodeData> nodes = new List<NodeData>();

            // 1. Programming (Root)
            NodeData nProg = new NodeData("1", "", "Programming", "Programming");
            nProg.Hyperlinks.Add(new HyperlinkItem("Developer Resources", "https://learn.microsoft.com"));
            nProg.Hyperlinks.Add(new HyperlinkItem("Community Forum", "https://stackoverflow.com"));
            nodes.Add(nProg);

            // 2. Java (Child of 1)
            NodeData nJava = new NodeData("2", "1", "Java", "Java");
            nJava.Hyperlinks.Add(new HyperlinkItem("Language Reference", "https://dev.java"));
            nodes.Add(nJava);

            // 3. Python (Child of 1)
            NodeData nPython = new NodeData("3", "1", "Python", "Python");
            nPython.Hyperlinks.Add(new HyperlinkItem("Python Documentation", "https://www.python.org"));
            nodes.Add(nPython);

            // 4. C Programming (Child of 1)
            NodeData nCProg = new NodeData("4", "1", "C Programming", "C_Programming");
            nCProg.Hyperlinks.Add(new HyperlinkItem("Standard Library Guide", "https://en.cppreference.com"));
            nodes.Add(nCProg);

            // 5. Database (Root)
            NodeData nDb = new NodeData("5", "", "Database", "Database");
            nodes.Add(nDb);

            // 6. SQL (Child of 5)
            NodeData nSql = new NodeData("6", "5", "SQL", "SQL");
            nSql.Hyperlinks.Add(new HyperlinkItem("SQL Syntax Guide", "https://www.w3schools.com/sql/"));
            nodes.Add(nSql);

            // 7. MongoDB (Child of 5)
            NodeData nMongo = new NodeData("7", "5", "MongoDB", "MongoDB");
            nodes.Add(nMongo);

            // 8. Operating Systems (Root)
            NodeData nOs = new NodeData("8", "", "Operating Systems", "Operating_Systems");
            nodes.Add(nOs);

            // Save to tree.txt
            SaveTree(nodes);

            // Create folders and seed files
            foreach (NodeData node in nodes)
            {
                CreateNodeStorage(node);
                SaveHyperlinks(node);
            }

            // Seed sample RTF description for Java
            DescriptionItem dJava1 = new DescriptionItem("desc_1.rtf", "Module Notes");
            nJava.Descriptions.Add(dJava1);
            SaveDescriptionsIndex(nJava);
            string rtfJava = @"{\rtf1\ansi\deff0 {\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}
\viewkind4\uc1\pard\lang1033\f0\fs24\b Course Module Notes\b0\par
\par
This section contains student notes and class lecture summaries.\par
\par
- Topic 1: Foundations\par
- Topic 2: Implementation\par
- Topic 3: Testing and Evaluation\par
}";
            WriteDescriptionContent(nJava, dJava1, rtfJava);

            // Seed sample file for Java
            string sampleJavaFile = Path.Combine(GetNodeFilesPath(nJava), "sample_notes.txt");
            File.WriteAllText(sampleJavaFile, "Sample coursework note file for demonstration.\n", Encoding.UTF8);
        }

        #endregion
    }
}
