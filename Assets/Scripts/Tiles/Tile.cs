using UnityEngine;

public abstract class Tile : MonoBehaviour
{
	public int X { get; private set; }
	public int Y { get; private set; }
	
    [SerializeField] protected SpriteRenderer _renderer;
	[SerializeField] private GameObject _highlight;
	[SerializeField] private bool _isWalkable;

	public BaseUnit OccupiedUnit;
	public bool Walkable => _isWalkable && OccupiedUnit == null;

	public virtual void Init(int x, int y)
	{
		X = x;
		Y = y;
	}

	public void Highlight(bool active)
	{
		_highlight.SetActive(active);
	}

	void OnMouseEnter()
	{
		Highlight(true);
		MenuManager.Instance.ShowTileInfo(this);

		BaseHero selectedHero = UnitManager.Instance.SelectedHero;

		if (OccupiedUnit is BaseHero hoveredHero)
		{
			GridManager.Instance.ClearMoveHighlights();
			GridManager.Instance.ClearLineOfSight();
			
			GridManager.Instance.ShowImmediateMoveOptions(hoveredHero);
			GridManager.Instance.ShowLineOfSight(hoveredHero);
			return;
		}
		
		if (selectedHero != null)
		{
			GridManager.Instance.ShowLineOfSight(selectedHero);
		}
	}

	void OnMouseExit()
	{
		Highlight(false);
		MenuManager.Instance.ShowTileInfo(null);

		BaseHero selectedHero = UnitManager.Instance.SelectedHero;
		
		GridManager.Instance.ClearMoveHighlights();
		GridManager.Instance.ClearLineOfSight();

		if (selectedHero != null)
		{
			GridManager.Instance.ShowImmediateMoveOptions(selectedHero);
			GridManager.Instance.ShowLineOfSight(selectedHero);
		}
	}

	private void OnMouseDown()
	{
		if (GameManager.Instance.GameState != GameState.HeroesTurn)
			return;

		BaseHero hero = UnitManager.Instance.SelectedHero;

		// Selecting a hero
		if (OccupiedUnit is BaseHero clickedHero)
		{
			UnitManager.Instance.SetSelectedHero(clickedHero);
			return;
		}

		if (hero == null)
			return;

		// Try contextual abilities
		foreach (UnitAbility ability in hero.Abilities)
		{
			if (!ability.IsContextual)
				continue;

			if (ability.CanExecute(hero, this))
			{
				ability.Execute(hero, this);
				UnitManager.Instance.SetSelectedHero(hero);
				return;
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
