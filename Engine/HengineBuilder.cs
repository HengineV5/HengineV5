using EnCS;

namespace Engine
{
	public class HengineBuilder
	{
		public delegate void ConfigAction(HengineConfig config);
		public delegate void LayoutAction(HengineLayout layout);
		public delegate void PipelineAction(HenginePipeline pipeline);
		public delegate void WorldAction(HengineWorld world);
		public delegate void ResourceAction(HengineResource resource);

		public ref struct HengineLayout
		{
			public HengineLayout Pipeline(string name, PipelineAction pipeline)
			{
				return this;
			}

			public HengineLayout World(string name, WorldAction world)
			{
				return this;
			}
		}

		public ref struct HengineConfig
		{
			public HengineConfig WithConfig<TConf>()
			{
				return this;
			}

			public HengineConfig Setup<T>(T conf)
			{
				return this;
			}
		}

		public ref struct HenginePipeline
		{
			public HenginePipeline Sequential<T>() where T : class
			{
				return this;
			}

			public HenginePipeline Paralell<T>() where T : class
			{
				return this;
			}
		}

		public ref struct HengineWorld
		{
			public HengineWorld Pipeline<T>() where T : class
			{
				return this;
			}
		}

		public ref struct HengineResource
		{
			public HengineResource ResourceManager<T>() where T : class
			{
				return this;
			}
		}

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

		public HengineBuilder Resource(ResourceAction resource)
		{
			return this;
		}

		public T Build<T, TEcs>() where T : class where TEcs : class
		{
			throw new NotImplementedException();
		}
	}
}
