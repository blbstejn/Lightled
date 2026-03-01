using UnityEngine;

/// <summary>
/// Overwatch ability - Unit steadies itself to react to enemy movement.
/// While on overwatch, when an enemy enters a tile in the hero's line of sight,
/// the hero makes a free ranged attack.
///
/// Overwatch ends when:
/// - The hero takes another action (move, turn, attack, etc.)
/// - The start of the next hero turn
///
/// Note: Does not immediately shoot when activated.
/// Sustained fire bonus applies to overwatch shots.
/// </summary>
[CreateAssetMenu(menuName = "Abilities/Overwatch")]
public class OverwatchAbility : UntargetedAbility
{
    public override string abilityName => "Overwatch";
    public override int actionPointCost => 2;

    public override bool CanExecute(BaseUnit unit)
    {
        if (unit == null)
            return false;

        // Must be a hero
        if (unit is not BaseHero hero)
            return false;

        // Already on overwatch - can't activate again
        if (hero.IsOnOverwatch)
            return false;

        int heroAP = unit.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Can execute if AP covers it, OR AP + CP covers it
        return heroAP >= actionPointCost || (heroAP + availableCP) >= actionPointCost;
    }

    public override void Execute(BaseUnit unit)
    {
        if (unit is not BaseHero hero)
            return;

        // Spend whatever AP the hero has (up to the cost)
        int apToSpend = Mathf.Min(unit.CurrentActionPoints, actionPointCost);
        if (apToSpend > 0)
        {
            unit.SpendActionPoints(apToSpend);
        }

        // Enter overwatch state
        hero.EnterOverwatch();

        Debug.Log($"[Overwatch] {hero.UnitName} is now on OVERWATCH! Watching {hero.OverwatchLOSTiles?.Count ?? 0} tiles.");

        // Check if any blips are now revealed (turning might reveal them)
        UnitManager.Instance.CheckAllBlipsForReveal();
    }
}
