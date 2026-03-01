using UnityEngine;

public enum VictoryConditionType
{
    Escape = 0,             // Heroes on target tiles are removed (escaped) at end of enemy turn
    FlamethrowerTarget = 1  // Flamethrower shot hitting any target tile = instant win
}

/// <summary>
/// Defines a level layout using ASCII-style string encoding.
/// Create new levels via: Right-click in Project > Create > Game > Level Data
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName = "Level 1";
    public int levelNumber = 1;

    [TextArea(2, 5)]
    [Tooltip("Description shown in the level select screen")]
    public string description = "Enter level description here...";

    [Header("Grid Layout")]
    [Tooltip("Each string represents one row.\n" +
             "Characters:\n" +
             "  '.' = walkable\n" +
             "  '#' = wall (blocks LOS)\n" +
             "  ' ' = void (empty space)\n" +
             "  '1'-'5' = hero spawn (specific hero)\n" +
             "  'B' = blip spawn point\n" +
             "  'A' = artifact pickup\n" +
             "  'T' = target area\n" +
             "Example:\n" +
             "\"##1.2.3.4.5##\"\n" +
             "\"#....A......#\"\n" +
             "\"#.....T..BBB#\"")]
    [TextArea(5, 15)]
    public string[] gridRows;

    [Header("Tile Type Characters")]
    [Tooltip("Character representing walkable tiles in gridRows")]
    public char walkableChar = '.';

    [Tooltip("Character representing wall tiles (blocks line of sight)")]
    public char wallChar = '#';

    [Tooltip("Character representing void/empty space")]
    public char voidChar = ' ';

    [Tooltip("Characters '1'-'5' represent hero spawn points for heroes 1-5")]
    public char heroSpawnStart = '1';

    [Tooltip("Character representing blip/enemy spawn points (AI controlled)")]
    public char blipSpawnChar = 'B';

    [Tooltip("Character representing artifact spawn location (artifact object placed on walkable tile)")]
    public char artifactChar = 'A';

    [Tooltip("Character representing target area tiles")]
    public char targetChar = 'T';

    [Header("Victory Conditions")]
    [Tooltip("The type of victory condition for this level.")]
    public VictoryConditionType victoryConditionType = VictoryConditionType.Escape;

    [Tooltip("If true, a hero must carry the artifact to the target. If false, just reaching the target is enough.")]
    public bool requiresArtifact = false;

    [Tooltip("(Escape mode only) Minimum number of heroes that must escape to win.")]
    [Min(1)]
    public int requiredEscapees = 1;

    // Computed properties
    public int Width => gridRows != null && gridRows.Length > 0 ? gridRows[0].Length : 0;
    public int Height => gridRows != null ? gridRows.Length : 0;

    /// <summary>
    /// Returns the tile type character at the given coordinates.
    /// Note: Y=0 is the TOP row (first string in array), increasing downward.
    /// </summary>
    public char GetTileChar(int x, int y)
    {
        if (y < 0 || y >= Height || x < 0 || x >= Width)
            return voidChar; // Out of bounds = void

        if (x >= gridRows[y].Length)
            return voidChar; // Row too short = void

        return gridRows[y][x];
    }

    /// <summary>
    /// Checks if the tile at given coordinates should be walkable.
    /// Hero spawns, artifacts, and targets are walkable. Blip spawns are not walkable by heroes.
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        char c = GetTileChar(x, y);
        if (c == walkableChar) return true;
        if (c >= heroSpawnStart && c <= '5') return true; // Hero spawn tiles are walkable
        if (c == artifactChar) return true;
        if (c == targetChar) return true;
        return false;
    }

    /// <summary>
    /// Returns the tile type for the given coordinates.
    /// </summary>
    public TileType GetTileType(int x, int y)
    {
        char c = GetTileChar(x, y);

        if (c == walkableChar) return TileType.Walkable;
        if (c == wallChar) return TileType.Wall;
        if (c == voidChar) return TileType.Void;
        if (c >= heroSpawnStart && c <= '5') return TileType.HeroSpawn;
        if (c == blipSpawnChar) return TileType.BlipSpawn;
        if (c == artifactChar) return TileType.ArtifactSpawn;
        if (c == targetChar) return TileType.Target;

        return TileType.Void; // Default unknown chars to void
    }

    /// <summary>
    /// Gets the hero index (1-5) for a hero spawn tile, or 0 if not a hero spawn.
    /// </summary>
    public int GetHeroSpawnIndex(int x, int y)
    {
        char c = GetTileChar(x, y);
        if (c >= heroSpawnStart && c <= '5')
            return c - heroSpawnStart + 1;
        return 0;
    }

    /// <summary>
    /// Validates the level data and logs any issues.
    /// </summary>
    public bool Validate()
    {
        if (gridRows == null || gridRows.Length == 0)
        {
            Debug.LogError($"LevelData '{name}': gridRows is empty!");
            return false;
        }

        int firstRowLength = gridRows[0].Length;
        for (int i = 1; i < gridRows.Length; i++)
        {
            if (gridRows[i].Length != firstRowLength)
            {
                Debug.LogWarning($"LevelData '{name}': Row {i} has different length ({gridRows[i].Length}) than row 0 ({firstRowLength})");
            }
        }

        // Validate Escape mode settings
        if (victoryConditionType == VictoryConditionType.Escape && requiredEscapees < 1)
        {
            Debug.LogWarning($"LevelData '{name}': Escape mode requires at least 1 escapee, setting to 1.");
            requiredEscapees = 1;
        }

        // Validate FlamethrowerTarget has target tiles
        if (victoryConditionType == VictoryConditionType.FlamethrowerTarget)
        {
            bool hasTargetTile = false;
            for (int y = 0; y < Height && !hasTargetTile; y++)
                for (int x = 0; x < Width && !hasTargetTile; x++)
                    if (GetTileType(x, y) == TileType.Target)
                        hasTargetTile = true;

            if (!hasTargetTile)
                Debug.LogWarning($"LevelData '{name}': FlamethrowerTarget mode requires at least one target tile ('T')!");
        }

        return true;
    }

    private void OnValidate()
    {
        // Auto-validate when edited in Inspector
        Validate();
    }
}
