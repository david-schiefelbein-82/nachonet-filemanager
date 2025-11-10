using Nachonet.FileManager.Errors;

namespace Nachonet.FileManager.Data
{
    public class RootFolders : Dictionary<string, DirectoryInfo>
    {
        public RootFolders(Dictionary<string, string> folders)
        {
            foreach (var kvp in folders)
            {
                AssertKeyOkay(kvp.Key);

                this[kvp.Key] = new DirectoryInfo(Path.GetFullPath(kvp.Value));
            }
        }

        private static void AssertKeyOkay(string key)
        {
            if (!@key.StartsWith(FolderPath.DirSep))
            {
                throw new FileManagerException("Root Folder should start with /", System.Net.HttpStatusCode.InternalServerError);
            }

            if (!@key.EndsWith(FolderPath.DirSep))
            {
                throw new FileManagerException("Root Folder should end with /", System.Net.HttpStatusCode.InternalServerError);
            }

            if (@key.Length == 0)
            {
                throw new FileManagerException("Root Folder should start with /", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public static bool Equals(RootFolders rootFolder1, RootFolders rootFolder2, StringComparison comparison)
        {
            if (rootFolder1.Count !=  rootFolder2.Count)
                return false;

            foreach (var kvp in rootFolder1)
            {
                if (rootFolder2.TryGetValue(kvp.Key, out var value))
                {
                    if (!string.Equals(kvp.Value.FullName, value.FullName, comparison))
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
