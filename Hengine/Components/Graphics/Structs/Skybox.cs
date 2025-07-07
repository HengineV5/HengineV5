namespace Hengine.Graphics
{
	public struct Skybox
    {
        public string name;

        public ETextureHdr skybox;
        public ETextureHdr irradiance;
        public ETextureHdr specular;

        public static Skybox LoadSkybox(string name, string skyboxFolder)
        {
            return new Skybox()
            {
                name = name,
                skybox = ETextureHdr.LoadImage($"{name}", $"{skyboxFolder}/cubemap.png"),
                irradiance = ETextureHdr.LoadImage($"{name}_irradiance", $"{skyboxFolder}/irradiance.png"),
                specular = ETextureHdr.LoadImage($"{name}_specular", $"{skyboxFolder}/specular.png")
            };
        }
    }
}
