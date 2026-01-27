using UnityEngine;

public class MoveAbility : TargetedAbility
{
    public override string abilityName => "Move";
    public override int actionPointCost => 0; // Dynamic - calculated per movement

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (target == null)
            return false;

        TargetTile = target;

        int cost = unit.GetMoveCost(TargetTile);
        int heroAP = unit.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Can move if AP covers it, OR AP + CP covers it
        return cost <= heroAP || cost <= (heroAP + availableCP);
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        unit.MoveOneTile(TargetTile);

        // Mark that unit has moved this turn (enables free shooting)
        unit.SetHasMoved();
    }
}