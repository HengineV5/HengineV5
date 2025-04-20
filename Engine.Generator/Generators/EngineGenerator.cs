using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using TemplateGenerator;

namespace Engine.Generator
{
	struct EngineGeneratorData : IEquatable<EngineGeneratorData>
	{
		public string ecsName;
		public string engineName;
		public string ns;
		public Location location;

		public EcsEngine engine;
		public EquatableArray<FileUsing> usings;

		public EngineGeneratorData(string ecsName, string engineName, string ns, Location location, EcsEngine engine, EquatableArray<FileUsing> usings)
		{
			this.ecsName = ecsName;
			this.engineName = engineName;
			this.ns = ns;
			this.location = location;
			this.engine = engine;
			this.usings = usings;
		}

		public bool Equals(EngineGeneratorData other)
		{
			return engine.Equals(other.engine);
		}
	}

	class EngineGenerator : ITemplateSourceGenerator<IdentifierNameSyntax, EngineGeneratorData>
	{
		public string Template => ResourceReader.GetResource("Engine.tcs");

		public bool TryCreateModel(EngineGeneratorData data, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(data.ns));
			model.Set("engineName".AsSpan(), Parameter.Create(data.engineName));
			model.Set("ecsName".AsSpan(), Parameter.Create(data.ecsName));

			model.Set("usings".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.usings.Select(x => x.GetModel())));

