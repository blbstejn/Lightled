using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// ghoul
public class Enemy1 : BaseEnemy
{
    /// <summary>
    /// The hero this ghoul should prioritize attacking.
    /// Set when spawned from a blip reveal.
    /// </summary>
    public BaseHero PreferredTarget { get; set; }

    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
        SetupAI();
    }

    private void SetupAbilities()
    {
        var abilities = new List<UnitAbility>();

        // Enemy loadout: Move, Melee, Turn Left, Turn Right
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());
        abilities.Add(ScriptableObject.CreateInstance<MeleeAttackAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnLeftAbility>());
        abilities.Add(ScriptableObject.CreateInstance<TurnRightAbility>());

        unitAbilities = abilities;
    }

    private void SetupAI()
    {
        // Assign AI behavior if not set in Inspector
        if (aiBehavior == null)
        {
            aiBehavior = ScriptableObject.CreateInstance<GhoulBehavior>();
        }
    }
}
