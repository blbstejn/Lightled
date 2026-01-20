using UnityEngine;

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

    [Header("Grid Layout")]
    [Tooltip("Each string represents one row. Use '.' for walkable, '#' for void.\nExample:\n\"####....####\"\n\"#..........#\"\n\"####....####\"")]
    [TextArea(5, 15)]
    public string[] gridRows;

    [Header("Tile Type Characters")]
    [Tooltip("Character representing walkable tiles in gridRows")]
    public char walkableChar = '.';

    [Tooltip("Character representing void/obstacle tiles in gridRows")]
    public char voidChar = '#';

    [Header("Unit Spawning")]
    [Tooltip("Possible spawn positions for heroes (X, Y coordinates)")]
    public Vector2Int[] heroSpawnPoints;

    [Tooltip("Possible spawn positions for enemies (X, Y coordinates)")]
    public Vector2Int[] enemySpawnPoints;

    [Header("Unit Counts")]
    [Tooltip("Number of heroes to spawn for this level")]
    public int heroCount = 1;

    [Tooltip("Number of enemies to spawn for this level")]
    public int enemyCount = 1;

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
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        return GetTileChar(x, y) == walkableChar;
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

        if (heroSpawnPoints == null || heroSpawnPoints.Length == 0)
        {
            Debug.LogWarning($"LevelData '{name}': No hero spawn points defined!");
        }

        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning($"LevelData '{name}': No enemy spawn points defined!");
        }

        return true;
    }

    private void OnValidate()
    {
        // Auto-validate when edited in Inspector
        Validate();
    }
}
