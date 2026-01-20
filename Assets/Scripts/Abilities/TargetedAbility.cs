using UnityEngine;

/// <summary>
/// Base class for abilities that require a tile target (Move, Attack, etc.)
/// These abilities are triggered by clicking on tiles.
/// </summary>
public abstract class TargetedAbility : UnitAbility
{
    public sealed override bool isContextual => true; // Targeted abilities are always contextual

    /// <summary>
    /// The tile that was targeted (set before execution).
    /// </summary>
    public Tile TargetTile { get; protected set; }

    /// <summary>
    /// Set the target tile for this ability.
    /// </summary>
    public virtual void SetTarget(Tile tile)
    {
        TargetTile = tile;
    }
}