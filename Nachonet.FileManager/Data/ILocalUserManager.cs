namespace Nachonet.FileManager.Data
{
    public interface ILocalUserManager
    {
        void CreateOrUpdateUser(string oldUsername, string username, string label, string password);

        void CreateOrUpdateGroup(string oldGroupName, string groupName, string label);

        int DeleteUser(string username);

        int DeleteGroup(string groupName);
    }
}
