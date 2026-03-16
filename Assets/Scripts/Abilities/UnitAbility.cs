using UnityEngine;

/// <summary>
/// Base class for all unit abilities.
/// Two main types:
/// - Contextual abilities: Require a tile target (Move, Attack)
/// - UI abilities: Execute directly without a target (Turn Left/Right)
/// </summary>
public abstract class UnitAbility : ScriptableObject
{
    public abstract string abilityName { get; } // Hardcoded in each ability class
    public abstract int actionPointCost { get; } // Hardcoded in each ability class
    public abstract bool isContextual { get; } // Contextual = tile clicks, UI = button clicks

    [Header("UI")]
    [Tooltip("Icon displayed on the ability button. If left empty, the ability name is shown as text instead.")]
    public Sprite icon;

    // For backwards compatibility and targeted abilities
    public abstract bool CanExecute(BaseUnit unit, Tile target);
    public abstract void Execute(BaseUnit unit, Tile target);
}

/// <summary>
/// Base class for abilities that don't require a target (Turn, Skip, etc.)
/// These abilities are triggered from UI buttons.
/// </summary>
public abstract class UntargetedAbility : UnitAbility
{
    public sealed override bool isContextual => false; // UI abilities are never contextual

    /// <summary>
    /// Check if this ability can be executed by the unit.
    /// </summary>
    public abstract bool CanExecute(BaseUnit unit);

    /// <summary>
    /// Execute this ability on the unit.
    /// </summary>
    public abstract void Execute(BaseUnit unit);

    // Implement the base class methods - target is ignored for untargeted abilities
    public sealed override bool CanExecute(BaseUnit unit, Tile target)
    {
        return CanExecute(unit);
    }

    public sealed override void Execute(BaseUnit unit, Tile target)
    {
        Execute(unit);
    }
}