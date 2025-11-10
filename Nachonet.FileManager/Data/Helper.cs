using System.Text;

namespace Nachonet.FileManager.Data
{
    public static class Helper
    {
        public static string EncodeAttr(string attr)
        {
            return attr;
        }

        public static string JsEncode(string? name)
        {
            if (name == null)
                return "null";

            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char c in name)
            {
                if (c == '"')
                {
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(c);
                }
            }
            sb.Append('"');

            return sb.ToString();
        }
    }
}
