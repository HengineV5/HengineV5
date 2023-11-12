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

		public Model<ReturnType> CreateModel(Compilation compilation, IdentifierNameSyntax node)
		{
			var builderRoot = GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");
			var configStep = builderSteps.Single(x => x.Name.Identifier.Text == "Config");

			var model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("engineName".AsSpan(), Parameter.Create(GetEngineName(node)));
			model.Set("ecsName".AsSpan(), Parameter.Create(GetEcsName(node)));

			var usings = GetUsings(node);
			model.Set("usings".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(usings.Select(x => x.GetModel())));

			var pipelines = PipelineGenerator.GetPipelines(compilation, layoutStep);
			model.Set("pipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(pipelines.Select(x => x.GetModel())));

			var configSteps = GetConfigSteps(configStep);
			model.Set("config".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(configSteps.Select(x => x.GetModel())));

			var setupSteps = GetSetupSteps(compilation, configStep, configSteps);
			var uniqueSystemArgs = pipelines.SelectMany(x => x.systems).SelectMany(x => x.arguments).GroupBy(x => x.type).Select(x => x.First());
			var uniqueSetupArgs = setupSteps.SelectMany(x => x.nonConfigArgumentTypes).GroupBy(x => x.type).Select(x => x.First());

			var uniqueArgs = uniqueSystemArgs.Concat(uniqueSetupArgs.Select(x => new SystemArgument() { type = x.type })).GroupBy(x => x.type).Select(x => x.First());

			model.Set("uniqueArgs".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(uniqueArgs.Select(x => x.GetModel())));
			model.Set("setup".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(setupSteps.Select(x => x.GetModel(uniqueSystemArgs, uniqueSetupArgs))));

			return model;
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

		static List<SetupStep> GetSetupSteps(Compilation compilation, MemberAccessExpressionSyntax step, IEnumerable<ConfigStep> configSteps)
		{
			var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());

			List<SetupStep> models = new();

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

				models.Add(new SetupStep()
				{
					method = configMethod.ToFullString(),
					returnTypes = GetMethodReturnTypes(methodDeclaration),
					argumentTypes = GetMethodArguments(methodDeclaration, configSteps),
					nonConfigArgumentTypes = GetMethodNonConfigArguments(methodDeclaration, configSteps),
				});
			}

			return models;
		}

		static List<ConfigStep> GetConfigSteps(MemberAccessExpressionSyntax step)
		{
			List<ConfigStep> models = new();

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

				models.Add(new ConfigStep()
				{
					name = nameToken
				});
			}

			return models;
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

		static List<MethodArgumentType> GetMethodArguments(MethodDeclarationSyntax method, IEnumerable<ConfigStep> configSteps)
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

		static List<MethodArgumentType> GetMethodNonConfigArguments(MethodDeclarationSyntax method, IEnumerable<ConfigStep> configSteps)
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
			model.Set("argType".AsSpan(), Parameter.Create(type));

			return model;
		}
	}
}
