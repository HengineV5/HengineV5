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

			new TemplateGeneratorBuilder()
				.WithLogging(context)
				.WithGenerator(context, engineGenerator)
				.WithGenerator(context, pipelineGenerator)
				.WithInfoFile(context);
		}
	}
}
