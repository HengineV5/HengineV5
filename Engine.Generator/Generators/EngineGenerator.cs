using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TemplateGenerator;

namespace Engine.Generator
{
	class EngineGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public string Template => ResourceReader.GetResource("Engine.tcs");

		public bool TryCreateModel(Compilation compilation, IdentifierNameSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");
			var configStep = builderSteps.Single(x => x.Name.Identifier.Text == "Config");
			var resourceStep = builderSteps.Single(x => x.Name.Identifier.Text == "Resource");

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("engineName".AsSpan(), Parameter.Create(GetEngineName(node)));
			model.Set("ecsName".AsSpan(), Parameter.Create(GetEcsName(node)));

			var usings = GetUsings(node);
			model.Set("usings".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(usings.Select(x => x.GetModel())));

			var engineSuccess = TryGetEngine(compilation, layoutStep, configStep, resourceStep, out EcsEngine engine);

			model.Set("resourceManagers".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(engine.resourceManagers.Select(x => x.GetModel())));
			model.Set("pipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(engine.pipelines.Select(x => x.GetModel())));
			model.Set("worlds".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(engine.worlds.Select(x => x.GetModel())));
			model.Set("config".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(engine.configSteps.Select(x => x.GetModel())));

			var uniqueSetupArgs = engine.setupSteps.SelectMany(x => x.nonConfigArgumentTypes).GroupBy(x => x.type).Select(x => x.First());
			var uniqueContextArgs = engine.pipelines.SelectMany(x => x.contextArguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueSystemArgs = engine.pipelines.SelectMany(x => x.systems).SelectMany(x => x.arguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueArgs = uniqueSystemArgs.Concat(uniqueSetupArgs.Select(x => new SystemArgument() { type = x.type })).GroupBy(x => x.type).Select(x => x.First());

			model.Set("uniqueArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueArgs.Select(x => x.GetModel())));
			model.Set("uniqueContextArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueContextArgs.Select(x => x.GetModel())));
			model.Set("setup".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(engine.setupSteps.Select(x => x.GetModel(uniqueSystemArgs, uniqueSetupArgs))));

			return engineSuccess;
		}

		public bool Filter(IdentifierNameSyntax node)
		{
			if (node?.Parent is ClassDeclarationSyntax)
				return false;

			if (node?.Parent is MethodDeclarationSyntax)
				return false;

			return node.Identifier.Text == "HengineBuilder";
		}

		public string GetName(IdentifierNameSyntax node)
		{
			return GetEngineName(node);
		}

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

		static bool TryGetEngine(Compilation compilation, MemberAccessExpressionSyntax layoutStep, MemberAccessExpressionSyntax configStep, MemberAccessExpressionSyntax resourceStep, out EcsEngine engine)
		{
			bool configSuccess = TryGetConfigSteps(configStep, out List<ConfigStep> configSteps);
			bool setupSuccess = TryGetSetupSteps(compilation, configStep, configSteps, out List<SetupStep> setupSteps);
			bool resourceSuccess = TryGetResourceManagers(compilation, resourceStep, out List<ResourceManager> resourceManagers);

			bool pipelineSuccess = PipelineGenerator.TryGetPipelines(compilation, layoutStep, out List<Pipeline> pipelines);
			bool worldSuccess = TryGetWorlds(layoutStep, pipelines, out  List<World> worlds);

			engine = new EcsEngine()
			{
				configSteps = configSteps,
				setupSteps = setupSteps,
				resourceManagers = resourceManagers,
				pipelines = pipelines,
				worlds = worlds
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
					pipelines = worldPipelines
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

		static bool TryGetSetupSteps(Compilation compilation, MemberAccessExpressionSyntax step, List<ConfigStep> configSteps, out List<SetupStep> setupSteps)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());

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

				var methodDeclaration = nodes.FindNode<MethodDeclarationSyntax>(x => x.Identifier.Text == methodAccess.Name.Identifier.Text);

				setupSteps.Add(new SetupStep()
				{
					method = configMethod.ToFullString(),
					returnTypes = GetMethodReturnTypes(methodDeclaration),
					argumentTypes = GetMethodArguments(methodDeclaration, configSteps),
					nonConfigArgumentTypes = GetMethodNonConfigArguments(methodDeclaration, configSteps),
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

		static bool TryGetResourceManagers(Compilation compilation, MemberAccessExpressionSyntax step, out List<ResourceManager> resourceManagers)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());
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
				var nameToken = nameArg.Identifier.Text;

				var resourceManagerNode = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == nameToken);

				List<MethodArgumentType> arguments = new List<MethodArgumentType>();
				if (resourceManagerNode.Members.TryFindNode(out ConstructorDeclarationSyntax constructor))
				{
					foreach (var argument in constructor.ParameterList.Parameters)
					{
						var argName = argument.Type as IdentifierNameSyntax;

						arguments.Add(new MethodArgumentType()
						{
							type = argName.Identifier.Text
						});
					}
				}

				resourceManagers.Add(new ResourceManager()
				{
					name = nameToken,
					arguments = arguments,
					ns = resourceManagerNode.GetNamespace()
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

		static List<MethodReturnType> GetMethodReturnTypes(MethodDeclarationSyntax method)
		{
			List<MethodReturnType> models = new();

			if (method.ReturnType is IdentifierNameSyntax returnName)
			{
				models.Add(new MethodReturnType()
				{
					type = returnName.Identifier.Text
				});
			}
			else if (method.ReturnType is TupleTypeSyntax tuple)
			{
				foreach (var element in tuple.Elements)
				{
					var elemntName = element.Type as IdentifierNameSyntax;

					models.Add(new MethodReturnType()
					{
						type = elemntName.Identifier.Text
					});
				}
			}

			return models;
		}

		static List<MethodArgumentType> GetMethodArguments(MethodDeclarationSyntax method, List<ConfigStep> configSteps)
		{
			List<MethodArgumentType> models = new();

			foreach (var argument in method.ParameterList.Parameters)
			{
				var argName = argument.Type as IdentifierNameSyntax;

				models.Add(new MethodArgumentType()
				{
					type = argName.Identifier.Text
				});
			}

			return models;
		}

		static List<MethodArgumentType> GetMethodNonConfigArguments(MethodDeclarationSyntax method, List<ConfigStep> configSteps)
		{
			List<MethodArgumentType> models = new();

			foreach (var argument in method.ParameterList.Parameters)
			{
				var argName = argument.Type as IdentifierNameSyntax;

				if (configSteps.Any(x => x.name == argName.Identifier.Text))
					continue;

				models.Add(new MethodArgumentType()
				{
					type = argName.Identifier.Text
				});
			}

			return models;
		}
	}

	struct EcsEngine
	{
		public List<ConfigStep> configSteps;
		public List<SetupStep> setupSteps;
		public List<ResourceManager> resourceManagers;
		public List<Pipeline> pipelines;
		public List<World> worlds;
	}

	struct World
	{
		public string name;
		public List<Pipeline> pipelines;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("worldName".AsSpan(), Parameter.Create(name));
			model.Set("worldPipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(pipelines.Select(x => x.GetModel())));

			return model;
		}
	}

	struct FileUsing
	{
		public string name;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("usingName".AsSpan(), Parameter.Create(name));

			return model;
		}
	}

	struct SetupStep
	{
		public string method;
		public List<MethodReturnType> returnTypes;
		public List<MethodArgumentType> argumentTypes;
		public List<MethodArgumentType> nonConfigArgumentTypes;

		public Model<ReturnType> GetModel(IEnumerable<SystemArgument> usedArguments, IEnumerable<MethodArgumentType> usedConfigArguments)
		{
			var model = new Model<ReturnType>();
			model.Set("stepMethod".AsSpan(), Parameter.Create(method));
			model.Set("stepTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(returnTypes.Select(x => x.GetModel())));
			model.Set("stepArguments".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(argumentTypes.Select(x => x.GetModel())));
			model.Set("stepUsedTypes".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(returnTypes.Where(x => usedArguments.Any(y => y.type == x.type) || usedConfigArguments.Any(y => y.type == x.type)).Select(x => x.GetModel())));
			model.Set("stepCount".AsSpan(), Parameter.Create<float>(returnTypes.Count));

			return model;
		}
	}

	struct ConfigStep
	{
		public string name;

		public Model<ReturnType> GetModel()
		{
			var varName = char.ToLowerInvariant(name[0]) + name.Substring(1);

			var model = new Model<ReturnType>();
			model.Set("stepName".AsSpan(), Parameter.Create(name));
			model.Set("stepVarName".AsSpan(), Parameter.Create(varName));

			return model;
		}
	}

	struct MethodReturnType
	{
		public string type;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("returnType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}

	struct MethodArgumentType
	{
		public string type;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();
			model.Set("argName".AsSpan(), Parameter.Create($"arg{type}"));
			model.Set("argType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}

	struct ResourceManager
	{
		public string name;
		public string ns;
		public List<MethodArgumentType> arguments;

		public Model<ReturnType> GetModel()
		{
			var model = new Model<ReturnType>();

			model.Set("resourceManagerName".AsSpan(), Parameter.Create(name));
			model.Set("resourceManagerNamespace".AsSpan(), Parameter.Create(ns));
			model.Set("resourceManagerArguments".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(arguments.Select(x => x.GetModel())));

			return model;
		}
	}
}
