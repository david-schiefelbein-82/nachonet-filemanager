namespace Nachonet.FileManager.Models
{
    public class TextFileContent
    {
        public string Text { get; set; }

        public string Syntax { get; set; }

        public bool IsReadOnly { get; set; }

        public TextFileContent(string text, string syntax, bool isReadOnly)
        {
            Text = text;
            Syntax = syntax;
            IsReadOnly = isReadOnly;
        }
    }
}
