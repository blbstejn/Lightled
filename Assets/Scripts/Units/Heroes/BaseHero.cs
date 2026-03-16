using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal.Internal;

public class BaseHero : BaseUnit
{
    [Header("FieldOfView")]
    [SerializeField] private int shootConeWidth = 1;

    [Header("Overwatch")]
    public bool IsOnOverwatch { get; private set; }
    public HashSet<Tile> OverwatchLOSTiles { get; private set; }

    public override void Awake()
    {
        MaxActionPoints = 4; // Heroes have 4 AP

        // Set default facing to Right for heroes
        SetFacing(FacingDirection.Right);

        // Ensure heroes cannot move sideways
        if (movementProfile != null)
        {
            movementProfile.allowSideways = false;
        }

        base.Awake();
    }

    protected virtual void Start()
    {
        // Runs in Start so any Inspector-assigned unitAbilities are already loaded.
        // Only adds an ability if one of that type is not already in the list,
        // so Inspector assignments always take precedence.
        EnsureBaseAbilities();
    }

    /// <summary>
    /// Adds core abilities as fallbacks if they are not already assigned via Inspector.
    /// Contextual abilities (Move) and non-contextual abilities (Turn, Overwatch) are covered.
    /// </summary>
    private void EnsureBaseAbilities()
    {
        bool hasMove = false;
        bool hasTurnLeft = false;
        bool hasTurnRight = false;
        bool hasOverwatch = false;

        foreach (var ability in unitAbilities)
        {
            if (ability is MoveAbility) hasMove = true;
            if (ability is TurnLeftAbility) hasTurnLeft = true;
            if (ability is TurnRightAbility) hasTurnRight = true;
            if (ability is OverwatchAbility) hasOverwatch = true;
        }

        if (!hasMove)
            unitAbilities.Add(ScriptableObject.CreateInstance<MoveAbility>());
        if (!hasTurnLeft)
            unitAbilities.Add(ScriptableObject.CreateInstance<TurnLeftAbility>());
        if (!hasTurnRight)
            unitAbilities.Add(ScriptableObject.CreateInstance<TurnRightAbility>());
        if (!hasOverwatch && CanUseOverwatch)
            unitAbilities.Add(ScriptableObject.CreateInstance<OverwatchAbility>());
    }

    protected override void OnMoveEnded()
    {
        base.OnMoveEnded();

        // Refresh move highlights and LOS from the new tile position.
        if (UnitManager.Instance != null && UnitManager.Instance.SelectedHero == this)
            UnitManager.Instance.SetSelectedHero(this);
    }

    public override void StartNewTurn()
    {
        base.StartNewTurn();

        // Clear overwatch at start of new turn
        ExitOverwatch();
    }

    /// <summary>
    /// Whether this hero can use Overwatch ability. Override to disable for specific heroes.
    /// </summary>
    protected virtual bool CanUseOverwatch => true;

    protected override void OnDeath(BaseUnit killer)
    {
        base.OnDeath(killer);

        // Drop artifact if carrying one
        if (HasArtifact)
        {
            DropArtifact();
            // TODO: Optionally spawn artifact back on the tile where hero died
            Debug.Log($"{UnitName} dropped the artifact!");
        }

        // Notify UnitManager
        UnitManager.Instance.OnHeroDestroyed(this);

        // Check defeat condition
        GameManager.Instance.CheckDefeatCondition();
    }

    #region Overwatch

    /// <summary>
    /// Enters overwatch state, caching the current line of sight tiles.
    /// While on overwatch, the hero will react-shoot enemies entering their LOS.
    /// </summary>
    public void EnterOverwatch()
    {
        IsOnOverwatch = true;
        OverwatchLOSTiles = GetLineOfSightTiles();
        Debug.Log($"[Overwatch] {UnitName} entered overwatch, watching {OverwatchLOSTiles.Count} tiles");
    }

    /// <summary>
    /// Exits overwatch state, clearing cached LOS tiles.
    /// Called when taking another action or at start of turn.
    /// </summary>
    public void ExitOverwatch()
    {
        if (IsOnOverwatch)
        {
            Debug.Log($"[Overwatch] {UnitName} exited overwatch");
        }
        IsOnOverwatch = false;
        OverwatchLOSTiles = null;
    }

    /// <summary>
    /// Called when the hero takes any action that should break overwatch.
    /// </summary>
    public void BreakOverwatch()
    {
        if (IsOnOverwatch)
        {
            Debug.Log($"[Overwatch] {UnitName}'s overwatch broken by taking action");
            ExitOverwatch();
        }
    }

    #endregion

    public HashSet<Tile> GetLineOfSightTiles()
    {
        HashSet<Tile> result = new();

        Vector2Int origin = new(OccupiedTile.X, OccupiedTile.Y);
        Vector2Int forward = Forward;
        Vector2Int side = Perpendicular;

        // Track shadow angles - each wall creates a shadow cone behind it
        // Stored as (minAngle, maxAngle) pairs where angle is lateral offset / depth
        List<(float min, float max)> shadowAngles = new();

        int depth = 1;

        while (true)
        {
            bool anyTile = false;
            int lateral = shootConeWidth * depth;

            for (int offset = -lateral; offset <= lateral; offset++)
            {
                // Calculate the center angle of this tile
                float tileCenterAngle = (float)offset / depth;

                // Calculate the angular range this tile covers (for shadow casting)
                float tileMinAngle = (offset - 0.5f) / depth;
                float tileMaxAngle = (offset + 0.5f) / depth;

                // Check if this tile's center is blocked by any shadow
                bool isBlocked = false;
                foreach (var (min, max) in shadowAngles)
                {
                    // Tile is blocked if its center is within a shadow
                    if (tileCenterAngle >= min && tileCenterAngle <= max)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (isBlocked)
                    continue;

                Vector2Int pos = origin + forward * depth + side * offset;

                Tile tile = GridManager.Instance.GetTileAtPosition(pos);
                if (tile == null)
                    continue;

                anyTile = true;

                // Check if tile blocks LOS (walls, void, or fire)
                bool blocksLOS = tile.BlocksLineOfSight ||
                    (FireEffectManager.Instance != null && FireEffectManager.Instance.IsFireBlockingLOS(tile));

                // If this tile blocks line of sight, add its shadow cone but don't include it in result
                if (blocksLOS)
                {
                    // The shadow extends from this tile outward
                    // Using the tile's angular range as the shadow boundaries
                    shadowAngles.Add((tileMinAngle, tileMaxAngle));
                }
                else
                {
                    // Only add walkable tiles to the result
                    result.Add(tile);
                }
            }

            if (!anyTile)
                break;

            depth++;
        }

        return result;
    }
}
