using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Wall tile that blocks line of sight and movement.
/// Visually surrounds walkable areas.
/// </summary>
public class WallTile : Tile
{
    public override bool BlocksLineOfSight => true;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        OriginalColor = _renderer.color;
    }

    // Walls don't highlight or respond to pointer events
    public override void OnPointerEnter(PointerEventData eventData) { }
    public override void OnPointerExit(PointerEventData eventData) { }
    public override void OnPointerClick(PointerEventData eventData) { }
}
