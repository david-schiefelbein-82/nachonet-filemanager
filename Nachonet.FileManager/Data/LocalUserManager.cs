using Nachonet.Common.Web.AppLocal.Config;
using Nachonet.FileManager.Configuration;

namespace Nachonet.FileManager.Data
{
    public class LocalUserManager(IConfigManager configManager) : ILocalUserManager
    {
        private readonly IConfigManager _configManager = configManager;

        public void CreateOrUpdateUser(string oldUserName, string username, string label, string password)
        {
            var cmp = StringComparison.CurrentCultureIgnoreCase;
            var existingUser = _configManager.AppLocal.Users.FirstOrDefault(x => string.Equals(x.Username, oldUserName, cmp));
            var userNameChange = !string.Equals(oldUserName, username, cmp);
            var newUserExists = _configManager.AppLocal.Users.Any(x => string.Equals(x.Username, username, cmp));
            if (existingUser == null)
            {
                if (userNameChange && newUserExists)
                    throw new Exception("username " + username + " exists");

                if (string.IsNullOrWhiteSpace(password))
                    throw new Exception("password missing");

                _configManager.AppLocal.Users.Add(new AppLocalUserConfig()
                {
                    Username = username,
                    PreferredName = label,
                    Password = password,
                    PasswordSalt = string.Empty
                });
            }
            else
            {
                if (userNameChange && newUserExists)
                    throw new Exception("username " + username + " already exists");

                existingUser.Username = username;
                existingUser.PreferredName = label;
                if (!string.IsNullOrWhiteSpace(password))
                {
                    // note: password will get hashed on the save function
                    existingUser.Password = password;
                    existingUser.PasswordSalt = string.Empty;
                }
            }

            _configManager.SaveAppLocal();
        }

        public void CreateOrUpdateGroup(string oldGroupName, string groupName, string label)
        {
            var cmp = StringComparison.CurrentCultureIgnoreCase;
            var existingUser = _configManager.AppLocal.Groups.FirstOrDefault(x => string.Equals(x.GroupName, oldGroupName, cmp));
            var groupNameChange = !string.Equals(oldGroupName, groupName, cmp);
            var newGroupExists = _configManager.AppLocal.Groups.Any(x => string.Equals(x.GroupName, groupName, cmp));
            if (existingUser == null)
            {
                if (groupNameChange && newGroupExists)
                    throw new Exception("group " + groupName + " already exists");

                _configManager.AppLocal.Groups.Add(
                    new AppLocalGroupConfig()
                    {
                        GroupName = groupName,
                        PreferredName = label,
                    });
            }
            else
            {
                existingUser.GroupName = groupName;
                existingUser.PreferredName = label;
            }

            // note: password will get hashed on the save function
            _configManager.SaveAppLocal();
        }

        public int DeleteUser(string username)
        {
            int count = _configManager.AppLocal.Users.RemoveAll(x => string.Equals(x.Username, username, StringComparison.CurrentCultureIgnoreCase));
            if (count > 0)
            {
                _configManager.SaveAppLocal();
            }

            return count;
        }

        public int DeleteGroup(string groupName)
        {
            int count = _configManager.AppLocal.Groups.RemoveAll(x => string.Equals(x.GroupName, groupName, StringComparison.CurrentCultureIgnoreCase));
            if (count > 0)
            {
                _configManager.SaveAppLocal();
            }

            return count;
        }
    }
}
