using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// ghoul
public class Enemy1 : BaseEnemy
{
    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
    }

    private void SetupAbilities()
    {
        var abilities = new List<UnitAbility>();

        // Enemy loadout: Move, Melee, Turn Left, Turn Right
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());
        abilities.Add(ScriptableObject.CreateInstance<MeleeAttackAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnLeftAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnRightAbility>());

        // Use reflection to set the private _abilities field
        var field = typeof(BaseUnit).GetField("_abilities", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(this, abilities);
    }
}
