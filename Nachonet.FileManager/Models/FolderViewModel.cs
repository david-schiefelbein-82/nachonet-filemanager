using System;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    /// <summary>
    /// </summary>
    public class FolderViewModel(string id, string text)
    {
        public string Id { get; set; } = id;

        public string Text { get; set; } = text;

        public List<FolderViewModel> Children { get; set; } = [];

        public bool? HasChildren { get; set; }

        public bool? Opened { get; set; }

        public bool? Selected { get; set; }
    }
}
