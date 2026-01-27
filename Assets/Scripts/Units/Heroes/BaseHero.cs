using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal.Internal;

public class BaseHero : BaseUnit
{
    [Header("FieldOfView")]
    [SerializeField] private int shootConeWidth = 1;

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

        // Add turn abilities if not already present
        EnsureTurnAbilities();
    }

    /// <summary>
    /// Ensures the hero has Turn Left and Turn Right abilities.
    /// Creates them at runtime if not assigned in Inspector.
    /// </summary>
    private void EnsureTurnAbilities()
    {
        bool hasTurnLeft = false;
        bool hasTurnRight = false;

        foreach (var ability in unitAbilities)
        {
            if (ability is TurnLeftAbility) hasTurnLeft = true;
            if (ability is TurnRightAbility) hasTurnRight = true;
        }

        // Create and add Turn Left ability if missing
        if (!hasTurnLeft)
        {
            var turnLeft = ScriptableObject.CreateInstance<TurnLeftAbility>();
            unitAbilities.Add(turnLeft);
        }

        // Create and add Turn Right ability if missing
        if (!hasTurnRight)
        {
            var turnRight = ScriptableObject.CreateInstance<TurnRightAbility>();
            unitAbilities.Add(turnRight);
        }
    }

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

                // If this tile blocks line of sight, add its shadow cone but don't include it in result
                if (tile.BlocksLineOfSight)
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
