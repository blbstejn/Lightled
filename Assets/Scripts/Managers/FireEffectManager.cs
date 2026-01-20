using System.Collections.Generic;
using UnityEngine;

public class FireEffectManager : MonoBehaviour
{
    public static FireEffectManager Instance;

    private List<FireTileEffect> _activeFireEffects = new List<FireTileEffect>();

    void Awake()
    {
        Instance = this;
    }

    public void CreateFireEffect(Tile tile)
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
        fireEffect.Initialize(tile);

        _activeFireEffects.Add(fireEffect);

        // Immediately kill any aliens on the tile
        fireEffect.KillAliensOnTile();
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
}
