using Content.Shared.ScavPrototype.NewMedical.Woundable.Components;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;
using Content.Shared.ScavPrototype.NewMedical.Woundable.Systems;

namespace Content.Server.ScavPrototype.NewMedical.Woundable;
public sealed class WoundableSystem : SharedWoundableSystem
{
    public override void UpdateWoundable(EntityUid uid)
    {
        base.UpdateWoundable(uid);

        if (TryComp<WoundableComponent>(uid, out var woundable))
        {
            Dirty(uid, woundable);
            RaiseNetworkEvent(new TargetIntegrityChangeEvent(GetNetEntity(uid)), uid);
        }
    }
}
