using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public int X { get; private set; }
	public int Y { get; private set; }

	[SerializeField] private GameObject _highlight;
	[SerializeField] private bool _isWalkable;
	[SerializeField] protected SpriteRenderer _renderer;

	public BaseUnit OccupiedUnit;
	public Color OriginalColor { get; protected set; }

	/// <summary>
	/// Whether this tile blocks line of sight. Override in subclasses (e.g., WallTile).
	/// </summary>
	public virtual bool BlocksLineOfSight => false;

	/// <summary>Whether this tile's type allows walking, regardless of occupancy or temporary effects.</summary>
	public bool IsWalkableType => _isWalkable;

	public bool Walkable
	{
		get
		{
			if (!_isWalkable || OccupiedUnit != null)
				return false;

			// Check if tile is on fire (blocks movement)
			if (FireEffectManager.Instance != null && FireEffectManager.Instance.IsFireBlockingMovement(this))
				return false;

			return true;
		}
	}

	public virtual void Init(int x, int y)
	{
		X = x;
		Y = y;
	}

	public void Highlight(bool active, Color? color = null)
	{
		if (_highlight == null) return;
		_highlight.SetActive(active);
		if (active && color.HasValue)
		{
			var sr = _highlight.GetComponent<SpriteRenderer>();
			if (sr != null) sr.color = color.Value;
		}
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		Highlight(true);

		BaseHero selectedHero = UnitManager.Instance.SelectedHero;

		if (OccupiedUnit is BaseHero hoveredHero)
		{
			GridManager.Instance.ClearMoveHighlights();
			GridManager.Instance.ClearLineOfSight();

			GridManager.Instance.ShowLineOfSight(hoveredHero);
			GridManager.Instance.ShowImmediateMoveOptions(hoveredHero);

			// Show tooltip for unselected heroes
			if (hoveredHero != selectedHero && InGameUIManager.Instance != null)
			{
				InGameUIManager.Instance.ShowUnitTooltip(hoveredHero);
			}
			return;
		}

		// Show tooltip for enemies
		if (OccupiedUnit != null && InGameUIManager.Instance != null)
		{
			InGameUIManager.Instance.ShowUnitTooltip(OccupiedUnit);
		}

		if (selectedHero != null)
		{
			GridManager.Instance.ShowLineOfSight(selectedHero);
		}
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
		Highlight(false);

		BaseHero selectedHero = UnitManager.Instance.SelectedHero;

		GridManager.Instance.ClearMoveHighlights();
		GridManager.Instance.ClearLineOfSight();

		// Hide tooltip
		if (InGameUIManager.Instance != null)
		{
			InGameUIManager.Instance.HideUnitTooltip();
		}

		if (selectedHero != null)
		{
			GridManager.Instance.ShowLineOfSight(selectedHero);
			GridManager.Instance.ShowImmediateMoveOptions(selectedHero);
		}
	}

	public virtual void OnPointerClick(PointerEventData eventData)
	{
		Debug.Log($"[Tile] OnPointerClick at ({X},{Y}) - GameState: {GameManager.Instance.GameState}");

		if (GameManager.Instance.GameState != GameState.HeroesTurn)
		{
			Debug.Log($"[Tile] Click ignored - not heroes turn");
			return;
		}

		BaseHero currentlySelected = UnitManager.Instance.SelectedHero;

		// Clicking on a hero
		if (OccupiedUnit is BaseHero clickedHero)
		{
			Debug.Log($"[Tile] Clicked on hero: {clickedHero.UnitName}");
			// Toggle off if clicking the same hero
			if (currentlySelected == clickedHero)
			{
				UnitManager.Instance.SetSelectedHero(null);
			}
			else
			{
				// Select this hero (works for both new selection and switching)
				UnitManager.Instance.SetSelectedHero(clickedHero);
			}
			return;
		}

		if (currentlySelected == null)
		{
			Debug.Log($"[Tile] No hero selected, click ignored");
			return;
		}

		Debug.Log($"[Tile] Trying contextual abilities for {currentlySelected.UnitName} (has {currentlySelected.Abilities.Count} abilities)");

		// Try contextual abilities
		foreach (UnitAbility ability in currentlySelected.Abilities)
		{
			if (!ability.isContextual)
			{
				Debug.Log($"[Tile] Skipping non-contextual ability: {ability.abilityName}");
				continue;
			}

			Debug.Log($"[Tile] Checking ability: {ability.abilityName}");

			if (ability.CanExecute(currentlySelected, this))
			{
				Debug.Log($"[Tile] {ability.abilityName} CAN execute");

				// Check if we need to use Command Points
				int apNeeded = ability.actionPointCost;
				int heroAP = currentlySelected.CurrentActionPoints;

				// Special case for MoveAbility - get the actual dynamic cost
				if (ability is MoveAbility)
				{
					apNeeded = currentlySelected.GetMoveCost(this);
				}

				Debug.Log($"[Tile] AP needed: {apNeeded}, Hero AP: {heroAP}");

				if (heroAP < apNeeded)
				{
					// Try to use Command Points for the deficit
					int cpNeeded = apNeeded - heroAP;
					Debug.Log($"[Tile] Need {cpNeeded} CP to cover deficit");
					if (GameManager.Instance.CanAffordCommandPoints(cpNeeded))
					{
						GameManager.Instance.SpendCommandPoints(cpNeeded);
						Debug.Log($"[Tile] Spent {cpNeeded} CP");
					}
					else
					{
						Debug.Log($"[Tile] Cannot afford CP, skipping ability");
						// Can't afford - skip this ability
						continue;
					}
				}

				Debug.Log($"[Tile] EXECUTING {ability.abilityName}");
				ability.Execute(currentlySelected, this);
				UnitManager.Instance.SetSelectedHero(currentlySelected);
				return;
			}
			else
			{
				Debug.Log($"[Tile] {ability.abilityName} CANNOT execute");
			}
		}

		Debug.Log($"[Tile] No ability could execute on this tile");
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
