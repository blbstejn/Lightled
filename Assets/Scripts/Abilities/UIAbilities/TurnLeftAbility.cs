using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Turn Left")]
public class TurnLeftAbility : UnitAbility
{
    public override string Name => "Turn Left";
    public override int APCost => 1;
    public override bool isContextual => false;

    public override bool CanExecute(BaseUnit unit, Tile target)
    {
        if (unit == null)
            return false;

        return unit.CanPay(APCost);
    }

    public override void Execute(BaseUnit unit, Tile target)
    {
        if (!CanExecute(unit, target))
            return;

        unit.SpendActionPoints(APCost);
        unit.SetFacing(TurnLeft(unit.Facing));

        // Turning breaks sustained fire (changes line of sight)
        unit.BreakSustainedFire();
    }

    private FacingDirection TurnLeft(FacingDirection dir)
    {
        return dir switch
        {
            FacingDirection.Up    => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Up,
            _ => dir
        };
    }
}