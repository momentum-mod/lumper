namespace Lumper.Lib.Tasks;
using JsonSubTypes;
using Lumper.Lib.BSP;
using Newtonsoft.Json;

public enum TaskResult { Unknwon, Success, Failed }

[JsonConverter(typeof(JsonSubtypes), "Type")]
public abstract class LumperTask
{
    public abstract string Type { get; }

    [JsonIgnore]
    public TaskProgress Progress { get; protected set; } = new();

    public abstract TaskResult Run(BspFile map);
}
