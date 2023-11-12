using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Engine.Generator.Tests
{
	public static class TestHelper
	{
		public static Task Verify(string source)
		{
			SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
			CSharpCompilation compilation = CSharpCompilation.Create("Tests", new[] { syntaxTree });

			GeneratorDriver driver = CSharpGeneratorDriver.Create(new TemplateGenerator());
			driver = driver.RunGenerators(compilation);

			return Verifier.Verify(driver);
		}
	}

	[UsesVerify]
	public class EngineTest
	{
		[Fact]
		public Task Engine()
		{
			string source = @"
using System.Text;
using Henkgine;

namespace Test
{
	public class HengineBuilder
	{
		public HengineBuilder()
		{
		}

		public HengineBuilder Config(ConfigAction config)
		{
			return this;
		}

		public HengineBuilder Layout(LayoutAction layout)
		{
			return this;
		}

		public T Build<T, TEcs>() where T : class where TEcs : class
		{
			throw new NotImplementedException();
		}
	}

	public static class GlfwSetup
	{
		public static GL OpenGLSetup()
		{

		}
	}

	public static class NewClass0
	{
		public static TestClass5 Setup(TestConfig2 config)
		{

		}
	}

	public static class NewClass
	{
		public static (TestClass, TestClass3, TestClass4) Setup2(TestClass5 ts)
		{

		}
	}

	public partial class Hengine
	{

	}

	[System]
	public partial class OpenGLRenderSystem
	{
		public OpenGLRenderSystem(GL gl, TestClass w)
        {
            this.gl = gl;
		}

		public void Init()
		{
		}

		public void PreRun()
		{
		}

		public void PostRun()
		{
		}
	}

	[System]
	public partial class System2
	{
		public System2(TestClass3 m, GL gl)
        {
		}

		public void Dispose()
		{
		}
	}

	[System]
	public partial class System3
	{
	}

	static void Main()
	{
		new HengineBuilder()
			.Config(x =>
			{
				x.WithConfig<TestConfig1>();
				x.WithConfig<TestConfig2>();

				x.Setup(GlfwSetup.OpenGLSetup);
				x.Setup(NewClass0.Setup);
				x.Setup(NewClass.Setup2);
			})
			.Layout(x =>
			{
				x.Pipeline(""Graphics"", x =>
				{
					x.Sequential<OpenGLRenderSystem>();
					x.Sequential<System2>();
					x.Sequential<System3>();
				});

				x.Pipeline(""Physics"", x =>
				{
					x.Sequential<System2>();
				});

				x.World(""Main"", x =>
				{
					x.Pipeline<Hengine.GraphicsPipeline>();
					x.Pipeline<Hengine.PhysicsPipeline>();
				});

				x.World(""World2"", x =>
				{
					x.Pipeline<Hengine.GraphicsPipeline>();
				});
			})
			.Build<Hengine, HengineEcs>();
	}
}
";

			return TestHelper.Verify(source);
		}
	}
}