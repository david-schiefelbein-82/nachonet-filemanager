using System;
using System.Security.AccessControl;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Data
{
    public enum FilesSortOrder
    {
        NameAsc,
        NameDesc,
        SizeAsc,
        SizeDesc,
    }

    public class FolderPath : IEquatable<FolderPath?>
    {
        public const char DirSep = '/';
        private readonly StringComparison _comparison;

        public FileViewModelType FileType { get; }

        public string[] Parts { get; }

        public string Value { get; }

        public FolderPath(string path, StringComparison stringComparison)
            : this(path, FileViewModelType.Unknown, stringComparison)
        {
        }

        public FolderPath(string path, FileViewModelType fileType, StringComparison stringComparison)
        {
            // pre-adjust path:
            // Unknown - no adjustment
            // Folder - ends with /, eg "path/to/folder" -> "path/to/folder/"
            // File - cannot end with slash /, eg "path/to/file/" -> "path/to/file"
            switch (fileType)
            {
                case FileViewModelType.File:
                    if (path.EndsWith(DirSep))
                        path = path[..^1];
                    break;
                case FileViewModelType.Folder:
                    if (!path.EndsWith(DirSep))
                        path += DirSep;
                    break;
            }

            Value = path;
            _comparison = stringComparison;

            if (path.EndsWith(DirSep))
            {
                // if path ends with / then this is a folder
                FileType = FileViewModelType.Folder;
                Parts = path[..^1].Split([DirSep]);
            }
            else
            {
                // otherwise it's a file
                FileType = FileViewModelType.File;
                Parts = path.Split([DirSep]);
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as FolderPath);
        }

        public bool Equals(FolderPath? other)
        {
            return other is not null &&
                   string.Equals(Value, other.Value, _comparison);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public FolderPath Child(string sub, FileViewModelType childType)
        {
            if (FileType == FileViewModelType.Folder)
            {
                return new FolderPath(Value + sub, childType, _comparison);
            }
            else
            {
                throw new FileManagerIoException("unable to get the child of a file");
            }
        }

        public string Name => Parts.Length == 0 ? string.Empty : Parts[^1];

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(FolderPath? left, FolderPath? right)
        {
            return EqualityComparer<FolderPath>.Default.Equals(left, right);
        }

        public static bool operator !=(FolderPath? left, FolderPath? right)
        {
            return !(left == right);
        }

        public static implicit operator string(FolderPath p) => p.Value;

        /// <summary>
        /// gets the index of this path relative the other
        /// eg if this path is '/abc' and the other path is '/abc/def'
        /// </summary>
        /// <returns>-1 if this path isn't an ancestor of other,
        /// 0 if this path is equal to the other
        /// 1 if this path is the immediate parent of other
        /// 2 if this path is the grandparent (parent of parent) of other
        /// etc
        /// </returns>
        public int GetAncesorIndex(FolderPath other)
        {
            if (other.Parts.Length < Parts.Length)
                return -1;

            for (int i = 0; i < Parts.Length; ++i)
            {
                if (!string.Equals(Parts[i], other.Parts[i], _comparison))
                    return -1;
            }

            return other.Parts.Length - Parts.Length;
        }

        public FolderPath Parent
        {
            get
            {
                if (Parts.Length > 0)
                    return new FolderPath(string.Join(DirSep.ToString(), Parts.SkipLast(1)) + DirSep, _comparison);
                else
                    return new FolderPath("/", _comparison);
            }
        }

        public DirectoryInfo ToDirectory(RootFolders rootFolders)
        {
            if (IsRoot)
                throw new FileManagerIoException("cannot get directory of root folder");

            string relativePath = Value;

            foreach (var rootFolder in rootFolders)
            {
                var rootFolderName = rootFolder.Key;
                if (relativePath.StartsWith(rootFolderName))
                {
                    relativePath = relativePath[rootFolderName.Length..].Replace(DirSep, Path.DirectorySeparatorChar);
                    return new DirectoryInfo(Path.Combine(rootFolder.Value.FullName, relativePath));
                }
            }

            throw new FileManagerIoException("cannot get directory of folder " + relativePath + " invalid path");
        }

        public List<FileViewModel> GetFiles(IConfigManager configManager, RootFolders rootFolders, FilesSortOrder sortOrder, bool getDirectories)
        {
            var list = new List<FileViewModel>();
            if (IsRoot)
            {
                if (getDirectories)
                {
                    foreach (var rootFolder in rootFolders)
                    {
                        bool accessible;
                        int subFileCount;
                        var dir = rootFolder.Value;

                        try
                        {
                            subFileCount = dir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).Count();
                            accessible = true;
                        }
                        catch (Exception)
                        {
                            subFileCount = 0;
                            accessible = false;
                        }

                        var path = new FolderPath(rootFolder.Key, _comparison);
                        list.Add(new FileViewModel(path, path.Name, FileViewModelType.Folder, Models.FileType.Unknown, null, accessible, dir.CreationTime, dir.LastWriteTime, subFileCount, false));
                    }
                }
            }
            else
            {
                var cmp = new FileSorter(StringComparison.CurrentCultureIgnoreCase); // sort ignores case even if the OS is case sensitive
                if (getDirectories)
                {
                    var parent = ToDirectory(rootFolders);

                    DirectoryInfo[] subDirs;
                    try
                    {
                        subDirs = parent.GetDirectories("*");
                        subDirs.Sort(cmp.GetDirectoryComparer(sortOrder));
                    }
                    catch (Exception)
                    {
                        subDirs = [];
                    }

                    foreach (var dir in subDirs)
                    {
                        bool accessible;
                        int subFileCount = 0;

                        try
                        {
                            subFileCount = dir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).Count();
                            accessible = true;
                        }
                        catch
                        {
                            subFileCount = 0;
                            accessible = false;
                        }

                        var path = ToRelativePath(dir, rootFolders, _comparison);
                        list.Add(new FileViewModel(path, dir.Name, FileViewModelType.Folder, Models.FileType.Unknown, null, accessible, dir.CreationTime, dir.LastWriteTime, subFileCount, false));
                    }

                    FileInfo[] subFiles;
                    try
                    {
                        subFiles = parent.GetFiles();
                        subFiles.Sort(cmp.GetFileComparer(sortOrder));
                    }
                    catch (Exception)
                    {
                        subFiles = [];
                    }

                    foreach (var file in subFiles)
                    {
                        var path = ToRelativePath(file, rootFolders, _comparison);
                        var type = configManager.FileServer.GetFileType(file);
                        var accessible = true;

                        list.Add(new FileViewModel(path, file.Name, FileViewModelType.File, type.FileType, type.FileIcon, accessible, file.CreationTime, file.LastWriteTime, file.Length, file.IsReadOnly));
                    }
                }
            }

            return list;
        }

        public List<FolderViewModel> GetFolders(RootFolders rootFolders, FolderPath? selectedPath)
        {
            var list = new List<FolderViewModel>();
            if (IsRoot)
            {
                foreach (var kvp in rootFolders)
                {
                    var subDirs = kvp.Value.GetDirectories();
                    var name = kvp.Key[1..^1];
                    var folder = new FolderViewModel(kvp.Key, name)
                    {
                        HasChildren = subDirs.Length > 0,
                    };
                    if (selectedPath != null)
                    {
                        int ancestorOfSelected = GetAncesorIndex(selectedPath);
                        if (ancestorOfSelected >= 0)
                        {
                            var subPath = kvp.Key;
                            var subFolder = new FolderPath(subPath, FileViewModelType.Folder, _comparison);
                            folder.HasChildren = null;
                            folder.Children = subFolder.GetFolders(rootFolders, selectedPath);
                            folder.Opened = ancestorOfSelected >= 0;
                            folder.Selected = ancestorOfSelected == 0;
                        }
                    }

                    list.Add(folder);
                }

                return list;
            }

            // TODO: return folder list of defined types
            return list;
        }

        public bool IsRoot
        {
            get => (string.IsNullOrEmpty(Value) || Value == "/" || Value == "#");
        }

        public bool IsRootFolder(RootFolders rootFolders)
        {
            foreach (var rootFolder in rootFolders)
            {
                if (string.Equals(rootFolder.Key, Value, _comparison))
                {
                    return true;
                }
            }

            return false;
        }

        public FileInfo ToFile(RootFolders rootFolders)
        {
            if (IsRoot)
            {
                throw new FileManagerIoException("root is not a file");
            }

            foreach (var rootFolder in rootFolders)
            {
                if (Value.StartsWith(rootFolder.Key, _comparison))
                {
                    var relativePath = Value[rootFolder.Key.Length..].Replace(DirSep, Path.DirectorySeparatorChar);
                    return new FileInfo(Path.Combine(rootFolder.Value.FullName, relativePath));
                }
            }

            throw new FileManagerIoException("invalid path " + Value);
        }

        /// <summary>
        /// converts a file on disk to a relative folder-path
        /// </summary>
        public static FolderPath ToRelativePath(FileInfo file, RootFolders rootFolders, StringComparison comparison)
        {
            string fullName = file.FullName;

            foreach (var rootFolder in rootFolders)
            {
                var rootFolderFullName = rootFolder.Value.FullName;
                if (!rootFolderFullName.EndsWith(Path.DirectorySeparatorChar))
                    rootFolderFullName += Path.DirectorySeparatorChar;

                if (fullName.StartsWith(rootFolderFullName))
                {
                    string relative = rootFolder.Key + fullName[rootFolderFullName.Length..].Replace(Path.DirectorySeparatorChar, DirSep);
                    return new FolderPath(relative, comparison);
                }
            }

            throw new FileManagerIoException("invalid path");
        }

        /// <summary>
        /// converts a directory on disk to a relative folder-path
        /// </summary>
        public static FolderPath ToRelativePath(DirectoryInfo dir, RootFolders rootFolders, StringComparison comparison)
        {
            string fullName = dir.FullName;
            
            if (!fullName.EndsWith(Path.DirectorySeparatorChar))
                fullName += Path.DirectorySeparatorChar;

            foreach (var rootFolder in rootFolders)
            {
                var rootFolderFullName = rootFolder.Value.FullName;
                if (!rootFolderFullName.EndsWith(Path.DirectorySeparatorChar))
                    rootFolderFullName += Path.DirectorySeparatorChar;

                if (fullName.StartsWith(rootFolderFullName, comparison))
                {
                    string relative = rootFolder.Key + (fullName[rootFolderFullName.Length..]).Replace(Path.DirectorySeparatorChar, DirSep);
                    return new FolderPath(relative, comparison);
                }
            }

            throw new FileManagerIoException("invalid path");
        }

        /// <summary>
        /// returns true if this is an immediate or decendent child of srcPath
        /// </summary>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        public bool IsChildOf(FolderPath srcPath)
        {
            return Value.Contains(srcPath.Value, _comparison);
        }
    }
}
