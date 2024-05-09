using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Engine.Generator.Tests
{
	public static class TestHelper
	{
		public static Task Verify(params string[] source)
		{
			List<SyntaxTree> trees = new List<SyntaxTree>();
			foreach (var item in source)
			{
				trees.Add(CSharpSyntaxTree.ParseText(item));
			}

			var r = new MetadataReference[1];
			r[0] = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

			CSharpCompilation compilation = CSharpCompilation.Create("Tests", trees, references: r);
			var diag = compilation.GetDiagnostics();

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
			string fixedArraySource = @"
using System.Runtime.CompilerServices;

namespace EnCS
{
	[InlineArray(2)]
	public struct FixedArray2<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}

	[InlineArray(4)]
	public struct FixedArray4<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}

	[InlineArray(8)]
	public struct FixedArray8<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}

	[InlineArray(16)]
	public struct FixedArray16<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}

	[InlineArray(32)]
	public struct FixedArray32<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}

	[InlineArray(64)]
	public struct FixedArray64<T>
	{
		T _element0;

		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public ref T GetPinnableReference()
		{
			return ref Unsafe.AsRef(ref _element0);
		}
	}
}

";

			string interfaceSource = @"
namespace EnCS
{
	public interface IResourceManager<TResource>
	{
		public uint Store(in TResource resource);

		public ref TResource Get(uint id);
	}

	public interface IResourceManager<TIn, TOut>
	{
		public uint Store(in TIn resource);

		public ref TOut Get(uint id);
	}
}
";

			string attribSource = @"
using System;

namespace EnCS.Attributes
{
	public class ComponentAttribute : System.Attribute
	{

	}

	public class ArchTypeAttribute : System.Attribute
	{

	}

	public class ResourceManagerAttribute : System.Attribute
	{

	}

	public class SystemAttribute : System.Attribute
	{

	}

	public class SystemContextAttribute<T1> : System.Attribute where T1 : unmanaged
	{

	}

	public class SystemContextAttribute<T1, T2> : System.Attribute where T1 : unmanaged where T2 : unmanaged
	{

	}

	public class SystemContextAttribute<T1, T2, T3> : System.Attribute where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
	{

	}

	public class SystemContextAttribute<T1, T2, T3, T4> : System.Attribute where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
	{

	}

	public class SystemLayerAttribute : System.Attribute
	{
		public SystemLayerAttribute(int layer)
		{
            
		}

		public SystemLayerAttribute(int layer, int chunk)
		{

		}
	}

	public class SystemUpdateAttribute : System.Attribute
	{

	}

	public class SystemPreLoopAttribute : System.Attribute
	{

	}

	public class SystemPostLoopAttribute : System.Attribute
	{

	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	public class UsingResourceAttribute<T> : System.Attribute
	{

	}
}
";

			string source = @"
using System.Text;
using Henkgine;

namespace Test
{
	class TestClass
	{

	}

	class TestClass1
	{

	}

	class TestClass2
	{

	}

	class TestClass3
	{

	}

	class TestClass4
	{

	}

	class TestClass5
	{

	}

	class TestClass6
	{

	}

	struct TestContext
	{
		public float val1;
	}

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

	[ResourceManager]
	public partial class MeshResourceManager : IResourceManager<Mesh, MeshBuffer>
	{
		public MeshResourceManager(TestClass4 context)
		{
		}

		public ref Graphics.MeshBuffer Get(uint id)
		{
			return ref meshBuffers.Span[(int)id];
		}

		public uint Store(in Graphics.Mesh mesh)
		{
			// Very cool implementation
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
	[SystemContext<TestContext>]
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
			.Resource(x =>
			{
				x.ResourceManager<MeshResourceManager>();
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

				x.World<Ecs.Main.Interface>(x =>
				{
					x.Pipeline<Hengine.GraphicsPipeline>();
					x.Pipeline<Hengine.PhysicsPipeline>();
				});

				x.World<Ecs.World2.Interface>(x =>
				{
					x.Pipeline<Hengine.GraphicsPipeline>();
				});
			})
			.Build<Hengine, HengineEcs>();
	}
}
";

			return TestHelper.Verify(source, attribSource, interfaceSource, fixedArraySource);
		}
	}
}