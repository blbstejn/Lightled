using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArtifactPickup : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color _artifactColor = new Color(1f, 0.85f, 0.2f); // Gold
    [SerializeField] private float _pulseSpeed = 2f;
    [SerializeField] private float _pulseIntensity = 0.3f;

    private SpriteRenderer _spriteRenderer;
    private float _baseIntensity;

    public Tile OccupiedTile { get; private set; }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _artifactColor;
            _spriteRenderer.sortingOrder = 1; // Above tiles
        }
    }

    private void Update()
    {
        // Pulsing glow effect
        if (_spriteRenderer != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseIntensity;
            _spriteRenderer.color = _artifactColor * pulse;
        }
    }

    public void Init(Tile tile)
    {
        OccupiedTile = tile;
        transform.position = tile.transform.position;
    }

    public void Pickup(BaseUnit unit)
    {
        unit.PickupArtifact();

        // Notify GridManager
        GridManager.Instance.OnArtifactPickedUp(this);

        Destroy(gameObject);
    }
}
