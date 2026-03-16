using UnityEngine;

public class Hero3 : BaseHero
{
    /// <summary>
    /// Flamethrower hero cannot use Overwatch ability.
    /// </summary>
    protected override bool CanUseOverwatch => false;

    protected override void Start()
    {
        base.Start(); // runs EnsureBaseAbilities (Move, TurnLeft, TurnRight)

        // Ensure FlamethrowerAbility is present; Inspector assignment takes precedence.
        bool hasFlamethrower = false;
        foreach (var a in unitAbilities)
            if (a is FlamethrowerAbility) { hasFlamethrower = true; break; }

        if (!hasFlamethrower)
            unitAbilities.Add(ScriptableObject.CreateInstance<FlamethrowerAbility>());
    }
}
