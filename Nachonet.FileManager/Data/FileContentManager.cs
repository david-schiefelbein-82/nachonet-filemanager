using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{

    public class FileBinaryContentResult(string fileName, byte[] data, long start, long end, int length, long fileSize)
    {
        public string FileName { get; set; } = fileName;

        public long Start { get; } = start;

        public long End { get; } = end;

        public byte[] Data { get; set; } = data;

        public int Length { get; } = length;

        public long FileSize { get; } = fileSize;
    }

    public class FileContentManager(ILogger<FileContentManager> logger, IConfigManager configManager)
    {
        private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;

        private readonly ILogger<FileContentManager> _logger = logger;
        private readonly RootFolders _rootFolders = new (configManager.FileServer.RootFolders);
        private readonly IConfigManager _configManager = configManager;

        public FileTypeConfig GetFileType(FolderPath filePath)
        {
            var extn = filePath.ToFile(_rootFolders).Extension.ToLower();
            if (_configManager.FileServer.FileTypes.TryGetValue(extn, out var type))
                return type;
            else
                return new FileTypeConfig() { FileType = FileType.Unknown, IsReadOnly = true, Syntax = "" };
        }

        const string DEFAULT_TYPE = "text";

        public TextFileContent GetFileContents(string fileId)
        {
            var filePath = new FolderPath(fileId, _comparison);
            var type = GetFileType(filePath);
            char[] block = new char[type.MaxSize];
            if (filePath.FileType == FileViewModelType.File)
            {
                var file = filePath.ToFile(_rootFolders);
                var filelen = file.Length;
                bool isReadOnly = type.IsReadOnly || file.IsReadOnly;
                using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                int len = reader.ReadBlock(block, 0, block.Length);
                if (len < 0)
                    throw new FileManagerIoException("cannot read file");

                if (filelen > len)
                {
                    isReadOnly = true;
                    return new TextFileContent(new string(block, 0, len) + Environment.NewLine +
                        string.Format("=========== {0:N0} bytes not shown ===========", filelen - len), type?.Syntax ?? DEFAULT_TYPE, isReadOnly);
                }

                return new TextFileContent(new string(block, 0, len), type?.Syntax ?? DEFAULT_TYPE, isReadOnly);
            }
            else
            {
                throw new FileManagerIoException("file is a folder, not a file");
            }
        }

        public TextFileContent SetFileContents(string fileId, string content)
        {
            var filePath = new FolderPath(fileId, _comparison);
            try
            {
                if (filePath.FileType == FileViewModelType.File)
                {
                    var type = GetFileType(filePath);
                    if (type.FileType != FileType.Text)
                        throw new FileManagerIoException("file is type " + type.FileType + " - this app can only save text file types");
                    if (content.Length > type.MaxSize)
                        throw new FileManagerIoException("file is too large - " + type + " size: " + content.Length + " max-size " + type.MaxSize);
                    if (type.IsReadOnly)
                        throw new FileManagerIoException("file type " + type + " is readonly");

                    var file = filePath.ToFile(_rootFolders);
                    using (var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using var writer = new StreamWriter(fs);
                        writer.Write(content);
                        writer.Flush();
                        writer.Close();
                    }

                    return new TextFileContent(content, type.Syntax, false);
                }
                else
                {
                    throw new FileManagerIoException("file is a folder, not a file");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "SetFileContents {path}", filePath);
                throw new FileManagerUnauthorizedAccessException("Unauthorized - access to the file is denied");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "SetFileContents {path}", filePath);
                throw new FileManagerIoException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetFileContents {path}", filePath);
                throw;
            }
        }
    }
}
