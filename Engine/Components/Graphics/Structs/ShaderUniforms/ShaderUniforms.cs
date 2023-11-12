namespace Engine.Graphics
{
	public struct ShaderUniforms
	{
		public ModelUniforms Model { get; set; }

		public CameraUniforms Camera { get; set; }

		public LightUniforms Light { get; set; }

		public MaterialUniforms Material { get; set; }
	}
}
