using UnityEngine;

public class BaseEnemy : BaseUnit
{
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
