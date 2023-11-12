using EnCS.Attributes;
using Engine.Components;
using Engine.Components.Graphics;
using Engine.Graphics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using Shader = Engine.Graphics.Shader;

namespace Engine
{
	[System]
	public partial class OpenGLRenderSystem
	{
		private static readonly Material defaultMaterial = new Material
		{
			Ambient = new Vector3(0.0215f, 0.1745f, 0.0215f),
			Diffuse = new Vector3(0.07568f, 0.61424f, 0.07568f),
			Specular = new Vector3(0.633f, 0.727811f, 0.633f),
			Shininess = 2f
		};

		private static readonly Light defaultLight = new Light
		{
			Ambient = new Vector3(0.2f, 0.2f, 0.2f),
			Diffuse = new Vector3(0.5f, 0.5f, 0.5f),
			Specular = new Vector3(1, 1, 1)
		};

		GL gl;
		IWindow window;
		Camera camera;

        public OpenGLRenderSystem(GL gl, IWindow window)
        {
            this.gl = gl;
			this.window = window;

			camera = new Camera
			{
				Width = 800,
				Height = 600,
				Fov = 1.22173f, // 70 degrees
				ZNear = 0.1f,
				ZFar = 1000
			};
		}

		public void Init()
		{

		}

		public void Dispose()
		{

		}

		public void PreRun()
		{
			gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			window.DoEvents();

			Thread.Sleep(10);
		}

        public unsafe void Update(Position.Ref position, Rotation.Ref rotation, Scale.Ref scale, VertexArrayObject.Ref vao, ShaderProgram.Ref shader)
		{
			ShaderUniforms shaderUniforms = Shader.GetShaderUniforms(gl, shader);

			gl.BindVertexArray(vao.ID);
			gl.UseProgram((uint)shader.ID);

			SetModelUniforms(position, rotation, scale, shaderUniforms);
			SetCameraUniforms(camera, new Position(), new Rotation(), shaderUniforms);
			SetMaterialUniforms(defaultMaterial, shaderUniforms);
			SetLightUniforms(defaultLight, new Position(), shaderUniforms);

			gl.DrawElements(PrimitiveType.Triangles, vao.length, DrawElementsType.UnsignedInt, null);
		}

		public void PostRun()
		{
			window.SwapBuffers();
		}

		unsafe void SetModelUniforms(in Position.Ref position, in Rotation.Ref rotation, in Scale.Ref scale, in ShaderUniforms uniforms)
		{
			// Model matrix components
			Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(new Vector3(position.x, position.y, position.z));
			Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
			Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(new Vector3(scale.x, scale.y, scale.z));

			UniformMatrix4(gl, uniforms.Model.Translation, false, translationMatrix);
			UniformMatrix4(gl, uniforms.Model.Rotation, false, rotationMatrix);
			UniformMatrix4(gl, uniforms.Model.Scale, false, scaleMatrix);
		}

		unsafe void SetCameraUniforms(in Camera camera, in Position position, in Rotation rotation, in ShaderUniforms uniforms)
		{
			Matrix4x4 view = Matrix4x4.CreateTranslation(-new Vector3(position.x, position.y, position.z)) * Matrix4x4.CreateFromQuaternion(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
			Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov, camera.Width / camera.Height, camera.ZNear, camera.ZFar);

			UniformMatrix4(gl, uniforms.Camera.View, false, view);
			gl.Uniform3(uniforms.Camera.ViewPos, 1, position.x);
			UniformMatrix4(gl, uniforms.Camera.Projection, false, projection);
		}

		void SetMaterialUniforms(in Material material, in ShaderUniforms uniforms)
		{
			gl.Uniform3(uniforms.Material.Ambient, 1, material.Ambient.X);
			gl.Uniform3(uniforms.Material.Diffuse, 1, material.Diffuse.X);
			gl.Uniform3(uniforms.Material.Specular, 1, material.Specular.X);
			gl.Uniform1(uniforms.Material.Shininess, material.Shininess);
		}

		void SetLightUniforms(in Light light, in Position position, in ShaderUniforms uniforms)
		{
			gl.Uniform3(uniforms.Light.Ambient, 1, light.Ambient.X);
			gl.Uniform3(uniforms.Light.Diffuse, 1, light.Diffuse.X);
			gl.Uniform3(uniforms.Light.Specular, 1, light.Specular.X);
			gl.Uniform3(uniforms.Light.Position, 1, position.x);
		}

		unsafe void UniformMatrix4(GL gl, int location, bool transpose, in Matrix4x4 matrix)
		{
			gl.UniformMatrix4(location, 1, transpose, matrix.M11);
		}

		/*
		public void Update(ref Position.Vectorized position, ref Rotation.Vectorized rotation, ref Scale.Vectorized scale, ref VertexBuffer.Vectorized vb, ref ElementBuffer.Vectorized eb, ref ShaderProgram.Vectorized shader)
		{
		}
		*/
	}
}
