using System.Runtime.CompilerServices;

namespace Engine.Generator.Tests
{
	public static class ModuleInitializer
	{
		[ModuleInitializer]
		public static void Init()
		{
			VerifySourceGenerators.Initialize();
		}
	}
}