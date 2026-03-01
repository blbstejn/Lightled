using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages overwatch reaction shots.
/// When an enemy enters a tile that's in a hero's overwatch line of sight,
/// the hero gets a free ranged attack against that enemy.
/// </summary>
public class OverwatchManager : MonoBehaviour
{
    public static OverwatchManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float reactionShotDelay = 0.3f;

    private bool _isProcessingOverwatch;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Called when an enemy enters a tile. Checks if any heroes on overwatch
    /// can see this tile and triggers reaction shots.
    /// </summary>
    /// <param name="enemy">The enemy that moved</param>
    /// <param name="enteredTile">The tile the enemy entered</param>
    public void OnEnemyEnteredTile(BaseUnit enemy, Tile enteredTile)
    {
        if (enemy == null || enteredTile == null)
            return;

        // Don't process during another overwatch reaction
        if (_isProcessingOverwatch)
            return;

        // Find all heroes on overwatch who can see this tile
        var overwatchingHeroes = GetHeroesWatchingTile(enteredTile);

        if (overwatchingHeroes.Count > 0)
        {
            StartCoroutine(ProcessOverwatchShots(overwatchingHeroes, enemy, enteredTile));
        }
    }

    /// <summary>
    /// Gets all heroes currently on overwatch who have the given tile in their LOS.
    /// </summary>
    private List<BaseHero> GetHeroesWatchingTile(Tile tile)
    {
        List<BaseHero> result = new List<BaseHero>();

        if (UnitManager.Instance == null)
            return result;

        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (hero == null || !hero.IsAlive)
                continue;

            if (!hero.IsOnOverwatch)
                continue;

            // Check if this tile is in the hero's cached overwatch LOS
            if (hero.OverwatchLOSTiles != null && hero.OverwatchLOSTiles.Contains(tile))
            {
                result.Add(hero);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes overwatch shots from multiple heroes against an enemy.
    /// </summary>
    private IEnumerator ProcessOverwatchShots(List<BaseHero> heroes, BaseUnit enemy, Tile targetTile)
    {
        _isProcessingOverwatch = true;

        foreach (var hero in heroes)
        {
            // Skip if hero is no longer valid
            if (hero == null || !hero.IsAlive || !hero.IsOnOverwatch)
                continue;

            // Skip if enemy is already dead
            if (enemy == null || !enemy.IsAlive)
                break;

            // Skip if enemy is not targetable (e.g., blip on spawn tile)
            if (!enemy.IsTargetable)
                continue;

            Debug.Log($"[Overwatch] {hero.UnitName} reacts to {enemy.UnitName} entering tile ({targetTile.X},{targetTile.Y})!");

            // Small delay for visual feedback
            yield return new WaitForSeconds(reactionShotDelay);

            // Execute the overwatch shot
            ExecuteOverwatchShot(hero, enemy);

            // Overwatch is NOT consumed after firing - hero stays on overwatch
            // until they take another action or their turn starts
        }

        _isProcessingOverwatch = false;
    }

    /// <summary>
    /// Executes a single overwatch shot from a hero against an enemy.
    /// Uses the same hit mechanics as ranged attack but costs no AP.
    /// Sustained fire bonus applies.
    /// </summary>
    private void ExecuteOverwatchShot(BaseHero hero, BaseUnit enemy)
    {
        if (hero == null || enemy == null || !enemy.IsAlive)
            return;

        Debug.Log($"[Overwatch] {hero.UnitName} takes overwatch shot at {enemy.UnitName}!");

        // Determine hit threshold (same as ranged attack)
        int hitThreshold = 6; // Default: need 6+ to hit

        // Sustained fire bonus applies
        if (hero.HasSustainedFire && hero.SustainedFireTarget == enemy)
        {
            hitThreshold = 5; // Sustained fire: need 5+ to hit
            Debug.Log($"[Overwatch] {hero.UnitName} has SUSTAINED FIRE bonus against {enemy.UnitName}! (5+ to hit)");
            CombatDebugLog.LogSustainedFire(hero.UnitName, enemy.UnitName);
        }

        // Flash the target to show they're being attacked
        enemy.FlashHit();

        // Roll two dice (same as ranged attack)
        int roll1 = Random.Range(1, 7);
        int roll2 = Random.Range(1, 7);
        Debug.Log($"[Overwatch] Dice rolls: {roll1}, {roll2} (need {hitThreshold}+ to hit)");

        bool hit = (roll1 >= hitThreshold) || (roll2 >= hitThreshold);

        // Log the attack to combat debug
        CombatDebugLog.LogOverwatchAttack(hero.UnitName, enemy.UnitName, roll1, roll2, hitThreshold, hit);

        if (hit)
        {
            Debug.Log($"[Overwatch] HIT! {enemy.UnitName} is killed!");
            CombatDebugLog.LogKill(enemy.UnitName, $"{hero.UnitName} (Overwatch)");
            enemy.Kill(hero);
            hero.BreakSustainedFire();
        }
        else
        {
            Debug.Log($"[Overwatch] MISS! Both dice ({roll1}, {roll2}) failed to meet threshold {hitThreshold}+");

            // Set sustained fire target for next shot even on miss
            if (enemy != null && enemy.IsAlive)
            {
                hero.SetSustainedFireTarget(enemy);
                Debug.Log($"[Overwatch] Sustained fire target set to {enemy.UnitName}");
            }
        }
    }

    /// <summary>
    /// Checks if any hero is currently on overwatch.
    /// </summary>
    public bool AnyHeroOnOverwatch()
    {
        if (UnitManager.Instance == null)
            return false;

        return UnitManager.Instance.SpawnedHeroes.Any(h => h != null && h.IsAlive && h.IsOnOverwatch);
    }

    /// <summary>
    /// Clears overwatch for all heroes. Called at end of enemy turn if needed.
    /// </summary>
    public void ClearAllOverwatch()
    {
        if (UnitManager.Instance == null)
            return;

        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (hero != null && hero.IsOnOverwatch)
            {
                hero.ExitOverwatch();
            }
        }
    }
}
