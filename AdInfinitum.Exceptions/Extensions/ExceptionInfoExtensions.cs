using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AdInfinitum.Exceptions.Extensions
{
    public static class ExceptionInfoExtensions
    {
        public static string FullErrorMessage(this Exception ex)
        {
            var sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine(ex.Message);
                if (ex is ReflectionTypeLoadException tle)
                {
                    foreach (var tleLoaderException in tle.LoaderExceptions)
                    {
                        sb.AppendLine(tleLoaderException.FullErrorMessage());
                    }
                }
                ex = ex.InnerException;
            }
            return sb.ToString();
        }

        public static string FullErrorStackTrace(this Exception ex)
        {
            var sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine(ex.StackTrace);
                if (ex is ReflectionTypeLoadException tle)
                {
                    foreach (var tleLoaderException in tle.LoaderExceptions)
                    {
                        sb.AppendLine(tleLoaderException.FullErrorStackTrace());
                    }
                }
                ex = ex.InnerException;
            }
            return sb.ToString();
        }
    }
}
