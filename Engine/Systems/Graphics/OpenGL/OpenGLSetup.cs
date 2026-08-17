using RenderLib.OpenGL;

namespace Engine
{
	public static class OpenGLSetup
	{
		public static GlRenderContext RenderSetup(GlContext glContext)
		{
			GlRenderContext glRenderContext = new GlRenderContext(glContext);
			glRenderContext.Setup();

			return glRenderContext;
		}
	}
}
