using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI behavior for Blips (Enemy2).
/// Aggression scales with the number of active blips on the field.
/// More blips = bolder, less hiding, more rushing toward heroes.
/// Reveals (spawns ghouls) when adjacent to a hero.
/// </summary>
[CreateAssetMenu(menuName = "AI/Blip Behavior")]
public class BlipBehavior : EnemyBehavior
{
    public override string BehaviorName => "Blip Ambush";

    [Header("Behavior Settings")]
    [Tooltip("Ideal distance from heroes to set up ambush (tiles)")]
    [SerializeField] private int idealAmbushDistance = 2;

    [Header("Aggression Scaling")]
    [Tooltip("Number of active blips at which aggression is fully maxed out")]
    [SerializeField] private int fullAggressionBlipCount = 3;

    [Tooltip("Base aggression even with a single blip (0-1). Higher = braver lone blips.")]
    [SerializeField, Range(0f, 1f)] private float baseAggression = 0.25f;

    [Tooltip("Aggression threshold to charge while visible instead of fleeing (0-1)")]
    [SerializeField, Range(0f, 1f)] private float chargeWhileVisibleThreshold = 0.4f;

    [Tooltip("Aggression threshold to ignore hero facing direction for charges (0-1)")]
    [SerializeField, Range(0f, 1f)] private float ignoreFacingThreshold = 0.5f;

    [Tooltip("Aggression threshold to take risky LOS moves even when hero IS looking (0-1)")]
    [SerializeField, Range(0f, 1f)] private float riskyMoveThreshold = 0.3f;

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

        // --- Calculate aggression from active blip count ---
        int activeBlipCount = UnitManager.Instance.SpawnedEnemies
            .Count(e => e is Enemy2 e2 && e2.IsAlive && !e2.IsRevealed);
        float aggressionFactor = Mathf.Clamp01(
            baseAggression + (1f - baseAggression) * (activeBlipCount / (float)fullAggressionBlipCount));

        // Layer 2: Director AI modulates aggression based on player pressure
        if (DirectorAI.Instance != null)
            aggressionFactor = Mathf.Clamp01(aggressionFactor * DirectorAI.Instance.GetAggressionModifier());

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

        // Compute BFS distance map from target — accounts for walls and obstacles
        var distMap = PathfindingUtils.ComputeDistanceMap(target.OccupiedTile);

        bool isCurrentlyVisible = heroLOS.Contains(enemy.OccupiedTile);
        bool isAdjacentToHero = PathfindingUtils.IsAdjacentToAnyHero(enemy);
        int distanceToTarget = PathfindingUtils.GetPathDistance(enemy.OccupiedTile, distMap);
        bool heroFacingUs = PathfindingUtils.IsHeroFacingToward(target, enemy.OccupiedTile);
        Tile prevTile = enemy.PreviousTile;

        AIDebugLog.LogLOSCheck(enemy.UnitName, isCurrentlyVisible, heroLOS.Count);
        Debug.Log($"[AI] {enemy.UnitName}: Aggression={aggressionFactor:F2} (blips={activeBlipCount})");

        // Priority 1: AMBUSH - If adjacent to a hero -> REVEAL
        if (isAdjacentToHero)
        {
            Debug.Log($"[AI] {enemy.UnitName}: AMBUSH! Revealing next to hero!");
            AIDebugLog.LogDecision(enemy.UnitName, "REVEAL", "Adjacent to hero - AMBUSH!");
            return AIAction.RevealBlip();
        }

        // Priority 2: If currently visible - decide fight or flight based on aggression
        if (isCurrentlyVisible)
        {
            // HIGH AGGRESSION: Charge toward the hero instead of fleeing
            if (aggressionFactor >= chargeWhileVisibleThreshold)
            {
                Tile chargeMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
                if (chargeMove != null && enemy.GetMoveCost(chargeMove) <= enemy.CurrentActionPoints)
                {
                    Debug.Log($"[AI] {enemy.UnitName}: Spotted but aggressive (aggro={aggressionFactor:F2}) - charging hero!");
                    int moveCost = enemy.GetMoveCost(chargeMove);
                    AIDebugLog.LogDecision(enemy.UnitName, "CHARGE_VISIBLE",
                        $"Spotted but {activeBlipCount} blips active - charging instead of fleeing!");
                    AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                        chargeMove.X, chargeMove.Y, moveCost, $"Aggressive charge toward {target.UnitName}");
                    return AIAction.MoveTo(chargeMove);
                }
            }

