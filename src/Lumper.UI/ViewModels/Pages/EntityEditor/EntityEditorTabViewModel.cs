namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;
using ReactiveUI;

public abstract class EntityEditorTabViewModel
{
    public abstract EntityViewModel Entity { get; }

    public abstract string Name { get; protected set; }

    public bool IsPinned { get; set; }

    public abstract bool Pinnable { get; }
}

public class EntityEditorTabSingleEntityViewModel(EntityViewModel entity) : EntityEditorTabViewModel
{
    public override EntityViewModel Entity { get; } = entity;

    public override string Name { get; protected set; } = entity.Classname;

    public override bool Pinnable => true;
}

public sealed class EntityEditorTabMultipleEntityViewModel : EntityEditorTabViewModel, IDisposable
{
    public override EntityViewModel Entity { get; }

    public List<EntityViewModel> RealEntities { get; }

    public override string Name { get; protected set; }

    public override bool Pinnable => false;

    private const string DifferentLabel = "<different>";

    private readonly List<IDisposable> _observers;

    public EntityEditorTabMultipleEntityViewModel(List<EntityViewModel> realEntities)
    {
        // Generate an entity with all the shared keys of the real entities, with values set to "<different>"
        // when values are non-matching.
        // This thing is a genuine EntityViewModel so UI displays it fine, just doesn't actually exist in the entity
        // lump.
        Entity fakeEnt = CreateFakeEntity(realEntities);

        // Passing entity lump viewmodel here *should* behave okay, though we don't need the fake entity
        // do handle IsModified stuff since changes to its values trigger updates to real entities.
        // Refactoring HierarchicalBspNode to allow nullable parents would be a headache, and we really
        // want this "fake entity" system avoid restructuring the shared entity viewmodel system.
        var fakeEntity = new EntityViewModel(fakeEnt, BspService.Instance.EntityLumpViewModel!);

        Name = ComputeName(realEntities);

        // Set up observers binding the fake entity values to the real entities
        _observers = SetupRealEntityObservables(fakeEntity, realEntities);

        Entity = fakeEntity;
        RealEntities = realEntities;
    }

    private static Entity CreateFakeEntity(List<EntityViewModel> realEntities)
    {
        // Only care about properties shared by each entity, so start with first selected then
        // whittle down using the rest.
        var fakeEnt = new Entity(
            realEntities[0]
                .Properties.Select(prop => new KeyValuePair<string, string>(
                    prop.Key,
                    prop.EntityProperty.ValueString ?? ""
                ))
        );

        // Iterating over list is probs faster than hashmap given so few KVs but haven't benched.
        foreach (Entity.EntityProperty fakeProp in fakeEnt.Properties.ToList())
        {
            foreach (EntityViewModel entity in realEntities[1..])
            {
                EntityPropertyViewModel? match = entity.Properties.FirstOrDefault(x => x.Key == fakeProp.Key);
                if (match is null || match.EntityProperty.GetType() != fakeProp.GetType())
                {
                    fakeEnt.Properties.Remove(fakeProp);
                }
                else if (fakeProp is Entity.EntityProperty<string> fakeStringProp)
                {
                    // Checked types match so cast is safe
                    if (fakeStringProp.Value != ((EntityPropertyStringViewModel)match).Value)
                        fakeStringProp.Value = DifferentLabel;
                }
                // Ifs are exhaustive
                else if (fakeProp is Entity.EntityProperty<EntityIo> { Value: EntityIo fakeIoProp })
                {
                    var ioMatch = (EntityPropertyIoViewModel)match;

                    if (fakeIoProp.TargetEntityName != ioMatch.TargetEntityName)
                        fakeIoProp.TargetEntityName = DifferentLabel;

                    if (fakeIoProp.Input != ioMatch.Input)
                        fakeIoProp.Input = DifferentLabel;

                    if (fakeIoProp.Parameter != ioMatch.Parameter)
                        fakeIoProp.Parameter = DifferentLabel;

                    if (fakeIoProp.Delay != ioMatch.Delay)
                        fakeIoProp.Delay = null;

                    if (fakeIoProp.TimesToFire != ioMatch.TimesToFire)
                        fakeIoProp.TimesToFire = null;
                }
            }
        }

        return fakeEnt;
    }

    private static string ComputeName(List<EntityViewModel> realEntities)
    {
        IEnumerable<string> classnames = realEntities.Select(ent => ent.Classname).Distinct().ToList();
        return classnames.Count() == 1 ? $"Multiple {classnames.First()}s" : "Multiple Entities";
    }

    private static List<IDisposable> SetupRealEntityObservables(
        EntityViewModel fakeEntity,
        List<EntityViewModel> realEntities
    ) =>
        fakeEntity
            .Properties.SelectMany(fakeProp =>
            {
                List<IDisposable> observers = [];

                IObservable<string> fakeKey = fakeProp.ObservableForProperty(x => x.Key).Select(x => x.Value);

                IEnumerable<EntityPropertyViewModel> realProps = realEntities.Select(realEnt =>
                    realEnt.Properties.FirstOrDefault(realProp => realProp.Key == fakeProp.Key)!
                );

                observers.Add(
                    fakeKey.Subscribe(fake =>
                    {
                        foreach (EntityPropertyViewModel realProp in realProps)
                            realProp.Key = fake;
                    })
                );

                switch (fakeProp)
                {
                    case EntityPropertyStringViewModel fakeStringProp:
                    {
                        IObservable<string?> fakeValue = fakeStringProp
                            .ObservableForProperty(x => x.Value)
                            .Select(x => x.Value);

                        observers.Add(
                            fakeValue.Subscribe(fake =>
                            {
                                // Cast is safe with how we constructured fake entity
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyStringViewModel)realProp).Value = fake;
                            })
                        );
                        break;
                    }
                    case EntityPropertyIoViewModel ioProp:
                    {
                        IObservable<string?> fakeTarget = ioProp
                            .ObservableForProperty(x => x.TargetEntityName)
                            .Select(x => x.Value);

                        observers.Add(
                            fakeTarget.Subscribe(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyIoViewModel)realProp).TargetEntityName = fake;
                            })
                        );

                        IObservable<string?> fakeInput = ioProp
                            .ObservableForProperty(x => x.Input)
                            .Select(x => x.Value);

                        observers.Add(
                            fakeInput.Subscribe(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyIoViewModel)realProp).Input = fake;
                            })
                        );

                        IObservable<string?> fakeParam = ioProp
                            .ObservableForProperty(x => x.Parameter)
                            .Select(x => x.Value);

                        observers.Add(
                            fakeParam.Subscribe(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyIoViewModel)realProp).Parameter = fake;
                            })
                        );

                        IObservable<float?> fakeDelay = ioProp.ObservableForProperty(x => x.Delay).Select(x => x.Value);

                        observers.Add(
                            fakeDelay.Subscribe(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyIoViewModel)realProp).Delay = fake;
                            })
                        );

                        IObservable<int?> fakeTimes = ioProp
                            .ObservableForProperty(x => x.TimesToFire)
                            .Select(x => x.Value);

                        observers.Add(
                            fakeTimes.Subscribe(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyIoViewModel)realProp).TimesToFire = fake;
                            })
                        );
                        break;
                    }
                }

                return observers;
            })
            .ToList();

    public void Dispose()
    {
        foreach (IDisposable observer in _observers)
            observer.Dispose();
    }
}
