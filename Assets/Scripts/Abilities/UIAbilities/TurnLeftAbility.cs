using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Turn Left")]
public class TurnLeftAbility : UntargetedAbility
{
    public override string abilityName => "Turn Left";
    public override int actionPointCost => 1;

    public override bool CanExecute(BaseUnit unit)
    {
        if (unit == null)
            return false;

        return unit.CanPay(actionPointCost);
    }

    public override void Execute(BaseUnit unit)
    {
        if (!CanExecute(unit))
            return;

        unit.SpendActionPoints(actionPointCost);
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