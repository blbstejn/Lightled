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

    public List<Tile> FindPath(Tile start, Tile goal)
    {
        var frontier = new PriorityQueue<Tile>();
        frontier.Enqueue(start, 0);
        
        Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();
        Dictionary<Tile, float> costSoFar = new Dictionary<Tile, float>();
        
        cameFrom[start] = null;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Tile current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (Tile neighbour in GetNeighbours(current))
            {
                if (!neighbour.Walkable && neighbour != goal)
                    continue;
                
                float newCost = costSoFar[current] + 1f;

                if (!costSoFar.ContainsKey(neighbour) || newCost < costSoFar[neighbour])
                {
                    costSoFar[neighbour] = newCost;
                    float priority = newCost + Mathf.Abs(neighbour.X - goal.X) + Mathf.Abs(neighbour.Y - goal.Y);
                    frontier.Enqueue(neighbour, priority);
                    cameFrom[neighbour] = current;
                }
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private List<Tile> ReconstructPath(Dictionary<Tile, Tile> cameFrom, Tile start, Tile goal)
    {
        List<Tile> path = new List<Tile>();

        if (!cameFrom.ContainsKey(goal))
            return path;

        Tile current = goal;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        
        path.Add(start);
        path.Reverse();
        
        return path;
    }

    public IEnumerable<Tile> GetNeighbours(Tile tile)
    {
        Vector2[] dirs =
        {
            new Vector2(1, 0),
            new Vector2(-1, 0),
            new Vector2(0, 1),
            new Vector2(0, -1),
        };

        foreach (var dir in dirs)
        {
            Vector2 checkPos = new Vector2(tile.X + (int)dir.x, tile.Y + (int)dir.y);

            if (_tiles.TryGetValue(checkPos, out Tile neighbour))
                yield return neighbour;
        }
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
}
