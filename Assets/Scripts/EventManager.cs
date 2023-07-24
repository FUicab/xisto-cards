using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static event UnityAction DeckReady;
    public static void OnDeckReady() => DeckReady?.Invoke();
}