            // LOW AGGRESSION: Try to escape to safety
            var safeMoves = PathfindingUtils.GetSafeMoves(enemy, heroLOS);
            if (prevTile != null)
                safeMoves.RemoveAll(t => t == prevTile);
            AIDebugLog.LogAvailableMoves(enemy.UnitName, safeMoves.Count + 1, safeMoves.Count);

            if (safeMoves.Count > 0)
            {
                Tile escapeTile = safeMoves
                    .OrderBy(t => PathfindingUtils.GetPathDistance(t, distMap))
                    .FirstOrDefault();

                if (escapeTile != null && enemy.GetMoveCost(escapeTile) <= enemy.CurrentActionPoints)
                {
                    Debug.Log($"[AI] {enemy.UnitName}: Spotted with low aggression - escaping.");
                    int moveCost = enemy.GetMoveCost(escapeTile);
                    AIDebugLog.LogDecision(enemy.UnitName, "ESCAPE", "Spotted with low aggression - fleeing to safety");
                    AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                        escapeTile.X, escapeTile.Y, moveCost, "Escape to shadow");
                    return AIAction.MoveTo(escapeTile);
                }
            }

            // No safe escape - move toward hero anyway (nothing to lose)
            Tile desperateMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
            if (desperateMove != null && enemy.GetMoveCost(desperateMove) <= enemy.CurrentActionPoints)
            {
                Debug.Log($"[AI] {enemy.UnitName}: Spotted with no escape - charging out of desperation!");
                int moveCost = enemy.GetMoveCost(desperateMove);
                AIDebugLog.LogDecision(enemy.UnitName, "DESPERATE_CHARGE", "No escape routes - charging hero");
                AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                    desperateMove.X, desperateMove.Y, moveCost, $"Desperate charge toward {target.UnitName}");
                return AIAction.MoveTo(desperateMove);
            }

            Debug.Log($"[AI] {enemy.UnitName}: Spotted with no moves at all, waiting.");
            AIDebugLog.LogWait(enemy.UnitName, "Spotted with no moves available");
            return AIAction.Wait();
        }

        // --- From here on, the blip is hidden ---

        AIDebugLog.LogPathfinding(enemy.UnitName, target.UnitName, distanceToTarget, distanceToTarget, true);

        // Priority 3: CHARGE - close range OR high aggression expands charge range
        // With high aggression, charge from further away and ignore hero facing
        int effectiveChargeRange = idealAmbushDistance + Mathf.RoundToInt(aggressionFactor * 4);
        bool willingToChargeFacing = aggressionFactor >= ignoreFacingThreshold;

        if (distanceToTarget <= effectiveChargeRange && (!heroFacingUs || willingToChargeFacing))
        {
            Tile aggressiveMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
            if (aggressiveMove != null && enemy.GetMoveCost(aggressiveMove) <= enemy.CurrentActionPoints)
            {
                string reason = heroFacingUs
                    ? $"Hero facing us but aggro={aggressionFactor:F2} - charging anyway!"
                    : $"Hero not facing us at distance {distanceToTarget} - closing in!";
                Debug.Log($"[AI] {enemy.UnitName}: {reason}");
                int moveCost = enemy.GetMoveCost(aggressiveMove);
                AIDebugLog.LogDecision(enemy.UnitName, "CHARGE", reason);
                AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                    aggressiveMove.X, aggressiveMove.Y, moveCost, $"Charge toward {target.UnitName}");
                return AIAction.MoveTo(aggressiveMove);
            }
        }

        // Priority 4: Move closer - prefer direct moves over safe-only when aggressive
        // High aggression: use direct path; Low aggression: use safe path with progress requirement
        Tile bestMove;
        if (aggressionFactor >= chargeWhileVisibleThreshold)
        {
            // Aggressive: take the direct route, ignore LOS
            bestMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
        }
        else
        {
            // Cautious: sneak using safe tiles, but only if it makes progress toward the target
            bestMove = PathfindingUtils.FindBestSafeMoveToward(enemy, distMap, heroLOS, prevTile);
            if (bestMove != null)
            {
                int currentDist = PathfindingUtils.GetPathDistance(enemy.OccupiedTile, distMap);
                int moveDist = PathfindingUtils.GetPathDistance(bestMove, distMap);
                if (moveDist >= currentDist)
                    bestMove = null; // Safe path doesn't make progress
            }
            // If no safe path makes progress, take the direct route instead of stalling
            if (bestMove == null)
                bestMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
        }

        if (bestMove != null && enemy.GetMoveCost(bestMove) <= enemy.CurrentActionPoints)
        {
            bool movesIntoLOS = heroLOS.Contains(bestMove);
            string label = movesIntoLOS ? "ADVANCE" : "SNEAK";
            string desc = movesIntoLOS
                ? $"No safe progress - advancing through LOS (aggro={aggressionFactor:F2})"
                : "Moving closer while staying hidden";
            Debug.Log($"[AI] {enemy.UnitName}: {desc} toward {target.UnitName}.");
            int moveCost = enemy.GetMoveCost(bestMove);
            AIDebugLog.LogDecision(enemy.UnitName, label, desc);
            AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                bestMove.X, bestMove.Y, moveCost, $"{label} toward {target.UnitName}");
            return AIAction.MoveTo(bestMove);
        }

        // Priority 5: Risky move through LOS - aggression lowers the bar
        if (!heroFacingUs || aggressionFactor >= riskyMoveThreshold)
        {
            Tile riskyMove = PathfindingUtils.FindBestMoveToward(enemy, distMap, prevTile);
            if (riskyMove != null && enemy.GetMoveCost(riskyMove) <= enemy.CurrentActionPoints)
            {
                string reason = heroFacingUs
                    ? $"Hero is looking but aggro={aggressionFactor:F2} - pushing through!"
                    : "Hero not looking - moving through LOS";
                Debug.Log($"[AI] {enemy.UnitName}: {reason}");
                int moveCost = enemy.GetMoveCost(riskyMove);
                AIDebugLog.LogDecision(enemy.UnitName, "RISK_MOVE", reason);
                AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                    riskyMove.X, riskyMove.Y, moveCost, $"Risky move toward {target.UnitName}");
                return AIAction.MoveTo(riskyMove);
            }
        }

        // Priority 6: Last resort - any walkable move toward the hero
        var anyMoves = PathfindingUtils.GetWalkableMoves(enemy);
        if (anyMoves.Count > 0)
        {
            Tile closestMove = anyMoves
                .OrderBy(t => PathfindingUtils.GetPathDistance(t, distMap))
                .FirstOrDefault();
            if (closestMove != null && enemy.GetMoveCost(closestMove) <= enemy.CurrentActionPoints)
            {
                Debug.Log($"[AI] {enemy.UnitName}: Last resort - moving toward hero.");
                int moveCost = enemy.GetMoveCost(closestMove);
                AIDebugLog.LogDecision(enemy.UnitName, "LAST_RESORT", "No preferred moves - taking any step toward hero");
                AIDebugLog.LogMove(enemy.UnitName, enemy.OccupiedTile.X, enemy.OccupiedTile.Y,
                    closestMove.X, closestMove.Y, moveCost, $"Last resort toward {target.UnitName}");
                return AIAction.MoveTo(closestMove);
            }
        }

        // No options - wait
        Debug.Log($"[AI] {enemy.UnitName}: No moves available, waiting.");
        AIDebugLog.LogWait(enemy.UnitName, "No moves available");
        return AIAction.Wait();
    }
}
