using Nachonet.Common.Web.Configuration;
using Nachonet.FileManager.Data;

namespace Nachonet.FileManager.Models
{
    public class AuthEntityViewModelWithError : AuthEntityViewModel
    {
        public string? ErrorMessage { get; set; }

        public AuthEntityViewModelWithError()
        {
        }

        public AuthEntityViewModelWithError(AuthEntityConfig config)
        {
            CopyFrom(config);
        }
    }

    public class AuthEntityViewModel
    {
        public AuthEntityViewModel()
        {
            Id = string.Empty;
            Label = string.Empty;
            Password = string.Empty;
        }

        public string Id { get; set; }

        public string Label { get; set; }

        public AuthEntityType Type { get; set; }

        public string Password { get; set; }

        public bool FileReader { get; set; }

        public bool FileWriter { get; set; }

        public bool FileDownloader { get; set; }

        public bool FileUploader { get; set; }

        public bool Admin { get; set; }

        public void CopyFrom(AuthEntityConfig config)
        {
            Id = config.Id;
            Label = config.Label;
            Type = config.Type;
            FileReader = config.Roles.Any(r => string.Equals(r, FileManagerRoles.FileReader, StringComparison.CurrentCultureIgnoreCase));
            FileWriter = config.Roles.Any(r => string.Equals(r, FileManagerRoles.FileWriter, StringComparison.CurrentCultureIgnoreCase));
            FileDownloader = config.Roles.Any(r => string.Equals(r, FileManagerRoles.FileDownloader, StringComparison.CurrentCultureIgnoreCase));
            FileUploader = config.Roles.Any(r => string.Equals(r, FileManagerRoles.FileUploader, StringComparison.CurrentCultureIgnoreCase));
            Admin = config.Roles.Any(r => string.Equals(r, FileManagerRoles.Admin, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
