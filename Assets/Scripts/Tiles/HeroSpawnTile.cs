using UnityEngine;

public class HeroSpawnTile : WalkableTile
{
    public int HeroIndex { get; private set; }

    public void SetHeroIndex(int index)
    {
        HeroIndex = index;
    }
}
