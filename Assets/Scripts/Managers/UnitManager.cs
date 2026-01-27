using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance;

    private List<ScriptableUnit> _units;
    private List<BaseHero> _spawnedHeroes = new List<BaseHero>();
    private List<BaseEnemy> _spawnedEnemies = new List<BaseEnemy>();

    public BaseHero SelectedHero;
    public IReadOnlyList<BaseHero> SpawnedHeroes => _spawnedHeroes;
    public IReadOnlyList<BaseEnemy> SpawnedEnemies => _spawnedEnemies;

    void Awake()
    {
        Instance = this;

        _units = Resources.LoadAll<ScriptableUnit>("Units").ToList();
    }

    public void SpawnHeroes()
    {
        _spawnedHeroes.Clear();

        // Get all hero spawn tiles from the grid
        var heroSpawnTiles = GridManager.Instance.GetAllHeroSpawnTiles();

        if (heroSpawnTiles.Count > 0)
        {
            // Spawn heroes on their designated spawn tiles (1-5)
            foreach (var kvp in heroSpawnTiles)
            {
                int heroIndex = kvp.Key;
                HeroSpawnTile spawnTile = kvp.Value;

                BaseHero heroPrefab = GetHeroByIndex(heroIndex);
                if (heroPrefab != null && spawnTile != null)
                {
                    var spawnedHero = Instantiate(heroPrefab);
                    spawnTile.SetUnit(spawnedHero);
                    _spawnedHeroes.Add(spawnedHero);
                }
            }
        }
        else
        {
            // Fallback: spawn using legacy system (random hero on random spawn)
            var heroCount = 1;
            for (int i = 0; i < heroCount; i++)
            {
                var randomPrefab = GetRandomUnit<BaseHero>(Faction.Hero);
                var spawnedHero = Instantiate(randomPrefab);
                var randomSpawnTile = GridManager.Instance.GetHeroSpawnTile();

                randomSpawnTile.SetUnit(spawnedHero);
                _spawnedHeroes.Add(spawnedHero);
            }
        }

        GameManager.Instance.ChangeState(GameState.SpawnEnemies);
    }

    private BaseHero GetHeroByIndex(int index)
    {
        // Find hero unit by name pattern (Hero1, Hero2, etc.)
        string heroName = $"Hero{index}";
        var heroUnit = _units.FirstOrDefault(u =>
            u.Faction == Faction.Hero &&
            u.name.Contains(heroName));

        return heroUnit?.UnitPrefab as BaseHero;
    }

    public void SpawnEnemies()
    {
        // No initial enemies - all enemies spawn from BlipSpawnTiles via AI
        _spawnedEnemies.Clear();
        GameManager.Instance.ChangeState(GameState.HeroesTurn);
    }

    /// <summary>
    /// Spawns a blip on a tactically chosen spawn tile. Called by AI.
    /// Prefers tiles that are hidden from hero LOS and closer to heroes for ambush potential.
    /// Returns the spawned enemy, or null if no spawn tile available.
    /// </summary>
    public BaseEnemy SpawnBlip()
    {
        var spawnTile = GetTacticalBlipSpawnTile();
        if (spawnTile == null)
        {
            Debug.Log("[AI Spawn] No available blip spawn tile!");
            return null;
        }

        // Get blip prefab specifically (Enemy2)
        var blipPrefab = GetBlipPrefab();
        if (blipPrefab == null)
        {
            Debug.LogError("[AI Spawn] No blip prefab found!");
            return null;
        }

        var spawnedEnemy = Instantiate(blipPrefab);
        spawnTile.SetUnit(spawnedEnemy);
        _spawnedEnemies.Add(spawnedEnemy);

        Debug.Log($"[AI Spawn] Blip spawned tactically at ({spawnTile.X}, {spawnTile.Y})");
        return spawnedEnemy;
    }

    /// <summary>
    /// Gets the blip (Enemy2) prefab from loaded units.
    /// </summary>
    private Enemy2 GetBlipPrefab()
    {
        var blipUnit = _units.FirstOrDefault(u =>
            u.Faction == Faction.Enemy &&
            u.UnitPrefab is Enemy2);

        return blipUnit?.UnitPrefab as Enemy2;
    }

    /// <summary>
    /// Tactically chooses the best blip spawn tile based on:
    /// 1. Not in hero LOS (hidden spawns preferred)
    /// 2. Closer to heroes (better ambush potential)
    /// </summary>
    private BlipSpawnTile GetTacticalBlipSpawnTile()
    {
        var allSpawnTiles = GridManager.Instance.GetAllBlipSpawnTiles();
        var availableTiles = allSpawnTiles
            .Where(t => t != null && t.OccupiedUnit == null)
            .ToList();

        if (availableTiles.Count == 0)
            return null;

        // Get combined hero LOS for tactical evaluation
        HashSet<Tile> heroLOS = PathfindingUtils.GetCombinedHeroLOS();

        // Find nearest hero position for distance calculations
        Vector2 heroCenter = GetHeroCenterPosition();

        // Score each tile: prefer hidden tiles closer to heroes
        BlipSpawnTile bestTile = null;
        float bestScore = float.MinValue;

        foreach (var tile in availableTiles)
        {
            float score = ScoreSpawnTile(tile, heroLOS, heroCenter);

            if (score > bestScore)
            {
                bestScore = score;
                bestTile = tile;
            }
        }

        if (bestTile != null)
        {
            bool isHidden = !heroLOS.Contains(bestTile);
            Debug.Log($"[AI Spawn] Chose tile ({bestTile.X}, {bestTile.Y}) - Hidden: {isHidden}, Score: {bestScore:F1}");
        }

        return bestTile;
    }

    /// <summary>
    /// Scores a spawn tile for tactical value.
    /// Higher score = better spawn location.
    /// </summary>
    private float ScoreSpawnTile(Tile tile, HashSet<Tile> heroLOS, Vector2 heroCenter)
    {
        float score = 0f;

        // Big bonus for being hidden from heroes
        if (!heroLOS.Contains(tile))
        {
            score += 100f;
        }

        // Distance to hero center (closer is better for ambush, but not too close)
        float distToHeroes = Vector2.Distance(new Vector2(tile.X, tile.Y), heroCenter);

        // Ideal ambush distance is 3-6 tiles - close enough to reach, far enough to maneuver
        if (distToHeroes >= 3f && distToHeroes <= 6f)
        {
            score += 50f;
        }
        else if (distToHeroes < 3f)
        {
            // Very close - good if hidden, risky if visible
            score += heroLOS.Contains(tile) ? -20f : 30f;
        }
        else
        {
            // Far away - less ideal but still usable
            score += Mathf.Max(0f, 30f - (distToHeroes - 6f) * 5f);
        }

        // Small random factor to add variety
        score += Random.Range(0f, 10f);

        return score;
    }

    /// <summary>
    /// Gets the average position of all living heroes.
    /// </summary>
    private Vector2 GetHeroCenterPosition()
    {
        if (_spawnedHeroes.Count == 0)
            return Vector2.zero;

        float sumX = 0f, sumY = 0f;
        int count = 0;

        foreach (var hero in _spawnedHeroes)
        {
            if (hero != null && hero.IsAlive && hero.OccupiedTile != null)
            {
                sumX += hero.OccupiedTile.X;
                sumY += hero.OccupiedTile.Y;
                count++;
            }
        }

        if (count == 0)
            return Vector2.zero;

        return new Vector2(sumX / count, sumY / count);
    }

    /// <summary>
    /// Removes a destroyed enemy from the tracking list.
    /// </summary>
    public void OnEnemyDestroyed(BaseEnemy enemy)
    {
        _spawnedEnemies.Remove(enemy);
    }

    /// <summary>
    /// Registers a newly spawned enemy (e.g., ghouls from blip reveals).
    /// </summary>
    public void RegisterEnemy(BaseEnemy enemy)
    {
        if (!_spawnedEnemies.Contains(enemy))
            _spawnedEnemies.Add(enemy);
    }

    /// <summary>
    /// Checks all blips for reveal based on hero line of sight.
    /// Should be called after any hero moves or turns.
    /// </summary>
    public void CheckAllBlipsForReveal()
    {
        Debug.Log("[Blip Check] Checking all blips for reveal...");

        // Create a copy of the list since revealing can modify it
        var enemies = new List<BaseEnemy>(_spawnedEnemies);

        int blipsChecked = 0;
        foreach (var enemy in enemies)
        {
            if (enemy is Enemy2 blip && !blip.IsRevealed)
            {
                blipsChecked++;
                blip.CheckReveal();
            }
        }

        Debug.Log($"[Blip Check] Checked {blipsChecked} unrevealed blips");
    }

    /// <summary>
    /// Removes a destroyed hero from the tracking list.
    /// </summary>
    public void OnHeroDestroyed(BaseHero hero)
    {
        _spawnedHeroes.Remove(hero);
        if (SelectedHero == hero)
            SelectedHero = null;
    }

    private T GetRandomUnit<T>(Faction faction) where T : BaseUnit
    {
        return (T)_units.Where(u => u.Faction == faction).OrderBy(o => Random.value).First().UnitPrefab;
    }

    // possiblity to access through canva (stats, etc)
    public void SetSelectedHero(BaseHero hero)
    {
        SelectedHero = hero;

        GridManager.Instance.ClearMoveHighlights();

        if (hero != null)
        {
            GridManager.Instance.ShowImmediateMoveOptions(hero);
            GridManager.Instance.ShowLineOfSight(hero);

            if (InGameUIManager.Instance != null)
                InGameUIManager.Instance.ShowHeroUI(hero);
        }
        else
        {
            if (InGameUIManager.Instance != null)
                InGameUIManager.Instance.HideHeroUI();
        }
    }
}
