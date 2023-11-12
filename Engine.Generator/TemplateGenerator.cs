using Microsoft.CodeAnalysis;
using System;
using TemplateGenerator;

namespace Engine.Generator
{
	[Generator]
	public class TemplateGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var engineGenerator = new EngineGenerator();
			var pipelineGenerator = new PipelineGenerator();

			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, engineGenerator);
			TemplateGeneratorHelpers.RegisterTemplateGenerator(context, pipelineGenerator);
		}
	}
}
