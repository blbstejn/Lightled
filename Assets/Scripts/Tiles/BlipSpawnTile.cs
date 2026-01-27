using UnityEngine;

public class BlipSpawnTile : Tile
{
    [SerializeField] private Color _baseColor = new Color(0.4f, 0.2f, 0.2f);
    [SerializeField] private Color _offsetColor = new Color(0.35f, 0.15f, 0.15f);

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        bool isOffset = (x + y) % 2 == 1;
        OriginalColor = isOffset ? _offsetColor : _baseColor;
        _renderer.color = OriginalColor;
    }
}
