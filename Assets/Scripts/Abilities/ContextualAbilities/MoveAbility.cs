using UnityEngine;

public class MoveAbility : TargetedAbility
{
    public override string Name => "Move";
    public override int APCost => 0; // dynamic
    public override bool isContextual => true;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (target == null)
            return false;

        TargetTile = target;
        
        int cost = unit.GetMoveCost(TargetTile);
        return cost <= unit.CurrentActionPoints;
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        unit.MoveOneTile(TargetTile);

        // Mark that unit has moved this turn (enables free shooting)
        unit.SetHasMoved();
    }
}