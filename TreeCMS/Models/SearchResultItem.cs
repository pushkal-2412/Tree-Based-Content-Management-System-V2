using System;

namespace TreeCMS.Models
{
    /// <summary>
    /// Category under which a search keyword was matched.
    /// </summary>
    public enum MatchCategory
    {
        NodeName,
        Hyperlink,
        Description,
        UploadedFile
    }

    /// <summary>
    /// Represents a search result returned when querying keywords across the CMS.
    /// </summary>
    public class SearchResultItem
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public string NodePath { get; set; }
        public MatchCategory Category { get; set; }
        public string MatchedText { get; set; }
        public string Detail { get; set; }

        public SearchResultItem()
        {
            NodeId = string.Empty;
            NodeName = string.Empty;
            NodePath = string.Empty;
            MatchedText = string.Empty;
            Detail = string.Empty;
        }

        public SearchResultItem(string nodeId, string nodeName, string nodePath, MatchCategory category, string matchedText, string detail)
        {
            NodeId = nodeId;
            NodeName = nodeName;
            NodePath = nodePath;
            Category = category;
            MatchedText = matchedText;
            Detail = detail;
        }
    }
}
