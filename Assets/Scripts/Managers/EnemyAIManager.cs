using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages enemy AI during the enemy turn phase.
/// Coordinates spawning, action point refresh, and enemy decision-making.
/// </summary>
public class EnemyAIManager : MonoBehaviour
{
    public static EnemyAIManager Instance;

    [Header("Ramping Spawn Settings")]
    [Tooltip("Number of blips to spawn on the first enemy turn")]
    [SerializeField] private int initialBlipsPerTurn = 1;

    [Tooltip("How many additional blips to spawn each turn (ramp rate)")]
    [SerializeField] private int blipRampPerTurn = 1;

    [Tooltip("Maximum blips to spawn per turn (cap for ramping)")]
    [SerializeField] private int maxBlipsPerTurn = 3;

    [Header("Turn Execution")]
    [Tooltip("Delay between enemy actions (seconds) for visual pacing")]
    [SerializeField] private float delayBetweenActions = 0.3f;

    private bool _isProcessingTurn = false;
    public bool IsProcessingTurn => _isProcessingTurn;

    private int _currentTurn = 0;
    public int CurrentTurn => _currentTurn;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Main entry point called by GameManager.StartEnemyTurn().
    /// Executes the entire enemy turn as a coroutine.
    /// </summary>
    public void ExecuteEnemyTurn()
    {
        if (_isProcessingTurn)
        {
            Debug.LogWarning("[AI] Already processing enemy turn!");
            return;
        }

        StartCoroutine(ExecuteEnemyTurnCoroutine());
    }

    private IEnumerator ExecuteEnemyTurnCoroutine()
    {
        _isProcessingTurn = true;
        _currentTurn++;
        Debug.Log($"[AI] === ENEMY TURN {_currentTurn} STARTED ===");
        AIDebugLog.LogTurnStart(_currentTurn);

        // Phase 1: Spawn new blips (ramping each turn)
        yield return SpawnPhase();

        // Phase 2: Refresh AP for all enemies
        RefreshEnemyAP();

        // Phase 3: Execute all enemy actions
        yield return ActionPhase();

        _isProcessingTurn = false;
        Debug.Log($"[AI] === ENEMY TURN {_currentTurn} COMPLETE ===");
        AIDebugLog.LogTurnEnd(_currentTurn);

        // Return control to GameManager
        GameManager.Instance.OnEnemyTurnComplete();
    }

    /// <summary>
    /// Calculates how many blips to spawn this turn based on ramping.
    /// Starts at initialBlipsPerTurn and increases by blipRampPerTurn each turn,
    /// up to maxBlipsPerTurn.
    /// </summary>
    private int GetBlipsToSpawnThisTurn()
    {
        // Turn 1: initial, Turn 2: initial + ramp, Turn 3: initial + 2*ramp, etc.
        int blipsThisTurn = initialBlipsPerTurn + ((_currentTurn - 1) * blipRampPerTurn);
        return Mathf.Min(blipsThisTurn, maxBlipsPerTurn);
    }

    /// <summary>
    /// Phase 1: Spawns new blips on available BlipSpawnTiles.
    /// Spawn count ramps up each turn until max is reached.
    /// </summary>
    private IEnumerator SpawnPhase()
    {
        int blipsToSpawn = GetBlipsToSpawnThisTurn();
        Debug.Log($"[AI] Spawn Phase: Spawning {blipsToSpawn} blips (turn {_currentTurn})");

        int spawned = 0;
        for (int i = 0; i < blipsToSpawn; i++)
        {
            var spawnedBlip = UnitManager.Instance.SpawnBlip();
            if (spawnedBlip != null)
            {
                spawned++;
                Debug.Log($"[AI] Spawn Phase: Spawned blip #{spawned}");
                if (spawnedBlip.OccupiedTile != null)
                {
                    AIDebugLog.LogSpawn(spawnedBlip.UnitName, spawnedBlip.OccupiedTile.X, spawnedBlip.OccupiedTile.Y, spawned, blipsToSpawn);
                }
                yield return new WaitForSeconds(delayBetweenActions);
            }
            else
            {
                Debug.Log("[AI] Spawn Phase: No available spawn tiles");
                break;
            }
        }

        Debug.Log($"[AI] Spawn Phase: Completed ({spawned}/{blipsToSpawn} blips spawned)");
    }

