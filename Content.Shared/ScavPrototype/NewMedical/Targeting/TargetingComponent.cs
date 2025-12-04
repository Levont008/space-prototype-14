using Robust.Shared.GameStates;

namespace Content.Shared.ScavPrototype.NewMedical.Targeting;

/// <summary>
/// Controls entity limb targeting for actions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public TargetBodyPart Target = TargetBodyPart.Torso;

    /// <summary>
    /// What odds are there for every part targeted to be hit?
    /// </summary>
    [DataField]
    public Dictionary<TargetBodyPart, Dictionary<TargetBodyPart, float>> TargetOdds = new()
    {
        {
            TargetBodyPart.Head, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Head, 0.3f },
                { TargetBodyPart.Torso, 0.7f },
            }
        },
        {
            TargetBodyPart.Torso, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Torso, 0.8f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightArm, 0.05f },
                { TargetBodyPart.LeftArm, 0.05f },
            }
        },
        {
            TargetBodyPart.Groin, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Groin, 0.4f },
                { TargetBodyPart.Torso, 0.6f },
            }
        },
        {
            TargetBodyPart.RightArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightArm, 0.25f },
                { TargetBodyPart.Torso, 0.6f },
                { TargetBodyPart.RightHand, 0.15f },
            }
        },
        {
            TargetBodyPart.LeftArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftArm, 0.25f },
                { TargetBodyPart.Torso, 0.6f },
                { TargetBodyPart.LeftHand, 0.15f },
            }
        },
        {
            TargetBodyPart.RightHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightHand, 0.2f },
                { TargetBodyPart.Torso, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.RightArm, 0.1f },
            }
        },
        {
            TargetBodyPart.LeftHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftHand, 0.2f },
                { TargetBodyPart.Torso, 0.6f },
                { TargetBodyPart.Groin, 0.1f },
                { TargetBodyPart.LeftArm, 0.1f },
            }
        },
        {
            TargetBodyPart.RightLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightLeg, 0.25f },
                { TargetBodyPart.Torso, 0.4f },
                { TargetBodyPart.Groin, 0.25f },
                { TargetBodyPart.RightFoot, 0.1f },
            }
        },
        {
            TargetBodyPart.LeftLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftLeg, 0.25f },
                { TargetBodyPart.Torso, 0.4f },
                { TargetBodyPart.Groin, 0.25f },
                { TargetBodyPart.LeftFoot, 0.1f },
            }
        },
        {
            TargetBodyPart.RightFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightFoot, 0.2f },
                { TargetBodyPart.Torso, 0.5f },
                { TargetBodyPart.Groin, 0.2f },
                { TargetBodyPart.RightLeg, 0.1f },
            }
        },
        {
            TargetBodyPart.LeftFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftFoot, 0.2f },
                { TargetBodyPart.Torso, 0.5f },
                { TargetBodyPart.Groin, 0.2f },
                { TargetBodyPart.LeftLeg, 0.1f },
            }
        },
    };

    /// <summary>
    /// What noise does the entity play when swapping targets?
    /// </summary>
    [DataField]
    public string SwapSound = "/Audio/Effects/toggleoncombat.ogg";
}
