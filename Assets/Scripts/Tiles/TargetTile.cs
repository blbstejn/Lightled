using UnityEngine;

public class TargetTile : Tile
{
    [SerializeField] private Color _baseColor = new Color(0.2f, 0.4f, 0.6f);
    [SerializeField] private Color _offsetColor = new Color(0.15f, 0.35f, 0.55f);
    [SerializeField] private GameObject _targetVisual;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        bool isOffset = (x + y) % 2 == 1;
        OriginalColor = isOffset ? _offsetColor : _baseColor;
        _renderer.color = OriginalColor;

        if (_targetVisual != null)
            _targetVisual.SetActive(true);
    }

    public bool IsOccupiedByHero()
    {
        return OccupiedUnit != null && OccupiedUnit is BaseHero;
    }
}
