using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Turn Right")]
public class TurnRightAbility : UntargetedAbility
{
    public override string abilityName => "Turn Right";
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