namespace Lumper.Lib.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JsonSubTypes;
using Lumper.Lib.Bsp;
using Newtonsoft.Json;
using NLog;

// Interface so we can use static virtual (new C# 12 thing) - need a static version of JobName for use in UI menus.
// Implementers need to implement both Job *and* IJob for this to work - if you hit the below NotSupportedException,
// you need to override JobName!
public interface IJob
{
    // User-facing name of the Job
    public static virtual string JobName => throw new NotSupportedException();
}

[JsonConverter(typeof(JsonSubtypes), "JobType")]
public abstract class Job : IJob
{
    // Implement this with => JobName
    // Static virtual only works on interfaces, so this (abstract) class can't say anything about JobName.
    [JsonIgnore]
    public abstract string JobNameInternal { get; }

    [JsonIgnore]
    public JobProgress Progress { get; protected set; } = new();

    // For serialization
    [JsonProperty(Order = -1000)] // Make sure top of order
    public string JobType => GetType().Name;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public abstract bool Run(BspFile bsp);

    public static bool TryLoadWorkflow(Stream stream, [MaybeNullWhen(false)] out List<Job> workflow)
    {
        var serializer = new JsonSerializer();
        using var sr = new StreamReader(stream);
        using var reader = new JsonTextReader(sr);
        try
        {
            List<Job>? deserialized = serializer.Deserialize<List<Job>>(reader);
            if (deserialized is not null)
            {
                workflow = deserialized;
                return true;
            }
            else
            {
                workflow = null;
                return false;
            }
        }
        catch (JsonSerializationException ex)
        {
            Logger.Error(ex, "Error loading jobs workflow");
            workflow = null;
            return false;
        }
    }

    public static void SaveWorkflow(Stream stream, List<Job> workflow)
    {
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        using var sw = new StreamWriter(stream);
        using var writer = new JsonTextWriter(sw);
        serializer.Serialize(writer, workflow);
    }
}
