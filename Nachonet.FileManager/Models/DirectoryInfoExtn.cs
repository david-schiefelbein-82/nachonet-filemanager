namespace Nachonet.FileManager.Models
{
    public static class DirectoryInfoExtn
    {
        public static void CopyTo(this DirectoryInfo src, DirectoryInfo dest, bool recursive)
        {
            // Check if the source directory exists
            if (!src.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {src.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] srcDirs = src.GetDirectories();

            // Create the destination directory
            if (!dest.Exists)
                dest.Create();

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in src.GetFiles())
            {
                string targetFilePath = Path.Combine(dest.FullName, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo srcSubDir in srcDirs)
                {
                    var destSubDir = new DirectoryInfo(Path.Combine(dest.FullName, srcSubDir.Name));
                    CopyTo(srcSubDir, destSubDir, true);
                }
            }
        }
    }
}
