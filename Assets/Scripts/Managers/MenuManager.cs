using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [SerializeField] private GameObject _selectedHeroObject, _hoveredUnitObject;
    void Awake ()
    {
        Instance = this;
    }

    public void ShowTileInfo(Tile tile)
    {
        if (tile == null)
        {
            _hoveredUnitObject.SetActive(false);
            return;
        }
        
        if (tile.OccupiedUnit != null)
        {
            _hoveredUnitObject.GetComponentInChildren<Text>().text = tile.OccupiedUnit.UnitName;
            _hoveredUnitObject.SetActive(true);
        }
        
        
    }
    
    public void ShowSelectedHero(BaseHero hero)
    {
        if (hero == null)
        {
            _selectedHeroObject.SetActive(false);

            // Also hide the in-game UI
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.HideHeroUI();
            }
            return;
        }

        _selectedHeroObject.GetComponentInChildren<Text>().text = hero.UnitName;
        _selectedHeroObject.SetActive(true);

        // Update the in-game UI with hero stats and abilities
        if (InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.ShowHeroUI(hero);
        }
    }
}
