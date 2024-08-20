namespace Lumper.Lib.Jobs;

using System;
using JsonSubTypes;
using Lumper.Lib.Bsp;
using Newtonsoft.Json;

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

    public abstract bool Run(BspFile bsp);
}
