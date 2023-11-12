using Engine.Graphics;
using System.IO;
using Silk.NET.OpenGL;
using Engine.Components.Graphics;
using System.Text;

namespace Engine.Graphics
{
	public struct Shader
	{
		public int ID { get; set; }

		public byte[] Vertex { get; set; }

		public byte[] Fragment { get; set; }

		public ShaderProgram CreateProgram(GL gl)
		{
			uint program = gl.CreateProgram();
			uint vertexID = gl.CreateShader(ShaderType.VertexShader);
			uint fragmentID = gl.CreateShader(ShaderType.FragmentShader);

			CompileShader(gl, vertexID, Vertex);
			CompileShader(gl, fragmentID, Fragment);

			gl.AttachShader(program, vertexID);
			gl.AttachShader(program, fragmentID);

			gl.LinkProgram(program);
			gl.ValidateProgram(program);

			gl.DeleteShader(vertexID);
			gl.DeleteShader(fragmentID);

			gl.GetProgram(program, GLEnum.LinkStatus, out var status);
			if (status == 0)
				Console.WriteLine($"Error linking shader {gl.GetProgramInfoLog(program)}");

			return new ShaderProgram()
			{
				ID = program,
			};
		}

		public static ShaderUniforms GetShaderUniforms(GL gl, in ShaderProgram.Ref program)
		{
			return new ShaderUniforms
			{
				Model = new ModelUniforms
				{
					Translation = gl.GetUniformLocation((uint)program.ID, "u_Translation"),
					Rotation = gl.GetUniformLocation((uint)program.ID, "u_Rotation"),
					Scale = gl.GetUniformLocation((uint)program.ID, "u_Scale"),
				},

				Camera = new CameraUniforms
				{
					View = gl.GetUniformLocation((uint)program.ID, "u_View"),
					ViewPos = gl.GetUniformLocation((uint)program.ID, "u_ViewPos"),
					Projection = gl.GetUniformLocation((uint)program.ID, "u_Projection"),
				},

				Light = new LightUniforms
				{
					Ambient = gl.GetUniformLocation((uint)program.ID, "u_Light.ambient"),
					Diffuse = gl.GetUniformLocation((uint)program.ID, "u_Light.diffuse"),
					Specular = gl.GetUniformLocation((uint)program.ID, "u_Light.specular"),
					Position = gl.GetUniformLocation((uint)program.ID, "u_Light.position"),
				},

				Material = new MaterialUniforms
				{
					Ambient = gl.GetUniformLocation((uint)program.ID, "u_Material.ambient"),
					Diffuse = gl.GetUniformLocation((uint)program.ID, "u_Material.diffuse"),
					Specular = gl.GetUniformLocation((uint)program.ID, "u_Material.specular"),
					Shininess = gl.GetUniformLocation((uint)program.ID, "u_Material.shininess"),
				}
			};
		}

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
