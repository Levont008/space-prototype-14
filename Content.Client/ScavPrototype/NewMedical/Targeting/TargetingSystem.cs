using Content.Shared.Input;
using Content.Shared.ScavPrototype.NewMedical.Targeting;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.ScavPrototype.NewMedical.Targeting;
public sealed class TargetingSystem : SharedTargetingSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<TargetingComponent>? TargetingStartup;
    public event Action? TargetingShutdown;
    public event Action<TargetBodyPart>? TargetChange;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TargetingComponent, LocalPlayerAttachedEvent>(HandlePlayerAttached);
        SubscribeLocalEvent<TargetingComponent, LocalPlayerDetachedEvent>(HandlePlayerDetached);
        SubscribeLocalEvent<TargetingComponent, ComponentStartup>(OnTargetingStartup);
        SubscribeLocalEvent<TargetingComponent, ComponentShutdown>(OnTargetingShutdown);
    }

    private void HandlePlayerAttached(EntityUid uid, TargetingComponent component, LocalPlayerAttachedEvent args)
    {
        TargetingStartup?.Invoke(component);
    }

    private void HandlePlayerDetached(EntityUid uid, TargetingComponent component, LocalPlayerDetachedEvent args)
    {
        TargetingShutdown?.Invoke();
    }

    private void OnTargetingStartup(EntityUid uid, TargetingComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        TargetingStartup?.Invoke(component);
    }

    private void OnTargetingShutdown(EntityUid uid, TargetingComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        TargetingShutdown?.Invoke();
    }

    private void HandleTargetChange(ICommonSession? session, TargetBodyPart target)
    {
        if (session == null
            || session.AttachedEntity is not { } uid
            || !TryComp<TargetingComponent>(uid, out var targeting))
            return;

        TargetChange?.Invoke(target);
    }
}
