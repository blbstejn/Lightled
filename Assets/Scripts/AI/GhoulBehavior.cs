using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI behavior for Ghouls (Enemy1).
/// Aggressively pursues heroes and attacks when adjacent.
/// Prefers attacking from behind but doesn't weight it too heavily (mindless).
/// </summary>
[CreateAssetMenu(menuName = "AI/Ghoul Behavior")]
public class GhoulBehavior : EnemyBehavior
{
    public override string BehaviorName => "Ghoul Aggression";

    public override AIAction DecideAction(BaseEnemy enemy)
    {
        if (enemy == null || !enemy.IsAlive || enemy.OccupiedTile == null)
            return AIAction.Wait();

        // Get preferred target if this ghoul was spawned from a blip
        BaseHero preferredTarget = null;
        if (enemy is Enemy1 ghoul)
        {
            preferredTarget = ghoul.PreferredTarget;
        }

        // Use preferred target if still alive, otherwise find nearest hero
        BaseHero target;
        if (preferredTarget != null && preferredTarget.IsAlive && preferredTarget.OccupiedTile != null)
        {
            target = preferredTarget;
            Debug.Log($"[AI] {enemy.UnitName}: Pursuing preferred target {target.UnitName}.");
        }
        else
        {
            target = FindBestTarget(enemy);
        }

        if (target == null)
        {
            Debug.Log($"[AI] {enemy.UnitName}: No heroes found, waiting.");
            AIDebugLog.LogWait(enemy.UnitName, "No heroes found");
            return AIAction.Wait();
        }

        int distanceToTarget = PathfindingUtils.ManhattanDistance(enemy.OccupiedTile, target.OccupiedTile);
        AIDebugLog.LogPathfinding(enemy.UnitName, target.UnitName, distanceToTarget, distanceToTarget, true);

        // Priority 1: If facing a hero and adjacent -> ATTACK
        Tile forwardTile = enemy.GetForwardTile();
        if (forwardTile != null &&
            forwardTile.OccupiedUnit is BaseHero heroInFront &&
            heroInFront.IsAlive &&
            enemy.CurrentActionPoints >= 1)
        {
            Debug.Log($"[AI] {enemy.UnitName}: Attacking {heroInFront.UnitName}!");
            AIDebugLog.LogDecision(enemy.UnitName, "ATTACK", $"Hero {heroInFront.UnitName} in front tile");
            AIDebugLog.LogAttack(enemy.UnitName, heroInFront.UnitName, forwardTile.X, forwardTile.Y);
            return AIAction.Attack(forwardTile);
        }

        // Priority 2: If adjacent to a hero but not facing -> TURN toward them
        // Prefer turning toward heroes not facing us, but it's not a strong preference
        BaseHero adjacentHero = GetBestAdjacentHero(enemy);
        if (adjacentHero != null)
        {
            AIDebugLog.LogAdjacency(enemy.UnitName, adjacentHero.UnitName);

            FacingDirection dirToHero = PathfindingUtils.GetDirectionToward(
                enemy.OccupiedTile,
                adjacentHero.OccupiedTile
            );

            if (dirToHero != enemy.Facing)
            {
                Debug.Log($"[AI] {enemy.UnitName}: Turning toward {adjacentHero.UnitName}.");
                AIDebugLog.LogDecision(enemy.UnitName, "TURN", $"Adjacent to {adjacentHero.UnitName} but facing wrong way");
                AIDebugLog.LogTurn(enemy.UnitName, enemy.Facing.ToString(), dirToHero.ToString(), $"Face {adjacentHero.UnitName}");
                return AIAction.TurnTo(dirToHero);
            }
        }

        // Priority 3: Move toward target hero
        Tile bestMove = PathfindingUtils.FindBestMoveToward(enemy, target.OccupiedTile);
        if (bestMove != null && enemy.GetMoveCost(bestMove) <= enemy.CurrentActionPoints)
        {
            Debug.Log($"[AI] {enemy.UnitName}: Moving toward {target.UnitName}.");
            int moveCost = enemy.GetMoveCost(bestMove);
            AIDebugLog.LogDecision(enemy.UnitName, "MOVE", $"Pursuing {target.UnitName}");
            AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y, bestMove.X, bestMove.Y, moveCost, $"Toward {target.UnitName}");
            return AIAction.MoveTo(bestMove);
        }

        // Cannot move (not enough AP or no valid tiles) -> wait
        Debug.Log($"[AI] {enemy.UnitName}: Cannot move or attack, waiting.");
        AIDebugLog.LogWait(enemy.UnitName, "No valid moves or attacks available");
        return AIAction.Wait();
    }

    /// <summary>
    /// Finds the best target hero. Slightly prefers heroes not facing the ghoul.
    /// </summary>
    private BaseHero FindBestTarget(BaseEnemy enemy)
    {
        var heroes = UnitManager.Instance.SpawnedHeroes
            .Where(h => h != null && h.IsAlive && h.OccupiedTile != null)
            .ToList();

        if (heroes.Count == 0)
            return null;

        // Score each hero: lower is better (closer distance, not facing = bonus)
        BaseHero bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (var hero in heroes)
        {
            int distance = PathfindingUtils.ManhattanDistance(enemy.OccupiedTile, hero.OccupiedTile);
            float score = distance;

            // Small bonus for heroes not facing us (ghouls are mindless, so only slight preference)
            // -0.3 means if two heroes are same distance, prefer the one not looking
            if (!PathfindingUtils.IsHeroFacingToward(hero, enemy.OccupiedTile))
            {
                score -= 0.3f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = hero;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Gets the best adjacent hero to attack. Slightly prefers heroes not facing the ghoul.
    /// </summary>
    private BaseHero GetBestAdjacentHero(BaseEnemy enemy)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return null;

        var neighbors = GridManager.Instance.GetNeighbours(enemy.OccupiedTile);
        var adjacentHeroes = new List<BaseHero>();

        foreach (var tile in neighbors)
        {
            if (tile?.OccupiedUnit is BaseHero hero && hero.IsAlive)
                adjacentHeroes.Add(hero);
        }

        if (adjacentHeroes.Count == 0)
            return null;

        if (adjacentHeroes.Count == 1)
            return adjacentHeroes[0];

        // Multiple adjacent heroes - slight preference for ones not facing us
        return adjacentHeroes
            .OrderBy(h => PathfindingUtils.IsHeroFacingToward(h, enemy.OccupiedTile) ? 1 : 0)
            .First();
    }
}
