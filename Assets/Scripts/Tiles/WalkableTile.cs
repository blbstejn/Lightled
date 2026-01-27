using UnityEngine;

public class WalkableTile : Tile
{
    [SerializeField] private Color _baseColor, _offsetColor;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        bool isOffset = (x + y) % 2 == 1;
        OriginalColor = isOffset ? _offsetColor : _baseColor;
        _renderer.color = OriginalColor;
    }
}
