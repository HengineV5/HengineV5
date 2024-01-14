using SixLabors.ImageSharp.Formats;

namespace Engine.Parsing
{
    internal class HdrFormat : IImageFormat
    {
        public static HdrFormat Instance { get; } = new HdrFormat();

        public string Name => "HDR";

        public string DefaultMimeType => "image/hdr";

        public IEnumerable<string> MimeTypes => new string[2] { "image/hdr", "image/rgbe" };

        public IEnumerable<string> FileExtensions => new string[2] { "png", "apng" };
    }
}
