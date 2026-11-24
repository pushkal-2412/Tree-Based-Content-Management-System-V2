using System;

namespace TreeCMS.Models
{
    /// <summary>
    /// Represents a rich-text description associated with a category node.
    /// The index is stored in Data/Nodes/{FolderName}/descriptions.txt
    /// The formatted content is stored in Data/Nodes/{FolderName}/{FileName} in RTF format.
    /// </summary>
    public class DescriptionItem
    {
        public string FileName { get; set; } // e.g. "desc_1.rtf"
        public string Title { get; set; }    // e.g. "Introduction and Core Concepts"

        public DescriptionItem()
        {
            FileName = string.Empty;
            Title = string.Empty;
        }

        public DescriptionItem(string fileName, string title)
        {
            FileName = fileName;
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
