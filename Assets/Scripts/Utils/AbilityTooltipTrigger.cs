using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lightweight hover handler for ability buttons.
/// Replaces EventTrigger (which implements IScrollHandler and consumes scroll events).
/// Only subscribes to PointerEnter/PointerExit so scroll reaches the camera freely.
/// </summary>
public class AbilityTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string TooltipText;
    public Action<string> OnEnter;
    public Action OnExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter?.Invoke(TooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit?.Invoke();
    }
}
