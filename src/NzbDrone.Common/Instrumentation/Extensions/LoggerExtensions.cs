using Microsoft.Extensions.Logging;
using NLog.Fluent;

namespace NzbDrone.Common.Instrumentation.Extensions
{
    public static class LoggerExtensions
    {
        public static void ProgressInfo(this ILogger logger, string message, params object[] args)
            => LogProgressMessage(logger, LogLevel.Information, message, args);


        public static void ProgressDebug(this ILogger logger, string message, params object[] args)
            => LogProgressMessage(logger, LogLevel.Debug, message, args);

        public static void ProgressTrace(this ILogger logger, string message, params object[] args)
            => LogProgressMessage(logger, LogLevel.Trace, message, args);

        private static void LogProgressMessage(ILogger logger, LogLevel level, string message, params object[] args)
        {
            //TODO: Check `Properties.Add()` (NLog.Fluent)
            //var logEvent = new LogEventInfo(level, logger.Name, message);
            //logEvent.Properties.Add("Status", "");
            //logger.Log(logEvent);

            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            logger.Log(logLevel: level, message: message, args: args);
        }
    }
}
