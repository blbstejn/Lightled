using UnityEngine;

/// <summary>
/// Types of actions an enemy AI can decide to take.
/// </summary>
public enum AIActionType
{
    Wait,       // Do nothing, end this enemy's turn
    Move,       // Move to an adjacent tile
    Attack,     // Melee attack forward tile
    Turn,       // Rotate to face a new direction
    Reveal      // Blip intentionally reveals itself (spawns ghouls)
}

/// <summary>
/// Represents an AI decision for what action an enemy should take.
/// </summary>
public class AIAction
{
    public AIActionType Type { get; set; }
    public Tile TargetTile { get; set; }
    public FacingDirection? NewFacing { get; set; }

    public static AIAction Wait() => new AIAction { Type = AIActionType.Wait };

    public static AIAction MoveTo(Tile tile) => new AIAction
    {
        Type = AIActionType.Move,
        TargetTile = tile
    };

    public static AIAction Attack(Tile tile) => new AIAction
    {
        Type = AIActionType.Attack,
        TargetTile = tile
    };

    public static AIAction TurnTo(FacingDirection facing) => new AIAction
    {
        Type = AIActionType.Turn,
        NewFacing = facing
    };

    public static AIAction RevealBlip() => new AIAction { Type = AIActionType.Reveal };
}
