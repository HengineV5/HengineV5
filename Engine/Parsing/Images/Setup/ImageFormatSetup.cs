using SixLabors.ImageSharp.Formats;

namespace Engine.Parsing
{
    public static class ImageFormatSetup
    {
        static DecoderOptions decoderOptions = new DecoderOptions();

        public static void HdrSetup()
        {
            decoderOptions.Configuration.ImageFormatsManager.AddImageFormat(HdrFormat.Instance);
            decoderOptions.Configuration.ImageFormatsManager.AddImageFormatDetector(new HdrFormatDetector());
            decoderOptions.Configuration.ImageFormatsManager.SetDecoder(HdrFormat.Instance, HdrDecoder.Instance);
        }
    }
}
