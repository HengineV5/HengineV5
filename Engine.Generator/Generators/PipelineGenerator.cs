using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TemplateGenerator;

namespace Engine.Generator
{
	struct PipelineGeneratorData : IEquatable<PipelineGeneratorData>, ITemplateData
	{
		public string ecsName;
		public string engineName;
		public string ns;
		public Location location;

		public EquatableArray<Pipeline> pipelines;
		public EquatableArray<FileUsing> usings;

		public PipelineGeneratorData(string ecsName, string engineName, string ns, Location location, EquatableArray<Pipeline> pipelines, EquatableArray<FileUsing> usings)
		{
			this.ecsName = ecsName;
			this.engineName = engineName;
			this.ns = ns;
			this.location = location;
			this.pipelines = pipelines;
			this.usings = usings;
		}

		public bool Equals(PipelineGeneratorData other)
		{
			return pipelines.Equals(other.pipelines);
		}

		public string GetIdentifier()
			=> $"Component Generator ({ns}.{ecsName}) ({location})";
	}

	class PipelineGenerator : ITemplateSourceGenerator<IdentifierNameSyntax, PipelineGeneratorData>
	{
		public string Template => ResourceReader.GetResource("Pipeline.tcs");

		public bool TryCreateModel(PipelineGeneratorData data, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			/*
			var builderRoot = EngineGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");
			*/

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(data.ns));
			model.Set("engineName".AsSpan(), Parameter.Create(data.engineName));
			model.Set("ecsName".AsSpan(), Parameter.Create(data.ecsName));

			//var usings = EngineGenerator.GetUsings(node);
			model.Set("usings".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.usings.Select(x => x.GetModel())));

			//var pipelineSuccess = TryGetPipelines(compilation, layoutStep, out List<Pipeline> pipelines);
			model.Set("pipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.pipelines.Select(x => x.GetModel())));

			return true;
		}

		public bool TryGetData(IdentifierNameSyntax node, SemanticModel semanticModel, out PipelineGeneratorData data, out List<Diagnostic> diagnostics)
		{
			diagnostics = new();
			Unsafe.SkipInit(out data);

			if (node?.Parent is ClassDeclarationSyntax)
				return false;

			if (node?.Parent is MethodDeclarationSyntax)
				return false;

			if (node.Identifier.Text != "HengineBuilder")
				return false;

			var builderRoot = EngineGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");

			if (!TryGetPipelines(semanticModel, layoutStep, out List<Pipeline> pipelines))
				return false;

			var usings = EngineGenerator.GetUsings(node);

			data = new PipelineGeneratorData(EngineGenerator.GetEcsName(node), EngineGenerator.GetEngineName(node), builderRoot.GetNamespace(), builderRoot.GetLocation(), new(pipelines.ToArray()), new(usings.ToArray()));
			return true;
		}

		public string GetName(PipelineGeneratorData data)
			=> $"{data.engineName}_Pipeline";

		public Location GetLocation(PipelineGeneratorData data)
			=> data.location;

		public static bool TryGetPipelines(SemanticModel semanticModel, MemberAccessExpressionSyntax step, out List<Pipeline> pipelines)
		{
			pipelines = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not IdentifierNameSyntax name)
					continue;

				if (name.Identifier.Text != "Pipeline")
					continue;

				var nameArgument = invocation.ArgumentList.Arguments.First();
				var layoutLambda = invocation.ArgumentList.Arguments.Skip(1).First();

				if (nameArgument.Expression is not LiteralExpressionSyntax nameExpression)
					continue;

				if (!TryGetPipelineSystems(semanticModel, layoutLambda.Expression as SimpleLambdaExpressionSyntax, out List<PipelineSystem> systems))
					continue;

