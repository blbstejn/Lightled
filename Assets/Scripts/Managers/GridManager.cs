using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    [SerializeField] private LevelData _currentLevel;

    [Header("Tile Prefabs")]
    [SerializeField] private Tile _walkableTile;
    [SerializeField] private Tile _voidTile;
    [SerializeField] private WallTile _wallTile;
    [SerializeField] private HeroSpawnTile _heroSpawnTile;
    [SerializeField] private BlipSpawnTile _blipSpawnTile;
    [SerializeField] private TargetTile _targetTile;

    [Header("Pickup Prefabs")]
    [SerializeField] private ArtifactPickup _artifactPrefab;

    [SerializeField] private Transform _cam;

    [SerializeField] private Color reachableTileColor = Color.darkOliveGreen;
    private readonly HashSet<Tile> _highlightedTiles = new HashSet<Tile>();

    [SerializeField] private Color _lineOfSightColor = Color.cadetBlue;
    private readonly HashSet<Tile> _losTiles = new HashSet<Tile>();

    private Dictionary<Vector2, Tile> _tiles;
    private Dictionary<int, HeroSpawnTile> _heroSpawnTiles = new Dictionary<int, HeroSpawnTile>();
    private List<BlipSpawnTile> _blipSpawnTiles = new List<BlipSpawnTile>();
    private List<TargetTile> _targetTiles = new List<TargetTile>();
    private List<ArtifactPickup> _artifacts = new List<ArtifactPickup>();

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

        _heroSpawnTiles.Clear();
        _blipSpawnTiles.Clear();
        _targetTiles.Clear();

        foreach (var artifact in _artifacts)
        {
            if (artifact != null)
                Destroy(artifact.gameObject);
        }
        _artifacts.Clear();

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
        _heroSpawnTiles.Clear();
        _blipSpawnTiles.Clear();
        _targetTiles.Clear();
        _artifacts.Clear();

        int width = _currentLevel.Width;
        int height = _currentLevel.Height;

        // Generate grid from string layout
        // Note: Y=0 in array is TOP row, but in world space Y=0 is bottom
        // We flip Y coordinate during instantiation
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TileType tileType = _currentLevel.GetTileType(x, y);

                // Flip Y: bottom row of world = last string in array
                int worldY = height - 1 - y;
                Tile spawnedTile = SpawnTileOfType(tileType, x, y, worldY);

                spawnedTile.name = $"Tile {x} {worldY}";
                spawnedTile.Init(x, worldY);

                _tiles[new Vector2(x, worldY)] = spawnedTile;
            }
        }

        // Center camera on grid and fit to level size
        FitCameraToGrid(width, height);

        // Spawn artifacts on their designated tiles
        SpawnArtifacts();

        GameManager.Instance.ChangeState(GameState.SpawnHeroes);
    }

    // Tiles that need artifacts spawned on them after grid generation
    private List<Tile> _pendingArtifactTiles = new List<Tile>();

    private Tile SpawnTileOfType(TileType tileType, int gridX, int gridY, int worldY)
    {
        Vector3 position = new Vector3(gridX, worldY);

        switch (tileType)
        {
            case TileType.HeroSpawn:
                var heroTile = Instantiate(_heroSpawnTile, position, Quaternion.identity);
                int heroIndex = _currentLevel.GetHeroSpawnIndex(gridX, gridY);
                heroTile.SetHeroIndex(heroIndex);
                _heroSpawnTiles[heroIndex] = heroTile;
                return heroTile;

            case TileType.BlipSpawn:
                var blipTile = Instantiate(_blipSpawnTile, position, Quaternion.identity);
                _blipSpawnTiles.Add(blipTile);
                return blipTile;

            case TileType.ArtifactSpawn:
                // Spawn a walkable tile, artifact will be placed on it after
                var walkableTileForArtifact = Instantiate(_walkableTile, position, Quaternion.identity);
                _pendingArtifactTiles.Add(walkableTileForArtifact);
                return walkableTileForArtifact;

            case TileType.Target:
                var targetTile = Instantiate(_targetTile, position, Quaternion.identity);
                _targetTiles.Add(targetTile);
                return targetTile;

            case TileType.Wall:
                return Instantiate(_wallTile, position, Quaternion.identity);

            case TileType.Void:
                return Instantiate(_voidTile, position, Quaternion.identity);

            case TileType.Walkable:
            default:
                return Instantiate(_walkableTile, position, Quaternion.identity);
        }
    }

    private void SpawnArtifacts()
    {
        foreach (var tile in _pendingArtifactTiles)
        {
            if (_artifactPrefab != null)
            {
                var artifact = Instantiate(_artifactPrefab, tile.transform.position, Quaternion.identity);
                artifact.Init(tile);
                _artifacts.Add(artifact);
            }
        }
        _pendingArtifactTiles.Clear();
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
            tile.GetComponent<SpriteRenderer>().color = tile.OriginalColor;

        _highlightedTiles.Clear();
    }

    /// <summary>
    /// Gets the spawn tile for a specific hero (1-5).
    /// </summary>
    public HeroSpawnTile GetHeroSpawnTile(int heroIndex)
    {
        if (_heroSpawnTiles.TryGetValue(heroIndex, out var tile))
            return tile;
        return null;
    }

    /// <summary>
    /// Gets a random available hero spawn tile.
    /// </summary>
    public Tile GetHeroSpawnTile()
    {
        // Find an unoccupied hero spawn tile
        var availableSpawnTile = _heroSpawnTiles.Values
            .Where(t => t != null && t.Walkable)
            .OrderBy(t => Random.value)
            .FirstOrDefault();

        if (availableSpawnTile != null)
            return availableSpawnTile;

        // Fallback: random walkable tile on left half
        return _tiles.Where(t => t.Key.x < _currentLevel.Width / 2 && t.Value.Walkable)
                     .OrderBy(t => Random.value)
                     .First().Value;
    }

    /// <summary>
    /// Gets all hero spawn tiles in the level.
    /// </summary>
    public IReadOnlyDictionary<int, HeroSpawnTile> GetAllHeroSpawnTiles()
    {
        return _heroSpawnTiles;
    }

    public Tile GetEnemySpawnTile()
    {
        // Use blip spawn tiles defined in the grid
        var availableBlipTile = _blipSpawnTiles
            .Where(t => t != null && t.OccupiedUnit == null)
            .OrderBy(t => Random.value)
            .FirstOrDefault();

        if (availableBlipTile != null)
            return availableBlipTile;

        // Fallback: random walkable tile on right half
        return _tiles.Where(t => t.Key.x > _currentLevel.Width / 2 && t.Value.Walkable)
                     .OrderBy(t => Random.value)
                     .First().Value;
    }

    /// <summary>
    /// Gets a random available blip spawn tile for AI-controlled enemy spawning.
    /// </summary>
    public BlipSpawnTile GetBlipSpawnTile()
    {
        return _blipSpawnTiles
            .Where(t => t != null && t.OccupiedUnit == null)
            .OrderBy(t => Random.value)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets all blip spawn tiles in the level.
    /// </summary>
    public IReadOnlyList<BlipSpawnTile> GetAllBlipSpawnTiles()
    {
        return _blipSpawnTiles;
    }

    /// <summary>
    /// Gets all artifacts in the level.
    /// </summary>
    public IReadOnlyList<ArtifactPickup> GetAllArtifacts()
    {
        return _artifacts;
    }

    /// <summary>
    /// Gets the artifact on a specific tile, if any.
    /// </summary>
    public ArtifactPickup GetArtifactOnTile(Tile tile)
    {
        return _artifacts.FirstOrDefault(a => a != null && a.OccupiedTile == tile);
    }

    /// <summary>
    /// Called when an artifact is picked up.
    /// </summary>
    public void OnArtifactPickedUp(ArtifactPickup artifact)
    {
        _artifacts.Remove(artifact);
    }

    /// <summary>
    /// Gets all target tiles in the level.
    /// </summary>
    public IReadOnlyList<TargetTile> GetAllTargetTiles()
    {
        return _targetTiles;
    }

    /// <summary>
    /// Checks if any target tile is occupied by a hero.
    /// </summary>
    public bool IsAnyTargetOccupiedByHero()
    {
        return _targetTiles.Any(t => t.IsOccupiedByHero());
    }

    /// <summary>
    /// Gets all target tiles currently occupied by heroes.
    /// </summary>
    public IEnumerable<TargetTile> GetOccupiedTargetTiles()
    {
        return _targetTiles.Where(t => t.IsOccupiedByHero());
    }
    
    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (_tiles.TryGetValue(pos, out var tile))
        {
            return tile;
        }

        return null;
    }

    /// <summary>
    /// Positions and sizes the camera to fit the entire grid with padding.
    /// Also sets up CameraController bounds if present.
    /// </summary>
    private void FitCameraToGrid(int width, int height)
    {
        Camera cam = _cam.GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
        {
            // Fallback: just center the camera
            _cam.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10);
            return;
        }

        float padding = 1f; // Extra space around the grid

        // Calculate orthographic size to fit the grid
        float screenRatio = (float)Screen.width / Screen.height;
        float gridRatio = (float)width / height;

        float orthoSize;
        if (gridRatio > screenRatio)
        {
            // Grid is wider than screen - fit by width
            orthoSize = (width / screenRatio / 2f) + padding;
        }
        else
        {
            // Grid is taller than screen - fit by height
            orthoSize = (height / 2f) + padding;
        }

        cam.orthographicSize = orthoSize;

        // Center position on grid
        float centerX = (width - 1) / 2f;
        float centerY = (height - 1) / 2f;

        _cam.transform.position = new Vector3(centerX, centerY, -10);

        // Set up CameraController bounds if present
        CameraController cameraController = _cam.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetBounds(width, height, 3f);
        }
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
            tile.GetComponent<SpriteRenderer>().color = tile.OriginalColor;

        _losTiles.Clear();
    }
}
