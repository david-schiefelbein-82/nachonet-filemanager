using Nachonet.FileManager.Data;
using System;
using System.DirectoryServices.ActiveDirectory;
using System.Security.AccessControl;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    public enum FileViewModelType
    {
        Unknown,
        File,
        Folder,
    }

    public class FileViewModel
    {
        [JsonIgnore]
        public FolderPath Id { get; set; }

        [JsonPropertyName("fileId")]
        public string FileId { get => Id; }

        public string Name { get; set; }

        public FileViewModelType Type { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public long Size { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FileType FileType { get; set; }

        [JsonPropertyName("file-icon")]
        public string? FileIcon { get; set; }

        [JsonPropertyName("size-text")]
        public string SizeTextStr { get => SizeText(); }

        [JsonPropertyName("created-text")]
        public string CreatedTextLong { get => CreatedText("long"); }

        [JsonPropertyName("modified-text")]
        public string ModifiedTextLong { get => ModifiedText("long"); }

        [JsonPropertyName("read-only")]
        public bool IsReadOnly { get; }

        [JsonPropertyName("accessible")]
        public bool Accessible { get; }

        public string ETag
        {
            get
            {
                return string.Format("{0:x}-{1:x}", Size, Modified.Ticks);
            }
        }

        public FileViewModel(FolderPath id, string name, FileViewModelType type, FileType fileType, string? fileIcon, bool accessible, DateTime created, DateTime modified, long size, bool isReadOnly)
        {
            Id = id;
            Name = name;
            Type = type;
            Accessible = accessible;
            FileType = fileType;
            FileIcon = fileIcon;
            Created = created;
            Modified = modified;
            Size = size;
            IsReadOnly = isReadOnly;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            });
        }

        public static string TimestampToRelativeTextShort(DateTime timestamp)
        {
            var now = DateTime.Now;
            var diff = now - timestamp;

            if (diff < TimeSpan.FromMinutes(1))
            {
                return "Just Now";
            }
            else if (diff < TimeSpan.FromHours(1))
            {
                return string.Format("{0} mins ago", (int)diff.TotalMinutes);
            }
            else if (diff < TimeSpan.FromDays(1))
            {
                return string.Format("{0} hours ago", (int)diff.TotalHours);
            }

            if (timestamp.Year == now.Year)
            {
                return timestamp.ToString("dd MMM").ToUpper();
            }
            else
            {
                return timestamp.ToString("dd MMM yyyy");
            }
        }

        public static string TimestampToRelativeTextLong(DateTime timestamp)
        {
            var now = DateTime.Now;
            var diff = now - timestamp;

            if (diff < TimeSpan.FromMinutes(1))
            {
                return "Just Now";
            }
            else if (diff < TimeSpan.FromHours(1))
            {
                return string.Format("{0} mins ago", (int)diff.TotalMinutes);
            }
            else if (diff < TimeSpan.FromDays(1))
            {
                return string.Format("{0} hours ago", (int)diff.TotalHours);
            }

            return timestamp.ToString("dd MMMM yyyy");
        }

        public string CreatedText(string format)
        {
            return format switch
            {
                "l" or "long" => TimestampToRelativeTextLong(Created),
                "s" or "short" or _ => TimestampToRelativeTextShort(Created),
            };
        }

        public string ModifiedText(string format)
        {
            return format switch
            {
                "l" or "long" => TimestampToRelativeTextLong(Modified),
                "s" or "short" or _ => TimestampToRelativeTextShort(Modified),
            };
        }

        /// <summary>
        /// returns the size with a qualifier
        /// eg "6" (bytes) "5 KB", "6 MB", "7 GB"
        /// </summary>
        /// <returns></returns>
        public string SizeText()
        {
            if (Type == FileViewModelType.Folder)
            {
                return string.Format("{0} {1}", Size, Size == 1 ? "item" : "items");
            }

            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            const long TB = GB * 1024;

            if (Size < KB)
            {
                return string.Format("{0}", Size);
            }
            else if (Size < MB)
            {
                // note: Int64 devision - 0 decimal places intentially
                return string.Format("{0} KB", Size / KB);
            }
            else if (Size < GB)
            {
                // note: Int64 devision - 0 decimal places intentially
                return string.Format("{0} MB", Size / MB);
            }
            else if (Size < TB)
            {
                return string.Format("{0} GB", Size / GB);
            }
            else
            {
                // note: Int64 devision - 0 decimal places intentially
                return string.Format("{0} TB", Size / TB);
            }
        }
    }
}
