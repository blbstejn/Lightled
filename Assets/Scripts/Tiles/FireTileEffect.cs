using UnityEngine;

public class FireTileEffect : MonoBehaviour
{
    public Tile AffectedTile { get; private set; }

    private SpriteRenderer _fireRenderer;

    public void Initialize(Tile tile, Sprite sprite)
    {
        AffectedTile = tile;

        // Create visual effect
        GameObject fireVisual = new GameObject("FireEffect");
        fireVisual.transform.SetParent(tile.transform);
        fireVisual.transform.localPosition = Vector3.zero;
        fireVisual.transform.localScale = Vector3.one;

        _fireRenderer = fireVisual.AddComponent<SpriteRenderer>();
        _fireRenderer.sprite = sprite;
        _fireRenderer.sortingOrder = 2; // Above tiles but below units
    }

    private void OnDestroy()
    {
        if (_fireRenderer != null)
            Destroy(_fireRenderer.gameObject);
    }

    public void KillAliensOnTile()
    {
        if (AffectedTile.OccupiedUnit != null && AffectedTile.OccupiedUnit.IsAlive)
        {
            BaseUnit unit = AffectedTile.OccupiedUnit;

            // Only affect targetable enemies (aliens) - blips lurking on spawn tiles are immune
            if (unit.Faction == Faction.Enemy && unit.IsTargetable)
            {
                // Roll 2+ dice (d6, success on 2-6)
                int roll = Random.Range(1, 7);
                int threshold = 2;
                bool killed = roll >= threshold;

                Debug.Log($"Fire roll for {unit.UnitName} at ({AffectedTile.X}, {AffectedTile.Y}): {roll}");

                // Log to combat debug
                CombatDebugLog.LogFireDamage(unit.UnitName, AffectedTile.X, AffectedTile.Y, roll, threshold, killed);

                if (killed)
                {
                    unit.Kill();
                    Debug.Log($"{unit.UnitName} killed by fire!");
                    CombatDebugLog.LogKill(unit.UnitName, "Fire");
                }
                else
                {
                    Debug.Log($"{unit.UnitName} survived the fire!");
                }
            }
        }
    }
}
