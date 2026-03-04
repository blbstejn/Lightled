using UnityEngine;

public class TargetTile : Tile
{
    [SerializeField] private GameObject _targetVisual;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        OriginalColor = _renderer.color;

        if (_targetVisual != null)
            _targetVisual.SetActive(true);
    }

    public bool IsOccupiedByHero()
    {
        return OccupiedUnit != null && OccupiedUnit is BaseHero;
    }
}
