using Hengine.Graphics;
using System.IO;
using Silk.NET.OpenGL;
using Hengine.Components.Graphics;
using System.Text;

namespace Hengine.Graphics
{
	public struct Shader
	{
		public byte[] Vertex { get; set; }

		public byte[] Fragment { get; set; }

		public static Shader FromFiles(string vertexPath, string fragmentPath)
		{
			return new Shader
			{
				Vertex = File.ReadAllBytes(vertexPath),
				Fragment = File.ReadAllBytes(fragmentPath),
			};
		}

		static void CompileShader(GL gl, uint id, byte[] source)
		{
			gl.ShaderSource(id, Encoding.UTF8.GetString(source));
			gl.CompileShader(id);

			gl.GetShader(id, ShaderParameterName.CompileStatus, out int result);
			if (result == 0)
				throw new Exception(gl.GetShaderInfoLog(id));
		}
	}
}
