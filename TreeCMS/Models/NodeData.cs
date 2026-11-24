using System;
using System.Collections.Generic;

namespace TreeCMS.Models
{
    /// <summary>
    /// Represents the data model for a node in the Category Tree.
    /// Each node corresponds to a record in tree.txt and a directory inside Data/Nodes/
    /// </summary>
    public class NodeData
    {
        public string Id { get; set; }           // Unique Node ID (e.g., "1", "2")
        public string ParentId { get; set; }     // Parent Node ID, or empty string for root nodes
        public string Name { get; set; }         // Display name of category / topic
        public string FolderName { get; set; }   // Folder name in Data/Nodes/
        public DateTime CreatedDate { get; set; }

        public List<HyperlinkItem> Hyperlinks { get; set; }
        public List<DescriptionItem> Descriptions { get; set; }

        public NodeData()
        {
            Id = string.Empty;
            ParentId = string.Empty;
            Name = string.Empty;
            FolderName = string.Empty;
            CreatedDate = DateTime.Now;
            Hyperlinks = new List<HyperlinkItem>();
            Descriptions = new List<DescriptionItem>();
        }

        public NodeData(string id, string parentId, string name, string folderName)
        {
            Id = id;
            ParentId = parentId ?? string.Empty;
            Name = name;
            FolderName = folderName;
            CreatedDate = DateTime.Now;
            Hyperlinks = new List<HyperlinkItem>();
            Descriptions = new List<DescriptionItem>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
