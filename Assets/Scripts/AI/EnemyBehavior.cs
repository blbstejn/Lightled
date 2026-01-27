using UnityEngine;

/// <summary>
/// Abstract base class for enemy AI behaviors.
/// Subclasses implement specific decision-making logic for different enemy types.
/// </summary>
public abstract class EnemyBehavior : ScriptableObject
{
    /// <summary>
    /// Display name for this behavior (used for debugging).
    /// </summary>
    public abstract string BehaviorName { get; }

    /// <summary>
    /// Decides what action the enemy should take given its current state.
    /// Called each time the enemy has AP to spend during its turn.
    /// </summary>
    /// <param name="enemy">The enemy making the decision</param>
    /// <returns>The action to take</returns>
    public abstract AIAction DecideAction(BaseEnemy enemy);
}
