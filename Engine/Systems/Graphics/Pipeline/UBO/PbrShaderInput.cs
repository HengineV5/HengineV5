using Engine.Graphics;
using UtilLib.Memory;

using RenderLib;

namespace Engine
{
	public struct PbrShaderInput<TStorage> : IUniformBufferObject<PbrShaderInput<TStorage>, TStorage>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
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

		public unsafe static PbrShaderInput<TStorage> Map(ref TStorage storage, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TStorage>(ref storage, descriptorSet)
						.Variable<MeshUniformBufferObject>(0)
						.Variable<PbrMaterialInfo>(10)
						.Array<Light>(11, 4);

			var uniform = TStorage.CreateMappedUniformBuffer(ref storage, uniformBufferBuilder.GetSize());

			PbrShaderInput<TStorage> shaderInput = new PbrShaderInput<TStorage>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<MeshUniformBufferObject>(uniform.ptr, 0);
			shaderInput.material = uniformBufferBuilder.GetElement<PbrMaterialInfo>(uniform.ptr, 1);
			for (int b = 0; b < 4; b++)
			{
				shaderInput.lights[b] = uniformBufferBuilder.GetElement<Light>(uniform.ptr, 2 + (uint)b);
			}

			uniformBufferBuilder.UpdateDescriptorSet(ref storage, uniform.buffer);

			return shaderInput;
		}
	}

	public struct GuiShaderInput<TStorage> : IUniformBufferObject<GuiShaderInput<TStorage>, TStorage>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
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

		public unsafe static GuiShaderInput<TStorage> Map(ref TStorage storage, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TStorage>(ref storage, descriptorSet)
						.Variable<GuiUniformBufferObject>(0)
						.Variable<GuiStateBufferObject>(2);

			var uniform = TStorage.CreateMappedUniformBuffer(ref storage, uniformBufferBuilder.GetSize());

			var shaderInput = new GuiShaderInput<TStorage>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<GuiUniformBufferObject>(uniform.ptr, 0);
			shaderInput.guiState = uniformBufferBuilder.GetElement<GuiStateBufferObject>(uniform.ptr, 1);

			uniformBufferBuilder.UpdateDescriptorSet(ref storage, uniform.buffer);

			return shaderInput;
		}
	}

	public struct GizmoShaderInput<TStorage> : IUniformBufferObject<GizmoShaderInput<TStorage>, TStorage>
		where TStorage : struct, IRenderBackend<TStorage>, allows ref struct
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

		public unsafe static GizmoShaderInput<TStorage> Map(ref TStorage storage, GpuDescriptorSet descriptorSet)
		{
			var uniformBufferBuilder = new UniformBufferBuilder<TStorage>(ref storage, descriptorSet)
						.Variable<MeshUniformBufferObject>(0)
						.Variable<GizmoUniformBufferObject>(1);

			var uniform = TStorage.CreateMappedUniformBuffer(ref storage, uniformBufferBuilder.GetSize());

			var shaderInput = new GizmoShaderInput<TStorage>();

			shaderInput.ubo = uniformBufferBuilder.GetElement<MeshUniformBufferObject>(uniform.ptr, 0);
			shaderInput.gizmoUbo = uniformBufferBuilder.GetElement<GizmoUniformBufferObject>(uniform.ptr, 1);

			uniformBufferBuilder.UpdateDescriptorSet(ref storage, uniform.buffer);

			return shaderInput;
		}
	}
}