			model.Set("resourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.engine.resourceManagers.Select(x => x.GetModel())));
			model.Set("pipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.engine.pipelines.Select(x => x.GetModel())));
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.engine.worlds.Select(x => x.GetModel())));
			model.Set("config".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.engine.configSteps.Select(x => x.GetModel())));

			var uniqueSetupArgs = data.engine.setupSteps.SelectMany(x => x.nonConfigArgumentTypes).GroupBy(x => x.type).Select(x => x.First());
			var uniqueContextArgs = data.engine.pipelines.SelectMany(x => x.contextArguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueResourceManagerArgs = data.engine.resourceManagers.SelectMany(x => x.arguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueSystemArgs = data.engine.pipelines.SelectMany(x => x.systems).SelectMany(x => x.arguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueArgs = uniqueSystemArgs // TODO: Bit on edge but IDGAF
				.Concat(uniqueSetupArgs.Select(x => new SystemArgument() { type = x.type }))
				.Concat(uniqueResourceManagerArgs.Select(x => new SystemArgument() { type = x.type }))
				.GroupBy(x => x.type).Select(x => x.First());

			model.Set("uniqueArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueArgs.Select(x => x.GetModel())));
			model.Set("uniqueContextArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueContextArgs.Select(x => x.GetModel())));
			model.Set("setup".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(data.engine.setupSteps.Select(x => x.GetModel(uniqueSystemArgs, uniqueSetupArgs, uniqueResourceManagerArgs))));

			return true;
		}

		public EngineGeneratorData? Filter(IdentifierNameSyntax node, SemanticModel semanticModel)
		{
			if (node?.Parent is ClassDeclarationSyntax)
				return null;

			if (node?.Parent is MethodDeclarationSyntax)
				return null;

			if (node.Identifier.Text != "HengineBuilder")
				return null;

			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");
			var configStep = builderSteps.Single(x => x.Name.Identifier.Text == "Config");
			var resourceStep = builderSteps.Single(x => x.Name.Identifier.Text == "Resource");

			if (!TryGetEngine(semanticModel, layoutStep, configStep, resourceStep, out EcsEngine engine))
				return null;

			var usings = GetUsings(node);

			return new EngineGeneratorData(GetEcsName(node), GetEngineName(node), builderRoot.GetNamespace(), node.GetLocation(), engine, new(usings.ToArray()));
		}

		public string GetName(EngineGeneratorData data)
		 => data.engineName;

		public Location GetLocation(EngineGeneratorData data)
			=> data.location;

		public static SyntaxNode GetBuilderRoot(SyntaxNode node)
		{
			if (node is StatementSyntax)
				return node;

			return GetBuilderRoot(node.Parent);
		}

		public static string GetEngineName(IdentifierNameSyntax node)
		{
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var buildStep = builderSteps.Single(x => x.Name.Identifier.Text == "Build");
			var genricName = buildStep.Name as GenericNameSyntax;

			return genricName.TypeArgumentList.Arguments[0].ToString();
		}

		public static string GetEcsName(IdentifierNameSyntax node)
		{
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var buildStep = builderSteps.Single(x => x.Name.Identifier.Text == "Build");
			var genricName = buildStep.Name as GenericNameSyntax;

			return genricName.TypeArgumentList.Arguments[1].ToString();
		}

		public static List<FileUsing> GetUsings(SyntaxNode node)
		{
			List<FileUsing> models = new();

			var compilationUnit = node.Ancestors().Where(x => x is CompilationUnitSyntax).Cast<CompilationUnitSyntax>().Single();
			foreach (var u in compilationUnit.Usings)
			{
				models.Add(new FileUsing()
				{
					name = u.Name.ToString(),
				});
			}

			return models;
		}

		static bool TryGetEngine(SemanticModel semanticModel, MemberAccessExpressionSyntax layoutStep, MemberAccessExpressionSyntax configStep, MemberAccessExpressionSyntax resourceStep, out EcsEngine engine)
		{
			bool configSuccess = TryGetConfigSteps(configStep, out List<ConfigStep> configSteps);
			bool setupSuccess = TryGetSetupSteps(semanticModel, configStep, configSteps, out List<SetupStep> setupSteps);
			bool resourceSuccess = TryGetResourceManagers(semanticModel, resourceStep, out List<ResourceManager> resourceManagers);

			bool pipelineSuccess = PipelineGenerator.TryGetPipelines(semanticModel, layoutStep, out List<Pipeline> pipelines);
			bool worldSuccess = TryGetWorlds(layoutStep, pipelines, out  List<World> worlds);

			engine = new EcsEngine()
			{
				configSteps = new(configSteps.ToArray()),
				setupSteps = new(setupSteps.ToArray()),
				resourceManagers = new(resourceManagers.ToArray()),
				pipelines = new(pipelines.ToArray()),
				worlds = new(worlds.ToArray())
			};


			return resourceSuccess && configSuccess && setupSuccess && worldSuccess && pipelineSuccess;
		}

		static bool TryGetWorlds(MemberAccessExpressionSyntax step, List<Pipeline> pipelines, out List<World> worlds)
		{
			worlds = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "World")
					continue;

				var worldName = genericName.TypeArgumentList.Arguments.First() as QualifiedNameSyntax;
				var layoutLambda = invocation.ArgumentList.Arguments.First();

				if (!TryGetWorldPipelines(layoutLambda.Expression as SimpleLambdaExpressionSyntax, pipelines, out List<Pipeline> worldPipelines))
					continue;

				worlds.Add(new World()
				{
					name = worldName.ToString(),
					pipelines = new(worldPipelines.ToArray())
				});
			}

			return true;
		}

		static bool TryGetWorldPipelines(SimpleLambdaExpressionSyntax lambda, List<Pipeline> pipelines, out List<Pipeline> worldPipelines)
		{
			worldPipelines = new();
			
			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
					continue;

				if (memberAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "Pipeline")
					continue;

				var qualifiedName = genericName.TypeArgumentList.Arguments.First() as QualifiedNameSyntax;
				var systemName = qualifiedName.Right.GetName();

				worldPipelines.AddRange(pipelines.Where(x => $"{x.name}Pipeline" == systemName));
			}

			return worldPipelines.Count > 0;
		}

		static bool TryGetSetupSteps(SemanticModel semanticModel, MemberAccessExpressionSyntax step, List<ConfigStep> configSteps, out List<SetupStep> setupSteps)
		{
			setupSteps = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax invocationAccess)
					continue;

				var methodName = GetMethodName(invocationAccess);
				if (methodName != "Setup")
					continue;

				var configMethod = invocation.ArgumentList.Arguments.Single();

				if (configMethod.Expression is not MemberAccessExpressionSyntax methodAccess)
					continue;

				var name = configMethod.ToString().Split('.');
				bool FindType(INamedTypeSymbol symbol)
				{
                    return symbol.Name == name[0];
				}

				bool FindMember(ISymbol symbol)
				{
					return symbol.Name == name[1];
				}

				var foundTypeSymbol = GeneratorExtensions.GetTypeSymbols(semanticModel.Compilation, FindType).Single();
				var foundSymbol = GeneratorExtensions.GetMemebers(foundTypeSymbol, FindMember).Single();
				if (foundSymbol is not IMethodSymbol methodSymbol)
					throw new Exception($"Type is not {typeof(IMethodSymbol).Name}, type is {foundSymbol.GetType().Name}");

				setupSteps.Add(new SetupStep()
				{
					method = configMethod.ToFullString(),
					returnTypes = new(GetMethodReturnTypes(methodSymbol).ToArray()),
					argumentTypes = new(GetMethodArguments(methodSymbol, configSteps).ToArray()),
					nonConfigArgumentTypes = new(GetMethodNonConfigArguments(methodSymbol, configSteps).ToArray()),
				});
			}

			return true;
		}

		static bool TryGetConfigSteps(MemberAccessExpressionSyntax step, out List<ConfigStep> configSteps)
		{
			configSteps = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax invocationAccess)
					continue;

				var methodName = GetMethodName(invocationAccess);
				if (methodName != "WithConfig")
					continue;

				if (invocationAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.TypeArgumentList.Arguments.Count != 1)
					continue;

				var nameArg = genericName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax;
				var nameToken = nameArg.Identifier.Text;

				configSteps.Add(new ConfigStep()
				{
					name = nameToken
				});
			}

			return true;
		}

		static bool TryGetResourceManagers(SemanticModel semanticModel, MemberAccessExpressionSyntax step, out List<ResourceManager> resourceManagers)
		{
			resourceManagers = new();

			var parentExpression = step.Parent as InvocationExpressionSyntax;
			var lambda = parentExpression.ArgumentList.Arguments.Single().Expression as SimpleLambdaExpressionSyntax;

			foreach (var pipeline in lambda.Block.Statements.Where(x => x is ExpressionStatementSyntax).Cast<ExpressionStatementSyntax>())
			{
				if (pipeline.Expression is not InvocationExpressionSyntax invocation)
					continue;

				if (invocation.Expression is not MemberAccessExpressionSyntax invocationAccess)
					continue;

				if (invocationAccess.Name is not GenericNameSyntax genericName)
					continue;

				if (genericName.Identifier.Text != "ResourceManager")
					continue;

				if (genericName.TypeArgumentList.Arguments.Count != 1)
					continue;

				var nameArg = genericName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax;

				var name = nameArg.Identifier.ToFullString();
				bool FindType(INamedTypeSymbol symbol)
				{
					return symbol.Name == name;
				}

				var foundSymbol = GeneratorExtensions.GetTypeSymbols(semanticModel.Compilation, FindType).FirstOrDefault();
				if (foundSymbol is not INamedTypeSymbol typeSymbol)
					continue;

				if (typeSymbol.Constructors.Length == 0)
					continue;

				List<MethodArgumentType> arguments = new List<MethodArgumentType>();
				var constructor = typeSymbol.Constructors.First();

				foreach (var argument in constructor.Parameters)
				{
					arguments.Add(new MethodArgumentType()
					{
						type = argument.Type.Name
					});
				}

				resourceManagers.Add(new ResourceManager()
				{
					name = foundSymbol.Name,
					arguments = new(arguments.ToArray()),
					ns = typeSymbol.ContainingNamespace.ToString()
				});
			}

			return true;
		}

		static string GetMethodName(MemberAccessExpressionSyntax methodAccess)
		{
			if (methodAccess.Name is IdentifierNameSyntax identifierName)
			{
				return identifierName.Identifier.Text;
			}
			else if (methodAccess.Name is GenericNameSyntax genericName)
			{
				return genericName.Identifier.Text;
			}

			throw new Exception("Unknown name type");
		}

		static List<MethodReturnType> GetMethodReturnTypes(IMethodSymbol method)
		{
			List<MethodReturnType> models = new();

			if (method.ReturnsVoid)
				return models;

			if (!method.ReturnType.IsTupleType)
			{
				models.Add(new()
				{
					type = method.ReturnType.Name
				});
			}
			else
			{
				if (method.ReturnType is not INamedTypeSymbol typeSymbol)
					throw new Exception();

				foreach (var element in typeSymbol.TupleElements)
				{
					models.Add(new MethodReturnType()
					{
						type = element.Type.Name
					});
				}
			}

			return models;
		}

		static List<MethodArgumentType> GetMethodArguments(IMethodSymbol method, List<ConfigStep> configSteps)
		{
			List<MethodArgumentType> models = new();

			foreach (var argument in method.Parameters)
			{
				models.Add(new MethodArgumentType()
				{
					type = argument.Type.Name
				});
			}

			return models;
		}

		static List<MethodArgumentType> GetMethodNonConfigArguments(IMethodSymbol method, List<ConfigStep> configSteps)
		{
			List<MethodArgumentType> models = new();

			foreach (var argument in method.Parameters)
			{
				if (configSteps.Any(x => x.name == argument.Type.Name))
					continue;

				models.Add(new MethodArgumentType()
				{
					type = argument.Type.Name
				});
			}

			return models;
		}
	}

	struct EcsEngine : IEquatable<EcsEngine>
	{
		public EquatableArray<ConfigStep> configSteps;
		public EquatableArray<SetupStep> setupSteps;
		public EquatableArray<ResourceManager> resourceManagers;
		public EquatableArray<Pipeline> pipelines;
		public EquatableArray<World> worlds;

        public EcsEngine()
        {
			configSteps = EquatableArray<ConfigStep>.Empty;
			setupSteps = EquatableArray<SetupStep>.Empty;
			resourceManagers = EquatableArray<ResourceManager>.Empty;
			pipelines = EquatableArray<Pipeline>.Empty;
			worlds = EquatableArray<World>.Empty;
		}

		public bool Equals(EcsEngine other)
		{
			return configSteps.Equals(other.configSteps) &&
				setupSteps.Equals(other.setupSteps) &&
				resourceManagers.Equals(other.resourceManagers) &&
				pipelines.Equals(other.pipelines) &&
				worlds.Equals(other.worlds);
		}
	}

	struct World : IEquatable<World>
	{
		public string name;
		public EquatableArray<Pipeline> pipelines;

        public World()
        {
			name = "";
			pipelines = EquatableArray<Pipeline>.Empty;

		}

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("worldName".AsSpan(), Parameter.Create(name));
			model.Set("worldSafeName".AsSpan(), Parameter.Create(name.Replace('.', '_')));
			model.Set("worldPipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(pipelines.Select(x => x.GetModel())));

			return model;
		}

		public bool Equals(World other)
		{
			return name.Equals(other.name) &&
				pipelines.Equals(other.pipelines);
		}
	}

	struct FileUsing : IEquatable<FileUsing>
	{
		public string name;

        public FileUsing()
        {
			name = "";
        }

        public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("usingName".AsSpan(), Parameter.Create(name));

			return model;
		}

		public bool Equals(FileUsing other)
		{
			return name.Equals(other.name);
		}
	}

	struct SetupStep : IEquatable<SetupStep>
	{
		public string method;
		public EquatableArray<MethodReturnType> returnTypes;
		public EquatableArray<MethodArgumentType> argumentTypes;
		public EquatableArray<MethodArgumentType> nonConfigArgumentTypes;

        public SetupStep()
        {
			method = "";
			returnTypes = EquatableArray<MethodReturnType>.Empty;
			argumentTypes = EquatableArray<MethodArgumentType>.Empty;
			nonConfigArgumentTypes = EquatableArray<MethodArgumentType>.Empty;
		}

		public Model<ReturnType> GetModel(IEnumerable<SystemArgument> usedArguments, IEnumerable<MethodArgumentType> usedConfigArguments, IEnumerable<MethodArgumentType> usedResourceArguments)
		{
			var model = new Model<ReturnType>();
			model.Set("stepMethod".AsSpan(), Parameter.Create(method));
			model.Set("stepTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(returnTypes.Select(x => x.GetModel())));
			model.Set("stepArguments".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(argumentTypes.Select(x => x.GetModel())));
			model.Set("stepUsedTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(returnTypes.Where(x => usedArguments.Any(y => y.type == x.type) || usedConfigArguments.Any(y => y.type == x.type) || usedResourceArguments.Any(y => y.type == x.type)).Select(x => x.GetModel())));
			model.Set("stepCount".AsSpan(), Parameter.Create<float>(returnTypes.Count));

			return model;
		}

		public bool Equals(SetupStep other)
		{
			return method.Equals(other.method) &&
				returnTypes.Equals(other.returnTypes) &&
				argumentTypes.Equals(other.argumentTypes) &&
				nonConfigArgumentTypes.Equals(other.nonConfigArgumentTypes);
		}
	}

	struct ConfigStep : IEquatable<ConfigStep>
	{
		public string name;

        public ConfigStep()
        {
			name = "";
        }

		public Model<ReturnType> GetModel()
		{
			var varName = char.ToLowerInvariant(name[0]) + name.Substring(1);

			var model = new Model<ReturnType>();
			model.Set("stepName".AsSpan(), Parameter.Create(name));
			model.Set("stepVarName".AsSpan(), Parameter.Create(varName));

			return model;
		}

		public bool Equals(ConfigStep other)
		{
			return name.Equals(other.name);
		}
	}

	struct MethodReturnType : IEquatable<MethodReturnType>
	{
		public string type;

        public MethodReturnType()
        {
			type = "";
        }

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("returnType".AsSpan(), Parameter.Create(type));

			return model;
		}

		public bool Equals(MethodReturnType other)
		{
			return type.Equals(other.type);
		}
	}

	struct MethodArgumentType : IEquatable<MethodArgumentType>
	{
		public string type;

        public MethodArgumentType()
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

		public bool Equals(MethodArgumentType other)
		{
			return type.Equals(other.type);
		}
	}

	struct ResourceManager : IEquatable<ResourceManager>
	{
		public string name;
		public string ns;
		public EquatableArray<MethodArgumentType> arguments;

        public ResourceManager()
        {
			name = "";
			ns = "";
			arguments = EquatableArray<MethodArgumentType>.Empty;

		}

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("resourceManagerName".AsSpan(), Parameter.Create(name));
			model.Set("resourceManagerNamespace".AsSpan(), Parameter.Create(ns));
			model.Set("resourceManagerArguments".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(arguments.Select(x => x.GetModel())));

			return model;
		}

		public bool Equals(ResourceManager other)
		{
			return other.name.Equals(name) &&
				other.ns.Equals(ns) &&
				other.arguments.Equals(arguments);
		}
	}
}
