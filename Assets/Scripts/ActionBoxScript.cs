using UnityEngine;
using UnityEngine.EventSystems;

public class ActionBoxScript : MonoBehaviour, IPointerDownHandler
{
    private GameManager GM;
    public CardActionObject action;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GM = FindObjectOfType<GameManager>(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (action.canBeUsed && action.action.actionType != ActionTypes.DoNothing)
        {
            GM.StartAction(action);
        }
    }
}
