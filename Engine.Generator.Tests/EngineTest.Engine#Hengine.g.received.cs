//HintName: Hengine.g.cs
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Engine;
using Microsoft.Extensions.Logging;

// Source file usings
using System.Text;
using Henkgine;

namespace Test
{
	public partial class Hengine
	{
		HengineEcs ecs;

		GraphicsPipeline _GraphicsPipeline;
		PhysicsPipeline _PhysicsPipeline;

		public GL argGL;
		public TestClass argTestClass;
		public TestClass3 argTestClass3;
		public TestClass5 argTestClass5;
		public TestClass4 argTestClass4;

		TestConfig1 argTestConfig1;
		TestConfig2 argTestConfig2;

		Test.MeshResourceManager _MeshResourceManager;

		bool initialized = false;
		Stopwatch sw = new Stopwatch();

		public Hengine(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
		{
			this.argILoggerFactory = loggerFactory;
		}

		public HengineEcs GetEcs()
		{
			if (!initialized)
				throw new System.Exception("Hengine must be initialized by running before GetEcs() can be called.");

			return ecs;
		}

		public void Initialize(TestConfig1 testConfig1, TestConfig2 testConfig2)
		{
			argTestConfig1 = testConfig1;
			argTestConfig2 = testConfig2;

			var _GL = GlfwSetup.OpenGLSetup();
			argGL = _GL;
			
			var _TestClass5 = NewClass0.Setup(argTestConfig2);
			argTestClass5 = _TestClass5;
			
			NewClass0.Setup2(argTestConfig2);
			
			
			var (_TestClass, _TestClass3, _TestClass4) = NewClass.Setup2(argTestClass5);
			argTestClass = _TestClass;
			argTestClass3 = _TestClass3;
			argTestClass4 = _TestClass4;
			
			var argMeshResourceManager = new Test.MeshResourceManager(argTestClass4);
			_MeshResourceManager = argMeshResourceManager;

			_GraphicsPipeline = new GraphicsPipeline(argGL, argTestClass, argTestClass3);
			_PhysicsPipeline = new PhysicsPipeline(argTestClass3, argGL);

			_GraphicsPipeline.Init();
			_PhysicsPipeline.Init();

			ecs = new HengineEcs(_MeshResourceManager);

			initialized = true;
		}

		public void Start()
		{
			if (!initialized)
				throw new System.Exception("Hengine must be initialized by running initialize() before start.");

			sw.Restart();

			var engineContext = new EngineContext();
			while (true)
			{
				engineContext.dt = sw.ElapsedMilliseconds / 1000f;
				sw.Restart();

				TestContext argTestContext = new TestContext();

				{ // Ecs.Main.Interface
					_GraphicsPipeline.Run<Ecs.Main.Interface>(ref engineContext, ecs, ref argTestContext);
					_PhysicsPipeline.Run<Ecs.Main.Interface>(ref engineContext, ecs);
				}{ // Ecs.World2.Interface
					_GraphicsPipeline.Run<Ecs.World2.Interface>(ref engineContext, ecs, ref argTestContext);
				}

				if (ShouldExit())
				{
					_GraphicsPipeline.Dispose();
					_PhysicsPipeline.Dispose();
					break;
				}
			}
		}
	}
}