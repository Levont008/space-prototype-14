using Content.Shared.ScavPrototype.NewMedical.Woundable.Components;
using Content.Shared.ScavPrototype.NewMedical.Targeting.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Shared.ScavPrototype.NewMedical.Woundable.Systems;
public abstract class SharedWoundableSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(WoundableInit);
    }

    private void WoundableInit(Entity<WoundableComponent> ent, ref ComponentInit _)
    {
        UpdateWoundable(ent.Owner);
    }

    public void ChangeIntegrity(EntityUid uid, float totalDamage)
    {
        if(!TryComp<WoundablePartComponent>(uid, out var component))
            return;

        var integrityChanged =  Math.Clamp(component.Integrity - totalDamage / component.MaxDamage, 0, 1f);
        component.Integrity = integrityChanged;

        if (!TryComp<BodyPartComponent>(uid, out var bodyPart)
            || bodyPart.Body is not { } bodyUid)
            return;

        UpdateWoundable(bodyUid);
    }

    public float GetMaxDamage(EntityUid uid)
    {
        if(!TryComp<WoundablePartComponent>(uid, out var component))
            return 0;

        return component.MaxDamage;
    }

    public virtual void UpdateWoundable(EntityUid uid)
    {
        if (!TryComp<BodyComponent>(uid, out var body)
            || body.RootContainer == null
            || !TryComp<WoundableComponent>(uid, out var woundable))
            return;

        var _partsWoundable = new List<(BodyPartType type, BodyPartSymmetry symmetry, float integrity)>();

        foreach (var (partUid, partComp) in _body.GetBodyChildren(uid, body))
        {
            if (!TryComp<WoundablePartComponent>(partUid, out var partWoundable))
                continue;

            _partsWoundable.Add((partComp.PartType, partComp.Symmetry, partWoundable.Integrity));
        }

        woundable.PartsWoundable = _partsWoundable;
    }
}
