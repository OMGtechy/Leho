using NLog.Config;
using NLog.Targets;
using System;
using System.Linq;

namespace Leho
{
    public static class Logger
    {
        private static LoggingConfiguration config;
        private static NLog.Logger internalLogger;

        public static void Configure(Options options)
        {
            var consoleTarget = new ConsoleTarget();

            config = new LoggingConfiguration();
            config.AddRuleForAllLevels(consoleTarget);

            NLog.LogManager.Configuration = config;

            internalLogger = NLog.LogManager.GetCurrentClassLogger();

            SetLogLevel(options.LogLevel);
            Log(LogLevel.Debug, "Logger constructed.");
        }

        private static void SetLogLevel(LogLevel logLevel)
        {
            var internalLogLevel = logLevel.ToInternal();

            foreach (var loggingRule in NLog.LogManager.Configuration.LoggingRules)
            {
                // it's easier to just disable everything and then reenable
                // the ones you want than to calculate some kind of delta
                loggingRule.DisableLoggingForLevels(NLog.LogLevel.Trace, NLog.LogLevel.Off);
                loggingRule.EnableLoggingForLevels(internalLogLevel, NLog.LogLevel.Off);
            }

            NLog.LogManager.ReconfigExistingLoggers();
        }

        public enum LogLevel
        {
            Trace,
            Debug,
            Info,
            Warning,
            Error,
            Fatal,
            None
        }

        private static NLog.LogLevel ToInternal(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return NLog.LogLevel.Trace;
                case LogLevel.Debug:
                    return NLog.LogLevel.Debug;
                case LogLevel.Info:
                    return NLog.LogLevel.Info;
                case LogLevel.Warning:
                    return NLog.LogLevel.Warn;
                case LogLevel.Error:
                    return NLog.LogLevel.Error;
                case LogLevel.Fatal:
                    return NLog.LogLevel.Fatal;
                case LogLevel.None:
                    return NLog.LogLevel.Off;
                default:
                    Log(LogLevel.Fatal, $"Got unknown log level {logLevel}, overriding with {LogLevel.Fatal}.");
                    return ToInternal(LogLevel.Fatal);
            }
        }

        public static void Log(LogLevel logLevel, string message)
        {
            internalLogger.Log(logLevel.ToInternal(), message);
        }
    }
}
