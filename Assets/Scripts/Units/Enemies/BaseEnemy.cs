using UnityEngine;

public class BaseEnemy : BaseUnit
{
    public override void Awake()
    {
        MaxActionPoints = 6; // Enemies have 6 AP
        base.Awake();
    }

    public void TurnFree(FacingDirection newFacing)
    {
        if (facing == newFacing)
            return;
        
        facing = newFacing;
        OnFacingChanged();
    }

    private void OnFacingChanged()
    {
        // hook for anim
    }
}
