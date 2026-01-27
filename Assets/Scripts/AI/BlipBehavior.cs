using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI behavior for Blips (Enemy2).
/// Tactically avoids hero line of sight and positions for ambushes.
/// Reveals (spawns ghouls) when adjacent to a hero while hidden.
/// </summary>
[CreateAssetMenu(menuName = "AI/Blip Behavior")]
public class BlipBehavior : EnemyBehavior
{
    public override string BehaviorName => "Blip Ambush";

    [Header("Behavior Settings")]
    [Tooltip("Ideal distance from heroes to set up ambush (tiles)")]
    [SerializeField] private int idealAmbushDistance = 2;

    public override AIAction DecideAction(BaseEnemy enemy)
    {
        if (enemy == null || !enemy.IsAlive || enemy.OccupiedTile == null)
            return AIAction.Wait();

        // Blips that are revealed shouldn't use this behavior
        if (enemy is Enemy2 blip && blip.IsRevealed)
        {
            Debug.Log($"[AI] {enemy.UnitName}: Already revealed, waiting.");
            AIDebugLog.LogWait(enemy.UnitName, "Already revealed");
            return AIAction.Wait();
        }

        // Get combined hero LOS
        HashSet<Tile> heroLOS = PathfindingUtils.GetCombinedHeroLOS();

        // Find nearest hero
        BaseHero target = PathfindingUtils.FindNearestHero(enemy);
        if (target == null)
        {
            Debug.Log($"[AI] {enemy.UnitName}: No heroes found, waiting.");
            AIDebugLog.LogWait(enemy.UnitName, "No heroes found");
            return AIAction.Wait();
        }

        bool isCurrentlyVisible = heroLOS.Contains(enemy.OccupiedTile);
        bool isAdjacentToHero = PathfindingUtils.IsAdjacentToAnyHero(enemy);

        AIDebugLog.LogLOSCheck(enemy.UnitName, isCurrentlyVisible, heroLOS.Count);

        // Priority 1: AMBUSH - If adjacent to a hero AND not visible -> REVEAL!
        if (isAdjacentToHero && !isCurrentlyVisible)
        {
            Debug.Log($"[AI] {enemy.UnitName}: AMBUSH! Revealing next to hero!");
            AIDebugLog.LogDecision(enemy.UnitName, "REVEAL", "Adjacent to hero while hidden - AMBUSH!");
            return AIAction.RevealBlip();
        }

        // Priority 2: If currently visible, try to escape to safety
        if (isCurrentlyVisible)
        {
            var safeMoves = PathfindingUtils.GetSafeMoves(enemy, heroLOS);
            AIDebugLog.LogAvailableMoves(enemy.UnitName, safeMoves.Count + 1, safeMoves.Count);

            if (safeMoves.Count > 0)
            {
                // Pick any safe tile (prioritize ones closer to heroes for future ambush)
                Tile escapeTile = safeMoves
                    .OrderBy(t => PathfindingUtils.ManhattanDistance(t, target.OccupiedTile))
                    .FirstOrDefault();

                if (escapeTile != null && enemy.GetMoveCost(escapeTile) <= enemy.CurrentActionPoints)
                {
                    Debug.Log($"[AI] {enemy.UnitName}: Spotted! Escaping to safety.");
                    int moveCost = enemy.GetMoveCost(escapeTile);
                    AIDebugLog.LogDecision(enemy.UnitName, "ESCAPE", "Spotted by heroes - fleeing to safety");
                    AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y, escapeTile.X, escapeTile.Y, moveCost, "Escape to shadow");
                    return AIAction.MoveTo(escapeTile);
                }
            }

            // No safe escape - wait (will likely be revealed by hero LOS check)
            Debug.Log($"[AI] {enemy.UnitName}: Spotted with no escape, waiting.");
            AIDebugLog.LogWait(enemy.UnitName, "Spotted with no escape routes");
            return AIAction.Wait();
        }

        // Priority 3: Move closer to heroes while staying hidden
        int distanceToTarget = PathfindingUtils.ManhattanDistance(enemy.OccupiedTile, target.OccupiedTile);
        AIDebugLog.LogPathfinding(enemy.UnitName, target.UnitName, distanceToTarget, distanceToTarget, true);

        // If we're at a good ambush distance, wait for opportunity
        if (distanceToTarget <= idealAmbushDistance)
        {
            Debug.Log($"[AI] {enemy.UnitName}: In ambush position ({distanceToTarget} tiles from hero), waiting.");
            AIDebugLog.LogDecision(enemy.UnitName, "HOLD", $"In ambush position ({distanceToTarget} tiles)");
            AIDebugLog.LogWait(enemy.UnitName, $"Holding ambush position at distance {distanceToTarget}");
            return AIAction.Wait();
        }

        // Try to move closer while staying hidden
        Tile bestMove = PathfindingUtils.FindBestSafeMoveToward(enemy, target.OccupiedTile, heroLOS);
        if (bestMove != null && enemy.GetMoveCost(bestMove) <= enemy.CurrentActionPoints)
        {
            Debug.Log($"[AI] {enemy.UnitName}: Sneaking closer to {target.UnitName}.");
            int moveCost = enemy.GetMoveCost(bestMove);
            AIDebugLog.LogDecision(enemy.UnitName, "SNEAK", "Moving closer while staying hidden");
            AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y, bestMove.X, bestMove.Y, moveCost, $"Sneak toward {target.UnitName}");
            return AIAction.MoveTo(bestMove);
        }

        // No safe moves toward target - try any safe move
        var anySafeMoves = PathfindingUtils.GetSafeMoves(enemy, heroLOS);
        AIDebugLog.LogAvailableMoves(enemy.UnitName, anySafeMoves.Count, anySafeMoves.Count);

        if (anySafeMoves.Count > 0)
        {
            // Move randomly among safe tiles
            Tile randomSafe = anySafeMoves[Random.Range(0, anySafeMoves.Count)];
            if (enemy.GetMoveCost(randomSafe) <= enemy.CurrentActionPoints)
            {
                Debug.Log($"[AI] {enemy.UnitName}: No direct path, moving to safe tile.");
                int moveCost = enemy.GetMoveCost(randomSafe);
                AIDebugLog.LogDecision(enemy.UnitName, "REPOSITION", "No direct path, moving to random safe tile");
                AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y, randomSafe.X, randomSafe.Y, moveCost, "Random safe reposition");
                return AIAction.MoveTo(randomSafe);
            }
        }

        // No options - wait
        Debug.Log($"[AI] {enemy.UnitName}: No safe moves available, waiting.");
        AIDebugLog.LogWait(enemy.UnitName, "No safe moves available");
        return AIAction.Wait();
    }
}
