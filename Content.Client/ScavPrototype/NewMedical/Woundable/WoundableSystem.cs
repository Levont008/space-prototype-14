using Content.Shared.ScavPrototype.NewMedical.Woundable.Systems;
using Content.Shared.ScavPrototype.NewMedical.Woundable.Components;
using Content.Shared.Input;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.ScavPrototype.NewMedical.Woundable;

public sealed class WoundableSystem : SharedWoundableSystem
{
    //Space Prototype Changes start
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<WoundableComponent>? PartStatusStartup;
    public event Action<WoundableComponent>? PartStatusUpdate;
    public event Action? PartStatusShutdown;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WoundableComponent, LocalPlayerAttachedEvent>(HandlePlayerAttached);
        SubscribeLocalEvent<WoundableComponent, LocalPlayerDetachedEvent>(HandlePlayerDetached);
        SubscribeLocalEvent<WoundableComponent, ComponentStartup>(OnPartStatusStartup);
        SubscribeLocalEvent<WoundableComponent, ComponentShutdown>(OnPartStatusShutdown);
        SubscribeNetworkEvent<TargetIntegrityChangeEvent>(OnTargetIntegrityChange);
    }

    private void HandlePlayerAttached(EntityUid uid, WoundableComponent component, LocalPlayerAttachedEvent args)
    {
        PartStatusStartup?.Invoke(component);
    }

    private void HandlePlayerDetached(EntityUid uid, WoundableComponent component, LocalPlayerDetachedEvent args)
    {
        PartStatusShutdown?.Invoke();
    }

    private void OnPartStatusStartup(EntityUid uid, WoundableComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        PartStatusStartup?.Invoke(component);
    }

    private void OnPartStatusShutdown(EntityUid uid, WoundableComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        PartStatusShutdown?.Invoke();
    }

    private void OnTargetIntegrityChange(TargetIntegrityChangeEvent args)
    {
        if (!TryGetEntity(args.Uid, out var uid)
            || !_playerManager.LocalEntity.Equals(uid)
            || !TryComp(uid, out WoundableComponent? component)
            || !args.RefreshUi)
            return;

        PartStatusUpdate?.Invoke(component);
    }
    //Space Prototype Changes end
}