				pipelines.Add(new Pipeline()
				{
					name = nameExpression.Token.Value.ToString(),
					systems = new(systems.ToArray()),
					contextArguments = new(systems.SelectMany(x => x.contextArguments).Where(x => x.type != "EngineContext").GroupBy(x => x.type).Select(x => x.First()).ToArray())
				});
			}

			return pipelines.Count > 0;
		}

		static bool TryGetPipelineSystems(SemanticModel semanticModel, SimpleLambdaExpressionSyntax lambda, out List<PipelineSystem> pipelineSystems)
		{
			pipelineSystems = new();

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "Sequential" && genericName.Identifier.Text != "Paralell")
					continue;

				var systemName = genericName.TypeArgumentList.Arguments.First() as IdentifierNameSyntax;

				var foundSymbol = semanticModel.Compilation.GetSymbolsWithName(systemName.Identifier.ToFullString(), SymbolFilter.Type).Single();
				if (foundSymbol is not INamedTypeSymbol typeSymbol)
					throw new Exception();

				var methods = typeSymbol.GetMembers().Where(x => x is IMethodSymbol).Cast<IMethodSymbol>();

				bool hasInit = methods.Any(x => x.Name == "Init");
				bool hasDispose = methods.Any(x => x.Name == "Dispose");
				bool hasPreRun = methods.Any(x => x.Name == "PreRun");
				bool hasPostRun = methods.Any(x => x.Name == "PostRun");

				if (!TryGetSystemContexts(typeSymbol, new List<Diagnostic>(), out List<SystemArgument> contextArguments))
					continue;

				pipelineSystems.Add(new PipelineSystem()
				{
					name = systemName.Identifier.Text,
					arguments = new(GetSystemConstructorArguments(typeSymbol).ToArray()),
					contextArguments = new(contextArguments.ToArray()),
					hasInit = hasInit,
					hasDispose = hasDispose,
					hasPreRun = hasPreRun,
					hasPostRun = hasPostRun
				});
			}

			return pipelineSystems.Count > 0;
		}

		static List<SystemArgument> GetSystemConstructorArguments(INamedTypeSymbol system)
		{
			List<SystemArgument> args = new();

			if (system.Constructors.Length == 0)
				return args;

			var constructor = system.Constructors[0];

			foreach (var arg in constructor.Parameters)
			{
				args.Add(new SystemArgument()
				{
					type = arg.Type.Name
				});
			}

			return args;
		}

		static bool TryGetSystemContexts(INamedTypeSymbol node, List<Diagnostic> diagnostics, out List<SystemArgument> contexts)
		{
			contexts = new List<SystemArgument>();

			foreach (var attribute in node.GetAttributes())
			{
				var attribName = attribute.AttributeClass.Name;
				if (attribName != "SystemContext" && attribName != "SystemContextAttribute") // TODO: Might not need reduncancy check for shorform
					continue;

				if (attribute.AttributeClass is null)
					continue;

				if (attribute.AttributeClass.TypeArguments.Length == 0)
					continue;

				foreach (var type in attribute.AttributeClass.TypeArguments)
				{
					contexts.Add(new SystemArgument()
					{
						type = type.Name
					});
				}
			}

			return true;
		}
	}

	struct Pipeline : IEquatable<Pipeline>
	{
		public string name;
		public EquatableArray<PipelineSystem> systems;
		public EquatableArray<SystemArgument> contextArguments;

        public Pipeline()
        {
			name = "";
			systems = EquatableArray<PipelineSystem>.Empty;
			contextArguments = EquatableArray<SystemArgument>.Empty;
		}

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("pipelineName".AsSpan(), Parameter.Create($"{name}Pipeline"));
			model.Set("pipelineSystems".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(systems.Select(x => x.GetModel())));
			model.Set("pipelineContextArguments".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(contextArguments.Select(x => x.GetModel())));

			var uniqueArgs = systems.SelectMany(x => x.arguments).GroupBy(x => x.type).Select(x => x.First());
			model.Set("uniqueArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueArgs.Select(x => x.GetModel())));

			return model;
		}

		public bool Equals(Pipeline other)
		{
			return name.Equals(other.name) &&
				systems.Equals(other.systems) &&
				contextArguments.Equals(other.contextArguments);
		}
	}

	struct PipelineSystem : IEquatable<PipelineSystem>
	{
		public string name;
		public EquatableArray<SystemArgument> arguments;
		public EquatableArray<SystemArgument> contextArguments;

		public bool hasInit;
		public bool hasDispose;
		public bool hasPreRun;
		public bool hasPostRun;

        public PipelineSystem()
        {
			name = "";
			arguments = EquatableArray<SystemArgument>.Empty;
			contextArguments = EquatableArray<SystemArgument>.Empty;
		}

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("systemName".AsSpan(), Parameter.Create(name));
			model.Set("systemArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(arguments.Select(x => x.GetModel())));
			model.Set("systemContextArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(contextArguments.Select(x => x.GetModel())));
			model.Set("hasInit".AsSpan(), Parameter.Create(hasInit));
			model.Set("hasDispose".AsSpan(), Parameter.Create(hasDispose));
			model.Set("hasPreRun".AsSpan(), Parameter.Create(hasPreRun));
			model.Set("hasPostRun".AsSpan(), Parameter.Create(hasPostRun));

			return model;
		}

		public bool Equals(PipelineSystem other)
		{
			return name.Equals(other.name) &&
				arguments.Equals(other.arguments) &&
				contextArguments.Equals(other.contextArguments) &&
				hasInit.Equals(other.hasInit) &&
				hasDispose.Equals(other.hasDispose) &&
				hasPreRun.Equals(other.hasPreRun) &&
				hasPostRun.Equals(other.hasPostRun);
		}
	}

	struct SystemArgument : IEquatable<SystemArgument>
	{
		public string type;

        public SystemArgument()
        {
			type = "";
        }

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("argName".AsSpan(), Parameter.Create($"arg{type}"));
			model.Set("argType".AsSpan(), Parameter.Create(type));

			return model;
		}

		public bool Equals(SystemArgument other)
		{
			return type.Equals(other.type);
		}
	}
}
