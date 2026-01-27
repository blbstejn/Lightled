using UnityEngine;

public class Hero1 : BaseHero
{
    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
    }

    private void SetupAbilities()
    {
        // Standard loadout: Melee, Move, Ranged Attack, Turn Left, Turn Right
        var abilities = new System.Collections.Generic.List<UnitAbility>();

        // Contextual abilities
        var move = ScriptableObject.CreateInstance<MoveAbility>();
        abilities.Add(move);

        var melee = ScriptableObject.CreateInstance<MeleeAttackAbility>();
        abilities.Add(melee);

        var ranged = ScriptableObject.CreateInstance<RangedAttackAbility>();
        abilities.Add(ranged);

        // UI abilities
        var turnLeft = ScriptableObject.CreateInstance<TurnLeftAbility>();
        abilities.Add(turnLeft);

        var turnRight = ScriptableObject.CreateInstance<TurnRightAbility>();
        abilities.Add(turnRight);

        unitAbilities = abilities;
    }
}
