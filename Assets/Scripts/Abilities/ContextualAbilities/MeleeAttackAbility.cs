using UnityEditor;
using UnityEngine;

public class MeleeAttackAbility : TargetedAbility
{
    public override string Name => "Melee Attack";
    public override int APCost => 1;
    public override bool isContextual => true;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit.CurrentActionPoints < APCost)
            return false;

        Tile targetTile = unit.GetForwardTile();
        if (targetTile == null)
            return false;

        if (targetTile.OccupiedUnit == null)
            return false;

        return targetTile.OccupiedUnit.Faction != unit.Faction;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        if (!CanExecute(unit, target))
            return;

        Tile targetTile = unit.GetForwardTile();
        BaseUnit targetUnit = targetTile.OccupiedUnit;

        unit.SpendActionPoints(APCost);

        // Melee attack breaks sustained fire
        unit.BreakSustainedFire();

        targetUnit.TakeDamage(1);
    }
}