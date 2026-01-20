using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Turn Right")]
public class TurnRightAbility : UnitAbility
{
    public override string Name => "Turn Right";
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
        unit.SetFacing(TurnRight(unit.Facing));

        // Turning breaks sustained fire (changes line of sight)
        unit.BreakSustainedFire();
    }

    private FacingDirection TurnRight(FacingDirection dir)
    {
        return dir switch
        {
            FacingDirection.Up    => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Up,
            _ => dir
        };
    }
}