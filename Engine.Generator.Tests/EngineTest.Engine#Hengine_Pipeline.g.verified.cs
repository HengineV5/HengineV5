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

			public void Run<TWorldInterface>(ref EngineContext argEngineContext, HengineEcs ecs)
				where TWorldInterface : IWorld<HengineEcs, OpenGLRenderSystem, TestContext>, IWorld<HengineEcs, System2>, IWorld<HengineEcs, System3>
			{
				_OpenGLRenderSystem.PreRun();
				TestContext argTestContext = new TestContext();

				TWorldInterface.Loop(ecs, _OpenGLRenderSystem, ref argTestContext);
				TWorldInterface.Loop(ecs, _System2);
				TWorldInterface.Loop(ecs, _System3);

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

			public void Run<TWorldInterface>(ref EngineContext argEngineContext, HengineEcs ecs)
				where TWorldInterface : IWorld<HengineEcs, System2>
			{
				
				

				TWorldInterface.Loop(ecs, _System2);

				
			}

			public void Dispose()
			{
				_System2.Dispose();
			}
		}
	}
}