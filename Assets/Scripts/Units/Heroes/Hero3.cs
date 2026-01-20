using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Hero3 : BaseHero
{
    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
    }

    private void SetupAbilities()
    {
        var abilities = new List<UnitAbility>();

        // Flamethrower loadout: Melee, Move, FLAMETHROWER (12 charges), Turn Left, Turn Right
        abilities.Add(ScriptableObject.CreateInstance<MeleeAttackAbility>());
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());

        // Create and initialize flamethrower with 12 charges
        var flamethrower = ScriptableObject.CreateInstance<FlamethrowerAbility>();
        flamethrower.Initialize();
        abilities.Add(flamethrower);

        abilities.Add(ScriptableObject.CreateInstance<TurnLeftAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnRightAbility>());

        // Use reflection to set the private _abilities field
        var field = typeof(BaseUnit).GetField("_abilities", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(this, abilities);
    }
}
