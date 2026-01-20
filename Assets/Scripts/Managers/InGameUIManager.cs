using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the in-game UI including hero stats display and ability buttons.
/// Shows HP, AP, and non-contextual abilities for the selected hero.
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance;

    [Header("Hero Stats Display")]
    [SerializeField] private GameObject heroStatsPanel;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private TextMeshProUGUI heroHPText;
    [SerializeField] private TextMeshProUGUI heroAPText;

    [Header("Ability Buttons Container")]
    [SerializeField] private Transform abilityButtonContainer;
    [SerializeField] private GameObject abilityButtonPrefab; // Optional: can be null for auto-generation

    private BaseHero _currentHero;
    private List<Button> _abilityButtons = new List<Button>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Hide stats panel initially
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Continuously update hero stats if a hero is selected
        if (_currentHero != null && heroStatsPanel != null && heroStatsPanel.activeSelf)
        {
            UpdateHeroStats();
        }
    }

    /// <summary>
    /// Updates the UI to show the selected hero's stats and abilities.
    /// Called when a hero is selected.
    /// </summary>
    public void ShowHeroUI(BaseHero hero)
    {
        _currentHero = hero;

        if (hero == null)
        {
            HideHeroUI();
            return;
        }

        // Show stats panel
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(true);
        }

        // Update stats
        UpdateHeroStats();

        // Generate ability buttons
        GenerateAbilityButtons();
    }

    /// <summary>
    /// Updates the hero's HP and AP display.
    /// Call this whenever hero stats change.
    /// </summary>
    public void UpdateHeroStats()
    {
        if (_currentHero == null) return;

        // Update hero name
        if (heroNameText != null)
        {
            heroNameText.text = _currentHero.UnitName;
        }

        // Update HP
        if (heroHPText != null)
        {
            heroHPText.text = $"HP: {_currentHero.CurrentHP}/{_currentHero.maxHP}";
        }

        // Update AP
        if (heroAPText != null)
        {
            heroAPText.text = $"AP: {_currentHero.CurrentActionPoints}/{_currentHero.MaxActionPoints}";
        }
    }

    /// <summary>
    /// Hides the hero UI panel.
    /// </summary>
    public void HideHeroUI()
    {
        if (heroStatsPanel != null)
        {
            heroStatsPanel.SetActive(false);
        }

        ClearAbilityButtons();
    }

    /// <summary>
    /// Generates buttons for all non-contextual abilities (UI abilities).
    /// These are abilities that can be triggered from UI buttons (like Turn Left/Right).
    /// </summary>
    private void GenerateAbilityButtons()
    {
        if (abilityButtonContainer == null)
        {
            Debug.LogWarning("InGameUIManager: Ability button container not assigned!");
            return;
        }

        // Clear existing buttons
        ClearAbilityButtons();

        if (_currentHero == null) return;

        // Get all non-contextual abilities (UI abilities)
        List<UnitAbility> uiAbilities = _currentHero.unitAbilities.FindAll(a => !a.isContextual);

        if (uiAbilities.Count == 0) return;

        // Create a button for each UI ability
        foreach (UnitAbility ability in uiAbilities)
        {
            if (ability == null) continue;

            Button button = CreateAbilityButton(ability);
            if (button != null)
            {
                _abilityButtons.Add(button);
            }
        }
    }

    /// <summary>
    /// Creates a single ability button.
    /// </summary>
    private Button CreateAbilityButton(UnitAbility ability)
    {
        GameObject buttonObj;

        // Use prefab if provided, otherwise create programmatically
        if (abilityButtonPrefab != null)
        {
            buttonObj = Instantiate(abilityButtonPrefab, abilityButtonContainer);
        }
        else
        {
            buttonObj = CreateAbilityButtonProgrammatically(ability.abilityName);
        }

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"InGameUIManager: Button component missing on ability button for {ability.abilityName}");
            Destroy(buttonObj);
            return null;
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"{ability.abilityName}\n({ability.actionPointCost} AP)";
        }

        // Add click listener
        button.onClick.AddListener(() => OnAbilityButtonClicked(ability));

        // Update button interactability based on AP cost
        UpdateAbilityButtonState(button, ability);

        return button;
    }

    /// <summary>
    /// Creates an ability button programmatically (no prefab needed).
    /// </summary>
    private GameObject CreateAbilityButtonProgrammatically(string abilityName)
    {
        // Create button GameObject
        GameObject buttonObj = new GameObject($"AbilityButton_{abilityName}", typeof(RectTransform));
        buttonObj.transform.SetParent(abilityButtonContainer, false);

        // Configure RectTransform
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(120, 60);

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Add Image component for visual
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.35f, 0.55f, 1f); // Blue-ish

        // Create text child
        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        // Add TextMeshProUGUI
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 14;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.enableWordWrapping = true;

        // Setup button colors
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.35f, 0.55f, 1f);
        colors.highlightedColor = new Color(0.2f, 0.45f, 0.65f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.5f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        button.colors = colors;

        return buttonObj;
    }

    /// <summary>
    /// Called when an ability button is clicked.
    /// </summary>
    private void OnAbilityButtonClicked(UnitAbility ability)
    {
        if (_currentHero == null || ability == null) return;

        // Check if hero has enough AP
        if (_currentHero.CurrentActionPoints < ability.actionPointCost)
        {
            Debug.Log($"Not enough AP to use {ability.abilityName}. Need {ability.actionPointCost}, have {_currentHero.CurrentActionPoints}");
            return;
        }

        // Execute the ability (untargeted abilities ignore the null target)
        if (ability is UntargetedAbility untargetedAbility)
        {
            untargetedAbility.Execute(_currentHero);
        }
        else
        {
            // For targeted abilities, we'd need to handle target selection differently
            Debug.LogWarning($"Ability {ability.abilityName} requires a target but was triggered from UI button!");
        }

        // Update UI to reflect new stats
        UpdateHeroStats();

        // Update all ability button states
        UpdateAllAbilityButtons();
    }

    /// <summary>
    /// Updates the interactability of an ability button based on AP.
    /// </summary>
    private void UpdateAbilityButtonState(Button button, UnitAbility ability)
    {
        if (_currentHero == null || button == null) return;

        bool canAfford = _currentHero.CurrentActionPoints >= ability.actionPointCost;
        button.interactable = canAfford;
    }

    /// <summary>
    /// Updates all ability buttons' interactability.
    /// Call this after AP changes.
    /// </summary>
    public void UpdateAllAbilityButtons()
    {
        if (_currentHero == null) return;

        List<UnitAbility> uiAbilities = _currentHero.unitAbilities.FindAll(a => !a.isContextual);

        for (int i = 0; i < _abilityButtons.Count && i < uiAbilities.Count; i++)
        {
            UpdateAbilityButtonState(_abilityButtons[i], uiAbilities[i]);
        }
    }

    /// <summary>
    /// Clears all ability buttons.
    /// </summary>
    private void ClearAbilityButtons()
    {
        foreach (Button button in _abilityButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        _abilityButtons.Clear();
    }
}
