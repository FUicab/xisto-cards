using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static Card;
using static ActionType;

public class CardSpace : MonoBehaviour, IDropHandler {

    public bool Occupied = false;
    public CardLine Line = CardLine.Backline;
    // public bool IsDefensive = false;
    // public bool IsTrap = false;
    public GameObject CardObject;
    public CardDisplay PlayingCard;
    public List<CardSpace> Defenders = new List<CardSpace>();
    public CardSpace AttachedTrap;
    public enum PlayerRole {
        Host,
        Opponent
    }
    public PlayerRole Owner = PlayerRole.Host;

    /* ONLY FOR TESTING*/
    public bool AutoDrawCards = false; // Will draw a random card (valid for the space) from the deck at the start of the game.
    public Card PreGeneratedCard; // Will generate the designated card for this space without affecting the deck.

    private GameManager GM;
    private Image CardSocketImage;
    public Outline outline;

    public void OnDrop(PointerEventData eventData){
        // Debug.Log("OnDrop");
        if(eventData.pointerDrag != null){
            GameObject obj = eventData.pointerDrag;
            CardDisplay display = obj.GetComponent<CardDisplay>();
            AttemptToPlaceCard(display);
        }
    }

    public bool AttemptToPlaceCard(CardDisplay display){
        bool WasPlaced = false;

        if(display != null && !display.HasBeenPlayed){
            PlayingCard = display;
            CardObject = PlayingCard.gameObject;
        } else {
            return false;
        }
        
        if(!Occupied && !PlayingCard.HasBeenPlayed){

            //Check if the unit is defender and if the slot is also not for traps
            if(CanPlaceCard(PlayingCard.card) ){
                //Check if we can buy this card
                if(GM.CanBuyCard(PlayingCard)){
                    PlaceCard(PlayingCard);
                    GM.CurrentAction.Clean();
                    GM.CurrentAction.Action = ActionType.CardPurchase;
                    GM.CurrentAction.BoughtCard = PlayingCard;
                    GM.CurrentAction.PurchasePrice = PlayingCard.card.Cost;
                    GM.CurrentAction.HandIndexOrigin = PlayingCard.HandIndex;
                    PlayingCard.SetPurchaseAction(GM.RegisterCurrentAction());
                    Occupied = true;
                    WasPlaced = true;
                } else {
                    PlayingCard = null;
                }
            }
        }

        return WasPlaced;
    }

    public bool CanPlaceCard(Card card){
        return (Owner == GM.PlayerAtPlay.Role) &&
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
        card.transform.SetParent(card.OriginParent);
        card.rectTransform.anchoredPosition = card.OriginParent.position;
        card.rectTransform.rotation = card.OriginParent.rotation;
        GM.PlayerAtPlay.AvailableCardSlots[card.HandIndex] = true;
        if(card.mySpace != null){
            card.mySpace.Occupied = false;
        }
        card.mySpace = this;
    }
    public void UndoPlaceCard(){
        PlayingCard.HasBeenPlayed = false;
        PlayingCard.OriginParent = null;
        PlayingCard.mySpace = null;
        FreeSpace();
        // if(PlayingCard.mySpace != null){
        //     PlayingCard.mySpace.Occupied = false;
        // }
    }

    public void FreeSpace(){
        Occupied = false;
        CardObject = null;
        PlayingCard = null;
    }

    void Start(){
        // GM = FindObjectOfType<GameManager>();
        // GM = GameObject.Find("GameManager").GetComponent<GameManager>();
        CardSocketImage = gameObject.GetComponent<Image>();
        if(Owner == PlayerRole.Opponent){
            // Debug.Log(CardSocketImage);
            // CardSocketImage.Colorize = new Color(255,238,238,255);
        }
        outline = GetComponent<Outline>();
    }

    private void AutoDraw(){
        if(AutoDrawCards && CardObject == null && GM != null){
            Card RandomCard;
            do { RandomCard = GM.Deck[Random.Range(0, GM.Deck.Count)]; }
            while (!CardPlacingIsValid(RandomCard));
            CardObject = Instantiate(GM.CardObject,transform);
            PlayingCard = CardObject.GetComponent<CardDisplay>();
            PlayingCard.card = RandomCard;
            PlaceCard(PlayingCard);
            GM.Deck.Remove(RandomCard);
        }
    }

    void OnEnable(){
        GM = FindObjectOfType<GameManager>();
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

public enum CardLine {
    Defensive,
    Backline,
    Trap
}