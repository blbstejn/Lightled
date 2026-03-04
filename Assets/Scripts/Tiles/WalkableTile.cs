using UnityEngine;

public class WalkableTile : Tile
{
    public override void Init(int x, int y)
    {
        base.Init(x, y);
        OriginalColor = _renderer.color;
    }
}
