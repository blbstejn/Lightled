using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Wall tile that blocks line of sight and movement.
/// Visually surrounds walkable areas.
/// </summary>
public class WallTile : Tile
{
    [Header("Wall Colors")]
    [SerializeField] private Color _baseColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color _offsetColor = new Color(0.1f, 0.1f, 0.1f);

    /// <summary>
    /// Walls block line of sight.
    /// </summary>
    public override bool BlocksLineOfSight => true;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        bool isOffset = (x + y) % 2 == 1;
        OriginalColor = isOffset ? _offsetColor : _baseColor;
        _renderer.color = OriginalColor;
    }

    // Walls don't highlight or respond to pointer events
    public override void OnPointerEnter(PointerEventData eventData) { }
    public override void OnPointerExit(PointerEventData eventData) { }
    public override void OnPointerClick(PointerEventData eventData) { }
}
