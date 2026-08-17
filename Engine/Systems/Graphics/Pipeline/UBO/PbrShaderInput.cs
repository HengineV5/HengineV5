using Engine.Graphics;
using UtilLib.Memory;

using RenderLib;

namespace Engine
{
	public struct PbrShaderInput<TBackend> : IUniformBufferObject<PbrShaderInput<TBackend>, TBackend>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public MappedMemory<MeshUniformBufferObject> ubo;
		public MappedMemory<PbrMaterialInfo> material;
		public FixedBuffer4<MappedMemory<Light>> lights;

		public static DescriptorSetLayoutDesc GetLayoutDesc()
		{
			return new DescriptorSetLayoutBuilder()
				.Uniform(ShaderStage.Vertex, 1)         // UBO
				.Samplers(ShaderStage.Fragment, 1, 9)   // PBR textures
				.Uniform(ShaderStage.Fragment, 1)       // Lights
				.Uniform(ShaderStage.Fragment, 4)       // Cubemap
				.Build();
		}

		public unsafe static PbrShaderInput<TBackend> Map(ref TBackend backend, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TBackend>(ref backend, descriptorSet)
						.Variable<MeshUniformBufferObject>(0)
						.Variable<PbrMaterialInfo>(10)
						.Array<Light>(11, 4);

			var uniform = TBackend.CreateMappedUniformBuffer(ref backend, uniformBufferBuilder.GetSize());

			PbrShaderInput<TBackend> shaderInput = new PbrShaderInput<TBackend>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<MeshUniformBufferObject>(uniform.ptr, 0);
			shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterialInfo>(uniform.ptr, 1);
			for (int b = 0; b < 4; b++)
			{
				shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(uniform.ptr, 2 + (uint)b);
			}

			uniformBufferBuilder.UpdateDescriptorSet(ref backend, uniform.buffer);

			return shaderInput;
		}
	}

	public struct GuiShaderInput<TBackend> : IUniformBufferObject<GuiShaderInput<TBackend>, TBackend>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public MappedMemory<GuiUniformBufferObject> ubo;
		public MappedMemory<GuiStateBufferObject> guiState;

		public static DescriptorSetLayoutDesc GetLayoutDesc()
		{
			return new DescriptorSetLayoutBuilder()
				.Uniform(ShaderStage.Vertex, 1)         // UBO
				.Samplers(ShaderStage.Fragment, 1, 1)   // UI Atlas map
				.Uniform(ShaderStage.Fragment, 1)       // GUI State
				.Build();
		}

		public unsafe static GuiShaderInput<TBackend> Map(ref TBackend backend, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TBackend>(ref backend, descriptorSet)
						.Variable<GuiUniformBufferObject>(0)
						.Variable<GuiStateBufferObject>(2);

			var uniform = TBackend.CreateMappedUniformBuffer(ref backend, uniformBufferBuilder.GetSize());

			var shaderInput = new GuiShaderInput<TBackend>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<GuiUniformBufferObject>(uniform.ptr, 0);
			shaderInput.guiState = uniformBufferBuilder.GetElement<GuiStateBufferObject>(uniform.ptr, 1);

			uniformBufferBuilder.UpdateDescriptorSet(ref backend, uniform.buffer);

			return shaderInput;
		}
	}

	public struct GizmoShaderInput<TBackend> : IUniformBufferObject<GizmoShaderInput<TBackend>, TBackend>
		where TBackend : struct, IRenderBackend<TBackend>, allows ref struct
	{
		public MappedMemory<MeshUniformBufferObject> ubo;
		public MappedMemory<GizmoUniformBufferObject> gizmoUbo;

		public static DescriptorSetLayoutDesc GetLayoutDesc()
		{
			return new DescriptorSetLayoutBuilder()
				.Uniform(ShaderStage.Vertex, 1)  // UBO
				.Uniform(ShaderStage.Vertex, 1)  // Gizmo UBO
				.Build();
		}

		public unsafe static GizmoShaderInput<TBackend> Map(ref TBackend backend, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TBackend>(ref backend, descriptorSet)
						.Variable<MeshUniformBufferObject>(0)
						.Variable<GizmoUniformBufferObject>(1);

			var uniform = TBackend.CreateMappedUniformBuffer(ref backend, uniformBufferBuilder.GetSize());

			var shaderInput = new GizmoShaderInput<TBackend>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<MeshUniformBufferObject>(uniform.ptr, 0);
			shaderInput.gizmoUbo = uniformBufferBuilder.GetElement<GizmoUniformBufferObject>(uniform.ptr, 1);

			uniformBufferBuilder.UpdateDescriptorSet(ref backend, uniform.buffer);

			return shaderInput;
		}
	}
}
