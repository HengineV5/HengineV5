using LightParser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TemplateGenerator;

namespace Engine.Generator
{
	class PipelineGenerator : ITemplateSourceGenerator<IdentifierNameSyntax>
	{
		public string Template => ResourceReader.GetResource("Pipeline.tcs");

		public bool TryCreateModel(Compilation compilation, IdentifierNameSyntax node, out Model<ReturnType> model, out List<Diagnostic> diagnostics)
		{
			diagnostics = new List<Diagnostic>();
			var builderRoot = EngineGenerator.GetBuilderRoot(node);

			var builderSteps = builderRoot.DescendantNodes()
				.Where(x => x is MemberAccessExpressionSyntax)
				.Cast<MemberAccessExpressionSyntax>();

			var layoutStep = builderSteps.Single(x => x.Name.Identifier.Text == "Layout");

			model = new Model<ReturnType>();
			model.Set("namespace".AsSpan(), Parameter.Create(node.GetNamespace()));
			model.Set("engineName".AsSpan(), Parameter.Create(EngineGenerator.GetEngineName(node)));
			model.Set("ecsName".AsSpan(), Parameter.Create(EngineGenerator.GetEcsName(node)));

			var usings = EngineGenerator.GetUsings(node);
			model.Set("usings".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(usings.Select(x => x.GetModel())));

			var pipelines = GetPipelines(compilation, layoutStep);
			model.Set("pipelines".AsSpan(), Parameter.CreateEnum<IModel<ReturnType>>(pipelines.Select(x => x.GetModel())));

			return true;
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
			return $"{EngineGenerator.GetEngineName(node)}_Pipeline";
		}

		public static List<Pipeline> GetPipelines(Compilation compilation, MemberAccessExpressionSyntax step)
		{
			List<Pipeline> models = new();

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

				var systems = GetPipelineSystems(compilation, layoutLambda.Expression as SimpleLambdaExpressionSyntax);

				models.Add(new Pipeline()
				{
					name = nameExpression.Token.Value.ToString(),
					systems = systems,
					contextArguments = systems.SelectMany(x => x.contextArguments).GroupBy(x => x.type).Select(x => x.First()).ToList()
				});
			}

			return models;
		}

		static List<PipelineSystem> GetPipelineSystems(Compilation compilation, SimpleLambdaExpressionSyntax lambda)
		{
			List<PipelineSystem> models = new();

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
				var nodes = compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodesAndSelf());

				var systemClass = nodes.FindNode<ClassDeclarationSyntax>(x => x.Identifier.Text == systemName.Identifier.Text);

				var methods = systemClass.Members.Where(x => x is MethodDeclarationSyntax).Cast<MethodDeclarationSyntax>();

				bool hasInit = methods.Any(x => x.Identifier.Text == "Init");
				bool hasDispose = methods.Any(x => x.Identifier.Text == "Dispose");
				bool hasPreRun = methods.Any(x => x.Identifier.Text == "PreRun");
				bool hasPostRun = methods.Any(x => x.Identifier.Text == "PostRun");

				TryGetSystemContexts(systemClass, new List<Diagnostic>(), out List<SystemArgument> contextArguments);

				models.Add(new PipelineSystem()
				{
					name = systemName.Identifier.Text,
					arguments = GetSystemConstructorArguments(systemClass),
					contextArguments = contextArguments,
					hasInit = hasInit,
					hasDispose = hasDispose,
					hasPreRun = hasPreRun,
					hasPostRun = hasPostRun
				});
			}

			return models;
		}

		static List<SystemArgument> GetSystemConstructorArguments(ClassDeclarationSyntax system)
		{
			List<SystemArgument> args = new();

			var constructor = system.Members.Where(x => x is ConstructorDeclarationSyntax).Cast<ConstructorDeclarationSyntax>().FirstOrDefault();
			if (constructor is null)
				return args;

			var constructorArguments = constructor.ParameterList.Parameters;

			foreach (var arg in constructorArguments)
			{
				var argName = (arg.Type as IdentifierNameSyntax).Identifier.Text;
				args.Add(new SystemArgument()
				{
					type = argName
				});
			}

			return args;
		}

		static bool TryGetSystemContexts(ClassDeclarationSyntax node, List<Diagnostic> diagnostics, out List<SystemArgument> contexts)
		{
			var attribute = node.AttributeLists.SelectMany(x => x.Attributes).First(x => x.Name.GetName() == "System" || x.Name.GetName() == "SystemAttribute");
			contexts = new List<SystemArgument>();

			if (attribute.Name is not GenericNameSyntax g)
				return true;

			foreach (TypeSyntax type in g.TypeArgumentList.Arguments)
			{
				if (type is not IdentifierNameSyntax i)
					continue;

				contexts.Add(new SystemArgument()
				{
					type = i.Identifier.Text
				});
			}

			return true;
		}
	}

	struct Pipeline
	{
		public string name;
		public List<PipelineSystem> systems;
		public List<SystemArgument> contextArguments;

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
	}

	struct PipelineSystem
	{
		public string name;
		public List<SystemArgument> arguments;
		public List<SystemArgument> contextArguments;

		public bool hasInit;
		public bool hasDispose;
		public bool hasPreRun;
		public bool hasPostRun;

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
	}

	struct SystemArgument
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
}
