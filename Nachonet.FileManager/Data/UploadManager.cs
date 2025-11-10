// #define TESTING
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using static NuGet.Packaging.PackagingConstants;

namespace Nachonet.FileManager.Data
{
    public class UploadManager
    {
        const string TEMP_EXTENSION = ".fm-temp";
        private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;

        private readonly ILogger<UploadManager> _logger;
        private readonly RootFolders _rootFolders;
        private readonly DirectoryInfo _uploadDir;
        private readonly List<FileUpload> _uploads;
        private readonly IConfigManager _configManager;

        public UploadManager(ILogger<UploadManager> logger, IConfigManager configManager)
        {
            _uploads = [];
            _configManager = configManager;
            _logger = logger;
            _comparison = StringComparison.InvariantCultureIgnoreCase;
            _rootFolders = new RootFolders(configManager.FileServer.RootFolders);
            _uploadDir = new DirectoryInfo(Path.GetFullPath(configManager.FileServer.UploadDirectory));

            RunCleanup();
        }

        public int UploadChunkSize
        {
            get => _configManager.FileServer.UploadChunkSize;
        }

        private async void RunCleanup()
        {
            // startup, clear any lefover files
            if (_uploadDir.Exists)
            {
                foreach (var file in _uploadDir.GetFiles("*" + TEMP_EXTENSION))
                {
                    _logger.LogWarning("deleting leftover file {file}", file.FullName);
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting leftover file {file}", file.FullName);
                    }
                }
            }
            else
            {
                _uploadDir.Create();
            }

            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                lock (this)
                {
                    var now = Environment.TickCount64;
                    var expiryTime = (Int64)(TimeSpan.FromMinutes(60).TotalMilliseconds);
                    var expired = (from x in _uploads
                                   where (now - x.Updated) > expiryTime
                                   select x).ToArray();
                    if (expired.Length > 0)
                        _logger.LogWarning("cleanup found {count} expired uploads", expired.Length);
                    else 
                        _logger.LogInformation("cleanup found {count} expired uploads", expired.Length);

                    foreach (var upload in expired)
                    {
                        _logger.LogInformation("upload {upload} expired (last updated {updated})", upload, upload.Updated);
                        try
                        {
                            upload.Expire();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "upload {upload} expired", upload);
                        }
                        _uploads.Remove(upload);
                    }
                }

            }
        }

        public FileUploadResult Start(string uploadId, string folderId, string fileName, long fileLen, int chunks, int chunkSize)
        {
            _logger.LogInformation("Upload Start uploadId:{uploadId}, folderId: {folderId}, fileName: {fileName}, fileLen: {fileLen}, chunks: {chunks}", uploadId, folderId, fileName, fileLen, chunks);
            lock (this)
            {
                try
                {
                    if (!_uploadDir.Exists)
                    {
                        _uploadDir.Create();
                        _logger.LogInformation("created upload directory {dir}", _uploadDir.FullName);
                    }

#if TESTING
                    if (fileName.EndsWith(".start-err", StringComparison.CurrentCultureIgnoreCase))
                    {
                        throw new FileManagerIoException("testing - error because of starterr");
                    }
#endif

                    var destFolder = new FolderPath(folderId, _comparison);
                    if (destFolder.IsRoot)
                    {
                        throw new FileManagerIoException("unable to upload to root");
                    }

                    var tempFile = new FileInfo(Path.Combine(_uploadDir.FullName, uploadId + TEMP_EXTENSION));
                    var upload = new FileUpload(uploadId, tempFile, destFolder, _rootFolders, fileName, fileLen, chunks, chunkSize);
                    _uploads.Add(upload);

                    return new FileUploadResult(uploadId, FileUploadStatus.Started);
                }
                catch (FileManagerException ex)
                {
                    _logger.LogError(ex, "unable to start uploadId:{uploadId}", uploadId);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "unable to start uploadId:{uploadId}", uploadId);
                    throw new FileManagerIoException("unable to start upload");
                }
            }
        }

        public FileUploadResult AddChunk(string uploadId, int chunkId, long fileOffset, byte[] data, int dataLen)
        {
            _logger.LogDebug("Upload AddChunk uploadId:{uploadId}, chunkId: {chunkId}, fileOffset: {fileOffset}, dataLen: {dataLen}", uploadId, chunkId, fileOffset, dataLen);
            lock (this)
            {
                try
                {
                    var upload = _uploads.FirstOrDefault(x => x.UploadId == uploadId) ??
                        throw new FileManagerIoException("Cannot find upload " + uploadId);


#if TESTING
                    if (upload.FileName.EndsWith(".chunk-err", StringComparison.CurrentCultureIgnoreCase) && chunkId == 10)
                    {
                        throw new FileManagerIoException("testing - error because of chunkerr");
                    }
#endif
                    upload.AddChunk(chunkId, fileOffset, data, dataLen);
                    return new FileUploadResult(uploadId, FileUploadStatus.Uploading);
                }
                catch (FileManagerException ex)
                {
                    _logger.LogError(ex, "unable to add chunk to uploadId:{uploadId}", uploadId);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "unable to add chunk to uploadId:{uploadId}", uploadId);
                    throw new FileManagerIoException("unable to write to upload stream");
                }
            }
        }

        public FileUploadResult Cancel(string uploadId)
        {
            _logger.LogInformation("Upload Cancel uploadId:{uploadId}", uploadId);
            try
            {
                FileUpload upload;
                lock (this)
                {
                    upload = _uploads.FirstOrDefault(x => x.UploadId == uploadId) ??
                        throw new FileManagerIoException("Cannot find upload " + uploadId);
                    _uploads.Remove(upload);
                }

                upload.Cancel();
                return new FileUploadResult(uploadId, FileUploadStatus.Cancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload Cancel uploadId:{uploadId}", uploadId);
                throw;
            }
        }

        public FileUploadResult Complete(string uploadId)
        {
            _logger.LogInformation("Upload Complete uploadId:{uploadId}", uploadId);
            try
            {
                FileUpload upload;
                lock (this)
                {
                    upload = _uploads.FirstOrDefault(x => x.UploadId == uploadId) ??
                        throw new FileManagerIoException("Cannot find upload " + uploadId);
                    _uploads.Remove(upload);
                }

#if TESTING
                if (upload.FileName.EndsWith(".complete-err", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new FileManagerIoException("testing - error because of chunkcomplete");
                }
#endif

                upload.Complete();
                return new FileUploadResult(uploadId, FileUploadStatus.Complete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload Complete uploadId:{uploadId}", uploadId);
                throw;
            }
        }
    }
}
