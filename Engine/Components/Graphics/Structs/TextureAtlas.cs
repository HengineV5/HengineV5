namespace Engine.Graphics
{
	public struct TextureAtlas
    {
        public string name;
        public int textures;

        public ETexture textureAtlas;

        public static TextureAtlas LoadAtlas(string name, int textures, string path)
        {
            return new TextureAtlas()
            {
                name = name,
                textures = textures,
                textureAtlas = ETexture.LoadImage($"{name}_atlas", path)
            };
        }
    }
}
