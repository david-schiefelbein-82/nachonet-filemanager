using Nachonet.FileManager.Configuration;

namespace Nachonet.FileManager.Data
{
    public class UserManager
    {
        private IConfigManager _configManager;

        public UserManager(IConfigManager configManager)
        {
            _configManager = configManager;
        }

        public UserSettings GetUserSettings(string userId)
        {
            var user = _configManager.UserPreference.Users.FirstOrDefault(
                x => string.Equals(x.UserId, userId, StringComparison.CurrentCultureIgnoreCase)) ??
                new UserSettings() { UserId = userId, Layout = Models.FileManagerLayout.Tiles };

            return user;
        }
        public UserSettings SaveUserSettings(string userId, UserSettings settings)
        {
            var user = _configManager.UserPreference.Users.FirstOrDefault(
                x => string.Equals(x.UserId, userId, StringComparison.CurrentCultureIgnoreCase));

            if (user == null)
            {
                user = settings;
                _configManager.UserPreference.Users.Add(user);
            }
            else
            {
                settings.CopyTo(user);
            }

            _configManager.UserPreference.TrySave();
            return user;
        }
    }
}
