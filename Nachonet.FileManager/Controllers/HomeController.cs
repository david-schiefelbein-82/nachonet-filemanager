using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nachonet.Common.Web.Oidc;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Models;
using System.Diagnostics;
using System.Web;

namespace Nachonet.FileManager.Controllers
{
    [AllowAnonymous]
    public partial class HomeController(ILogger<HomeController> logger, IConfigManager configManager, IOidcClient oidcClient) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly IOidcClient _oidcClient = oidcClient;
        private readonly IConfigManager _configManager = configManager;

        [HttpGet("/")]
        public IActionResult Index(string? page, string? error, string? info)
        {
            var host = Request.Host.Value ?? string.Empty;
            _logger.LogDebug("/ page:{page}, error: {error}, info: {info}, host: {host}", page, error, info, host);
            HttpContext.Session.SetString("FileManager-SID", HttpContext.Session.Id);
            _logger.LogInformation("GET {host}/ sessionid:{sid}", host, HttpContext.Session.Id);

            var sisterSites = (from x in
                              _configManager.WebServer.SisterSites
                              select new SisterSiteViewModel(x.Name, x.Url.Replace("${host}", host))).ToArray();
            var model = new UserActionViewModel(page, error, info, _configManager.WebServer.LoginMethod, _configManager.WebServer.OidcLoginText, _configManager.WebServer.AdLoginText, sisterSites);
            return View(model);
        }

        public IActionResult Login(string? page)
        {
            var host = Request.Host.Value ?? string.Empty;
            var state = "page=" + HttpUtility.UrlEncode(page) + "&sessionid=" + HttpContext.Session.Id;
            var url = _oidcClient.GetAuthenticationUrl(host, state);
            _logger.LogInformation("/Login page={page}, sessionid={sid} redirecting to {url}", page, HttpContext.Session.Id, url);
            return new RedirectResult(url);
        }

        public IActionResult Error()
        {
            _logger.LogWarning("/Error {sid}", HttpContext.Session.Id);
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}