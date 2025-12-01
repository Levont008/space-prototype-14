using Content.Client.Gameplay;
using Content.Client.ScavPrototype.NewMedical.UserInterface.PartStatus.Widgets;
using Content.Shared.ScavPrototype.NewMedical.Targeting;
using Content.Client.ScavPrototype.NewMedical.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.Player;
using Robust.Shared.Utility;
using Robust.Client.Graphics;
using Robust.Shared.Timing;
using Content.Shared.Body.Part;
using Content.Shared.Body.Components;
using Content.Client.Body.Systems;
using Robust.Shared.Timing;

namespace Content.Client.ScavPrototype.NewMedical.UserInterface.PartStatus;

public sealed class PartStatusUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<BodySystem>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BodySystem _body = default!;
    private SpriteSystem _spriteSystem = default!;
    private BodyComponent? _bodyComponent;
    private PartStatusControl? PartStatusControl => UIManager.GetActiveUIWidgetOrNull<PartStatusControl>();

    public void OnSystemLoaded(BodySystem system)
    {
        system.PartStatusStartup += AddPartStatusControl;
        system.PartStatusShutdown += RemovePartStatusControl;
        system.PartStatusUpdate += UpdatePartStatusControl;
    }

    public void OnSystemUnloaded(BodySystem system)
    {
        system.PartStatusStartup -= AddPartStatusControl;
        system.PartStatusShutdown -= RemovePartStatusControl;
        system.PartStatusUpdate -= UpdatePartStatusControl;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (PartStatusControl != null)
        {
            PartStatusControl.SetVisible(_bodyComponent != null);

            if (_bodyComponent != null)
                PartStatusControl.SetTextures(GetBodyParts(_bodyComponent));
        }
    }

    public void AddPartStatusControl(BodyComponent component)
    {
        _bodyComponent = component;

        if (PartStatusControl != null)
        {
            PartStatusControl.SetVisible(_bodyComponent != null);
            if (_bodyComponent != null)
                PartStatusControl.SetTextures(GetBodyParts(_bodyComponent));
        }

    }

    public void RemovePartStatusControl()
    {
        if (PartStatusControl != null)
            PartStatusControl.SetVisible(false);

        _bodyComponent = null;
    }

    public void UpdatePartStatusControl(BodyComponent component)
    {
        if (PartStatusControl != null && _bodyComponent != null)
            PartStatusControl.SetTextures(GetBodyParts(_bodyComponent));
    }

    public Texture GetTexture(SpriteSpecifier specifier)
    {
        if (_spriteSystem == null)
            _spriteSystem = _entManager.System<SpriteSystem>();

        return _spriteSystem.Frame0(specifier);
    }

    /*public void GetPartStatusMessage()
    {
        if (_playerManager.LocalEntity is not { } user
            || _entManager.GetComponent<TargetingComponent>(user) is not { } targetingComponent
            || PartStatusControl == null
            || !_timing.IsFirstTimePredicted)
            return;

        var player = _entManager.GetNetEntity(user);
        _net.SendSystemNetworkMessage(new GetPartStatusEvent(player));
    }*/

     public IEnumerable<(TargetBodyPart targetPart, BodyPartComponent Component)> GetBodyParts(BodyComponent component)
    {
        foreach (var (_, part) in _body.GetBodyChildren(_playerManager.LocalEntity, component))
        {
            var targPart = _body.GetTargetBodyPart(part.PartType, part.Symmetry);

            yield return (targPart, part);
        }
    }
}
