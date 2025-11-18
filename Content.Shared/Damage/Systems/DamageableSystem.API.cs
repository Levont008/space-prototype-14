using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

//SpacePrototype Changes
using System.Linq;
using Content.Shared.ScavPrototype.NewMedical.Damage;
using Content.Shared.ScavPrototype.NewMedical.Targeting;
using Content.Shared.ScavPrototype.NewMedical.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Random;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    /// <summary>
    ///     Directly sets the damage specifier of a damageable component.
    /// </summary>
    /// <remarks>
    ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
    ///     event is raised.
    /// </remarks>
    public void SetDamage(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Damage = damage;

        OnEntityDamageChanged((ent, ent.Comp));
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        TargetBodyPart? targetPart = null,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = false
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        return !TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers);
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        out DamageSpecifier newDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        TargetBodyPart? targetPart = null,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = false
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        newDamage = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers);
        return !damage.Empty;
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        TargetBodyPart? targetPart = null,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = false
    )
    {
        var damageDone = new DamageSpecifier();

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damageDone;

        if (damage.Empty)
            return damageDone;

        var before = new BeforeDamageChangedEvent(damage, origin);
        RaiseLocalEvent(ent, ref before);

        if (before.Cancelled)
            return damageDone;

        //SpacePrototype Changes start

        // For entities with a body, route damage through body parts and then sum it up
            if (_bodyQuery.TryGetComponent(ent, out var body))
            {
                if (targetPart == null) targetPart = TargetBodyPart.All;

                var appliedDamage = ApplyDamageToBodyParts(ent, damage, origin, ignoreResistances,
                    interruptsDoAfters, targetPart, splitDamage, canMiss);

                if (appliedDamage == null) return damageDone;

                return appliedDamage;
            }


        // Apply resistances
        if (!ignoreResistances)
        {
            if (
                ent.Comp.DamageModifierSetId != null &&
                _prototypeManager.Resolve(ent.Comp.DamageModifierSetId, out var modifierSet)
            )
                damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);

            // TODO DAMAGE
            // byref struct event.
            var ev = new DamageModifyEvent(damage, origin);
            RaiseLocalEvent(ent, ev);
            damage = ev.Damage;

            if (damage.Empty)
                return damageDone;
        }

        if (!ignoreGlobalModifiers)
            damage = ApplyUniversalAllModifiers(damage);


        damageDone.DamageDict.EnsureCapacity(damage.DamageDict.Count);

        var dict = ent.Comp.Damage.DamageDict;
        foreach (var (type, value) in damage.DamageDict)
        {
            // CollectionsMarshal my beloved.
            if (!dict.TryGetValue(type, out var oldValue))
                continue;

            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            dict[type] = newValue;
            damageDone.DamageDict[type] = newValue - oldValue;
        }

        if (!damageDone.Empty)
            OnEntityDamageChanged((ent, ent.Comp), damageDone, interruptsDoAfters, origin);

        return damageDone;
    }

    /// <summary>
    /// Applies the two universal "All" modifiers, if set.
    /// Individual damage source modifiers are set in their respective code.
    /// </summary>
    /// <param name="damage">The damage to be changed.</param>
    public DamageSpecifier ApplyUniversalAllModifiers(DamageSpecifier damage)
    {
        // Checks for changes first since they're unlikely in normal play.
        if (
            MathHelper.CloseToPercent(UniversalAllDamageModifier, 1f) &&
            MathHelper.CloseToPercent(UniversalAllHealModifier, 1f)
        )
            return damage;

        foreach (var (key, value) in damage.DamageDict)
        {
            if (value == 0)
                continue;

            if (value > 0)
            {
                damage.DamageDict[key] *= UniversalAllDamageModifier;

                continue;
            }

            if (value < 0)
                damage.DamageDict[key] *= UniversalAllHealModifier;
        }

        return damage;
    }

    public void ClearAllDamage(Entity<DamageableComponent?> ent)
    {
        SetAllDamage(ent, FixedPoint2.Zero);
    }

    /// <summary>
    ///     Sets all damage types supported by a <see cref="Components.DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    ///     Does nothing If the given damage value is negative.
    /// </remarks>
    public void SetAllDamage(Entity<DamageableComponent?> ent, FixedPoint2 newValue)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (newValue < 0)
            return;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            ent.Comp.Damage.DamageDict[type] = newValue;
        }

        // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
        // empty damage delta.
        OnEntityDamageChanged((ent, ent.Comp), new DamageSpecifier());
    }

    /// <summary>
    /// Set's the damage modifier set prototype for this entity.
    /// </summary>
    /// <param name="ent">The entity we're setting the modifier set of.</param>
    /// <param name="damageModifierSetId">The prototype we're setting.</param>
    public void SetDamageModifierSetId(Entity<DamageableComponent?> ent, ProtoId<DamageModifierSetPrototype>? damageModifierSetId)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.DamageModifierSetId = damageModifierSetId;

        Dirty(ent);
    }

    //SpacePrototype Changes

    private DamageSpecifier? ApplyDamageToBodyParts(
            EntityUid uid,
            DamageSpecifier damage,
            EntityUid? origin,
            bool ignoreResistances,
            bool interruptsDoAfters,
            TargetBodyPart? targetPart,
            SplitDamageBehavior splitDamageBehavior = SplitDamageBehavior.Split,
            bool canMiss = false,
            float partMultiplier = 1.00f)
        {
            DamageSpecifier? totalAppliedDamage = null;
            var adjustedDamage = damage * partMultiplier;
            // This cursed shitcode lets us know if the target part is a power of 2
            // therefore having multiple parts targeted.
            if (targetPart != null
                && targetPart != 0 && (targetPart & (targetPart - 1)) != 0)
            {
                // Extract only the body parts that are targeted in the bitmask
                var targetedBodyParts = new List<(EntityUid Id,
                    BodyPartComponent Component,
                    DamageableComponent Damageable)>();

                // Get only the primitive flags (powers of 2) - these are the actual individual body parts
                var primitiveFlags = Enum.GetValues<TargetBodyPart>()
                    .Where(flag => flag != 0 && (flag & (flag - 1)) == 0) // Power of 2 check
                    .ToList();

                foreach (var flag in primitiveFlags)
                {
                    // Check if this specific flag is set in our targetPart bitmask
                    if (targetPart.Value.HasFlag(flag))
                    {
                        var query = _body.ConvertTargetBodyPart(flag);
                        var parts = _body.GetBodyChildrenOfTypeWithComponent<DamageableComponent>(uid, query.Type,
                            symmetry: query.Symmetry).ToList();

                        if (parts.Count > 0)
                            targetedBodyParts.AddRange(parts);
                    }
                }

                // If we couldn't find any of the targeted parts, fall back to all body parts
                if (targetedBodyParts.Count == 0)
                {
                    var query = _body.GetBodyChildrenWithComponent<DamageableComponent>(uid).ToList();
                    if (query.Count > 0)
                        targetedBodyParts = query;
                    else
                        return null;
                }

                var damagePerPart = ApplySplitDamageBehaviors(splitDamageBehavior, adjustedDamage, targetedBodyParts);
                var appliedDamage = new DamageSpecifier();
                var surplusHealing = new DamageSpecifier();
                foreach (var (partId, _, partDamageable) in targetedBodyParts)
                {
                    var modifiedDamage = damagePerPart + surplusHealing;

                    // Apply damage to this part
                    var partDamageResult = ChangeDamage(partId, modifiedDamage, ignoreResistances,
                        interruptsDoAfters, origin);
                    UpdateParentDamageFromBodyParts(partId, adjustedDamage, interruptsDoAfters, origin);

                    if (partDamageResult != null && !partDamageResult.Empty)
                    {
                        appliedDamage += partDamageResult;

                        /*
                            Why this ugly shitcode? Its so that we can track chems and other sorts of healing surpluses.
                            Assume you're fighting in a spaced area. Your chest has 30 damage, and every other part
                            is getting 0.5 per tick. Your chems will only be 1/11th as effective, so we take the surplus
                            healing and pass it along parts. That way a chem that would heal you for 75 brute would truly
                            heal the 75 brute per tick, and not some weird shit like 6.8 per tick.
                        */
                        foreach (var (type, damageFromDict) in modifiedDamage.DamageDict)
                        {
                            if (damageFromDict >= 0
                                || !partDamageResult.DamageDict.TryGetValue(type, out var damageFromResult)
                                || damageFromResult > 0)
                                continue;

                            // If the damage from the dict plus the surplus healing is equal to the damage from the result,
                            // we can safely set the surplus healing to 0, as that means we consumed all of it.
                            if (damageFromDict >= damageFromResult)
                            {
                                surplusHealing.DamageDict[type] = FixedPoint2.Zero;
                            }
                            else
                            {
                                if (surplusHealing.DamageDict.TryGetValue(type, out var _))
                                    surplusHealing.DamageDict[type] = damageFromDict - damageFromResult;
                                else
                                    surplusHealing.DamageDict.TryAdd(type, damageFromDict - damageFromResult);
                            }
                        }
                    }
                }

                totalAppliedDamage = appliedDamage;
            }
            else
            {
                // Target a specific body part
                TargetBodyPart? target;
                var totalDamage = damage.GetTotal();

                if (totalDamage <= 0 || !canMiss) // Whoops i think i fucked up damage here.
                    target = _body.GetTargetBodyPart(uid, origin, targetPart);
                else
                    target = _body.GetRandomBodyPart(uid, origin, targetPart);

                var (partType, symmetry) = _body.ConvertTargetBodyPart(target);
                var possibleTargets = _body.GetBodyChildrenOfType(uid, partType, symmetry: symmetry).ToList();

                if (possibleTargets.Count == 0)
                {
                    if (totalDamage <= 0)
                        return null;

                    possibleTargets = _body.GetBodyChildren(uid).ToList();
                }

                // No body parts at all?
                if (possibleTargets.Count == 0)
                    return null;

                var chosenTarget = _random.PickAndTake(possibleTargets);

                if (!_damageableQuery.TryComp(chosenTarget.Id, out var partDamageable))
                    return null;

                totalAppliedDamage = ChangeDamage(chosenTarget.Id, adjustedDamage, ignoreResistances,
                    interruptsDoAfters, origin);
                UpdateParentDamageFromBodyParts(chosenTarget.Id, adjustedDamage, interruptsDoAfters, origin);
            }

            return totalAppliedDamage;
        }

    public DamageSpecifier ApplySplitDamageBehaviors(SplitDamageBehavior splitDamageBehavior,
            DamageSpecifier damage,
            List<(EntityUid Id, BodyPartComponent Component, DamageableComponent Damageable)> parts)
        {
            var newDamage = new DamageSpecifier(damage);
            switch (splitDamageBehavior)
            {
                case SplitDamageBehavior.None:
                    return newDamage;
                case SplitDamageBehavior.Split:
                    return newDamage / parts.Count;
                case SplitDamageBehavior.SplitEnsureAllDamaged:
                    var damagedParts = parts.Where(part =>
                        part.Damageable.TotalDamage > FixedPoint2.Zero).ToList();

                    parts.Clear();
                    parts.AddRange(damagedParts);

                    goto case SplitDamageBehavior.SplitEnsureAll;
                case SplitDamageBehavior.SplitEnsureAllOrganic:
                    var organicParts = parts.Where(part =>
                        part.Component.PartComposition == BodyPartComposition.Organic).ToList();

                    parts.Clear();
                    parts.AddRange(organicParts);

                    goto case SplitDamageBehavior.SplitEnsureAll;
                case SplitDamageBehavior.SplitEnsureAllDamagedAndOrganic:
                    var compatableParts = parts.Where(part =>
                        part.Damageable.TotalDamage > FixedPoint2.Zero &&
                        part.Component.PartComposition == BodyPartComposition.Organic).ToList();

                    parts.Clear();
                    parts.AddRange(compatableParts);
                    goto case SplitDamageBehavior.SplitEnsureAll;
                case SplitDamageBehavior.SplitEnsureAll:
                    foreach (var (type, val) in newDamage.DamageDict)
                    {
                        if (val > 0)
                        {
                            if (parts.Count > 0)
                                newDamage.DamageDict[type] = val / parts.Count;
                            else
                                newDamage.DamageDict[type] = FixedPoint2.Zero;
                        }
                        else if (val < 0)
                        {
                            var count = 0;

                            foreach (var (id, _, damageable) in parts)
                                if (damageable.Damage.DamageDict.TryGetValue(type, out var currentDamage)
                                    && currentDamage > 0)
                                    count++;

                            if (count > 0)
                                newDamage.DamageDict[type] = val / count;
                            else
                                newDamage.DamageDict[type] = FixedPoint2.Zero;
                        }
                    }
                    // We sort the parts to ensure that surplus damage gets passed from least to most damaged.
                    parts.Sort((a, b) => a.Damageable.TotalDamage.CompareTo(b.Damageable.TotalDamage));
                    return newDamage;
                default:
                    return damage;
            }
        }

    /// <summary>
    /// Updates the parent entity's damage values by summing damage from all body parts.
    /// Should be called after damage is applied to any body part.
    /// </summary>
    /// <param name="bodyPartUid">The body part that received damage</param>
    /// <param name="appliedDamage">The damage that was applied to the body part</param>
    /// <param name="interruptsDoAfters">Whether this damage change interrupts do-afters</param>
    /// <param name="origin">The entity that caused the damage</param>
    /// <returns>True if parent damage was updated, false otherwise</returns>
    private bool UpdateParentDamageFromBodyParts(
            EntityUid bodyPartUid,
            DamageSpecifier? appliedDamage,
            bool interruptsDoAfters,
            EntityUid? origin,
            BodyPartComponent? bodyPart = null)
        {
            // Check if this is a body part and get the parent body
            if (!Resolve(bodyPartUid, ref bodyPart, logMissing: false)
                || bodyPart.Body is not { } body
                || !TryComp(body, out DamageableComponent? parentDamageable))
                return false;

            // Reset the parent's damage values
            foreach (var type in parentDamageable.Damage.DamageDict.Keys.ToList())
                parentDamageable.Damage.DamageDict[type] = FixedPoint2.Zero;

            // Sum up damage from all body parts
            foreach (var (partId, _) in _body.GetBodyChildren(body))
            {
                if (!_damageableQuery.TryComp(partId, out var partDamageable))
                    continue;

                foreach (var (type, value) in partDamageable.Damage.DamageDict)
                {
                    if (value == 0)
                        continue;

                    if (parentDamageable.Damage.DamageDict.TryGetValue(type, out var existing))
                        parentDamageable.Damage.DamageDict[type] = existing + value;
                }
            }

            // Raise the damage changed event on the parent
            OnEntityDamageChanged((body, parentDamageable), appliedDamage, interruptsDoAfters, origin);

            return true;
        }
}
