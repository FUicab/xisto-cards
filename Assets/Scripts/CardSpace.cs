using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static Card;
using static ActionType;

public class CardSpace : MonoBehaviour, IDropHandler {

    public bool Occupied = false;
    public enum CardLine {
        Defensive,
        Backline,
        Trap
    }
    public CardLine Line = CardLine.Backline;
    // public bool IsDefensive = false;
    // public bool IsTrap = false;
    public GameObject CardObject;
    public CardDisplay PlayingCard;
    public enum PlayerRole {
        Host,
        Opponent
    }
    public PlayerRole Owner = PlayerRole.Host;

    /* ONLY FOR TESTING*/
    public bool AutoDrawCards = false; // Will draw a random card (valid for the space) from the deck at the start of the game.
    public Card PreGeneratedCard; // Will generate the designated card for this space without affecting the deck.

    private GameManager GM;
    public Outline outline;

    public void OnDrop(PointerEventData eventData){
        // Debug.Log("OnDrop");
        if(eventData.pointerDrag != null){
            CardObject = eventData.pointerDrag;
            PlayingCard = CardObject.GetComponent<CardDisplay>();
            // eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = GetComponent<RectTransform>().anchoredPosition;
            
            if(!Occupied && !PlayingCard.HasBeenPlayed){

                //Check if the unit is defender and if the slot is also not for traps
                if(CanPlaceCard(PlayingCard.card) ){
                    //Check if we can buy this card
                    if(GM.CanBuyCard(PlayingCard)){
                        PlaceCard(PlayingCard);
                        GM.CurrentAction.BoughtCard = PlayingCard;
                        GM.CurrentAction.PurchasePrice = PlayingCard.card.Cost;
                        GM.CurrentAction.HandIndexOrigin = PlayingCard.HandIndex;
                        PlayingCard.SetPurchaseAction(GM.RegisterCurrentAction());
                        // PlayingCard.PurchaseAction = GM.RegisterCurrentAction();
                        Occupied = true;
                    }
                }
            }
            // playingCard.transform.SetParent(transform);
            // eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = new Vector3(0,0,0);
        }
    }

    public bool CanPlaceCard(Card card){
        return (Owner != PlayerRole.Opponent) &&
               (CardPlacingIsValid(card));
    }

    private bool CardPlacingIsValid(Card card){
        return (card.Subtypes.Contains(UnitSubtype.Defender) && Line==CardLine.Defensive) ||
               (card.Type != UnitType.Trap && Line==CardLine.Backline) ||
               (card.Type == UnitType.Trap && Line==CardLine.Trap);
    }

    private void PlaceCard(CardDisplay card){
        card.HasBeenPlayed = true;
        card.OriginParent = transform;
        if(card.mySpace != null){
            card.mySpace.Occupied = false;
        }
        card.mySpace = this;
    }
    public void UndoPlaceCard(){
        PlayingCard.HasBeenPlayed = false;
        PlayingCard.OriginParent = null;
        PlayingCard.mySpace = null;
        this.Occupied = false;
        this.CardObject = null;
        this.PlayingCard = null;
        // if(PlayingCard.mySpace != null){
        //     PlayingCard.mySpace.Occupied = false;
        // }
    }

    void Start(){
        GM = FindObjectOfType<GameManager>();
        outline = GetComponent<Outline>();
    }

    private void AutoDraw(){
        if(AutoDrawCards && CardObject == null){
            Card RandomCard;
            do { RandomCard = GM.Deck[Random.Range(0, GM.Deck.Count)]; }
            while (!CardPlacingIsValid(RandomCard));
            CardObject = Instantiate(GM.CardObject,transform);
            CardObject.GetComponent<CardDisplay>().card = RandomCard;
            PlaceCard(CardObject.GetComponent<CardDisplay>());
            GM.Deck.Remove(RandomCard);
        }
    }

    void OnEnable(){
        EventManager.DeckReady += AutoDraw;
    }

    void OnDisable(){
        EventManager.DeckReady -= AutoDraw;
    }

    void OnMouseDown(){
        // PlayingCard = null;
        // Destroy(CardObject);
        // Occupied = false;
    }

}
