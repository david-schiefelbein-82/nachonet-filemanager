namespace Nachonet.FileManager.Models
{
    public class SisterSiteViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public SisterSiteViewModel(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}