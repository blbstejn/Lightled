using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireEffectManager : MonoBehaviour
{
    public static FireEffectManager Instance;

    [SerializeField] private Sprite fireSprite;
    [SerializeField] private float flashDuration = 0.5f;

    private List<FireTileEffect> _activeFireEffects = new List<FireTileEffect>();

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Creates a brief fire flash effect on a tile (visual only, no lasting fire).
    /// Used for flamethrower neighbor tiles to show damage area.
    /// </summary>
    public void CreateFireFlash(Tile tile, float duration = -1f)
    {
        if (tile == null || fireSprite == null)
            return;

        float actualDuration = duration > 0 ? duration : flashDuration;
        StartCoroutine(FireFlashCoroutine(tile, actualDuration));
    }

    private IEnumerator FireFlashCoroutine(Tile tile, float duration)
    {
        // Create temporary fire visual
        GameObject flashObj = new GameObject($"FireFlash_({tile.X},{tile.Y})");
        flashObj.transform.SetParent(tile.transform);
        flashObj.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = flashObj.AddComponent<SpriteRenderer>();
        renderer.sprite = fireSprite;
        renderer.sortingOrder = 3; // Above regular fire
        renderer.color = new Color(1f, 0.8f, 0.4f, 0.9f); // Bright flash color

        // Fade out over duration
        float elapsed = 0f;
        Color startColor = renderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(flashObj);
    }

    public void CreateFireEffect(Tile tile, bool rollDamageImmediately = true)
    {
        // Check if tile already has fire
        FireTileEffect existingFire = _activeFireEffects.Find(f => f.AffectedTile == tile);
        if (existingFire != null)
        {
            return; // Fire already exists on this tile
        }

        GameObject fireObj = new GameObject($"Fire_({tile.X},{tile.Y})");
        fireObj.transform.SetParent(transform);
        FireTileEffect fireEffect = fireObj.AddComponent<FireTileEffect>();
        fireEffect.Initialize(tile, fireSprite);

        _activeFireEffects.Add(fireEffect);

        // Optionally roll damage immediately (skip if flamethrower already did damage rolls)
        if (rollDamageImmediately)
        {
            fireEffect.KillAliensOnTile();
        }
    }

    public void ClearAllFire()
    {
        // Remove all fire effects at the end of alien turn
        List<FireTileEffect> firesCopy = new List<FireTileEffect>(_activeFireEffects);

        foreach (FireTileEffect fire in firesCopy)
        {
            if (fire != null)
            {
                Destroy(fire.gameObject);
            }
        }

        _activeFireEffects.Clear();
        Debug.Log("All fire cleared at end of alien turn");
    }

    public bool IsTileOnFire(Tile tile)
    {
        return _activeFireEffects.Exists(f => f != null && f.AffectedTile == tile);
    }

    // Call this to check if a tile is blocked by fire (for movement validation)
    public bool IsFireBlockingMovement(Tile tile)
    {
        return IsTileOnFire(tile);
    }

    // Fire blocks line of sight
    public bool IsFireBlockingLOS(Tile tile)
    {
        return IsTileOnFire(tile);
    }

    /// <summary>
    /// Checks if a unit entering a tile should take fire damage (adjacent to fire).
    /// Call this when a unit finishes moving.
    /// </summary>
    public void CheckAdjacentFireDamage(BaseUnit unit)
    {
        if (unit == null || !unit.IsAlive || unit.OccupiedTile == null)
            return;

        // Get neighbors of the unit's current tile
        var neighbors = GridManager.Instance.GetNeighbours(unit.OccupiedTile);

        foreach (Tile neighbor in neighbors)
        {
            if (IsTileOnFire(neighbor))
            {
                // Unit entered a tile adjacent to fire - roll for damage
                RollFireDamage(unit);
                return; // Only roll once even if adjacent to multiple fire tiles
            }
        }
    }

    private void RollFireDamage(BaseUnit unit)
    {
        // Flash the unit to show they're being damaged by fire
        unit.FlashHit();

        int roll = Random.Range(1, 7);
        int threshold = 2;
        bool killed = roll >= threshold;

        Debug.Log($"[Fire] {unit.UnitName} entered tile adjacent to fire! Roll: {roll} (need {threshold}+) - {(killed ? "KILLED" : "SURVIVED")}");

        CombatDebugLog.LogFireDamage(unit.UnitName, unit.OccupiedTile.X, unit.OccupiedTile.Y, roll, threshold, killed);

        if (killed)
        {
            // Blips are killed without spawning ghouls
            if (unit is Enemy2 blip)
            {
                blip.KillByFire();
            }
            else
            {
                unit.Kill();
            }
            CombatDebugLog.LogKill(unit.UnitName, "Fire (adjacent)");
        }
    }
}
