using UnityEngine;

public class RangedAttackAbility : TargetedAbility
{
    public override string Name => "Ranged Attack";
    public override int APCost => 1;
    public override bool isContextual => true;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null || target == null)
            return false;

        // Free shooting after movement
        if (!unit.HasMovedThisTurn && !unit.CanPay(APCost))
            return false;

        if (unit.Faction != Faction.Hero)
            return false;

        if (target.OccupiedUnit == null)
            return false;

        if (target.OccupiedUnit.Faction == unit.Faction)
            return false;

        // Check line of sight
        if (unit is BaseHero hero)
        {
            var losTiles = hero.GetLineOfSightTiles();
            if (!losTiles.Contains(target))
                return false;
        }

        return true;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        if (!CanExecute(unit, target))
            return;

        BaseUnit targetUnit = target.OccupiedUnit;

        // Only spend AP if unit hasn't moved this turn
        if (!unit.HasMovedThisTurn)
        {
            unit.SpendActionPoints(APCost);
        }

        // Determine hit threshold
        int hitThreshold = 6; // Default: need 6+ to hit
        bool isSustainedFire = false;

        if (unit.HasSustainedFire && unit.SustainedFireTarget == targetUnit)
        {
            hitThreshold = 5; // Sustained fire: need 5+ to hit
            isSustainedFire = true;
            Debug.Log($"{unit.UnitName} has sustained fire bonus against {targetUnit.UnitName}! (5+ to hit)");
        }
        else
        {
            Debug.Log($"{unit.UnitName} shooting at {targetUnit.UnitName} (6+ to hit)");
        }

        // Roll two dice
        int roll1 = Random.Range(1, 7);
        int roll2 = Random.Range(1, 7);
        Debug.Log($"Dice rolls: {roll1}, {roll2}");

        bool hit = (roll1 >= hitThreshold) || (roll2 >= hitThreshold);

        if (hit)
        {
            targetUnit.TakeDamage(1, unit);
            Debug.Log($"HIT! {targetUnit.UnitName} takes 1 damage");
        }
        else
        {
            Debug.Log($"MISS! Both dice failed to hit");
        }

        // Set sustained fire target for next shot
        unit.SetSustainedFireTarget(targetUnit);

        // If shot was free (after movement), log it
        if (unit.HasMovedThisTurn)
        {
            Debug.Log($"Free shot after movement!");
        }
    }
}
