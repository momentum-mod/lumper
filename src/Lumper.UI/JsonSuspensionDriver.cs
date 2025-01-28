namespace Lumper.UI;

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;

// Based on https://docs.avaloniaui.net/docs/concepts/reactiveui/data-persistence#creating-the-suspension-driver
public class JsonSuspensionDriver(string file) : ISuspensionDriver
{
    private readonly JsonSerializerSettings _settings = new() { TypeNameHandling = TypeNameHandling.All };

    public IObservable<Unit> InvalidateState()
    {
        if (File.Exists(file))
            File.Delete(file);
        return Observable.Return(Unit.Default);
    }

    public IObservable<object> LoadState()
    {
        if (!File.Exists(file))
            return Observable.Empty<object>();

        string lines = File.ReadAllText(file);
        object? state = JsonConvert.DeserializeObject<object>(lines, _settings);
        return state is not null ? Observable.Return(state) : Observable.Empty<object>();
    }

    public IObservable<Unit> SaveState(object state)
    {
        string lines = JsonConvert.SerializeObject(state, _settings);
        File.WriteAllText(file, lines);
        return Observable.Return(Unit.Default);
    }
}
