using Content.Shared.Body.Systems;

//Space Prototype Change
using Content.Shared.Input;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Shared.Body.Components;

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    //Space Prototype Changes start
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<BodyComponent>? PartStatusStartup;
    public event Action<BodyComponent>? PartStatusUpdate;
    public event Action? PartStatusShutdown;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, LocalPlayerAttachedEvent>(HandlePlayerAttached);
        SubscribeLocalEvent<BodyComponent, LocalPlayerDetachedEvent>(HandlePlayerDetached);
        SubscribeLocalEvent<BodyComponent, ComponentStartup>(OnTargetingStartup);
        SubscribeLocalEvent<BodyComponent, ComponentShutdown>(OnTargetingShutdown);
        SubscribeNetworkEvent<TargetIntegrityChangeEvent>(OnTargetIntegrityChange);
    }

    private void HandlePlayerAttached(EntityUid uid, BodyComponent component, LocalPlayerAttachedEvent args)
    {
        PartStatusStartup?.Invoke(component);
    }

    private void HandlePlayerDetached(EntityUid uid, BodyComponent component, LocalPlayerDetachedEvent args)
    {
        PartStatusShutdown?.Invoke();
    }

    private void OnTargetingStartup(EntityUid uid, BodyComponent component, ComponentStartup args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        PartStatusStartup?.Invoke(component);
    }

    private void OnTargetingShutdown(EntityUid uid, BodyComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalEntity != uid)
            return;

        PartStatusShutdown?.Invoke();
    }

    private void OnTargetIntegrityChange(TargetIntegrityChangeEvent args)
    {
        if (!TryGetEntity(args.Uid, out var uid)
            || !_playerManager.LocalEntity.Equals(uid)
            || !TryComp(uid, out BodyComponent? component)
            || !args.RefreshUi)
            return;

        PartStatusUpdate?.Invoke(component);
    }
    //Space Prototype Changes end
}
