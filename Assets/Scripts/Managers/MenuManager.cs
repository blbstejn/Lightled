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
            return;
        }
        
        _selectedHeroObject.GetComponentInChildren<Text>().text = hero.UnitName;
        _selectedHeroObject.SetActive(true);
    }
}
