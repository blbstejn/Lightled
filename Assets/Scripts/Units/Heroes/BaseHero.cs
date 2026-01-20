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
        base.Awake();
    }

    public HashSet<Tile> GetLineOfSightTiles()
    {
        HashSet<Tile> result = new();
        
        Vector2Int origin = new(OccupiedTile.X, OccupiedTile.Y);
        Vector2Int forward = Forward;
        Vector2Int side = Perpendicular;

        int depth = 1;

        while (true)
        {
            bool anyTile = false;
            int lateral = shootConeWidth * depth;

            for (int offset = -lateral; offset <= lateral; offset++)
            {
                Vector2Int pos = origin + forward * depth + side * offset;

                Tile tile = GridManager.Instance.GetTileAtPosition(pos);
                if (tile == null)
                    continue;
                
                anyTile = true;
                result.Add(tile);

                if (!tile.Walkable)
                    continue;
            }

            if (!anyTile)
                break;

            depth++;
        }
        
        return result;
    }
}
