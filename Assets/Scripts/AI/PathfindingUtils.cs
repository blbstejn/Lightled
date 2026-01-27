using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Static utility methods for AI pathfinding and LOS calculations.
/// </summary>
public static class PathfindingUtils
{
    /// <summary>
    /// Gets all tiles visible to any living hero (combined line of sight).
    /// </summary>
    public static HashSet<Tile> GetCombinedHeroLOS()
    {
        HashSet<Tile> combined = new HashSet<Tile>();

        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (hero != null && hero.IsAlive)
            {
                combined.UnionWith(hero.GetLineOfSightTiles());
            }
        }

        return combined;
    }

    /// <summary>
    /// Calculates Manhattan distance between two tiles.
    /// </summary>
    public static int ManhattanDistance(Tile a, Tile b)
    {
        if (a == null || b == null)
            return int.MaxValue;

        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Gets the cardinal direction from one tile toward another.
    /// Prefers the axis with the greater difference.
    /// </summary>
    public static FacingDirection GetDirectionToward(Tile from, Tile to)
    {
        if (from == null || to == null)
            return FacingDirection.Up;

        int dx = to.X - from.X;
        int dy = to.Y - from.Y;

        // Prefer the axis with greater difference
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return dx > 0 ? FacingDirection.Right : FacingDirection.Left;
        }
        else
        {
            return dy > 0 ? FacingDirection.Up : FacingDirection.Down;
        }
    }

    /// <summary>
    /// Finds the nearest living hero to the given enemy.
    /// </summary>
    public static BaseHero FindNearestHero(BaseEnemy enemy)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return null;

        BaseHero nearest = null;
        int minDistance = int.MaxValue;

        foreach (var hero in UnitManager.Instance.SpawnedHeroes)
        {
            if (hero == null || !hero.IsAlive || hero.OccupiedTile == null)
                continue;

            int dist = ManhattanDistance(enemy.OccupiedTile, hero.OccupiedTile);

            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = hero;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Gets adjacent tiles that are safe (walkable and NOT in hero LOS).
    /// </summary>
    public static List<Tile> GetSafeMoves(BaseEnemy enemy, HashSet<Tile> heroLOS)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return new List<Tile>();

        return GridManager.Instance.GetNeighbours(enemy.OccupiedTile)
            .Where(t => t != null && t.Walkable && !heroLOS.Contains(t))
            .ToList();
    }

    /// <summary>
    /// Gets all adjacent tiles that are walkable.
    /// </summary>
    public static List<Tile> GetWalkableMoves(BaseEnemy enemy)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return new List<Tile>();

        return GridManager.Instance.GetNeighbours(enemy.OccupiedTile)
            .Where(t => t != null && t.Walkable)
            .ToList();
    }

    /// <summary>
    /// Checks if the enemy is adjacent to any living hero.
    /// </summary>
    public static bool IsAdjacentToAnyHero(BaseEnemy enemy)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return false;

        var neighbors = GridManager.Instance.GetNeighbours(enemy.OccupiedTile);

        foreach (var tile in neighbors)
        {
            if (tile?.OccupiedUnit is BaseHero hero && hero.IsAlive)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the hero that the enemy is adjacent to (if any).
    /// Returns the first one found.
    /// </summary>
    public static BaseHero GetAdjacentHero(BaseEnemy enemy)
    {
        if (enemy == null || enemy.OccupiedTile == null)
            return null;

        var neighbors = GridManager.Instance.GetNeighbours(enemy.OccupiedTile);

        foreach (var tile in neighbors)
        {
            if (tile?.OccupiedUnit is BaseHero hero && hero.IsAlive)
                return hero;
        }

        return null;
    }

    /// <summary>
    /// Finds the best safe move toward a target (closest to target while avoiding hero LOS).
    /// Returns null if no safe moves exist.
    /// </summary>
    public static Tile FindBestSafeMoveToward(BaseEnemy enemy, Tile target, HashSet<Tile> heroLOS)
    {
        var safeMoves = GetSafeMoves(enemy, heroLOS);

        if (safeMoves.Count == 0)
            return null;

        // Sort by distance to target (ascending) then by movement cost (prefer cheaper moves)
        return safeMoves
            .OrderBy(t => ManhattanDistance(t, target))
            .ThenBy(t => enemy.GetMoveCost(t))
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds the best move toward a target (ignoring LOS, for ghouls).
    /// Returns null if no walkable moves exist.
    /// </summary>
    public static Tile FindBestMoveToward(BaseEnemy enemy, Tile target)
    {
        var walkableMoves = GetWalkableMoves(enemy);

        if (walkableMoves.Count == 0)
            return null;

        // Sort by distance to target (ascending) then by movement cost (prefer cheaper moves)
        return walkableMoves
            .OrderBy(t => ManhattanDistance(t, target))
            .ThenBy(t => enemy.GetMoveCost(t))
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a hero is facing toward the enemy's position.
    /// Returns true if the hero could potentially see the enemy in their forward direction.
    /// </summary>
    public static bool IsHeroFacingToward(BaseHero hero, Tile enemyTile)
    {
        if (hero == null || hero.OccupiedTile == null || enemyTile == null)
            return false;

        int dx = enemyTile.X - hero.OccupiedTile.X;
        int dy = enemyTile.Y - hero.OccupiedTile.Y;

        // Check if enemy is in the general direction the hero is facing
        switch (hero.Facing)
        {
            case FacingDirection.Up:
                return dy > 0; // Enemy is above hero
            case FacingDirection.Down:
                return dy < 0; // Enemy is below hero
            case FacingDirection.Right:
                return dx > 0; // Enemy is to the right
            case FacingDirection.Left:
                return dx < 0; // Enemy is to the left
            default:
                return false;
        }
    }
}
