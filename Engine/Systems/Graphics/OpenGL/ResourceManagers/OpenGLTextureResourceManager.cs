using EnCS;
using EnCS.Attributes;
using Silk.NET.OpenGL;

namespace Engine.Graphics
{
	public struct GlTextureBuffer
	{
	}

	[ResourceManager]
	public partial class OpenGLTextureResourceManager : IResourceManager<ETexture, GlTextureBuffer>
	{
		uint idx = 0;
		Memory<Graphics.ETexture> textures = new Graphics.ETexture[32];
		Memory<Graphics.GlTextureBuffer> textureBuffers = new Graphics.GlTextureBuffer[32];

		Dictionary<string, uint> textureCache = new Dictionary<string, uint>();

		GL gl;

        public OpenGLTextureResourceManager(GL gl)
        {
			this.gl = gl;
        }

        public ref Graphics.GlTextureBuffer Get(uint id)
		{
			return ref textureBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.ETexture texture)
		{
			if (textureCache.TryGetValue(texture.name, out uint id))
				return id;

			textureCache.Add(texture.name, idx);
			textures.Span[(int)idx] = texture;
			textureBuffers.Span[(int)idx] = CreateTextureBuffer(gl, texture);
			return idx++;
		}

		static GlTextureBuffer CreateTextureBuffer(GL gl, Graphics.ETexture texture)
		{
			return new GlTextureBuffer(); // TODO:
		}
	}
}
