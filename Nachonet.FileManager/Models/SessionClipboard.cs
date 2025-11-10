using Nachonet.FileManager.Data;

namespace Nachonet.FileManager.Models
{
    public class SessionClipboard
    {
        public string SessionId { get; }

        public string[] Files { get; }

        public SessionFolderClipboardAction Action { get; }

        public SessionClipboard(string sessionId, string[] files, SessionFolderClipboardAction action)
        {
            SessionId = sessionId;
            Files = files;
            Action = action;
        }

        public override string ToString()
        {
            return string.Format("sessionId: {0}, files: {1}, action: {2}", SessionId, string.Join(", ", Files.Select(f => '"' + f + '"')), Action);
        }
    }
}
