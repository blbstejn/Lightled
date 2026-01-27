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

        int heroAP = unit.CurrentActionPoints;
        int availableCP = GameManager.Instance != null ? GameManager.Instance.CurrentCommandPoints : 0;

        // Can execute if AP covers it, OR AP + CP covers it
        return heroAP >= actionPointCost || (heroAP + availableCP) >= actionPointCost;
    }

    public override void Execute(BaseUnit unit)
    {
        // Note: CanExecute is checked before calling Execute
        // CP is also spent by caller if needed, so we only spend remaining AP here

        // Spend whatever AP the hero has (up to the cost)
        int apToSpend = Mathf.Min(unit.CurrentActionPoints, actionPointCost);
        if (apToSpend > 0)
        {
            unit.SpendActionPoints(apToSpend);
        }

        unit.SetFacing(TurnRight(unit.Facing));

        // Turning breaks sustained fire (changes line of sight)
        unit.BreakSustainedFire();

        // Check if any blips are now revealed after turning
        Debug.Log($"[Turn Right] {unit.UnitName} turned right - checking blips for reveal");
        UnitManager.Instance.CheckAllBlipsForReveal();
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