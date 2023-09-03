using UnityEngine;
using UnityEngine.Events;
using static CardDisplay;

public static class EventManager
{
    public static event UnityAction DeckReady;
    public static event UnityAction<CardDisplay> ClickCard;
    public static void OnDeckReady() => DeckReady?.Invoke();

    public static void OnClickCard(CardDisplay card) => ClickCard?.Invoke(card);
}
