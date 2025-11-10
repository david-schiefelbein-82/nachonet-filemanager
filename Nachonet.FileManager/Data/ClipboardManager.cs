using Microsoft.CodeAnalysis;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public enum SessionFolderClipboardAction
    {
        Move,
        Copy,
    }

    public class ClipboardManager
    {
        private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;

        private readonly ILogger<ClipboardManager> _logger;
        private readonly RootFolders _rootFolders;
        private readonly Dictionary<string, SessionClipboard> _clipboards;
        private readonly Dictionary<string, PasteOperation> _operations;
        //private readonly IConfigManager _configManager;

        public ClipboardManager(ILogger<ClipboardManager> logger, IConfigManager configManager)
        {
            _logger = logger;
            //_configManager = configManager;
            _comparison = StringComparison.InvariantCultureIgnoreCase;
            _rootFolders = new RootFolders(configManager.FileServer.RootFolders);
            _clipboards = [];
            _operations = [];
            CleanupOldOperations();
        }

        public FileOperationResultViewModel Cut(string sessionId, string[] items)
        {
            lock (_clipboards)
            {
                var clipboard = new SessionClipboard(sessionId, items, SessionFolderClipboardAction.Move);
                _clipboards[sessionId] = clipboard;
                _logger.LogDebug("Cut({sid}) clipboard {clipboard}", sessionId, clipboard);
            }

            string msg = string.Format("cut {0} {1} to the virtual clipboard", items.Length, items.Length == 1 ? "item" : "items");
            return new FileOperationResultViewModel(Guid.NewGuid().ToString(), ClipboardResultCode.Success, msg, []);
        }

        public FileOperationResultViewModel Copy(string sessionId, string[] items)
        {
            lock (_clipboards)
            {
                var clipboard = new SessionClipboard(sessionId, items, SessionFolderClipboardAction.Copy);
                _clipboards[sessionId] = clipboard;
                _logger.LogDebug("Copy({sid}) clipboard {clipboard}", sessionId, clipboard);
            }

            string msg = string.Format("copied {0} {1} to the virtual clipboard", items.Length, items.Length == 1 ? "item" : "items");
            return new FileOperationResultViewModel(ClipboardResultCode.Success, msg, [ ]);
        }

        public async Task<FileOperationAsyncResultViewModel> PasteAsync(string sessionId, string destinationFolder, bool overwrite, CancellationToken cancellationToken)
        {
            var operationId = Guid.NewGuid().ToString();
            PasteOperation operation;
            lock (_clipboards)
            {
                if (_clipboards.TryGetValue(sessionId, out var clipboard))
                {
                    _logger.LogDebug("Paste({sid}) clipboard {clipboard}", sessionId, clipboard);
                    if (clipboard.Files.Length == 0)
                    {
                        _logger.LogWarning("Paste({sid}) - no files in clipboard {clipboard}", sessionId, clipboard);
                        return new FileOperationAsyncResultViewModel(operationId, ClipboardResultCode.Error, "Paste Error", "no files in clipboard", []);
                    }

                    // todo: use Path here
                    var destFolder = new FolderPath(destinationFolder, _comparison);
                    if (destFolder.IsRoot)
                    {
                        _logger.LogWarning("Paste({sid}) - cannot paste to root folder clipboard:{clipboard}", sessionId, clipboard);
                        return new FileOperationAsyncResultViewModel(operationId, ClipboardResultCode.Error, "Paste Error", "cannot paste into root folder", []);
                    }

                    _clipboards.Remove(sessionId);

                    operation = new PasteOperation(this, operationId, clipboard, destinationFolder, overwrite);
                    lock (this)
                    {
                        _operations[operationId] = operation;
                    }
                }
                else
                {
                    _logger.LogWarning("Paste({sid}) but no clipboard set", sessionId);
                    return new FileOperationAsyncResultViewModel(operationId, ClipboardResultCode.Error, "Paste Error", "no files in clipboard", []);
                }
            }

            return await operation.Wait(TimeSpan.Zero, cancellationToken);
        }

        public async Task<FileOperationAsyncResultViewModel> PastResultsAsync(string operationId, TimeSpan timeSpan, CancellationToken cancellationToken)
        {
            PasteOperation? operation;
            lock (this)
            {
                if (!_operations.TryGetValue(operationId, out operation))
                {
                    throw new FileManagerBadRequestException("Cannot find operation " + operationId);
                }
            }

            return await operation.Wait(timeSpan, cancellationToken);
        }


        public ClipboardResult PasteClipboard(SessionFolderClipboardAction action, string src, string destinationFolder, bool overwrite, Action<SessionFolderClipboardAction, FolderPath> beforeAction)
        {
            _logger.LogInformation("Paste {action} {src} {dest} {overwrite}", action, src, destinationFolder, overwrite);
            var srcPath = new FolderPath(src, _comparison);
            var destDirPath = new FolderPath(destinationFolder, _comparison);
            return srcPath.FileType switch
            {
                FileViewModelType.File => PasteClipboardFile(action, srcPath, destDirPath, overwrite, beforeAction),
                FileViewModelType.Folder => PasteClipboardDirectory(action, srcPath, destDirPath, overwrite, beforeAction),
                _ => new ClipboardResult(false, "unknown file-type " + srcPath.FileType),
            };
        }

        private async void CleanupOldOperations()
        {
            while (true)
            {
                await Task.Delay(60000);
                lock (this)
                {
                    var expiredOperations = (_operations.Where(x => x.Value.IsExpired).Select(x => x.Key)).ToArray();

                    foreach (var operationId in expiredOperations)
                    {
                        _logger.LogDebug("Removed Paste operation {operationId}", operationId);
                        _operations.Remove(operationId);
                    }
                }
            }
        }

        private ClipboardResult PasteClipboardFile(SessionFolderClipboardAction action, FolderPath srcPath, FolderPath destDirPath, bool overwrite, Action<SessionFolderClipboardAction, FolderPath> beforeAction)
        {
            _logger.LogInformation("PasteClipboardFile action:{action} srcPath:\"{srcPath}\", destDirPath:\"{destDirPath}\", overwrite:{overwrite}", action, srcPath, destDirPath, overwrite);
            beforeAction(action, srcPath);
            FileInfo src = srcPath.ToFile(_rootFolders);
            DirectoryInfo destFolder = destDirPath.ToDirectory(_rootFolders);
            var destFile = new FileInfo(Path.Combine(destFolder.FullName, src.Name));

            if (!overwrite)
            {
                destFile = destFile.Unique();
            }

            var destPath = FolderPath.ToRelativePath(destFile, _rootFolders, _comparison);

            try
            {
                switch (action)
                {
                    case SessionFolderClipboardAction.Move:
                        _logger.LogDebug("file \"{src}\" moving to \"{dest}\"", srcPath, destPath);
                        src.MoveTo(destFile.FullName, overwrite);
                        return new ClipboardResult(true, string.Format("{0} moved to {1}", srcPath, destPath));
                    case SessionFolderClipboardAction.Copy:
                        _logger.LogDebug("file \"{src}\" copying to \"{dest}\"", srcPath, destPath);
                        src.CopyTo(destFile.FullName, overwrite);
                        return new ClipboardResult(true, string.Format("{0} copied to {1}", srcPath, destPath));
                    default:
                        _logger.LogWarning("unknown paste");
                        throw new FileManagerClipboardException("unknown paste");
                }

            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "PasteClipboardFile IOException");
                return new ClipboardResult(false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasteClipboardFile");
                return new ClipboardResult(false, ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="srcPath"></param>
        /// <param name="destDirPath">a path to the target parent directory</param>
        /// <param name="allowOverwrite">if false, then a unique directory name will be chosen.  if true then the directory will be merged (and files overwritten)</param>
        /// <returns></returns>
        private ClipboardResult PasteClipboardDirectory(SessionFolderClipboardAction action, FolderPath srcPath,
            FolderPath destDirPath, bool overwrite, Action<SessionFolderClipboardAction, FolderPath> beforeAction)
        {
            _logger.LogInformation("PasteClipboardDirectory action:{action}, src:\"{srcPath}\", dest:\"{destDirPath}\", overwrite:{overwrite}", action, srcPath, destDirPath, overwrite);
            var folderName = srcPath.Name;
            var destPath = destDirPath.Child(folderName, FileViewModelType.Folder);
            var destDir = destPath.ToDirectory(_rootFolders);
            var src = srcPath.ToDirectory(_rootFolders);
            if (src.FullName == destDir.FullName)
            {
                // copying to itself... assume we need to make a copy1
                overwrite = false;
            }

            if (!overwrite)
            {
                destDir = destDir.Unique();
            }

            try
            {
                switch (action)
                {
                    case SessionFolderClipboardAction.Move:
                        beforeAction(action, srcPath);
                        _logger.LogDebug("dir \"{src}\" moving to \"{dest}\"...", srcPath, destPath);
                        // src.MoveTo(destDir.FullName);
                        MoveDirectory(srcPath, src, destDirPath, destDir, true, beforeAction);
                        return new ClipboardResult(true, string.Format("{0} moved to {1}", srcPath, destDirPath));
                    case SessionFolderClipboardAction.Copy:
                        _logger.LogDebug("dir \"{src}\" copying to \"{dest}\"...", srcPath, destPath);
                        CopyDirectory(srcPath, src, destDir, true, beforeAction);
                        return new ClipboardResult(true, string.Format("{0} copied to {1}", srcPath, destDirPath));
                    default:
                        throw new FileManagerClipboardException("unknown paste");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasteClipboardFile");
                return new ClipboardResult(false, ex.Message);
            }
        }

        /// <summary>
        /// move a directory by copying it's files then deleting the source directory once complete.
        /// 
        /// Note: there is a DirectoryInfo.MoveTo method but it only works if the src and dest are in the same different volume..
        /// </summary>
        private static void MoveDirectory(FolderPath srcPath, DirectoryInfo src, FolderPath destDirPath, DirectoryInfo dest, bool recursive, Action<SessionFolderClipboardAction, FolderPath> beforeAction)
        {
            if (destDirPath.IsChildOf(srcPath))
            {
                throw new IOException("cannot move a folder into itself");
            }

            if (srcPath.Parent.Equals(destDirPath))
            {
                // nothing to do.  Move a folder from its parent to itself.
                // succeed but don't make any FileSystem changes
                throw new IOException("source and destination are the same");
            }

            CopyDirectory(srcPath, src, dest, recursive, beforeAction);

            src.Delete(true);
        }

        private static void CopyDirectory(FolderPath srcPath, DirectoryInfo src, DirectoryInfo dest, bool recursive, Action<SessionFolderClipboardAction, FolderPath> beforeAction)
        {
            // Check if the source directory exists
            if (!src.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {src.FullName}");

            beforeAction(SessionFolderClipboardAction.Copy, srcPath);

            // Cache directories before we start copying
            DirectoryInfo[] srcDirs = src.GetDirectories();

            // Create the destination directory
            if (!dest.Exists)
                dest.Create();

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in src.GetFiles())
            {
                FolderPath srcFilePath = srcPath.Child(file.Name, FileViewModelType.File);
                beforeAction(SessionFolderClipboardAction.Copy, srcFilePath);
                string targetFilePath = Path.Combine(dest.FullName, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo srcSubDir in srcDirs)
                {
                    FolderPath srcSubPath = srcPath.Child(srcSubDir.Name, FileViewModelType.Folder);
                    var destSubDir = new DirectoryInfo(Path.Combine(dest.FullName, srcSubDir.Name));
                    CopyDirectory(srcSubPath, srcSubDir, destSubDir, true, beforeAction);
                }
            }
        }
    }
}
