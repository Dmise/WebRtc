using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using AdInfinitum.Exceptions.Extensions;
using Microsoft.Extensions.Logging;

namespace AdInfinitum.Logging
{
    public static class ExtensionsILogger
    {
        private static readonly ConcurrentDictionary<string, object> PrevValues = new ConcurrentDictionary<String, Object>();
        private static readonly ConcurrentDictionary<string, DateTime> LastLog = new ConcurrentDictionary<String, DateTime>();

        public const int OneMinAsMs = 60 * 1000;

        public static void LogLine(
            this ILogger logger,
            LogLevel logLevel,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, logLevel, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoLine(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Information, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnLine(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Warning, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorLine(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Error, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorLine(
            this ILogger logger,
            Exception ex,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Error, ex, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugLine(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Debug, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void TraceLine(
            this ILogger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Trace, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void LogLinePrivate(
            ILogger logger,
            LogLevel logLevel,
            string message,
            string callerMember,
            string callerFilePath,
            int callerLineNumber)
        {
            logger.Log(logLevel,
                $"{callerMember}:{message} [{System.IO.Path.GetFileName(callerFilePath)}@{callerLineNumber}]");
        }

        public static void LogLinePrivate(
            ILogger logger,
            LogLevel logLevel,
            Exception ex,
            string message,
            string callerMember,
            string callerFilePath,
            int callerLineNumber)
        {
            logger.Log(logLevel, ex,
                $"{callerMember}:{message} [{System.IO.Path.GetFileName(callerFilePath)}@{callerLineNumber}]");
        }

        public static void LogOnTimeout(
            this ILogger logger,
            string message,
            object value = null,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, LogLevel.None, LogLevel.None,
                1000, LogLevel.Information,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnTimeout(
            this ILogger logger,
            string message,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, null, unchangedLogLevel, LogLevel.Error,
                timeoutMs, LogLevel.Information,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnTimeout(
            this ILogger logger,
            string message,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, null, unchangedLogLevel, LogLevel.Information,
                timeoutMs, LogLevel.Information,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugOnTimeout(
            this ILogger logger,
            string message,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, null, unchangedLogLevel, LogLevel.None,
                timeoutMs, LogLevel.Debug,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = LogLevel.None,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Debug,
                timeoutMs, LogLevel.Debug,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void TraceOnTimeout(
            this ILogger logger,
            string message,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, null, unchangedLogLevel, LogLevel.None,
                timeoutMs, LogLevel.Trace,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void LogOnChange(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            LogLevel onChangeLogLevel,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, onChangeLogLevel,
                Timeout.Infinite, LogLevel.None,
                null, null,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnChange(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Information,
                Timeout.Infinite, LogLevel.None,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnOnChange(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Warning,
                Timeout.Infinite, LogLevel.None,
                null, null,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = LogLevel.None,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Error,
                Timeout.Infinite, LogLevel.None,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this ILogger logger,
            Exception ex,
            string message,
            object value,
            LogLevel unchangedLogLevel = LogLevel.None,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, ex, message, value, unchangedLogLevel, LogLevel.Error,
                Timeout.Infinite, LogLevel.None,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this ILogger logger,
            Exception ex,
            LogLevel unchangedLogLevel = LogLevel.None,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var message = ex.FullErrorMessage();

            SmartLog(
                logger, ex, message, message, unchangedLogLevel, LogLevel.Error,
                Timeout.Infinite, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = LogLevel.None,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Error,
                timeoutMs, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChangeOrTimeout(
            this ILogger logger,
            Exception ex,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, ex, message, value, unchangedLogLevel, LogLevel.Error,
                timeoutMs, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChangeOrTimeout(
            this ILogger logger,
            Exception ex,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var message = ex.FullErrorMessage();

            SmartLog(
                logger, ex, message, message, unchangedLogLevel, LogLevel.Error,
                timeoutMs, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChangeOrTimeout(
            this ILogger logger,
            Exception ex,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            var message = ex.FullErrorMessage();

            SmartLog(
                logger, ex, message, message, LogLevel.None, LogLevel.Error,
                timeoutMs, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Warning,
                timeoutMs, LogLevel.Warning,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Information,
                timeoutMs, LogLevel.Information,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }


        public static void TraceOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = LogLevel.None,
            int timeoutMs = OneMinAsMs,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Trace,
                timeoutMs, LogLevel.Trace,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void LogOnChangeOrTimeout(
            this ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            LogLevel onChangeLogLevel,
            int timeoutMs,
            LogLevel onTimeoutLogLevel,
            string logPointKey = null,
            string logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, onChangeLogLevel,
                timeoutMs, onTimeoutLogLevel,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        private static void SmartLog(
            ILogger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            LogLevel onChangeLogLevel,
            int timeOutMs,
            LogLevel onTimeoutLogLevel,
            string logPointKey,
            object logPointPrefix,
            string callerMember,
            string callerFilePath,
            int callerLineNumber)
        {
            SmartLog(
                logger, null, message, value, unchangedLogLevel, onChangeLogLevel,
                timeOutMs, onTimeoutLogLevel, logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }


        private static void SmartLog(
            ILogger logger,
            Exception ex,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            LogLevel onChangeLogLevel,
            int timeOutMs,
            LogLevel onTimeoutLogLevel,
            string logPointKey,
            object logPointPrefix,
            string callerMember,
            string callerFilePath,
            int callerLineNumber)
        {
            var key = logPointKey ?? DictKey(callerMember, callerFilePath, callerLineNumber, logPointPrefix?.ToString());
            
            bool onChangeLogRequired = false;
            bool onTimeoutLogRequired = false;

            if (onChangeLogLevel != LogLevel.None)
            {
                if (PrevValues.TryGetValue(key, out object prevVal))
                {
                    if (value != null)
                    {
                        onChangeLogRequired = !value.Equals(prevVal);
                    }
                    else if (prevVal != null)
                    {
                        onChangeLogRequired = true;
                    }
                }
                else
                {
                    onChangeLogRequired = true;
                }

                if (onChangeLogRequired) PrevValues[key] = value;
            }


            if (timeOutMs != Timeout.Infinite && onTimeoutLogLevel != LogLevel.None)
            {
                DateTime now = DateTime.Now;
                if (LastLog.TryGetValue(key, out DateTime lastLog))
                {
                    try
                    {
                        var interVal = now - lastLog;
                        onTimeoutLogRequired = interVal.TotalMilliseconds > timeOutMs;
                    }
                    catch
                    {
                        onTimeoutLogRequired = true;
                    }
                }
                else
                {
                    onTimeoutLogRequired = true;
                }

                if (onTimeoutLogRequired) LastLog[key] = now;
            }

            var logLevel = onChangeLogRequired
                ? onChangeLogLevel
                : (onTimeoutLogRequired ? onTimeoutLogLevel : unchangedLogLevel);

            if (ex == null)
            {
                logger.Log(logLevel, 
                    $"{callerMember}:{message} [{System.IO.Path.GetFileName(callerFilePath)}@{callerLineNumber}]", value);
            }
            else
            {
                logger.Log(logLevel, ex, 
                    $"{callerMember}:{message} [{System.IO.Path.GetFileName(callerFilePath)}@{callerLineNumber}]", value);
            }
        }

        public static string DictKey(string callerMember, string callerFilePath, int callerLineNumber, string logPointPrefix)
        {
            return $"{logPointPrefix}{callerFilePath}{callerLineNumber}{callerMember}";
        }
    }
}