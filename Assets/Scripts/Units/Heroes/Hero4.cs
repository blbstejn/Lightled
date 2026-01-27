using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Hero4 : BaseHero
{
    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
    }

    private void SetupAbilities()
    {
        var abilities = new List<UnitAbility>();

        // Standard loadout: Melee, Move, Ranged Attack, Turn Left, Turn Right
        abilities.Add(ScriptableObject.CreateInstance<MeleeAttackAbility>());
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());
        abilities.Add(ScriptableObject.CreateInstance<RangedAttackAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnLeftAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnRightAbility>());

        unitAbilities = abilities;
    }
}
