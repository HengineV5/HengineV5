using System.Runtime.InteropServices;

namespace Hengine
{
	// Phong
	[StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct Material
    {
        public Vector3f Ambient;
        public Vector3f Diffuse;
        public Vector3f Specular;
        public float Shininess;

        public static readonly Material Emerald = new Material
        {
            Ambient = new Vector3f(0.0215f, 0.1745f, 0.0215f),
            Diffuse = new Vector3f(0.07568f, 0.61424f, 0.07568f),
            Specular = new Vector3f(0.633f, 0.727811f, 0.633f),
            Shininess = 0.6f
        };

        public static readonly Material WhitePlastic = new Material
        {
            Ambient = Vector3f.Zero,
            Diffuse = new Vector3f(0.55f, 0.55f, 0.55f),
            Specular = new Vector3f(0.70f, 0.70f, 0.70f),
            Shininess = 0.25f
        };
    }
}
