using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ScavPrototype.NewMedical.Woundable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundablePartComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxDamage = 100f;

    [DataField, AutoNetworkedField, ViewVariables]
    public float Integrity = 1f;
}
