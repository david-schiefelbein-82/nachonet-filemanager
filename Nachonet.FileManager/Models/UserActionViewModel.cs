using Microsoft.AspNetCore.Mvc.RazorPages;
using Nachonet.FileManager.Configuration;

namespace Nachonet.FileManager.Models
{
    public class UserActionViewModel
    {
        public string? Page { get; set; }

        public string? ErrorMessage { get; set; }

        public string? InfoMessage { get; set; }

        public WebServerLoginType LoginType { get; set; }

        public string OidcLoginText { get; set; }

        public string AdLoginText { get; set; }

        public SisterSiteViewModel[] SisterSites { get; set; }

        public UserActionViewModel(string? page, string? errorMessage, string? infoMessage, WebServerLoginType loginType, string oidcLoginText, string adLoginText, SisterSiteViewModel[] sisterSites)
        {
            Page = page;
            ErrorMessage = errorMessage;
            InfoMessage = infoMessage;
            LoginType = loginType;
            OidcLoginText = oidcLoginText;
            AdLoginText = adLoginText;
            SisterSites = sisterSites;
        }
    }
}