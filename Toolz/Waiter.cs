using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ToolzLib
{
    
    public class Toolz
    {
       
        private ILogger? _logger;
        public Toolz() { }
        public Toolz(ILogger logger)
        {
            _logger = logger;
        }
        public void InjectLogger (ILogger<Toolz> logger)
        {
            _logger = logger;
        }
        public bool Waiter(Func<bool> desireCondition, TimeSpan timeout, string message = "", int loopdelay = 100)
        {
            // (EqualityComparer<T>.Default.Equals(expression.Invoke(), desirevalue))
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (desireCondition.Invoke())
                    return true;
                if (message != String.Empty)
                {
                    if (_logger != null)
                    {
                        _logger.LogTrace($"{message}");
                    }
                }
                Task.Delay(loopdelay).Wait();
            }
            return false;
        }
    }
}
