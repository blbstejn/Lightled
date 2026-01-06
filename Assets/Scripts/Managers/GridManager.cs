using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    [SerializeField] private int _width, _height;
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
    
    // use a string matrix to setup the grid the exact way i want it to be (ascii)
    public void GenerateGrid()
    {
        _tiles = new Dictionary<Vector2, Tile>();
        
        for ( int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var randomTile = Random.Range(0, 6) >= 3 ? _walkableTile : _voidTile;
                var spawnedTile = Instantiate(randomTile, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
                
                spawnedTile.Init(x, y);
                
                _tiles[new Vector2(x, y)] = spawnedTile;
            }
        }

        _cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);

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
        return _tiles.Where(t => t.Key.x < _width / 2 && t.Value.Walkable).OrderBy(t => Random.value).First().Value;
    }

    public Tile GetEnemySpawnTile()
    {
        return _tiles.Where(t => t.Key.x > _width / 2 && t.Value.Walkable).OrderBy(t => Random.value).First().Value;
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
