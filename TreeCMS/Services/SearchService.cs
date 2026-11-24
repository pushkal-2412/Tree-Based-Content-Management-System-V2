using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TreeCMS.Models;

namespace TreeCMS.Services
{
    /// <summary>
    /// Service that executes keyword searches across:
    /// - Node names
    /// - Hyperlinks (title and URL)
    /// - Description text (title and body content)
    /// - Uploaded filenames
    /// Performs case-insensitive matching and builds structured search results.
    /// </summary>
    public class SearchService
    {
        private readonly DataManager _dataManager;

        public SearchService(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        /// <summary>
        /// Performs a case-insensitive search across all nodes and their contents.
        /// </summary>
        public List<SearchResultItem> Search(List<NodeData> allNodes, string keyword)
        {
            List<SearchResultItem> results = new List<SearchResultItem>();

            if (string.IsNullOrWhiteSpace(keyword) || allNodes == null)
            {
                return results;
            }

            string query = keyword.Trim();

            // Build node lookup dictionary for quick parent traversal and path building
            Dictionary<string, NodeData> nodeLookup = new Dictionary<string, NodeData>();
            foreach (NodeData node in allNodes)
            {
                if (!nodeLookup.ContainsKey(node.Id))
                {
                    nodeLookup.Add(node.Id, node);
                }
            }

            foreach (NodeData node in allNodes)
            {
                string nodePath = BuildNodePath(node, nodeLookup);

                // 1. Search in Node Name
                if (ContainsIgnoreCase(node.Name, query))
                {
                    results.Add(new SearchResultItem(
                        node.Id,
                        node.Name,
                        nodePath,
                        MatchCategory.NodeName,
                        node.Name,
                        "Matched Category / Node Name"
                    ));
                }

                // 2. Search in Hyperlinks
                if (node.Hyperlinks != null)
                {
                    foreach (HyperlinkItem link in node.Hyperlinks)
                    {
                        if (ContainsIgnoreCase(link.Title, query) || ContainsIgnoreCase(link.Url, query))
                        {
                            results.Add(new SearchResultItem(
                                node.Id,
                                node.Name,
                                nodePath,
                                MatchCategory.Hyperlink,
                                link.Title,
                                "URL: " + link.Url
                            ));
                        }
                    }
                }

                // 3. Search in Descriptions (Title and Content)
                if (node.Descriptions != null)
                {
                    foreach (DescriptionItem desc in node.Descriptions)
                    {
                        bool matchedTitle = ContainsIgnoreCase(desc.Title, query);
                        bool matchedContent = false;
                        string snippet = string.Empty;

                        string rawContent = _dataManager.ReadDescriptionContent(node, desc);
                        string plainText = ExtractPlainTextFromRtf(rawContent);

                        if (ContainsIgnoreCase(plainText, query))
                        {
                            matchedContent = true;
                            snippet = GetSnippet(plainText, query);
                        }

                        if (matchedTitle || matchedContent)
                        {
                            string detail = matchedContent
                                ? "Content: " + snippet
                                : "Description Title Match";

                            results.Add(new SearchResultItem(
                                node.Id,
                                node.Name,
                                nodePath,
                                MatchCategory.Description,
                                desc.Title,
                                detail
                            ));
                        }
                    }
                }

                // 4. Search in Uploaded Filenames
                FileInfo[] files = _dataManager.GetUploadedFiles(node);
                if (files != null)
                {
                    foreach (FileInfo fi in files)
                    {
                        if (ContainsIgnoreCase(fi.Name, query))
                        {
                            results.Add(new SearchResultItem(
                                node.Id,
                                node.Name,
                                nodePath,
                                MatchCategory.UploadedFile,
                                fi.Name,
                                string.Format("File Size: {0:N0} bytes ({1})", fi.Length, fi.Extension)
                            ));
                        }
                    }
                }
            }

            return results;
        }

        private bool ContainsIgnoreCase(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return false;
            }
            return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Builds hierarchical breadcrumb path like "Programming > Java"
        /// </summary>
        public string BuildNodePath(NodeData node, Dictionary<string, NodeData> lookup)
        {
            List<string> parts = new List<string>();
            NodeData current = node;

            while (current != null)
            {
                parts.Insert(0, current.Name);
                if (!string.IsNullOrEmpty(current.ParentId) && lookup.ContainsKey(current.ParentId))
                {
                    current = lookup[current.ParentId];
                }
                else
                {
                    break;
                }
            }

            return string.Join(" > ", parts.ToArray());
        }

        /// <summary>
        /// Extracts readable plain text from RTF content for searching.
        /// </summary>
        private string ExtractPlainTextFromRtf(string rtf)
        {
            if (string.IsNullOrEmpty(rtf))
            {
                return string.Empty;
            }

            if (!rtf.StartsWith("{\\rtf"))
            {
                return rtf; // Already plain text
            }

            try
            {
                // Remove RTF control words and groups
                string text = Regex.Replace(rtf, @"\\par[d]?", "\n");
                text = Regex.Replace(text, @"\{\*?\\[^{}]+}|[{}]|\\\w+[0-9]*|\\'", " ");
                return text.Trim();
            }
            catch
            {
                return rtf;
            }
        }

        /// <summary>
        /// Creates a short snippet around the matched keyword.
        /// </summary>
        private string GetSnippet(string text, string keyword)
        {
            int idx = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;

            int start = Math.Max(0, idx - 25);
            int length = Math.Min(text.Length - start, keyword.Length + 50);

            string snippet = text.Substring(start, length).Replace("\r", " ").Replace("\n", " ");
            if (start > 0) snippet = "..." + snippet;
            if (start + length < text.Length) snippet = snippet + "...";

            return snippet;
        }
    }
}
