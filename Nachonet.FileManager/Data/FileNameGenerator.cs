namespace Nachonet.FileManager.Data
{
    public static class FileNameGenerator
    {
        public static string FileNameWithoutExtension(this FileInfo file)
        {
            return Path.GetFileNameWithoutExtension(file.Name);
        }

        public static FileInfo Unique(this FileInfo file)
        {
            int index = 0;
            var fname = file.FileNameWithoutExtension();
            var dir = file.Directory?.FullName;
            while (file.Exists)
            {
                index++;

                if (dir != null)
                {
                    file = new FileInfo(Path.Join(dir, fname + "(" + index + ")" + file.Extension));
                }
                else
                {
                    file = new FileInfo(fname + "(" + index + ")" + file.Extension);
                }
            }

            return file;
        }

        public static DirectoryInfo Unique(this DirectoryInfo dir)
        {
            int index = 0;
            var origName = dir.FullName;
            while (dir.Exists)
            {
                index++;
                var fullName = origName;
                if (fullName.EndsWith(Path.DirectorySeparatorChar))
                    fullName = fullName[..^1];

                dir = new DirectoryInfo(fullName + "(" + index + ")" + Path.DirectorySeparatorChar);
            }

            return dir;
        }
    }
}