    /// <summary>
    /// Phase 2: Refreshes action points for all enemies.
    /// </summary>
    private void RefreshEnemyAP()
    {
        Debug.Log("[AI] Refresh Phase: Resetting AP for all enemies...");

        foreach (var enemy in UnitManager.Instance.SpawnedEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                enemy.StartNewTurn();
                Debug.Log($"[AI] {enemy.UnitName} AP refreshed to {enemy.CurrentActionPoints}");
            }
        }
    }

    /// <summary>
    /// Phase 3: Processes actions for all enemies.
    /// Blips act first (to set up ambushes), then ghouls attack.
    /// </summary>
    private IEnumerator ActionPhase()
    {
        Debug.Log("[AI] Action Phase: Processing enemy actions...");

        // Get a snapshot of enemies (list may change during processing)
        var enemies = new List<BaseEnemy>(UnitManager.Instance.SpawnedEnemies);

        // Sort: Blips first (Enemy2), then Ghouls (Enemy1)
        enemies = enemies
            .Where(e => e != null && e.IsAlive)
            .OrderBy(e => e is Enemy2 ? 0 : 1)
            .ToList();

        Debug.Log($"[AI] Action Phase: Processing {enemies.Count} enemies");

        foreach (var enemy in enemies)
        {
            // Check if enemy is still valid (could have been destroyed or revealed)
            if (enemy == null || !enemy.IsAlive)
                continue;

            yield return ProcessEnemyActions(enemy);
        }

        Debug.Log("[AI] Action Phase: Completed");
    }

    /// <summary>
    /// Processes all actions for a single enemy until it runs out of AP or decides to wait.
    /// </summary>
    private IEnumerator ProcessEnemyActions(BaseEnemy enemy)
    {
        Debug.Log($"[AI] Processing {enemy.UnitName} (AP: {enemy.CurrentActionPoints})");

        string behaviorName = enemy.AIBehavior != null ? enemy.AIBehavior.BehaviorName : "None";
        AIDebugLog.LogEnemyActionStart(enemy.UnitName, enemy.CurrentActionPoints, behaviorName);

        // Keep acting until out of AP or the enemy decides to wait
        while (enemy != null && enemy.IsAlive && enemy.CurrentActionPoints > 0)
        {
            AIAction action = enemy.DecideNextAction();

            if (action == null || action.Type == AIActionType.Wait)
            {
                Debug.Log($"[AI] {enemy.UnitName}: Choosing to wait (ending turn)");
                break;
            }

            switch (action.Type)
            {
                case AIActionType.Move:
                    if (action.TargetTile != null)
                    {
                        enemy.ExecuteMove(action.TargetTile);
                        yield return new WaitUntil(() => !enemy.IsMoving);
                        yield return new WaitForSeconds(delayBetweenActions);
                    }
                    break;

                case AIActionType.Attack:
                    enemy.ExecuteAttack();
                    yield return new WaitForSeconds(delayBetweenActions);
                    break;

                case AIActionType.Turn:
                    if (action.NewFacing.HasValue)
                    {
                        enemy.TurnFree(action.NewFacing.Value);
                        yield return new WaitForSeconds(delayBetweenActions * 0.5f);
                    }
                    break;

                case AIActionType.Reveal:
                    if (enemy is Enemy2 blip)
                    {
                        blip.IntentionalReveal();
                        yield return new WaitForSeconds(delayBetweenActions);
                        // Blip is destroyed after reveal, exit loop
                        yield break;
                    }
                    break;
            }
        }

        Debug.Log($"[AI] {enemy.UnitName}: Turn ended (AP: {enemy.CurrentActionPoints})");
    }
}
