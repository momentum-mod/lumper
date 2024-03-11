using Newtonsoft.Json;
using JsonSubTypes;
using Lumper.Lib.BSP;
using Microsoft.Extensions.Logging;

namespace Lumper.Lib.Tasks
{
    public enum TaskResult { Unknwon, Success, Failed }

    [JsonConverter(typeof(JsonSubtypes), "Type")]
    public abstract class LumperTask
    {
        protected ILogger _logger;

        public abstract string Type { get; }

        [JsonIgnore]
        public TaskProgress Progress { get; protected set; } = new();

        public abstract TaskResult Run(BspFile map);

        public LumperTask()
        {
            _logger = LumperLoggerFactory.GetInstance().CreateLogger(GetType());
        }
    }
}