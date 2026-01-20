using UnityEngine;

/// <summary>
/// Automatically scales a unit to fit within a 1x1 tile.
/// Attach this to any unit prefab and it will scale to fit tiles perfectly.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class UnitScaler : MonoBehaviour
{
    [Tooltip("Target size in world units (should match tile size)")]
    [SerializeField] private float targetSize = 1f;

    [Tooltip("Optional padding to make unit slightly smaller than tile (0-1)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float padding = 0.05f;

    [Tooltip("Auto-scale on start")]
    [SerializeField] private bool scaleOnAwake = true;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        if (scaleOnAwake)
        {
            ScaleToFitTile();
        }
    }

    private void OnValidate()
    {
        // Auto-scale in editor when values change
        ScaleToFitTile();
    }

    /// <summary>
    /// Scales the unit to fit within the target tile size.
    /// </summary>
    public void ScaleToFitTile()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            return;

        // Get sprite bounds in local space (unscaled)
        Sprite sprite = _spriteRenderer.sprite;
        float spriteWidth = sprite.bounds.size.x;
        float spriteHeight = sprite.bounds.size.y;

        if (spriteWidth <= 0 || spriteHeight <= 0)
            return;

        // Calculate scale needed to fit in target size with padding
        float effectiveSize = targetSize * (1f - padding * 2f);

        // Use the larger dimension to ensure the sprite fits within the tile
        float maxDimension = Mathf.Max(spriteWidth, spriteHeight);
        float scale = effectiveSize / maxDimension;

        // Apply uniform scale
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>
    /// Editor button to manually trigger scaling.
    /// </summary>
    [ContextMenu("Scale To Fit Tile")]
    private void ScaleToFitTileMenu()
    {
        ScaleToFitTile();
        Debug.Log($"Scaled {gameObject.name} to fit {targetSize}x{targetSize} tile (scale: {transform.localScale.x:F3})");
    }
}
