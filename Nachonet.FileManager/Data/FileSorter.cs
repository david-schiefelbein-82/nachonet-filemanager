namespace Nachonet.FileManager.Data
{
    public class FileSorter(StringComparison stringComparison)
    {
        private readonly StringComparison _cmp = stringComparison;

        public Comparison<DirectoryInfo> GetDirectoryComparer(FilesSortOrder sortOrder)
        {
            return sortOrder switch
            {
                FilesSortOrder.NameAsc => (x, y) => string.Compare(x.FullName, y.FullName, _cmp),
                FilesSortOrder.NameDesc => (x, y) => string.Compare(y.FullName, x.FullName, _cmp),
                _ => (x, y) => string.Compare(x.FullName, y.FullName, _cmp)
            };
        }

        public Comparison<FileInfo> GetFileComparer(FilesSortOrder sortOrder)
        {
            return sortOrder switch
            {
                FilesSortOrder.NameAsc => (x, y) => string.Compare(x.FullName, y.FullName, _cmp),
                FilesSortOrder.NameDesc => (x, y) => string.Compare(y.FullName, x.FullName, _cmp),
                FilesSortOrder.SizeAsc => (x, y) => x.Length < y.Length ? -1 : (x.Length > y.Length ? 1 : 0),
                FilesSortOrder.SizeDesc => (x, y) => x.Length < y.Length ? 1 : (x.Length > y.Length ? -1 : 0),
                _ => (x, y) => string.Compare(x.FullName, y.FullName, _cmp)
            };
        }
    }
}
