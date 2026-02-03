using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// blip
public class Enemy2 : BaseEnemy
{
    private bool _isRevealed = false;
    private Tile _spawnTile; // Track spawn tile - blips can't be revealed here, ghouls can't spawn here
    private int _ghoulCount; // Determined at spawn time

    [SerializeField] private Enemy1 ghoulPrefab;
    [SerializeField] private int minGhouls = 1;
    [SerializeField] private int maxGhouls = 3;

    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
        SetupAI();
    }

    private void Start()
    {
        // Capture spawn tile on first frame (after SetUnit has been called)
        if (OccupiedTile != null)
        {
            _spawnTile = OccupiedTile;
            Debug.Log($"[Blip] {UnitName} spawn tile set to ({_spawnTile.X}, {_spawnTile.Y})");
        }

        // Determine ghoul count at spawn time
        _ghoulCount = Random.Range(minGhouls, maxGhouls + 1);
        Debug.Log($"[Blip] {UnitName} will spawn {_ghoulCount} ghouls when revealed");
    }

    private void SetupAbilities()
    {
        var abilities = new System.Collections.Generic.List<UnitAbility>();

        // Blip loadout: Move only (blips cannot attack - they must reveal first)
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());

        unitAbilities = abilities;
    }

    private void SetupAI()
    {
        // Assign AI behavior if not set in Inspector
        if (aiBehavior == null)
        {
            aiBehavior = ScriptableObject.CreateInstance<BlipBehavior>();
        }
    }

    public bool IsRevealed => _isRevealed;

    /// <summary>
    /// Blips on BlipSpawnTiles cannot be targeted - they must enter the battlefield first.
    /// </summary>
    public override bool IsTargetable => OccupiedTile != null && OccupiedTile is not BlipSpawnTile;

    /// <summary>
    /// Kills the blip instantly without spawning ghouls (used by flamethrower).
    /// </summary>
    public void KillByFire()
    {
        Debug.Log($"[Blip] {UnitName} incinerated by fire - no ghouls spawned!");

        // Clear tile
        if (OccupiedTile != null)
        {
            OccupiedTile.OccupiedUnit = null;
            OccupiedTile = null;
        }

        // Remove from tracking and destroy
        UnitManager.Instance.OnEnemyDestroyed(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// Called by AI to intentionally reveal the blip (for ambush).
    /// Triggers the same reveal process as being spotted by heroes.
    /// </summary>
    public void IntentionalReveal()
    {
        if (_isRevealed)
            return;

        Debug.Log($"[Blip] {UnitName}: Intentionally revealing for ambush!");
        RevealBlip();
    }

    protected override void OnMoveEnded()
    {
        base.OnMoveEnded();
        CheckReveal();
    }

    /// <summary>
    /// Checks if this blip should be revealed (is in line of sight of any hero).
    /// Can be called externally when heroes move or turn.
    /// Blips cannot be revealed while still on their spawn tile - only after moving.
    /// </summary>
    public void CheckReveal()
    {
        if (_isRevealed)
            return;

        // Blips cannot be revealed on their spawn tile - only when entering a new tile
        if (_spawnTile != null && OccupiedTile == _spawnTile)
        {
            return;
        }

        // Check if this enemy is in line of sight of any hero
        BaseHero[] heroes = FindObjectsByType<BaseHero>(FindObjectsSortMode.None);

        foreach (BaseHero hero in heroes)
        {
            if (!hero.IsAlive)
                continue;

            var losTiles = hero.GetLineOfSightTiles();

            if (losTiles.Contains(OccupiedTile))
            {
                Debug.Log($"[Blip] {UnitName} at ({OccupiedTile.X},{OccupiedTile.Y}) spotted by {hero.UnitName}!");
                RevealBlip();
                break;
            }
        }
    }

    private void RevealBlip()
    {
        _isRevealed = true;

        Debug.Log($"[Blip Reveal] Starting reveal for {UnitName}");

        // Check if ghoul prefab is assigned
        if (ghoulPrefab == null)
        {
            Debug.LogError($"[Blip Reveal] ERROR: ghoulPrefab is not assigned on {UnitName}! Cannot spawn ghouls.");
            Destroy(gameObject);
            return;
        }

        // Find the nearest hero - this will be the preferred target for spawned ghouls
        BaseHero preferredTarget = PathfindingUtils.FindNearestHero(this);
        if (preferredTarget != null)
        {
            Debug.Log($"[Blip Reveal] Ghouls will target {preferredTarget.UnitName}");
        }

        Debug.Log($"[Blip Reveal] Will spawn {_ghoulCount} ghouls (determined at spawn time)");

        // Ghouls inherit the blip's remaining action points
        int blipAP = CurrentActionPoints;

        // Save the blip's tile and clear it BEFORE spawning ghouls
        Tile blipTile = OccupiedTile;
        OccupiedTile.OccupiedUnit = null;
        OccupiedTile = null;

        // Get walkable neighbor tiles for additional ghouls (excluding spawn tile - ghouls cannot exist there)
        var neighbourTiles = GridManager.Instance.GetNeighbours(blipTile)
            .Where(t => t.Walkable && t != _spawnTile)
            .OrderBy(t => Random.value)
            .ToList();
        Debug.Log($"[Blip Reveal] Found {neighbourTiles.Count} walkable neighbour tiles (excluding spawn tile)");

        int spawned = 0;

        // First ghoul ALWAYS spawns on the blip's original tile
        Enemy1 firstGhoul = Instantiate(ghoulPrefab);
        if (firstGhoul != null)
        {
            firstGhoul.PreferredTarget = preferredTarget;
            firstGhoul.SetActionPoints(blipAP);
            blipTile.SetUnit(firstGhoul);
            UnitManager.Instance.RegisterEnemy(firstGhoul);
            spawned++;
            Debug.Log($"[Blip Reveal] First ghoul spawned on blip's tile ({blipTile.X},{blipTile.Y}) with {blipAP} AP");
        }

        // Spawn remaining ghouls on neighbor tiles
        int remainingGhouls = _ghoulCount - 1;
        for (int i = 0; i < remainingGhouls && i < neighbourTiles.Count; i++)
        {
            Tile tile = neighbourTiles[i];
            Debug.Log($"[Blip Reveal] Spawning ghoul on tile ({tile.X},{tile.Y})");
            Enemy1 ghoul = Instantiate(ghoulPrefab);
            if (ghoul != null)
            {
                ghoul.PreferredTarget = preferredTarget;
                ghoul.SetActionPoints(blipAP);
                tile.SetUnit(ghoul);
                UnitManager.Instance.RegisterEnemy(ghoul);
                spawned++;
                Debug.Log($"[Blip Reveal] Ghoul spawned successfully at ({tile.X},{tile.Y}) with {blipAP} AP");
            }
            else
            {
                Debug.LogError($"[Blip Reveal] Failed to instantiate ghoul!");
            }
        }

        Debug.Log($"[Blip Reveal] Blip revealed! Spawned {spawned} ghouls at position ({blipTile.X}, {blipTile.Y})");

        // Remove blip from tracking and destroy (tile already cleared above)
        UnitManager.Instance.OnEnemyDestroyed(this);
        Destroy(gameObject);
    }
}
