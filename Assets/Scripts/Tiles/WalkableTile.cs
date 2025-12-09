using UnityEngine;

public class WalkableTile : Tile
{
    [SerializeField] private Color _baseColor, _offsetColor;

    public override void Init(int x, int y)
    {
        bool isOffset = (x + y) % 2 == 1;
        _renderer.color = isOffset ? _offsetColor : _baseColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
