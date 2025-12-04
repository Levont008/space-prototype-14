using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Body.Part;

namespace Content.Shared.ScavPrototype.NewMedical.Woundable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WoundableComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<(BodyPartType type, BodyPartSymmetry symmetry, float integrity)> PartsWoundable = new List<(BodyPartType type, BodyPartSymmetry symmetry, float integrity)>(); //Потом изменить щиткод на систему боли
}
