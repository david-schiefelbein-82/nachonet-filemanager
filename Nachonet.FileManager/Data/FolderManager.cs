using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public class FolderManager(ILogger<FolderManager> logger, IConfigManager configManager)
    {
        private readonly StringComparison _comparison = StringComparison.InvariantCultureIgnoreCase;
        private readonly ILogger<FolderManager> _logger = logger;
        private readonly RootFolders _rootFolders = new (configManager.FileServer.RootFolders);
        private readonly IConfigManager _configManager = configManager;

        private List<FolderViewModel> GetFolders(DirectoryInfo parent, FolderPath? selectedPath)
        {
            var list = new List<FolderViewModel>();
            foreach (var dir in parent.GetDirectories())
            {
                var path = FolderPath.ToRelativePath(dir, _rootFolders, _comparison);
                var subDirs = dir.GetDirectories();

                var vm = new FolderViewModel(path, dir.Name) { HasChildren = subDirs.Length > 0 };
                if (selectedPath != null)
                {
                    int ancestorOfSelected = path.GetAncesorIndex(selectedPath);
                    if (ancestorOfSelected >= 0)
                    {
                        vm.HasChildren = null;
                        vm.Children = GetFolders(dir, selectedPath);
                        vm.Opened = ancestorOfSelected >= 0;
                        vm.Selected = ancestorOfSelected == 0;
                    }
                }

                list.Add(vm);
            }

            return list;
        }

        //private List<FileViewModel> GetFiles(DirectoryInfo parent)
        //{
        //    var list = new List<FileViewModel>();
        //    foreach (var dir in parent.GetDirectories())
        //    {
        //        var subFileCount = dir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).Count();
        //        var path = FolderPath.ToRelativePath(dir, _rootFolders, _comparison);
        //        list.Add(new FileViewModel(path, dir.Name, FileViewModelType.Folder, FileType.Unknown, null, dir.CreationTime, dir.LastWriteTime, subFileCount, false));
        //    }

        //    foreach (var file in parent.GetFiles())
        //    {
        //        var path = FolderPath.ToRelativePath(file, _rootFolders, _comparison);
        //        var type = _configManager.FileServer.GetFileType(file);

        //        list.Add(new FileViewModel(path, file.Name, FileViewModelType.File, type.FileType, type.FileIcon, file.CreationTime, file.LastWriteTime, file.Length, file.IsReadOnly));
        //    }

        //    return list;
        //}

        public JsonNode Load(string? id, bool select)
        {
            FolderPath? selectedPath = null;
            if (string.IsNullOrEmpty(id))
                id = "#";
            else
            {
                if (select)
                    selectedPath = new FolderPath(id, _comparison);
            }

            var path = new FolderPath(id, _comparison);
            var list = path.GetFolders(_rootFolders, selectedPath);
            return ToJson([.. list]);
        }

        public IEnumerable<FileViewModel> GetFiles(string id, FilesSortOrder sortOrder)
        {
            var path = new FolderPath(id, _comparison);
            if (path.FileType != FileViewModelType.Folder)
            {
                throw new FileManagerIoException("path " + id + " is not a directory");
            }

            var list = path.GetFiles(_configManager, _rootFolders, sortOrder, true);
            return list;
        }

        private static JsonArray ToJson(FolderViewModel[] folderViewModels)
        {
            var arr = new JsonArray();
            foreach (var item in folderViewModels)
            {
                var obj = new JsonObject
                {
                    { "id", item.Id },
                    { "text", item.Text }
                };

                if (item.HasChildren != null)
                {
                    obj.Add("children", item.HasChildren.Value);
                }
                else if (item.Children.Count > 0)
                {
                    obj.Add("children", ToJson([.. item.Children]));
                }

                if (item.Selected != null || item.Opened != null)
                {
                    var state = new JsonObject();
                    if (item.Selected != null)
                        state.Add("selected", item.Selected.Value);

                    if (item.Opened != null)
                        state.Add("opened", item.Opened.Value);

                    obj.Add("state", state);
                }

                arr.Add(obj);
            }
            return arr;
        }

        public FileOperationResultViewModel Delete(string[] items)
        {
            if (items.Length == 0)
            {
                return new FileOperationResultViewModel(ClipboardResultCode.Error, "no files selected", []);
            }

            var results = new List<ClipboardResult>();
            foreach (var item in items)
            {
                var path = new FolderPath(item, _comparison);

                if (path.IsRoot)
                {
                    return new FileOperationResultViewModel(ClipboardResultCode.Error, "Cannot Delete Root /", []);
                }

                if (path.IsRootFolder(_rootFolders))
                {
                    return new FileOperationResultViewModel(ClipboardResultCode.Error, "Cannot Delete Root Folder " + item, []);
                }

                results.Add(DeleteFileItem(path));
            }

            ClipboardResultCode resultCode = ClipboardResultCode.Success;
            string resultMessage = string.Empty;
            var numSuccess = results.Count(x => x.Success);
            if (numSuccess == 0)
            {
                resultCode = ClipboardResultCode.Error;
                resultMessage = results.Count == 1 ? results[0].Message : "multiple errors - no items deleted";
            }
            else if (numSuccess < results.Count)
            {
                resultCode = ClipboardResultCode.Partial;
                resultMessage = string.Format("{0} items deleted, {1} errors", numSuccess, results.Count - numSuccess);
            }
            else
            {
                resultMessage = results.Count == 1 ? "1 item deleted" : string.Format("{0} items deleted", results.Count);
            }

            return new FileOperationResultViewModel(resultCode, resultMessage, [.. results]);
        }

        private ClipboardResult DeleteFileItem(FolderPath path)
        {
            try
            {
                switch (path.FileType)
                {
                    case FileViewModelType.Folder:
                        var dir = path.ToDirectory(_rootFolders);
                        if (!dir.Exists)
                            return new ClipboardResult(false, "unable to delete " + path.Name + " because it doesn't exist.");

                        dir.Delete(true);
                        break;
                    case FileViewModelType.File:
                        var file = path.ToFile(_rootFolders);
                        if (!file.Exists)
                            return new ClipboardResult(false, "unable to delete " + path.Name + " because it doesn't exist.");

                        file.Delete();
                        break;
                }

                return new ClipboardResult(true, path.Name + " deleted");
            }
            catch (IOException ioex)
            {
                if (ioex.Message.Contains("because it is being used by another process.", StringComparison.CurrentCultureIgnoreCase))
                    return new ClipboardResult(false, "unable to delete " + path.Name + " because it is being used by another process.");
                else
                    return new ClipboardResult(false, "unable to delete " + path.Name);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Delete {path}", path);
                throw new FileManagerUnauthorizedAccessException("access to the file is denied");
            }
            catch (Exception ex)
            {
                return new ClipboardResult(false, "unable to delete " + path.Name + " - " + ex.Message);
            }
        }

        private static bool TryValidateFileName(string name, [NotNullWhen(false)] out string? err)
        {
            if (name.LastIndexOf('\\') >= 0 ||
               name.LastIndexOf('/') >= 0 ||
               name.LastIndexOf(':') >= 0 ||
               name.LastIndexOf('*') >= 0 ||
               name.LastIndexOf('?') >= 0 ||
               name.LastIndexOf('"') >= 0 ||
               name.LastIndexOf('<') >= 0 ||
               name.LastIndexOf('>') >= 0 ||
               name.LastIndexOf('|') >= 0 ||
               name.LastIndexOf('\t') >= 0 ||
               name.LastIndexOf('\r') >= 0 ||
               name.LastIndexOf('\n') >= 0)
            {
                err = "name cannot contain \\ / : * ? \" < > |";
                return false;
            }
            else if (name.Length == 0)
            {
                err = "name cannot be empty";
                return false;
            }
            else
            {
                err = null;
                return true;
            }
        }

        public FileOperationResultViewModel Rename(string fileId, string name)
        {
            if (!TryValidateFileName(name, out var err))
            {
                _logger.LogWarning("Rename {fileId} to \"{name}\" - Validation Error: {err}", fileId, name, err);
                return new FileOperationResultViewModel(ClipboardResultCode.Error, err, []);
            }

            var srcPath = new FolderPath(fileId, _comparison);
            if (srcPath.IsRoot)
            {
                _logger.LogWarning("Rename {fileId} to \"{name}\" - Cannot Rename Root: {err}", fileId, name, err);
                return new FileOperationResultViewModel(ClipboardResultCode.Error, "Cannot Rename Root /", []);
            }

            if (srcPath.IsRootFolder(_rootFolders))
            {
                return new FileOperationResultViewModel(ClipboardResultCode.Error, "Cannot Rename Root Folder " + fileId, []);
            }

            var result = new ClipboardResult(true, string.Empty);
            try
            {
                switch (srcPath.FileType)
                {
                    case FileViewModelType.Folder:
                        {
                            var destPath = srcPath.Parent.Child(name, FileViewModelType.Folder);
                            var destDir = destPath.ToDirectory(_rootFolders);
                            if (string.Equals(srcPath.Name, "new folder", _comparison))
                            {
                                // create new folder
                                if (destDir.Exists)
                                {
                                    _logger.LogWarning("directory {destDir} already exists", destDir.FullName);

                                    return new FileOperationResultViewModel(
                                        ClipboardResultCode.Error,
                                        string.Format("folder {0} already exists", name),
                                        [ new ClipboardResult(true, "error creatign folder") ]);
                                }
                                destDir.Create();
                                return new FileOperationResultViewModel(ClipboardResultCode.Success, string.Format("created folder {0}", name), [result]);
                            }
                            var srcDir = srcPath.ToDirectory(_rootFolders);
                            srcDir.MoveTo(destDir.FullName);
                            break;
                        }
                    case FileViewModelType.File:
                        {
                            var srcFile = srcPath.ToFile(_rootFolders);
                            var destPath = srcPath.Parent.Child(name, FileViewModelType.File);
                            var destFile = destPath.ToFile(_rootFolders);
                            srcFile.MoveTo(destFile.FullName);
                            break;
                        }
                    default:
                        throw new FileManagerClipboardException("cannot rename item - it is neither a file or a folder");
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Rename {fileId} to {name}", fileId, name);
                return new FileOperationResultViewModel(ClipboardResultCode.Error, ex.Message, [result]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rename {fileId} to {name}", fileId, name);
                return new FileOperationResultViewModel(ClipboardResultCode.Error, ex.Message, [result]);
            }

            return new FileOperationResultViewModel(ClipboardResultCode.Success, string.Format("{0} renamed", srcPath.Name), [result]);
        }

        public FileDownloadResult Download(string[] items)
        {
            _logger.LogInformation("Download {items}", string.Join(",", items));
            var paths = (from x in items select new FolderPath(x, _comparison)).ToArray();
            if (paths.Length == 1 && paths[0].FileType == FileViewModelType.File)
            {
                // single file, return it
                var file = paths[0].ToFile(_rootFolders);
                return new FileDownloadResult(file.Name, File.ReadAllBytes(file.FullName));
            }
            else if (paths.Length >= 1)
            {
                // multiple items or a single folder, zip them up and return the zip
                string fileName = paths.Length == 1
                    ? string.Format("{0}-{1}.zip", paths[0].Name, DateTime.Now.ToString("yyyyMMddHHmmss"))
                    : string.Format("download-{0}.zip", DateTime.Now.ToString("yyyyMMddHHmmss"));
                return new FileDownloadResult(fileName, new Compressor(_rootFolders).Archive(paths));
            }
            else
            {
                // 0 items, nothing to download
                _logger.LogWarning("Download - no items selected");
                throw new FileManagerClipboardException("no items selected");
            }
        }

        public Stream OpenRead(string id)
        {
            var fileId = new FolderPath(id, _comparison);
            var file = fileId.ToFile(_rootFolders);
            return new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
