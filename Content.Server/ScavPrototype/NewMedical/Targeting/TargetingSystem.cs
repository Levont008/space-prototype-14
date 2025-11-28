using Content.Shared.ScavPrototype.NewMedical.Targeting;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;

namespace Content.Server.ScavPrototype.NewMedical.Targeting;
public sealed class TargetingSystem : SharedTargetingSystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TargetChangeEvent>(OnTargetChange);
    }

    private void OnTargetChange(TargetChangeEvent message, EntitySessionEventArgs args)
    {
        if (!TryComp<TargetingComponent>(GetEntity(message.Uid), out var target))
            return;

        target.Target = message.BodyPart;
        Dirty(GetEntity(message.Uid), target);
    }
}
