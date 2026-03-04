using UnityEngine;

/// <summary>
/// Holds all lore content for the info screen.
/// Units are referenced as prefabs — name, title, portrait, and description
/// are read directly from each unit's own fields (set in the prefab Inspector).
/// Create via: Right-click in Project > Create > Game > Lore Data
/// </summary>
[CreateAssetMenu(fileName = "LoreData", menuName = "Game/Lore Data")]
public class LoreData : ScriptableObject
{
    [Header("Overall Story")]
    [TextArea(6, 16)]
    public string overallStory;

    [Header("Units")]
    [Tooltip("Drag unit prefabs here in the order you want them listed. UnitName, title, portrait and description are read from each prefab.")]
    public BaseUnit[] units;

    /// <summary>
    /// Called by Unity when the asset is first created or Reset is clicked.
    /// Pre-fills the overall story — unit arrays must be assigned by dragging prefabs in the Inspector.
    /// </summary>
    private void Reset()
    {
        overallStory =
            "The Colonial Authority stopped publishing casualty figures six months ago.\n\n" +
            "It started with the survey ships — the ones that pushed too far past the established perimeter. " +
            "They came back, but the people aboard didn't. Something else did. " +
            "Since then the forward installations have gone dark one by one, and every rescue team sent in has joined the silence.\n\n" +
            "The Authority calls them \"unresolved contact events.\" " +
            "The operatives who've seen them call them something shorter.\n\n" +
            "You are Fireteam Lightled — five specialists pulled from whatever assignment they were on and told to go in where no one else came back from. " +
            "Your orders change depending on the mission. Your odds don't.\n\n" +
            "Whatever is out there, it learns. It waits. And it is very good at making sure you never hear it coming.";
    }
}
