using UnityEngine;
using UnityEngine.Events;
using static CardDisplay;

public static class EventManager
{
    public static event UnityAction DeckReady;
    public static event UnityAction<CardDisplay> ClickCard;
    public static event UnityAction BoardUpdate;
    public static event UnityAction<TurnAction> TurnActionChange;
    public static event UnityAction RoundEnd;

    public static void OnDeckReady() => DeckReady?.Invoke();
    public static void OnClickCard(CardDisplay card) => ClickCard?.Invoke(card);
    public static void OnBoardUpdate() => BoardUpdate?.Invoke();
    public static void OnTurnActionChange(TurnAction turnAction) => TurnActionChange?.Invoke(turnAction);
    public static void OnRoundEnd() => RoundEnd?.Invoke();
}
