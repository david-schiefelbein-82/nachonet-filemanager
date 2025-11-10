using System;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    public class FolderContentsViewModel(string id, string name)
    {
        public string Id { get; set; } = id;

        public string Name { get; set; } = name;

        public List<FileViewModel> Files { get; set; } = [];
    }
}
