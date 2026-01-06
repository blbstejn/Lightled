using UnityEditor.Experimental.GraphView;
using UnityEngine;

public static class DirectionUtils
{
    public static Vector2Int ToVector(FacingDirection dir)
    {
        return dir switch
        {
            FacingDirection.Up => Vector2Int.up,
            FacingDirection.Down => Vector2Int.down,
            FacingDirection.Left => Vector2Int.left,
            FacingDirection.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }

    public static FacingDirection FromVector(Vector2Int vector)
    {
        if (vector == Vector2Int.up)
            return FacingDirection.Up;
        if (vector == Vector2Int.down)
            return FacingDirection.Down;
        if (vector == Vector2Int.left)
            return FacingDirection.Left;
        if (vector == Vector2Int.left)
            return FacingDirection.Left;
        
        return FacingDirection.Up;
    }
}