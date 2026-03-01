using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// After moving, ghouls get a free turn to face their target.
    /// </summary>
    protected override void OnMoveEnded()
    {
        base.OnMoveEnded();

        // Free turn after movement - face toward preferred target or nearest hero
        BaseHero target = PreferredTarget;
        if (target == null || !target.IsAlive || target.OccupiedTile == null)
        {
            // Find nearest hero
            target = UnitManager.Instance?.SpawnedHeroes
                .Where(h => h != null && h.IsAlive && h.OccupiedTile != null)
                .OrderBy(h => PathfindingUtils.ManhattanDistance(OccupiedTile, h.OccupiedTile))
                .FirstOrDefault();
        }

        if (target != null && OccupiedTile != null)
        {
            FacingDirection dirToTarget = PathfindingUtils.GetDirectionToward(OccupiedTile, target.OccupiedTile);
            if (dirToTarget != Facing)
            {
                TurnFree(dirToTarget);
                Debug.Log($"[Ghoul] {UnitName} free turn after move: now facing {dirToTarget} toward {target.UnitName}");
            }
        }
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
