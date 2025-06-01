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
		ref struct ScheduleContext
		{
			public Engine.EngineContext engineContext;
			public TestContext argTestContext;
		}

		HengineEcs ecs;
		System.Memory<Engine.Scheduling.ScheduledAction<ScheduleContext>> jobs;

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

			jobs = new Engine.Scheduling.ScheduledAction<ScheduleContext>[] {
				Ecs_Main_GraphicsPipeline,
				Ecs_Main_PhysicsPipeline,
				Ecs_World2_GraphicsPipeline,
			};

			initialized = true;
		}

		public void Start()
		{
			if (!initialized)
				throw new System.Exception("Hengine must be initialized by running initialize() before start.");

			Engine.Scheduling.Scheduler<ScheduleContext> scheduler = new(jobs.Span);
			sw.Restart();

			var scheduleContext = new ScheduleContext();
			scheduleContext.engineContext = new Engine.EngineContext();

			while (true)
			{
				scheduleContext.engineContext.dt = sw.ElapsedMilliseconds / 1000f;
				sw.Restart();

				// Rest args.
				scheduleContext.argTestContext = new TestContext();

				scheduler.ExecuteOneStep(scheduleContext.engineContext.dt, ref scheduleContext);

				if (ShouldExit())
				{
					_GraphicsPipeline.Dispose();
					_PhysicsPipeline.Dispose();
					break;
				}
			}
		}

		void Ecs_Main_GraphicsPipeline(ref ScheduleContext context)
			=> _GraphicsPipeline.Run<Ecs.Main>(ref context.engineContext, ecs, ref context.argTestContext);
		
		void Ecs_Main_PhysicsPipeline(ref ScheduleContext context)
			=> _PhysicsPipeline.Run<Ecs.Main>(ref context.engineContext, ecs);
		
		void Ecs_World2_GraphicsPipeline(ref ScheduleContext context)
			=> _GraphicsPipeline.Run<Ecs.World2>(ref context.engineContext, ecs, ref context.argTestContext);
		
	}
}