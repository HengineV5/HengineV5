using ImageLib;
using Microsoft.Extensions.Logging;

namespace Engine
{
	public static class LogSetup
	{
		public static void LoggerSetup(ILoggerFactory factory)
		{
			ImageLibLog.SetLoggerFactory(factory);
		}
	}
}
