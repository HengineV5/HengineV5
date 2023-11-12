//HintName: Hengine_Pipeline.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Engine;

// Source file usings
using System.Text;
using Henkgine;

namespace Test
{
	public partial class Hengine
	{
		public class GraphicsPipeline
		{
			OpenGLRenderSystem _OpenGLRenderSystem;
			System2 _System2;
			System3 _System3;

			public GraphicsPipeline(GL argGL, TestClass argTestClass, TestClass3 argTestClass3)
			{
				_OpenGLRenderSystem = new(argGL, argTestClass);
				_System2 = new(argTestClass3, argGL);
				_System3 = new();
			}

			public void Init()
			{
				_OpenGLRenderSystem.Init();
			}

			public void Run(HengineEcs.Main world)
			{
				_OpenGLRenderSystem.PreRun();

				world.Loop(_OpenGLRenderSystem);
				world.Loop(_System2);
				world.Loop(_System3);

				_OpenGLRenderSystem.PostRun();
			}

			public void Dispose()
			{
				_System2.Dispose();
			}
		}

		public class PhysicsPipeline
		{
			System2 _System2;

			public PhysicsPipeline(TestClass3 argTestClass3, GL argGL)
			{
				_System2 = new(argTestClass3, argGL);
			}

			public void Init()
			{
				
			}

			public void Run(HengineEcs.Main world)
			{
				

				world.Loop(_System2);

				
			}

			public void Dispose()
			{
				_System2.Dispose();
			}
		}
	}
}