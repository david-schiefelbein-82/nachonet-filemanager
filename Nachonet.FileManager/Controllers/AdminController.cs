using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Nachonet.Common.Web.Configuration;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;

namespace Nachonet.FileManager.Controllers
{
    [Authorize(Roles = FileManagerRoles.Admin)]
    public class AdminController(ILogger<AdminController> logger, IConfigManager configManager, ILocalUserManager localUserManager) : Controller
    {
        private readonly ILogger<AdminController> _logger = logger;
        private readonly IConfigManager _configManager = configManager;
        private readonly ILocalUserManager _localUserManager = localUserManager;

        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                User.AssertAdmin();
            }
            catch (FileManagerAccessException ex)
            {
                return RedirectToAction("Index", "Home", new { page = "admin", error = ex.Message });
            }

            var viewModel = from x
                            in _configManager.Authorization.UsersAndGroups
                            select new AuthEntityViewModelWithError(x);
            return View(viewModel);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string id, string label, string password, AuthEntityType type,
            bool fileReader, bool fileWriter, bool fileDownloader, bool fileUploader, bool admin)
        {
            try
            {
                if (_configManager.Authorization.UsersAndGroups.FirstOrDefault(x => x.Id == id && x.Type == type) != null)
                {
                    throw new Exception("user already exists");
                }

                switch (type)
                {
                    case AuthEntityType.AppLocalUser:
                        if (string.IsNullOrWhiteSpace(password))
                            throw new Exception("AppLocalUser requires a password");
                        _localUserManager.CreateOrUpdateUser(id, id, label, password);
                        break;
                    case AuthEntityType.AppLocalGroup:
                        if (!string.IsNullOrWhiteSpace(password))
                            throw new Exception("password only applies to LocalUsers");
                        _localUserManager.CreateOrUpdateGroup(id, id, label);
                        break;
                    default:
                        if (!string.IsNullOrWhiteSpace(password))
                            throw new Exception("password only applies to LocalUsers - for other types authentication is handled externally");

                        break;
                }

                var newUser = new AuthEntityConfig()
                {
                    Id = id,
                    Label = label,
                    Type = type,
                    Roles = BuildRoles(fileReader, fileWriter, fileDownloader, fileUploader, admin)
                };

                _configManager.Authorization.UsersAndGroups.Add(newUser);
                _configManager.Authorization.Save();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return View(
                    new AuthEntityViewModelWithError()
                    {
                        ErrorMessage = ex.Message,
                        Id = id,
                        Label = label,
                        Type = type,
                        FileReader = fileReader,
                        FileWriter = fileWriter,
                        FileDownloader = fileDownloader,
                        FileUploader = fileUploader,
                    });
            }
        }

        // GET
        public ActionResult Edit(string id, AuthEntityType type)
        {
            var viewModel = new AuthEntityViewModelWithError();
            var item = _configManager.Authorization.UsersAndGroups.FirstOrDefault(x => x.Id == id && x.Type == type);
            if (item == null)
            {
                viewModel.ErrorMessage = "Cannot find " + type + " with Id: " + id;
            }
            else
            {
                viewModel.CopyFrom(item);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string origId, AuthEntityType origType, string id, string label, string password, AuthEntityType type,
            bool fileReader, bool fileWriter, bool fileDownloader, bool fileUploader, bool admin)
        {
            try
            {
                var item = _configManager.Authorization.UsersAndGroups.FirstOrDefault(x => x.Id == origId && x.Type == origType)
                    ?? throw new Exception("cannot edit item");

                if (!string.IsNullOrEmpty(id) && string.Equals(id, this.User.Identity?.Name, StringComparison.CurrentCultureIgnoreCase) && !admin)
                {
                    var model = new AuthEntityViewModelWithError() { ErrorMessage = "Cannot remove admin permissions from yourself" };
                    model.CopyFrom(item);
                    model.Id = origId;
                    model.Type = origType;
                    model.Label = label;
                    model.FileReader = fileReader;
                    model.FileWriter = fileWriter;
                    model.FileDownloader = fileDownloader;
                    model.FileUploader = fileUploader;
                    model.FileReader = admin;
                    return View(model);
                }

                switch (type)
                {
                    case AuthEntityType.AppLocalUser:
                        _localUserManager.CreateOrUpdateUser(origId, id, label, password);
                        break;
                    case AuthEntityType.AppLocalGroup:
                        if (!string.IsNullOrWhiteSpace(password))
                            throw new Exception("password only applies to LocalUsers");

                        _localUserManager.CreateOrUpdateGroup(origId, id, label);
                        break;
                    default:
                        if (!string.IsNullOrWhiteSpace(password))
                            throw new Exception("password only applies to LocalUsers - for other types authentication is handled externally");

                        break;
                }

                item.Id = id;
                item.Label = label;
                item.Type = type;
                item.Roles = BuildRoles(fileReader, fileWriter, fileDownloader, fileUploader, admin);
                _configManager.Authorization.Save();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return View(new AuthEntityViewModelWithError()
                {
                    ErrorMessage = ex.Message,
                });
            }
        }

        private static string[] BuildRoles(bool fileReader, bool fileWriter, bool fileDownloader, bool fileUploader, bool admin)
        {
            var roles = new List<string>();
            if (fileReader)
                roles.Add(FileManagerRoles.FileReader);

            if (fileWriter)
                roles.Add(FileManagerRoles.FileWriter);

            if (fileDownloader)
                roles.Add(FileManagerRoles.FileDownloader);

            if (fileUploader)
                roles.Add(FileManagerRoles.FileUploader);

            if (admin)
                roles.Add(FileManagerRoles.Admin);

            return [.. roles];
        }

        public ActionResult Delete(string id, AuthEntityType type)
        {
            var viewModel = new AuthEntityViewModelWithError();
            var item = _configManager.Authorization.UsersAndGroups.FirstOrDefault(x => x.Id == id && x.Type == type);
            if (item == null)
            {
                viewModel.ErrorMessage = "Cannot find " + type + " with Id: " + id;
            }
            else
            {
                viewModel.CopyFrom(item);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, string label, AuthEntityType type)
        {
            try
            {
                if (!string.IsNullOrEmpty(id) && string.Equals(id, this.User.Identity?.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return View(new AuthEntityViewModelWithError() { ErrorMessage = "Cannot delete yourself", Id = id, Type = type, Label = label });
                }

                switch (type)
                {
                    case AuthEntityType.AppLocalUser:
                        _localUserManager.DeleteUser(id);
                        break;
                    case AuthEntityType.AppLocalGroup:
                        _localUserManager.DeleteGroup(id);
                        break;
                }

                var removed = _configManager.Authorization.UsersAndGroups.RemoveAll(x => x.Id == id && x.Type == type);
                if (removed > 0)
                {
                    _configManager.Authorization.Save();
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return View(new AuthEntityViewModelWithError() { ErrorMessage = ex.Message });
            }
        }
    }
}
