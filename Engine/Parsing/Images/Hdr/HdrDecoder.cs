using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Engine.Parsing
{
    internal class HdrDecoder : SpecializedImageDecoder<HdrDecoderOptions>
    {
        public static HdrDecoder Instance { get; } = new HdrDecoder();

        private static readonly Vector4f MaxBytes = new Vector4f(255f);

        private static readonly Vector4f Half = new Vector4f(0.5f);

        protected unsafe override Image<TPixel> Decode<TPixel>(HdrDecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            //stream.Read(stackalloc byte[8]);

            // Read header, not really interested. https://www.graphics.cornell.edu/~bjw/rgbe.html for documentation
            Span<byte> buff = stackalloc byte[51];
            stream.Read(buff);

            int yIdx = FindIdx(buff, 'Y');
            int xIdx = FindIdx(buff, 'X');
            int nlIdx = FindIdx(buff.Slice(xIdx), '\n') + xIdx;

            int width = int.Parse(buff.Slice(xIdx + 2, nlIdx - xIdx - 2));
            int height = int.Parse(buff.Slice(yIdx + 2, xIdx - yIdx - 4));

            if (width < 8 || width > 0x7fff)
                return ReadFlat<TPixel>(stream, width, height, options.GeneralOptions.Configuration);

            Image<TPixel> image = new Image<TPixel>(width, height);
            Buffer2D<TPixel> pixelBuffer = image.Frames.RootFrame.PixelBuffer;

            Span<byte> pixelBuff = stackalloc byte[2];
            Span<byte> rgbeBuff = stackalloc byte[4];

            IMemoryOwner<byte> row = MemoryPool<byte>.Shared.Rent(width * 4);
            IMemoryOwner<Rgba64> rowPixel = MemoryPool<Rgba64>.Shared.Rent(width);

            fixed (byte* pRowSpan = row.Memory.Span)
            fixed (Rgba64* pPixelSpan = rowPixel.Memory.Span)
            {
                BufferedStream bstream = new BufferedStream(stream);
                for (int y = 0; y < height; y++)
                {
                    bstream.Read(rgbeBuff);

                    if (rgbeBuff[0] != 2 || rgbeBuff[1] != 2 || (rgbeBuff[2] & 0x80) != 0)
                    {
                        throw new Exception("Not RLE encoded");
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        int a = 0;
                        while (a < width)
                        {
                            if (bstream.Read(pixelBuff) < pixelBuff.Length)
                                throw new Exception();

                            if (pixelBuff[0] > 128)
                            {
                                int count = pixelBuff[0] - 128;
                                if (count == 0 || count > width - a)
                                    throw new Exception();

                                for (int b = 0; b < count; b++)
                                {
                                    pRowSpan[a + width * i] = pixelBuff[1];
                                    a++;
                                }
                            }
                            else
                            {
                                int count = pixelBuff[0];
                                if (count == 0 || count > width - a)
                                    throw new Exception();

                                pRowSpan[a + width * i] = pixelBuff[1];
                                a++;

                                if (--count > 0)
                                {
                                    bstream.Read(row.Memory.Span.Slice(a + width * i, count * 1));

                                    a += count;
                                }
                            }
                        }
                    }

                    for (int i = 0; i < width; i++)
                    {
                        float f = MathF.Exp(pRowSpan[i + width * 3] - (128 + 8));
                        float r = pRowSpan[i] * f;
                        float g = pRowSpan[i + width * 1] * f;
                        float b = pRowSpan[i + width * 2] * f;

                        r *= 65535f;
                        g *= 65535f;
                        b *= 65535f;

                        r = MathF.Min(65535, r);
                        g = MathF.Min(65535, g);
                        b = MathF.Min(65535, b);

                        pPixelSpan[i] = new Rgba64((ushort)r, (ushort)g, (ushort)b, 0);
                    }

                    PixelOperations<TPixel>.Instance.FromRgba64(options.GeneralOptions.Configuration, rowPixel.Memory.Span, pixelBuffer.DangerousGetRowSpan(y));
                }
            }

            row.Dispose();
            rowPixel.Dispose();

            return image;
        }

        protected override Image Decode(HdrDecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override ImageInfo Identify(DecoderOptions options, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override HdrDecoderOptions CreateDefaultSpecializedOptions(DecoderOptions options)
        {
            return new HdrDecoderOptions
            {
                GeneralOptions = options
            };
        }

        Image<TPixel> ReadFlat<TPixel>(Stream stream, int width, int height, Configuration configuration) where TPixel : unmanaged, IPixel<TPixel>
        {
            Image<TPixel> image = new Image<TPixel>(width, height);
            Buffer2D<TPixel> pixelBuffer = image.Frames.RootFrame.PixelBuffer;

            //Span<byte> pixelBuff = stackalloc byte[4];
            Span<Rgba32> rowBuff = stackalloc Rgba32[width];
            //stream.Read(pixelBuff);

            for (int y = 0; y < height; y++)
            {
                Span<TPixel> row = pixelBuffer.DangerousGetRowSpan(y);
                stream.Read(MemoryMarshal.AsBytes(rowBuff));

                for (int x = 0; x < width; x++)
                {
                    rowBuff[x].A = 0;
                }

                PixelOperations<TPixel>.Instance.FromRgba32(configuration, rowBuff, row);
            }

            return image;
        }

        void ReadFlat(Span<byte> data, Span<Rgba32> pixels)
        {
            int length = data.Length / 4;
            for (int i = 0; i < length; i++)
            {
                pixels[i] = new Rgba32(data[i * 4 + 0], data[i * 4 + 1], data[i * 4 + 2], 0);
            }
        }

        int FindIdx(Span<byte> buff, byte data)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                if (buff[i] == data)
                    return i;
            }

            return -1;
        }

        int FindIdx(Span<byte> buff, char data)
        {
            return FindIdx(buff, (byte)data);
        }
    }
}
