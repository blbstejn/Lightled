using UnityEngine;
using UnityEngine.EventSystems;

public class VoidTile : Tile
{
    /// <summary>
    /// Void tiles block line of sight (just like walls).
    /// </summary>
    public override bool BlocksLineOfSight => true;

    public override void Init(int x, int y)
    {
        base.Init(x, y);
        OriginalColor = _renderer != null ? _renderer.color : Color.black;
    }

    // Void tiles don't highlight or respond to pointer events
    public override void OnPointerEnter(PointerEventData eventData) { }
    public override void OnPointerExit(PointerEventData eventData) { }
    public override void OnPointerClick(PointerEventData eventData) { }
}
