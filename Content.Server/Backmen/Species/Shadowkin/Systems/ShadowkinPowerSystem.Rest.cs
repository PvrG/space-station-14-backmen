using Content.Server.Backmen.Species.Shadowkin.Components;
using Content.Server.Backmen.Species.Shadowkin.Events;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs.Components;
using Content.Shared.Backmen.Species.Shadowkin.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Server.Backmen.Species.Shadowkin.Systems;

public sealed class ShadowkinRestSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ShadowkinPowerSystem _power = default!;
    [Dependency] private readonly SleepingSystem _sleeping = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowkinRestPowerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadowkinRestPowerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ShadowkinRestPowerComponent, ShadowkinRestEvent>(Rest);

        SubscribeLocalEvent<SleepingComponent, RefreshShadowkinPowerModifiersEvent>(OnRest);
        SubscribeLocalEvent<ShadowkinRestPowerComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    private void OnSleepStateChanged(Entity<ShadowkinRestPowerComponent> ent, ref SleepStateChangedEvent args)
    {
        _power.RefreshPowerModifiers(ent);
    }

    private void OnRest(Entity<SleepingComponent> ent, ref RefreshShadowkinPowerModifiersEvent args)
    {
        args.ModifySpeed(1.5f);
    }

    [ValidatePrototypeId<EntityPrototype>] private const string ShadowkinRest = "ShadowkinRest";
    private void OnInit(Entity<ShadowkinRestPowerComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent, ref ent.Comp.ShadowkinRestAction, ShadowkinRest);
    }

    private void OnShutdown(EntityUid uid, ShadowkinRestPowerComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ShadowkinRestAction);
    }

    private void Rest(EntityUid uid, ShadowkinRestPowerComponent component, ShadowkinRestEvent args)
    {
        // Need power to modify power
        if (!HasComp<ShadowkinComponent>(args.Performer) || component.ShadowkinRestAction is null)
            return;

        // Rest is a funny ability, keep it :)
        // // Don't activate abilities if handcuffed
        // if (_entity.HasComponent<HandcuffComponent>(args.Performer))
        //     return;

        SleepingComponent? sleepingComponent = null;

        // Resting
        var isSleepingByPower = HasComp<ShadowkinRestPowerUsedComponent>(args.Performer);
        if (!isSleepingByPower)
        {
            if (HasComp<StunnedComponent>(args.Performer))
                return;

            if(!_sleeping.TrySleeping(args.Performer))
                return;

            EnsureComp<ShadowkinRestPowerUsedComponent>(args.Performer);

            // Sleepy time
            _sleeping.TrySleeping(args.Performer);
            EnsureComp<ForcedSleepingComponent>(args.Performer);

            // No waking up normally (it would do nothing)
            //_actions.RemoveAction(args.Performer, new InstantAction(_prototype.Index<InstantActionPrototype>("Wake")));
            if (TryComp(args.Performer, out sleepingComponent) && sleepingComponent.WakeAction is { Valid: true })
                _actions.SetEnabled(sleepingComponent.WakeAction, false);

            _actions.SetCooldown(component.ShadowkinRestAction.Value, TimeSpan.FromSeconds(1));
            // No action cooldown
            args.Handled = false;
        }
        // Waking
        else if(isSleepingByPower && TryComp(args.Performer, out sleepingComponent))
        {
            if (sleepingComponent.WakeAction is { Valid: true })
                _actions.SetEnabled(sleepingComponent.WakeAction, true);

            // Wake up
            // Action cooldown
            RemCompDeferred<ShadowkinRestPowerUsedComponent>(args.Performer);
            RemComp<ForcedSleepingComponent>(args.Performer);
            args.Handled = _sleeping.TryWaking((args.Performer,sleepingComponent), true);
            _actions.SetCooldown(component.ShadowkinRestAction.Value, TimeSpan.FromMinutes(1));
        }
    }
}
