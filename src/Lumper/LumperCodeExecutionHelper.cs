using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Lumper
{
    public class LumperCodeExecutionHelper
    {
        protected ILogger _logger;

        public LumperCodeExecutionHelper()
        {
            _logger = LumperLoggerFactory.GetInstance().CreateLogger(GetType());
        }

        public async Task ExecuteAndLogError(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
