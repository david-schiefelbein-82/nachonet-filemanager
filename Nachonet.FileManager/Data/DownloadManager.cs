using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public class DownloadManager
    {
        private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;

        private readonly ILogger<FileContentManager> _logger;
        private readonly RootFolders _rootFolders;
        private readonly IConfigManager _configManager;
        private readonly Dictionary<string, FileDownload> _downloads = [];

        public DownloadManager(ILogger<FileContentManager> logger, IConfigManager configManager)
        {
            _logger = logger;
            _comparison = StringComparison.InvariantCultureIgnoreCase;
            _rootFolders = new RootFolders(configManager.FileServer.RootFolders);
            _configManager = configManager;
            RunCleanup();
        }

        private async void RunCleanup()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                lock (this)
                {
                    var now = Environment.TickCount64;
                    var expiryTime = (Int64)(TimeSpan.FromMinutes(15).TotalMilliseconds);
                    var expired = (from x in _downloads
                                   where (now - x.Value.Updated) > expiryTime
                                   select x).ToArray();
                    if (expired.Length > 0)
                        _logger.LogWarning("cleanup found {count} expired downloads", expired.Length);
                    else
                        _logger.LogInformation("cleanup found {count} expired downloads", expired.Length);

                    foreach (var download in expired)
                    {
                        _logger.LogInformation("download {download} expired (last updated {updated})", download.Value, download.Value.Updated);
                        _downloads.Remove(download.Key);
                    }
                }
            }
        }

        public FileTypeConfig GetFileType(FolderPath filePath)
        {
            var extn = filePath.ToFile(_rootFolders).Extension.ToLower();
            if (_configManager.FileServer.FileTypes.TryGetValue(extn, out var type))
                return type;
            else
                return new FileTypeConfig() { FileType = FileType.Unknown, IsReadOnly = true, Syntax = "" };
        }

        /// <summary>
        /// returns true if the list contains a reference to a single file.
        /// returns false if the list contains a reference to a single folder or multiple files
        /// </summary>
        /// <param name="fileIds"></param>
        /// <returns></returns>
        public bool IsSingleFile(string[] fileIds)
        {
            if (fileIds.Length == 1)
            {
                var fileId = new FolderPath(fileIds[0], _comparison);
                return (fileId.FileType == FileViewModelType.File);
            }

            return false;
        }

        public FileDownload Compress(string[] fileIds, bool cache)
        {
            var modified = DateTime.Now;
            string key = string.Join(";", fileIds);

            // only lookup the cache if enabled
            if (cache)
            {
                lock (this)
                {
                    if (_downloads.TryGetValue(key, out var fileDownload))
                    {
                        _logger.LogDebug("Loaded \"{key}\" from cache", key);
                        fileDownload.Refresh();
                        return fileDownload;
                    }
                }
            }

            var paths = (from x in fileIds select new FolderPath(x, _comparison)).ToArray();
            string fileName = paths.Length == 1
                    ? string.Format("{0}-{1}.zip", paths[0].Name, DateTime.Now.ToString("yyyyMMddHHmmss"))
                    : string.Format("download-{0}.zip", modified.ToString("yyyyMMddHHmmss"));
            var binary = new Compressor(_rootFolders).Archive(paths);
            var download = new FileDownload(
                fileIds,
                fileName,
                binary);

            // only save to the cache if enabled
            if (cache)
            {
                lock (this)
                {
                    _downloads[key] = download;
                    _logger.LogInformation("Saved {key} to cache {fileName} ({fileSize} bytes)", key, download.FileName, download.FileSize);
                }
            }
            else
            {
                _logger.LogInformation("Compressed \"{key}\" to {fileName} ({fileSize} bytes)", key, download.FileName, download.FileSize);
            }

            return download;
        }

        public FileViewModel GetFileInfo(string fileId)
        {
            var filePath = new FolderPath(fileId, _comparison);
            try
            {
                var type = GetFileType(filePath);
                var file = filePath.ToFile(_rootFolders);
                var accessible = true;

                return new FileViewModel(filePath, filePath.Name, FileViewModelType.File, type.FileType, type.FileIcon, accessible, file.CreationTime, file.LastWriteTime, file.Length, file.IsReadOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFileInfo {path}", filePath);
                throw;
            }
        }

        public FileBinaryContentResult GetFileBinaryContent(string fileId, ByteRange range)
        {
            var filePath = new FolderPath(fileId, _comparison);
            try
            {
                var file = filePath.ToFile(_rootFolders);
                var stream = file.OpenRead();

                long start = range.Start == null ? 0 : range.Start.Value;
                long end = (range.End == null ? file.Length : range.End.Value + 1);
                stream.Seek(start, SeekOrigin.Begin);

                if (end > file.Length)
                {
                    end = file.Length;
                }

                // get up to 1mb (1000 kb) per request
                int maxSize = 1024 * 1000;

                int bufferSize = (int)Math.Min(maxSize, end - start);
                byte[] data = new byte[bufferSize];
                int bytesRead = stream.Read(data, 0, bufferSize);
                if (bytesRead < data.Length)
                {
                    byte[] buffer = new byte[bytesRead];
                    Array.Copy(data, buffer, bytesRead);
                    data = buffer;
                }
                end = start + bytesRead;
                return new FileBinaryContentResult(file.Name, data, start, end, bytesRead, file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFileBinaryContent {path}", filePath);
                throw;
            }
        }

        public FileBinaryContentResult GetFileBinaryContent(FileDownload download, ByteRange range)
        {
            try
            {
                long start = range.Start == null ? 0 : range.Start.Value;
                long end = Math.Min(download.FileSize, (range.End == null ? download.FileSize : range.End.Value + 1));

                // get up to 1mb (1000 kb) per request
                var maxSize = 1024L * 1000;

                var bufferSize = Math.Min(maxSize, end - start);
                byte[] data = new byte[bufferSize];
                var bytesRead = download.Read(data, start, bufferSize);
                if (bytesRead < data.LongLength)
                {
                    byte[] buffer = new byte[bytesRead];
                    Array.Copy(data, buffer, bytesRead);
                    data = buffer;
                }

                end = start + bytesRead;
                return new FileBinaryContentResult(download.FileName, data, start, end, (int)bytesRead, download.FileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFileBinaryContent {download}", download.FileName);
                throw;
            }
        }

        public Stream GetFileStream(string fileId)
        {
            var filePath = new FolderPath(fileId, _comparison);
            var file = filePath.ToFile(_rootFolders);
            return file.OpenRead();
        }
    }
}