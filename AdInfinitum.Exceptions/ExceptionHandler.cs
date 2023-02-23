using AdInfinitum.Exceptions.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdInfinitum.Exceptions
{
    public class ExceptionHandler
    {
        protected static readonly Lazy<Logger> LoggerLazy = new Lazy<Logger>(LogManager.GetCurrentClassLogger);

        protected ILogger Logger => LoggerLazy.Value;

        public ExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        }

        public static ExceptionHandler Create()
        {
            return new ExceptionHandler();
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var message = $"{args.ExceptionObject?.ToString()}";
            if (args.ExceptionObject is Exception ex)
            {
                message = ex.FullErrorMessage();
            }
            Logger.Fatal($"UNHANDLED EXCEPTION:{Environment.NewLine}{message}");
        }
    }
}
