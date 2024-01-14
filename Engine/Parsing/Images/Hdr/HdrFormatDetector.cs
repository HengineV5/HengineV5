using SixLabors.ImageSharp.Formats;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Engine.Parsing
{
    internal class HdrFormatDetector : IImageFormatDetector
    {
        public int HeaderSize => 2;

        public bool TryDetectFormat(ReadOnlySpan<byte> header, [NotNullWhen(true)] out IImageFormat? format)
        {
            format = (IsSupportedFileFormat(header) ? HdrFormat.Instance : null);
            return format != null;
        }

        private bool IsSupportedFileFormat(ReadOnlySpan<byte> header)
        {
            if (header.Length >= HeaderSize)
            {
                return Encoding.ASCII.GetString(header.Slice(0, HeaderSize)) == "#?";
            }

            return false;
        }
    }
}
