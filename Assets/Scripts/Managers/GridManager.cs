using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    [SerializeField] private LevelData _currentLevel;
    [SerializeField] private Tile _walkableTile, _voidTile;
    [SerializeField] private Transform _cam;

    [SerializeField] private Color reachableTileColor = Color.darkOliveGreen;
    private readonly HashSet<Tile> _highlightedTiles = new HashSet<Tile>();
    
    [SerializeField] private Color _lineOfSightColor = Color.cadetBlue;
    private readonly HashSet<Tile> _losTiles = new HashSet<Tile>();
    
    private Dictionary<Vector2, Tile> _tiles;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Sets the level to be generated. Call this before GenerateGrid() to change levels.
    /// </summary>
    public void SetLevel(LevelData levelData)
    {
        _currentLevel = levelData;
    }

    /// <summary>
    /// Clears the existing grid tiles from the scene.
    /// </summary>
    public void ClearGrid()
    {
        if (_tiles != null)
        {
            foreach (var tile in _tiles.Values)
            {
                if (tile != null)
                    Destroy(tile.gameObject);
            }
            _tiles.Clear();
        }

        ClearMoveHighlights();
        ClearLineOfSight();
    }

    /// <summary>
    /// Generates the grid based on the currently set LevelData.
    /// Uses ASCII-style string encoding from LevelData.gridRows.
    /// </summary>
    public void GenerateGrid()
    {
        if (_currentLevel == null)
        {
            Debug.LogError("GridManager: Cannot generate grid - no LevelData assigned!");
            return;
        }

        if (!_currentLevel.Validate())
        {
            Debug.LogError($"GridManager: LevelData '{_currentLevel.name}' validation failed!");
            return;
        }

        _tiles = new Dictionary<Vector2, Tile>();

        int width = _currentLevel.Width;
        int height = _currentLevel.Height;

        // Generate grid from string layout
        // Note: Y=0 in array is TOP row, but in world space Y=0 is bottom
        // We flip Y coordinate during instantiation
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isWalkable = _currentLevel.IsWalkable(x, y);
                Tile tileToSpawn = isWalkable ? _walkableTile : _voidTile;

                // Flip Y: bottom row of world = last string in array
                int worldY = height - 1 - y;
                var spawnedTile = Instantiate(tileToSpawn, new Vector3(x, worldY), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {worldY}";
                spawnedTile.Init(x, worldY);

                _tiles[new Vector2(x, worldY)] = spawnedTile;
            }
        }

        // Center camera on grid
        _cam.transform.position = new Vector3(
            (float)width / 2 - 0.5f,
            (float)height / 2 - 0.5f,
            -10
        );

        GameManager.Instance.ChangeState(GameState.SpawnHeroes);
    }
    
    public IEnumerable<Tile> GetNeighbours(Tile tile)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                Vector2 pos = new Vector2(tile.X + dx, tile.Y + dy);
                if (_tiles.TryGetValue(pos, out Tile neighbour))
                    yield return neighbour;
            }
        }
    }

    public void ShowImmediateMoveOptions(BaseUnit unit)
    {
        ClearMoveHighlights();
        
        Tile origin = unit.OccupiedTile;

        foreach (Tile neighbour in GetNeighbours(origin))
        {
            if (unit.CanMoveTo(neighbour))
            {
                neighbour.GetComponent<SpriteRenderer>().color = reachableTileColor;
                _highlightedTiles.Add(neighbour);
            }
        }
    }

    public void ClearMoveHighlights()
    {
        foreach (Tile tile in _highlightedTiles)
            tile.GetComponent<SpriteRenderer>().color = Color.white;
        
        _highlightedTiles.Clear();
    }

    public Tile GetHeroSpawnTile()
    {
        if (_currentLevel.heroSpawnPoints != null && _currentLevel.heroSpawnPoints.Length > 0)
        {
            // Use defined spawn points
            Vector2Int spawnPoint = _currentLevel.heroSpawnPoints[Random.Range(0, _currentLevel.heroSpawnPoints.Length)];

            // Flip Y coordinate to match world space
            int worldY = _currentLevel.Height - 1 - spawnPoint.y;
            return GetTileAtPosition(new Vector2(spawnPoint.x, worldY));
        }

        // Fallback: random walkable tile on left half
        return _tiles.Where(t => t.Key.x < _currentLevel.Width / 2 && t.Value.Walkable)
                     .OrderBy(t => Random.value)
                     .First().Value;
    }

    public Tile GetEnemySpawnTile()
    {
        if (_currentLevel.enemySpawnPoints != null && _currentLevel.enemySpawnPoints.Length > 0)
        {
            // Use defined spawn points
            Vector2Int spawnPoint = _currentLevel.enemySpawnPoints[Random.Range(0, _currentLevel.enemySpawnPoints.Length)];

            // Flip Y coordinate to match world space
            int worldY = _currentLevel.Height - 1 - spawnPoint.y;
            return GetTileAtPosition(new Vector2(spawnPoint.x, worldY));
        }

        // Fallback: random walkable tile on right half
        return _tiles.Where(t => t.Key.x > _currentLevel.Width / 2 && t.Value.Walkable)
                     .OrderBy(t => Random.value)
                     .First().Value;
    }
    
    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (_tiles.TryGetValue(pos, out var tile))
        {
            return tile;
        }
        
        return null;
    }

    public void ShowLineOfSight(BaseHero hero)
    {
        ClearLineOfSight();

        foreach (Tile tile in hero.GetLineOfSightTiles())
        {
            tile.GetComponent<SpriteRenderer>().color = _lineOfSightColor;
            _losTiles.Add(tile);
        }
    }

    public void ClearLineOfSight()
    {
        foreach (Tile tile in _losTiles)
            tile.GetComponent<SpriteRenderer>().color = Color.white;
        
        _losTiles.Clear();
    }
}
