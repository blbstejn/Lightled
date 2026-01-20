using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// blip
public class Enemy2 : BaseEnemy
{
    private bool _isRevealed = false;

    [SerializeField] private Enemy1 ghoulPrefab;
    [SerializeField] private int minGhouls = 1;
    [SerializeField] private int maxGhouls = 3;

    public override void Awake()
    {
        base.Awake();
        SetupAbilities();
    }

    private void SetupAbilities()
    {
        var abilities = new System.Collections.Generic.List<UnitAbility>();

        // Blip loadout: Move and Melee only (no turn abilities - facing not important)
        abilities.Add(ScriptableObject.CreateInstance<MoveAbility>());
        abilities.Add(ScriptableObject.CreateInstance<MeleeAttackAbility>());

        // Use reflection to set the private _abilities field
        var field = typeof(BaseUnit).GetField("_abilities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(this, abilities);
    }

    protected override void OnMoveEnded()
    {
        base.OnMoveEnded();

        if (_isRevealed)
            return;

        // Check if this enemy is in line of sight of any hero
        BaseHero[] heroes = FindObjectsByType<BaseHero>(FindObjectsSortMode.None);

        foreach (BaseHero hero in heroes)
        {
            if (!hero.IsAlive)
                continue;

            var losTiles = hero.GetLineOfSightTiles();

            if (losTiles.Contains(OccupiedTile))
            {
                RevealBlip();
                break;
            }
        }
    }

    private void RevealBlip()
    {
        _isRevealed = true;

        // Determine how many ghouls to spawn
        int ghoulCount = Random.Range(minGhouls, maxGhouls + 1);

        // Get available tiles (current tile + neighbors)
        List<Tile> availableTiles = new List<Tile> { OccupiedTile };
        availableTiles.AddRange(GridManager.Instance.GetNeighbours(OccupiedTile)
            .Where(t => t.Walkable));

        // Shuffle and take only as many tiles as we need
        availableTiles = availableTiles.OrderBy(t => Random.value).Take(ghoulCount).ToList();

        // Spawn ghouls on available tiles
        foreach (Tile tile in availableTiles)
        {
            Enemy1 ghoul = Instantiate(ghoulPrefab);
            tile.SetUnit(ghoul);
        }

        Debug.Log($"Blip revealed! Spawned {availableTiles.Count} ghouls at position ({OccupiedTile.X}, {OccupiedTile.Y})");

        // Destroy the blip
        Destroy(gameObject);
    }
}
