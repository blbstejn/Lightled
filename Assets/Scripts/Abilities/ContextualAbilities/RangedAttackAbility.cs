using UnityEngine;

public class RangedAttackAbility : TargetedAbility
{
    public override string abilityName => "Ranged Attack";
    public override int actionPointCost => 1;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null || target == null)
        {
            Debug.Log($"[RangedAttack] CanExecute: unit or target is null");
            return false;
        }

        // Free shooting after movement
        if (!unit.HasMovedThisTurn)
        {
            int heroAP = unit.CurrentActionPoints;
            int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

            // Check if AP or AP + CP can cover the cost
            if (heroAP < actionPointCost && (heroAP + availableCP) < actionPointCost)
            {
                Debug.Log($"[RangedAttack] CanExecute: {unit.UnitName} cannot afford {actionPointCost} AP (has {heroAP} AP, {availableCP} CP, hasn't moved)");
                return false;
            }
        }
        else
        {
            Debug.Log($"[RangedAttack] CanExecute: {unit.UnitName} has moved - shot is FREE");
        }

        if (unit.Faction != Faction.Hero)
        {
            Debug.Log($"[RangedAttack] CanExecute: {unit.UnitName} is not a hero");
            return false;
        }

        if (target.OccupiedUnit == null)
        {
            Debug.Log($"[RangedAttack] CanExecute: Tile ({target.X},{target.Y}) has no unit");
            return false;
        }

        if (target.OccupiedUnit.Faction == unit.Faction)
        {
            Debug.Log($"[RangedAttack] CanExecute: Target {target.OccupiedUnit.UnitName} is same faction");
            return false;
        }

        // Check line of sight
        if (unit is BaseHero hero)
        {
            var losTiles = hero.GetLineOfSightTiles();
            if (!losTiles.Contains(target))
            {
                Debug.Log($"[RangedAttack] CanExecute: Target tile ({target.X},{target.Y}) is NOT in line of sight (LOS has {losTiles.Count} tiles)");
                return false;
            }
            Debug.Log($"[RangedAttack] CanExecute: {unit.UnitName} CAN shoot {target.OccupiedUnit.UnitName} at ({target.X},{target.Y}) - in LOS");
        }

        return true;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        Debug.Log($"[RangedAttack] Execute: {unit.UnitName} shooting at tile ({target.X},{target.Y})");

        // Note: CanExecute is checked in Tile.OnPointerClick before calling Execute
        // CP is also spent there if needed, so we only spend remaining AP here

        if (target.OccupiedUnit == null)
        {
            Debug.LogWarning($"[RangedAttack] Execute: No target on tile!");
            return;
        }

        BaseUnit targetUnit = target.OccupiedUnit;

        // Only spend AP if unit hasn't moved this turn (free shot after movement)
        if (!unit.HasMovedThisTurn)
        {
            // Spend whatever AP the hero has (up to the cost)
            int apToSpend = Mathf.Min(unit.CurrentActionPoints, actionPointCost);
            if (apToSpend > 0)
            {
                unit.SpendActionPoints(apToSpend);
                Debug.Log($"[RangedAttack] {unit.UnitName} spent {apToSpend} AP (remaining: {unit.CurrentActionPoints})");
            }
        }
        else
        {
            Debug.Log($"[RangedAttack] FREE SHOT - {unit.UnitName} moved this turn, no AP cost!");
        }

        // Determine hit threshold
        int hitThreshold = 6; // Default: need 6+ to hit

        if (unit.HasSustainedFire && unit.SustainedFireTarget == targetUnit)
        {
            hitThreshold = 5; // Sustained fire: need 5+ to hit
            Debug.Log($"[RangedAttack] {unit.UnitName} has SUSTAINED FIRE bonus against {targetUnit.UnitName}! (5+ to hit)");
            CombatDebugLog.LogSustainedFire(unit.UnitName, targetUnit.UnitName);
        }
        else
        {
            Debug.Log($"[RangedAttack] {unit.UnitName} shooting at {targetUnit.UnitName} (6+ to hit)");
        }

        // Roll two dice
        int roll1 = Random.Range(1, 7);
        int roll2 = Random.Range(1, 7);
        Debug.Log($"[RangedAttack] Dice rolls: {roll1}, {roll2} (need {hitThreshold}+ to hit)");

        bool hit = (roll1 >= hitThreshold) || (roll2 >= hitThreshold);

        // Log the attack to combat debug
        CombatDebugLog.LogRangedAttack(unit.UnitName, targetUnit.UnitName, roll1, roll2, hitThreshold, hit);

        if (hit)
        {
            targetUnit.TakeDamage(1, unit);
            Debug.Log($"[RangedAttack] HIT! {targetUnit.UnitName} takes 1 damage");

            // Only set sustained fire target if target survived
            if (targetUnit != null && targetUnit.IsAlive)
            {
                unit.SetSustainedFireTarget(targetUnit);
                Debug.Log($"[RangedAttack] Sustained fire target set to {targetUnit.UnitName}");
            }
            else
            {
                unit.BreakSustainedFire();
                Debug.Log($"[RangedAttack] Target killed! Sustained fire cleared.");
                CombatDebugLog.LogKill(targetUnit.UnitName, unit.UnitName);
            }
        }
        else
        {
            Debug.Log($"[RangedAttack] MISS! Both dice ({roll1}, {roll2}) failed to meet threshold {hitThreshold}+");

            // Set sustained fire target for next shot even on miss
            if (targetUnit != null && targetUnit.IsAlive)
            {
                unit.SetSustainedFireTarget(targetUnit);
                Debug.Log($"[RangedAttack] Sustained fire target set to {targetUnit.UnitName}");
            }
        }
    }
}
