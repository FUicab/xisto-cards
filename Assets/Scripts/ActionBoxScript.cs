using UnityEngine;
using UnityEngine.EventSystems;

public class ActionBoxScript : MonoBehaviour, IPointerDownHandler
{
    private GameManager GM;
    public CardActionObject action;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GM = FindObjectOfType<GameManager>(); 
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (action.canBeUsed)
        {
            GM.StartAction(action);
        }
    }
}
