using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Controllers
{
    [Authorize(Roles = FileManagerRoles.FileReader)]
    public class DirectoryController(
        ILogger<DirectoryController> logger,
        WebServerConfig webServerConfig,
        FolderManager folderManager,
        ClipboardManager clipboardManager,
        UploadManager uploadManager,
        UserManager userManager,
        DownloadManager downloadManager) : Controller
    {
        private readonly ILogger<DirectoryController> _logger = logger;
        private readonly WebServerConfig _webServerConfig = webServerConfig;
        private readonly FolderManager _folderManager = folderManager;
        private readonly ClipboardManager _clipboardManager = clipboardManager;
        private readonly UploadManager _uploadManager = uploadManager;
        private readonly UserManager _userManager = userManager;
        private readonly DownloadManager _downloadManager = downloadManager;

        [HttpGet("[controller]/")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                User.AssertFileReader();
            }
            catch (FileManagerAccessException ex)
            {
                return RedirectToAction("Index", "Home", new { page = "directory", error = ex.Message });
            }

            var vm = GetUserInfo();

            _logger.LogDebug("Index -- {sid} viewModel: {vm}", HttpContext.Session.Id, vm);
            return View(vm);
        }

        [HttpPost("[controller]/cut")]
        [FileManagerExceptionFilter]
        public FileOperationResultViewModel Cut(string[] fileIds)
        {
            User.AssertFileWriter();

            return _clipboardManager.Cut(HttpContext.Session.Id, fileIds);
        }

        [HttpPost("[controller]/copy")]
        [FileManagerExceptionFilter]
        public FileOperationResultViewModel Copy(string[] fileIds)
        {
            User.AssertFileWriter();

            return _clipboardManager.Copy(HttpContext.Session.Id, fileIds);
        }

        [HttpPost("[controller]/paste")]
        [FileManagerExceptionFilter]
        public async Task<FileOperationAsyncResultViewModel> Paste(string destId, bool overwrite, CancellationToken cancellationToken)
        {
            User.AssertFileWriter();

            return await _clipboardManager.PasteAsync(HttpContext.Session.Id, destId, overwrite, cancellationToken);
        }

        [HttpPost("[controller]/paste-results")]
        [FileManagerExceptionFilter]
        public async Task<FileOperationAsyncResultViewModel> PasteResults(string operationId, int timeout, CancellationToken cancellationToken)
        {
            User.AssertFileWriter();

            if (timeout < 50)
                timeout = 50;

            return await _clipboardManager.PastResultsAsync(operationId, TimeSpan.FromMilliseconds(timeout), cancellationToken);
        }

        [HttpPost("[controller]/delete")]
        [FileManagerExceptionFilter]
        public FileOperationResultViewModel Delete(string[] fileIds)
        {
            User.AssertFileWriter();

            return _folderManager.Delete(fileIds);
        }

        [HttpPost("[controller]/rename")]
        [FileManagerExceptionFilter]
        public FileOperationResultViewModel Rename(string fileId, string newName)
        {
            User.AssertFileWriter();

            return _folderManager.Rename(fileId, newName);
        }

        private static string PrintFileIds(string[] fileIds)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < fileIds.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append('\"').Append(fileIds[i]).Append('\"');
            }

            sb.Append(']');
            return sb.ToString();
        }

        [HttpGet("[controller]/download")]
        [FileManagerExceptionFilter]
        public FileResult Download(string[] fileIds)
        {
            User.AssertFileDownloader();

            if (fileIds == null || fileIds.Length == 0)
                throw new FileManagerBadRequestException("no items to download");

            var byteRange = ByteRange.FromRequest(HttpContext.Request);

            _logger.LogInformation("Download: {fileIds} byteRange: {byteRange}", PrintFileIds(fileIds), byteRange);

            if (_downloadManager.IsSingleFile(fileIds))
            {
                return DownloadSingleFile(fileIds[0], byteRange);
            }
            else
            {
                return DownloadMultiFiles(fileIds, byteRange);
            }
        }

        private FileResult DownloadSingleFile(string fileId, ByteRange byteRange)
        {
            var fileInfo = _downloadManager.GetFileInfo(fileId);
            if (!byteRange.RangeSpecified)
            {
                var fileStream = _downloadManager.GetFileStream(fileId);
                HttpContext.Response.Headers.ContentLength = fileInfo.Size;
                HttpContext.Response.Headers.ETag = fileInfo.ETag;
                HttpContext.Response.Headers.AcceptRanges = "bytes";
                _logger.LogDebug("DownloadSingleFile {fileId} {fileInfo}", fileId, fileInfo);
                return File(fileStream, "application/octet-stream", fileInfo.Name);
            }

            _logger.LogDebug("DownloadSingleFile {fileId} range: {range}", fileId, byteRange);
            var binaryContent = _downloadManager.GetFileBinaryContent(fileId, byteRange);

            HttpContext.Response.Headers.ContentLength = binaryContent.Length;
            HttpContext.Response.Headers.ETag = fileInfo.ETag;
            HttpContext.Response.Headers.AcceptRanges = "bytes";
            HttpContext.Response.Headers.ContentRange = string.Format("bytes {0}-{1}/{2}", binaryContent.Start, binaryContent.End - 1, binaryContent.FileSize);
            HttpContext.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            return File(binaryContent.Data, "application/octet-stream", fileInfo.Name);
        }

        private FileContentResult DownloadMultiFiles(string[] fileIds, ByteRange byteRange)
        {
            bool cache = byteRange.RangeSpecified; // only cache downloads where the client has specified Range: bytes ..."
            FileDownload download = _downloadManager.Compress(fileIds, cache);

            // var fileInfo = _downloadManager.GetFileInfo(fileId);
            if (!byteRange.RangeSpecified)
            {
                // var fileStream = _downloadManager.GetFileStream(fileId);
                HttpContext.Response.Headers.ETag = download.ETag;
                HttpContext.Response.Headers.AcceptRanges = "bytes";
                _logger.LogDebug("DownloadMultiFiles {fileId} {fileInfo}", string.Join(", ", fileIds), download.FileName);
                return File(download.Data, "application/octet-stream", download.FileName);
            }

            _logger.LogDebug("DownloadMultiFiles {fileIds} range: {range}", string.Join(", ", fileIds), byteRange);
            var binaryContent = _downloadManager.GetFileBinaryContent(download, byteRange);

            HttpContext.Response.Headers.ContentLength = binaryContent.Length;
            HttpContext.Response.Headers.ETag = download.ETag;
            HttpContext.Response.Headers.AcceptRanges = "bytes";
            HttpContext.Response.Headers.ContentRange = string.Format("bytes {0}-{1}/{2}", binaryContent.Start, binaryContent.End - 1, binaryContent.FileSize);
            HttpContext.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            return File(binaryContent.Data, "application/octet-stream", download.FileName);
        }

        [HttpPost("[controller]/start-upload-file")]
        [FileManagerExceptionFilter]
        public FileUploadResult StartUploadFile(string uploadId, string? folderId, string? fileName, long fileLen, int chunks, int chunkSize)
        {
            User.AssertFileUploader();

            if (string.IsNullOrWhiteSpace(uploadId))
            {
                throw new FileManagerBadRequestException("uploadId is not provided");
            }

            if (string.IsNullOrWhiteSpace(folderId))
            {
                throw new FileManagerBadRequestException("folderId is not provided");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new FileManagerBadRequestException("fileName is not provided");
            }

            return _uploadManager.Start(uploadId, folderId, fileName, fileLen, chunks, chunkSize);
        }

        [HttpPost("[controller]/cancel-upload-file")]
        [FileManagerExceptionFilter]
        public FileUploadResult CancelUploadFile(string uploadId)
        {
            User.AssertFileUploader();

            if (string.IsNullOrWhiteSpace(uploadId))
            {
                throw new FileManagerBadRequestException("uploadId is not provided");
            }

            return _uploadManager.Cancel(uploadId);
        }

        [HttpPost("[controller]/finish-upload-file")]
        [FileManagerExceptionFilter]
        public FileUploadResult FinishUploadFile(string uploadId)
        {
            User.AssertFileUploader();

            if (string.IsNullOrWhiteSpace(uploadId))
            {
                throw new FileManagerBadRequestException("uploadId is not provided");
            }

            return _uploadManager.Complete(uploadId);
        }

        [HttpPost("[controller]/upload-chunk")]
        [FileManagerExceptionFilter]
        public FileUploadResult UploadChunk(
            [FromForm(Name = "uploadId")] string? uploadId,
            [FromForm(Name = "chunkId")] int chunkId,
            [FromForm(Name = "fileOffset")] long fileOffset,
            [FromForm(Name = "data")] IFormFile fileData)
        {
            User.AssertFileUploader();

            if (string.IsNullOrWhiteSpace(uploadId))
            {
                throw new FileManagerBadRequestException("uploadId is not provided");
            }

            var data = new byte[fileData.Length + 1024];
            int dlen;
            using (var stream = fileData.OpenReadStream())
            {
                dlen = stream.Read(data, 0, data.Length);
            }

            return _uploadManager.AddChunk(uploadId, chunkId, fileOffset, data, dlen);
        }

        private FileManagerLayout GetFolderManagerLayout()
        {
            FileManagerLayout defaultValue = FileManagerLayout.Tiles;
            if (User.Identity?.Name != null)
                defaultValue = _userManager.GetUserSettings(User.Identity.Name).Layout;

            var layout = HttpContext.Session.GetString(FileManagerLayoutPropertiess.SessionKey);
            if (Enum.TryParse(layout, true, out FileManagerLayout dirLayout))
            {
                return dirLayout;
            }

            return defaultValue;
        }

        private void SetFolderManagerLayout(FileManagerLayout value)
        {
            HttpContext.Session.SetString(FileManagerLayoutPropertiess.SessionKey, value.ToString());
            if (User.Identity?.Name != null)
            {
                var settings = _userManager.GetUserSettings(User.Identity.Name);
                settings.Layout = value;
                _userManager.SaveUserSettings(User.Identity.Name, settings);
            }
        }

        private string GetSessionFolderId()
        {
            return HttpContext.Session.GetString(FolderIdKey) ?? "/";
        }
        private void SetSessionFolderId(string folderId)
        {
            HttpContext.Session.SetString(FolderIdKey, folderId);
        }

        private const string FolderIdKey = "Folder-Id";

        [HttpGet("[controller]/folder-contents")]
        public IActionResult FolderContents(string? folderId, string? layout, [FromQuery(Name = "sort")] string? sort)
        {
            if (string.IsNullOrWhiteSpace(folderId) || folderId == "#")
            {
                folderId = GetSessionFolderId();
            }
            else
            {
                SetSessionFolderId(folderId);
            }

            HttpContext.Response.Headers[FolderIdKey] = folderId;

            if (Enum.TryParse(layout, true, out FileManagerLayout dirLayout))
            {
                SetFolderManagerLayout(dirLayout);
            }
            else
            {
                dirLayout = GetFolderManagerLayout();
            }

            if (!Enum.TryParse(sort, true, out FilesSortOrder sortOrder))
            {
                sortOrder = FilesSortOrder.NameAsc;
            }

            _logger.LogInformation("FolderContents: {id}, {layout}", folderId, dirLayout);
            var vm = new FolderContentsViewModel(folderId, folderId);
            vm.Files.AddRange(_folderManager.GetFiles(folderId, sortOrder));
            return dirLayout switch
            {
                FileManagerLayout.List => View("FolderContentsList", vm),
                FileManagerLayout.Tiles or _ => View("FolderContentsTiles", vm),
            };
        }

        [HttpGet("[controller]/folders")]
        public JsonNode Folders(string? id, bool select)
        {
            var result = _folderManager.Load(id, select);
            _logger.LogInformation("Folders(id={id}, select={select}: {result}", id, select, JsonSerializer.Serialize<JsonNode>(result));
            return result;
        }

        private UserInfoViewModel GetUserInfo()
        {
            var vm = new UserInfoViewModel(_webServerConfig.ClientDebugging, GetFolderManagerLayout(), _uploadManager.UploadChunkSize);

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                vm.HasFileReaderRole = User.IsInRole(FileManagerRoles.FileReader);
                vm.HasFileWriterRole = User.IsInRole(FileManagerRoles.FileWriter);
                vm.HasFileDownloaderRole = User.IsInRole(FileManagerRoles.FileDownloader);
                vm.HasFileUploaderRole = User.IsInRole(FileManagerRoles.FileUploader);
            }

            return vm;
        }
    }

}