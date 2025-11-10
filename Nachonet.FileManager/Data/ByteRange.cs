namespace Nachonet.FileManager.Data
{
    public class ByteRange
    {
        public ByteRange(bool rangeSpecified, Int64? start, Int64? end)
        {
            RangeSpecified = rangeSpecified;
            Start = start;
            End = end;
        }

        public bool RangeSpecified { get; }

        public Int64? Start { get; }

        public Int64? End { get; }

        public override string ToString()
        {
            if (RangeSpecified)
            {
                return string.Format("bytes={0}-{1}", Start, End);
            }
            else
            {
                return "<not specified>";
            }
        }

        public static ByteRange FromRequest(HttpRequest request)
        {
            long? start = null;
            long? end = null;
            bool rangeSpecified = false;
            if (request.Headers.TryGetValue("Range", out var bytes))
            {
                string? b = bytes;
                if (b != null)
                {
                    if (b.StartsWith("bytes="))
                    {
                        b = b["bytes=".Length..].Trim();
                    }
                    var parts = b.Split(new char[] { '-' }, 2);
                    if (parts.Length == 2)
                    {
                        rangeSpecified = true;
                        if (long.TryParse(parts[0], out var startVal))
                            start = startVal;

                        if (long.TryParse(parts[1], out var endVal))
                            end = endVal;
                    }
                }
            }

            if (rangeSpecified && start == null && end == null)
                System.Diagnostics.Debug.WriteLine("...");

            return new ByteRange(rangeSpecified, start, end);
        }
    }
}