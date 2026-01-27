using UnityEngine;

public class MeleeAttackAbility : TargetedAbility
{
    public override string abilityName => "Melee Attack";
    public override int actionPointCost => 1;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null || target == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: unit or target is null");
            return false;
        }

        int heroAP = unit.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Check if AP or AP + CP can cover the cost
        if (heroAP < actionPointCost && (heroAP + availableCP) < actionPointCost)
        {
            Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} cannot afford {actionPointCost} AP (has {heroAP} AP, {availableCP} CP)");
            return false;
        }

        Tile forwardTile = unit.GetForwardTile();
        if (forwardTile == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} has no forward tile");
            return false;
        }

        // IMPORTANT: Check that the clicked tile IS the forward tile
        if (target != forwardTile)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Clicked tile ({target.X},{target.Y}) is not the forward tile ({forwardTile.X},{forwardTile.Y})");
            return false;
        }

        if (forwardTile.OccupiedUnit == null)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Forward tile ({forwardTile.X},{forwardTile.Y}) has no unit");
            return false;
        }

        if (forwardTile.OccupiedUnit.Faction == unit.Faction)
        {
            Debug.Log($"[MeleeAttack] CanExecute: Target {forwardTile.OccupiedUnit.UnitName} is same faction");
            return false;
        }

        Debug.Log($"[MeleeAttack] CanExecute: {unit.UnitName} CAN attack {forwardTile.OccupiedUnit.UnitName} at ({forwardTile.X},{forwardTile.Y})");
        return true;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        Debug.Log($"[MeleeAttack] Execute: {unit.UnitName} attacking tile ({target.X},{target.Y})");

        // Note: CanExecute is checked in Tile.OnPointerClick before calling Execute
        // CP is also spent there if needed, so we only spend remaining AP here

        Tile forwardTile = unit.GetForwardTile();
        if (forwardTile == null || forwardTile.OccupiedUnit == null)
        {
            Debug.LogWarning($"[MeleeAttack] Execute: No valid target in forward tile!");
            return;
        }

        BaseUnit targetUnit = forwardTile.OccupiedUnit;

        // Spend whatever AP the hero has (up to the cost)
        int apToSpend = Mathf.Min(unit.CurrentActionPoints, actionPointCost);
        if (apToSpend > 0)
        {
            unit.SpendActionPoints(apToSpend);
            Debug.Log($"[MeleeAttack] {unit.UnitName} spent {apToSpend} AP (remaining: {unit.CurrentActionPoints})");
        }

        // Melee attack breaks sustained fire
        unit.BreakSustainedFire();

        // Roll dice for melee attack (2d6, need 5+ on either die to hit)
        int roll1 = Random.Range(1, 7);
        int roll2 = Random.Range(1, 7);
        int hitThreshold = 5; // Melee is easier to hit

        Debug.Log($"[MeleeAttack] {unit.UnitName} attacking {targetUnit.UnitName} - Dice rolls: {roll1}, {roll2} (need {hitThreshold}+ to hit)");

        bool hit = (roll1 >= hitThreshold) || (roll2 >= hitThreshold);

        // Log the attack to combat debug
        CombatDebugLog.LogMeleeAttack(unit.UnitName, targetUnit.UnitName, roll1, roll2, hitThreshold, hit);

        if (hit)
        {
            targetUnit.TakeDamage(1, unit);

            if (targetUnit != null && targetUnit.IsAlive)
            {
                Debug.Log($"[MeleeAttack] HIT! {targetUnit.UnitName} takes 1 damage (HP: {targetUnit.CurrentHP}/{targetUnit.maxHP})");
            }
            else
            {
                Debug.Log($"[MeleeAttack] HIT! Target killed!");
                CombatDebugLog.LogKill(targetUnit.UnitName, unit.UnitName);
            }
        }
        else
        {
            Debug.Log($"[MeleeAttack] MISS! Both dice ({roll1}, {roll2}) failed to meet threshold {hitThreshold}+");
        }
    }
}