using UnityEngine;

public abstract class Tile : MonoBehaviour
{
    [SerializeField] protected SpriteRenderer _renderer;
	[SerializeField] private GameObject _highlight;
	[SerializeField] private bool _isWalkable;

	public BaseUnit OccupiedUnit;
	public bool Walkable => _isWalkable && OccupiedUnit == null;

	public virtual void Init(int x, int y)
	{}

	void OnMouseEnter()
	{
		_highlight.SetActive(true);
		MenuManager.Instance.ShowTileInfo(this);
	}

	void OnMouseExit()
	{
		_highlight.SetActive(false);
		MenuManager.Instance.ShowTileInfo(null);
	}

	private void OnMouseDown()
	{
		if (GameManager.Instance.GameState != GameState.HeroesTurn)
			return;
		
		if(OccupiedUnit != null)
		{
			if (OccupiedUnit.Faction == Faction.Hero)
				UnitManager.Instance.SetSelectedHero((BaseHero)this.OccupiedUnit);
			else
			{
				if (UnitManager.Instance.SelectedHero != null)
				{
					var enemy = (BaseEnemy)OccupiedUnit;
					//however to handle enemy selection
				}

			}
		}
		else
		{
			if (UnitManager.Instance.SelectedHero != null)
			{
				SetUnit(UnitManager.Instance.SelectedHero);
				UnitManager.Instance.SetSelectedHero(null);
			}
		}
			
	}
	
	public void SetUnit(BaseUnit unit)
	{
		if (unit.OccupiedTile != null)
			unit.OccupiedTile.OccupiedUnit = null;
		
		unit.transform.position = this.transform.position;
		this.OccupiedUnit = unit;
		unit.OccupiedTile = this;
	}
}
