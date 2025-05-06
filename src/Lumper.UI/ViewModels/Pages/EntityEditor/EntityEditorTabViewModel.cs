namespace Lumper.UI.ViewModels.Pages.EntityEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Lumper.Lib.Bsp.Struct;
using Lumper.UI.Services;
using Lumper.UI.ViewModels.Shared.Entity;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

public abstract class EntityEditorTabViewModel : ViewModel
{
    public EntityViewModel Entity { get; protected init; } = null!;

    [Reactive]
    public string Name { get; protected set; } = EntityViewModel.MissingClassname;

    [Reactive]
    public string? DocumentationUri { get; protected set; } = null;

    public bool IsPinned { get; set; }

    public abstract bool Pinnable { get; }
}

public class EntityEditorTabSingleEntityViewModel : EntityEditorTabViewModel
{
    public override bool Pinnable => true;

    public EntityEditorTabSingleEntityViewModel(EntityViewModel entity)
    {
        Entity = entity;

        entity
            .WhenAnyValue(x => x.Classname)
            .Subscribe(classname =>
            {
                Name = classname;
                DocumentationUri =
                    classname != EntityViewModel.MissingClassname
                        ? $"https://developer.valvesoftware.com/wiki/{classname}"
                        : null;
            });
    }
}

public sealed class EntityEditorTabMultipleEntityViewModel : EntityEditorTabViewModel, IDisposable
{
    public List<EntityViewModel> RealEntities { get; }

    public override bool Pinnable => false;

    private const string DifferentLabel = "<different>";

    private readonly CompositeDisposable _disposable;

    private bool _isDisposing = false;

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
        Entity = new EntityViewModel(fakeEnt, BspService.Instance.EntityLumpViewModel!);
        RealEntities = realEntities;

        Name = ComputeName(realEntities);

        _disposable = [];

        // EntityProperties isn't using a SourceList/Cache so need to transform to DynamicData stuff.
        // Even if it was a SourceCache I'd rather not expose it anyway, and this code doesn't run often.
        IObservable<IChangeSet<EntityPropertyViewModel>> changeset = Entity.Properties.ToObservableChangeSet();

        _disposable.Add(
            changeset
                // MergeMany maps new items to observable handling all the fake -> real property bindings,
                // and unsubscribing when the fake property is removed.
                .MergeMany(SetupRealEntityPropertyBindings)
                .Subscribe()
        );

        _disposable.Add(
            changeset
                // Fake property deletion needs to iterate through all real entities and remove corresponding
                .OnItemRemoved(HandleFakePropDeletion)
                .Subscribe()
        );
    }

    private Entity CreateFakeEntity(List<EntityViewModel> realEntities)
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

    private IObservable<Unit> SetupRealEntityPropertyBindings(EntityPropertyViewModel fakeProp)
    {
        var realProps = RealEntities
            .Select(realEnt => realEnt.Properties.FirstOrDefault(realProp => realProp.Key == fakeProp.Key))
            .OfType<EntityPropertyViewModel>()
            .ToList();

        // If true, we must be adding a new property via Entity Editor, since by initial setup, all the possible
        // fakeProps have corresponding real properties. So we need to add the new property to every real entity.
        if (realProps.Count == 0)
        {
            realProps = RealEntities
                .Select(realEnt => realEnt.AddProperty((Entity.EntityProperty)fakeProp.EntityProperty.Clone()))
                .ToList();
        }

        return Observable.Merge(
            [
                fakeProp
                    .ObservableForProperty(x => x.Key)
                    .Select(x => x.Value)
                    .Do(fake =>
                    {
                        foreach (EntityPropertyViewModel realProp in realProps)
                            realProp.Key = fake;
                    })
                    .Select(_ => Unit.Default),
                .. fakeProp switch
                {
                    EntityPropertyStringViewModel fakeStringProp =>
                    [
                        fakeStringProp
                            .ObservableForProperty(x => x.Value)
                            .Select(x => x.Value)
                            .Do(fake =>
                            {
                                foreach (EntityPropertyViewModel realProp in realProps)
                                    ((EntityPropertyStringViewModel)realProp).Value = fake;
                            })
                            .Select(_ => Unit.Default),
                    ],
                    EntityPropertyIoViewModel fakeIoProp => (IObservable<Unit>[])
                        [
                            fakeIoProp
                                .ObservableForProperty(x => x.TargetEntityName)
                                .Select(x => x.Value)
                                .Do(fake =>
                                {
                                    foreach (EntityPropertyViewModel realProp in realProps)
                                        ((EntityPropertyIoViewModel)realProp).TargetEntityName = fake;
                                })
                                .Select(_ => Unit.Default),
                            fakeIoProp
                                .ObservableForProperty(x => x.Input)
                                .Select(x => x.Value)
                                .Do(fake =>
                                {
                                    foreach (EntityPropertyViewModel realProp in realProps)
                                        ((EntityPropertyIoViewModel)realProp).Input = fake;
                                })
                                .Select(_ => Unit.Default),
                            fakeIoProp
                                .ObservableForProperty(x => x.Parameter)
                                .Select(x => x.Value)
                                .Do(fake =>
                                {
                                    foreach (EntityPropertyViewModel realProp in realProps)
                                        ((EntityPropertyIoViewModel)realProp).Parameter = fake;
                                })
                                .Select(_ => Unit.Default),
                            fakeIoProp
                                .ObservableForProperty(x => x.Delay)
                                .Select(x => x.Value)
                                .Do(fake =>
                                {
                                    foreach (EntityPropertyViewModel realProp in realProps)
                                        ((EntityPropertyIoViewModel)realProp).Delay = fake;
                                })
                                .Select(_ => Unit.Default),
                            fakeIoProp
                                .ObservableForProperty(x => x.TimesToFire)
                                .Select(x => x.Value)
                                .Do(fake =>
                                {
                                    foreach (EntityPropertyViewModel realProp in realProps)
                                        ((EntityPropertyIoViewModel)realProp).TimesToFire = fake;
                                })
                                .Select(_ => Unit.Default),
                        ],
                    _ => [],
                },
            ]
        );
    }

    private void HandleFakePropDeletion(EntityPropertyViewModel fakeProp)
    {
        // Disposal order of observables is finicky so this can get called during teardown
        if (_isDisposing)
            return;

        foreach (EntityViewModel realEnt in RealEntities)
        {
            if (
                realEnt.Properties.FirstOrDefault(prop =>
                    // Bit of a gross check, but want to avoid having to track fake -> real property mappings
                    (
                        prop.Key == fakeProp.Key
                        && fakeProp
                            is EntityPropertyStringViewModel { Value: DifferentLabel }
                                or EntityPropertyIoViewModel { TargetEntityName: DifferentLabel }
                    ) || prop.MemberwiseEquals(fakeProp)
                ) is
                { } realProp
            )
                realEnt.DeleteProperty(realProp);
        }
    }

    public void Dispose()
    {
        _isDisposing = true;
        _disposable.Dispose();
    }
}
