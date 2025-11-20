using System.IO.Compression;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public class Compressor(RootFolders rootFolders)
    {
        private readonly RootFolders _rootFolders = rootFolders;

        public byte[] Archive(FolderPath[] items)
        {
            byte[] data;
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in items)
                {
                    switch (item.FileType)
                    {
                        case FileViewModelType.File:
                            AddItem(archive, 0, string.Empty, item.ToFile(_rootFolders));
                            break;
                        case FileViewModelType.Folder:
                            AddItem(archive, 0, string.Empty, item.ToDirectory(_rootFolders));
                            break;
                        default:
                            throw new FileManagerClipboardException("unknown fileType");
                    }
                }
            }

            data = memoryStream.ToArray();
            return data;
        }

        private static void AddItem(ZipArchive archive, int level, string parent, FileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo is FileInfo file)
            {
                var fileEntry = archive.CreateEntry(parent + file.Name);
                using var fileEntryStream = fileEntry.Open();
                using var readStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                readStream.CopyTo(fileEntryStream);
            }
            else if (fileSystemInfo is DirectoryInfo dir)
            {
                var subFiles = dir.GetFiles();
                foreach (var subFile in subFiles)
                    AddItem(archive, level + 1, parent + dir.Name + "/", subFile);

                foreach (var subDir in dir.GetDirectories())
                {
                    AddItem(archive, level + 1, parent + dir.Name + "/", subDir);
                }
            }
        }
    }
}
