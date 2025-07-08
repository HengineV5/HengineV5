using ImageLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

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
