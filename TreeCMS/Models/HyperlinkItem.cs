using System;

namespace TreeCMS.Models
{
    /// <summary>
    /// Represents a single hyperlink associated with a category node.
    /// Saved line-by-line in Data/Nodes/{FolderName}/hyperlinks.txt
    /// Format: Title|URL
    /// </summary>
    public class HyperlinkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }

        public HyperlinkItem()
        {
            Title = string.Empty;
            Url = string.Empty;
        }

        public HyperlinkItem(string title, string url)
        {
            Title = title;
            Url = url;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Title, Url);
        }
    }
}
