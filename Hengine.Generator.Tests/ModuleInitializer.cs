﻿using System.Runtime.CompilerServices;

namespace Hengine.Generator.Tests
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