using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Engine
{
    internal sealed class HdrDecoderOptions : ISpecializedDecoderOptions
    {
        public DecoderOptions GeneralOptions { get; init; } = new DecoderOptions();
    }
}
