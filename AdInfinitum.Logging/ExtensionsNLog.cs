using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using AdInfinitum.Exceptions.Extensions;
using NLog;

namespace AdInfinitum.Logging
{
    public static class Extensions
    {
        private static readonly ConcurrentDictionary<string, object> PrevValues = new ConcurrentDictionary<String, Object>();
        private static readonly ConcurrentDictionary<string, DateTime> LastLog = new ConcurrentDictionary<String, DateTime>();

        public const int OneMinAsMs = 60 * 1000;

        public static int MinToMs(this int value)
        {
            return value * OneMinAsMs;
        }

        public static void LogLine(
            this Logger logger,
            LogLevel logLevel,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, logLevel, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoLine(
            this Logger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Info, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnLine(
            this Logger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Warn, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorLine(
            this Logger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Error, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorLine(
            this Logger logger,
            Exception ex,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Error, ex, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugLine(
            this Logger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Debug, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void TraceLine(
            this Logger logger,
            string message = "",
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            LogLinePrivate(logger, LogLevel.Trace, message, callerMember, callerFilePath, callerLineNumber);
        }

        public static void LogLinePrivate(
            Logger logger,
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
            Logger logger,
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
            this Logger logger,
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
                logger, message, value, LogLevel.Off, null,
                1000, LogLevel.Info,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnTimeout(
            this Logger logger,
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
                timeoutMs, LogLevel.Info,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnTimeout(
            this Logger logger,
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
                logger, message, null, unchangedLogLevel, LogLevel.Info,
                timeoutMs, LogLevel.Info,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugOnTimeout(
            this Logger logger,
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
                logger, message, null, unchangedLogLevel, null,
                timeoutMs, LogLevel.Debug,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void DebugOnChangeOrTimeout(
            this Logger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = null,
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
            this Logger logger,
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
                logger, message, null, unchangedLogLevel, null,
                timeoutMs, LogLevel.Trace,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void LogOnChange(
            this Logger logger,
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
                Timeout.Infinite, null,
                null, null,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnChange(
            this Logger logger,
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
                logger, message, value, unchangedLogLevel, LogLevel.Info,
                Timeout.Infinite, null,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnOnChange(
            this Logger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Warn,
                Timeout.Infinite, null,
                null, null,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this Logger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = null,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, message, value, unchangedLogLevel, LogLevel.Error,
                Timeout.Infinite, null,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this Logger logger,
            Exception ex,
            string message,
            object value,
            LogLevel unchangedLogLevel = null,
            string logPointKey = null,
            object logPointPrefix = null,
            [CallerMemberName] string callerMember = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            SmartLog(
                logger, ex, message, value, unchangedLogLevel, LogLevel.Error,
                Timeout.Infinite, null,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void ErrorOnChange(
            this Logger logger,
            Exception ex,
            LogLevel unchangedLogLevel = null,
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
            this Logger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = null,
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
            this Logger logger,
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
            this Logger logger,
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
            this Logger logger,
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
                logger, ex, message, message, LogLevel.Off, LogLevel.Error,
                timeoutMs, LogLevel.Error,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void WarnOnChangeOrTimeout(
            this Logger logger,
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
                logger, message, value, unchangedLogLevel, LogLevel.Warn,
                timeoutMs, LogLevel.Warn,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }

        public static void InfoOnChangeOrTimeout(
            this Logger logger,
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
                logger, message, value, unchangedLogLevel, LogLevel.Info,
                timeoutMs, LogLevel.Info,
                logPointKey, logPointPrefix,
                callerMember, callerFilePath, callerLineNumber);
        }


        public static void TraceOnChangeOrTimeout(
            this Logger logger,
            string message,
            object value,
            LogLevel unchangedLogLevel = null,
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
            this Logger logger,
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
            Logger logger,
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
            Logger logger,
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
            unchangedLogLevel = unchangedLogLevel ?? LogLevel.Off;

            bool onChangeLogRequired = false;
            bool onTimeoutLogRequired = false;

            if (onChangeLogLevel != null)
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


            if (timeOutMs != Timeout.Infinite && onTimeoutLogLevel != null)
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