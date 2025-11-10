using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.CodeAnalysis;
using NuGet.Packaging.Core;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;
using Serilog.Sinks.File;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nachonet.FileManager.Controllers
{
    [Authorize(Roles = FileManagerRoles.FileReader)]
    public class FileController(
        ILogger<FileController> logger,
        FileContentManager fileCotentManager,
        DownloadManager downloadManager) : Controller
    {
        private readonly ILogger<FileController> _logger = logger;
        private readonly FileContentManager _fileContentManager = fileCotentManager;
        private readonly DownloadManager _downloadManager = downloadManager;

        [AllowAnonymous]
        [HttpGet("[controller]/stream")]
        [FileManagerExceptionFilter]
        public async Task<ActionResult> Stream(string? fileId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new FileManagerBadRequestException("cannot get file stream - missing 'fileId'");

            User.AssertFileReader();

            await Task.Delay(1, cancellationToken);
            var byteRange = ByteRange.FromRequest(HttpContext.Request);

            if (!byteRange.RangeSpecified)
            {
                var fileInfo = _downloadManager.GetFileInfo(fileId);
                var fileStream = _downloadManager.GetFileStream(fileId);
                HttpContext.Response.Headers.ContentLength = fileInfo.Size;
                HttpContext.Response.Headers.CacheControl = "no-cache";
                HttpContext.Response.Headers.AcceptRanges = "bytes";
                HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                _logger.LogDebug("get stream {fileId} {fileInfo}", fileId, fileInfo);
                return File(fileStream, "application/octet-stream");
            }

            _logger.LogDebug("get stream {fileId} range: {range}", fileId, byteRange);
            var binaryContent = _downloadManager.GetFileBinaryContent(fileId, byteRange);

            HttpContext.Response.Headers.ContentLength = binaryContent.Length;
            HttpContext.Response.Headers.CacheControl = "no-cache";
            HttpContext.Response.Headers.ContentRange = string.Format("bytes {0}-{1}/{2}", binaryContent.Start, binaryContent.End - 1, binaryContent.FileSize);
            HttpContext.Response.StatusCode = (int)HttpStatusCode.PartialContent;
            return File(binaryContent.Data, "application/octet-stream");
        }

        [HttpGet("[controller]/info")]
        [FileManagerExceptionFilter]
        public FileViewModel Info(string? fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new FileManagerBadRequestException("cannot get file contents - missing 'fileId\'");

            _logger.LogDebug("get {fileId} info", fileId);

            return _downloadManager.GetFileInfo(fileId);
        }


        [HttpGet("[controller]/contents")]
        [FileManagerExceptionFilter]
        public TextFileContent Contents(string? fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                throw new FileManagerBadRequestException("cannot get file contents - missing 'fileId'");

            _logger.LogDebug("get {fileId} contents", fileId);

            var contents = _fileContentManager.GetFileContents(fileId);
            return contents;
        }

        [HttpPost("[controller]/contents")]
        [FileManagerExceptionFilter]
        public TextFileContent Contents([FromBody] SetFileContentsRequest request)
        {
            var fileId = request.FileId;
            var content = request.Content;

            User.AssertFileWriter();
            if (string.IsNullOrWhiteSpace(fileId))
                throw new FileManagerBadRequestException("cannot set file contents - missing 'fileId'");

            content ??= string.Empty;

            return _fileContentManager.SetFileContents(fileId, content);
        }
    }
}