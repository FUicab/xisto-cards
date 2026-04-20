using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static System.DateTime;
using static Card;
using static TurnMovementType;
using static PlayerProfile;

[System.Serializable]
public class CardSpace : MonoBehaviour, IDropHandler {

    public bool Occupied = false;
    public CardLine Line = CardLine.Backline;
    // public bool IsDefensive = false;
    // public bool IsTrap = false;
    public GameObject CardObject;
    public CardDisplay PlayingCard;
    public List<CardSpace> Defenders = new List<CardSpace>();
    public CardSpace AttachedTrap;
    public PlayerRole OwnerRole = PlayerRole.Host;
    public PlayerProfile Owner;

    /* ONLY FOR TESTING*/
    public bool AutoDrawCards = false; // Will draw a random card (valid for the space) from the deck at the start of the game.
    public Card PreGeneratedCard; // Will generate the designated card for this space without affecting the deck.

    private GameManager GM;
    private Image CardSocketImage;
    public Outline outline;
    public Image DefenderBarrier;
    public int myIndexInRow = 0;
    public BoardRow myRow;

    public void OnDrop(PointerEventData eventData){
        // Debug.Log("OnDrop");
        if(eventData.pointerDrag != null){
            GameObject obj = eventData.pointerDrag;
            CardDisplay display = obj.GetComponent<CardDisplay>();
            AttemptToPlaceCard(display);
        }
    }

    public bool AttemptToPlaceCard(CardDisplay CDisplay, System.Action OnAnimEnd = null){
        bool WasPlaced = false;

        if(CDisplay == null || CDisplay.HasBeenPlayed){
            return false;
        }
        
        if(!Occupied && !CDisplay.HasBeenPlayed){

            //Check if the unit is defender and if the slot is also not for traps
            if(CanPlaceCard(CDisplay.card) ){
                //Check if we can buy this card
                if(GM.CanBuyCard(CDisplay)){
                    if(OnAnimEnd == null){ PlaceCard(CDisplay); } else
                                         { PlaceCard(CDisplay, OnAnimEnd); }
                    GM.CurrentAction.Clean();
                    GM.CurrentAction.movementType = TurnMovementType.CardPurchase;
                    GM.CurrentAction.BoughtCard = CDisplay;
                    GM.CurrentAction.PurchasePrice = CDisplay.card.Cost;
                    GM.CurrentAction.HandIndexOrigin = CDisplay.HandIndex;
                    CDisplay.SetPurchaseAction(GM.RegisterCurrentAction());
                    Occupied = true;
                    WasPlaced = true;
                } else {
                    if(OnAnimEnd != null){ OnAnimEnd(); }
                }
            } else {
                if(OnAnimEnd != null){ OnAnimEnd(); }
            }
        }

        return WasPlaced;
    }

    public bool CanPlaceCard(Card card){
        return Owner == GM.PlayerAtPlay;
                // && CardPlacingIsValid(card); As of the new version, there are no longer trap cards and defender slots are no longer restricted to that subtype.
    }

    private bool CardPlacingIsValid(Card card){
        return (card.Subtypes.Contains(UnitSubtype.Defender) && Line==CardLine.Defensive) ||
               (card.Type != UnitType.Trap && Line==CardLine.Backline) ||
               (card.Type == UnitType.Trap && Line==CardLine.Trap);
    }

    private bool IsInSameLine(CardSpace otherSpace)
    {
        bool itIs = false;
        if (myRow.BoardSpaces.Contains(otherSpace))
        {
            itIs = true;
        }
        return itIs;
    }

    public bool IsNextToMe(CardSpace otherSpace)
    {
        bool itIs = false;
        if (IsInSameLine(otherSpace))
        {
            for (int i = 0; i < myRow.BoardSpaces.Count; i++)
            {
                if (myRow.BoardSpaces[i] == otherSpace && (i == myIndexInRow-1 || i == myIndexInRow+1 ))
                {
                    itIs = true;
                }
            }
        }
        return itIs;
    }

    // public delegate void VoidCallback();
    // private void AnimEnd(System.DateTime? time = null){
    //     Debug.Log("Animation ended");
    // }
    private void PlaceCard(CardDisplay card, System.Action OnAnimEnd = null){
        card.HasBeenPlayed = true;
        card.OriginParent = transform;
        card.transform.SetParent(card.OriginParent);
        card.rectTransform.rotation = card.OriginParent.rotation;
        // LeanTween.move(card.rectTransform, card.OriginParent.position, 0.25f);
        if(Owner.useAI){
            // Debug.Log(OnAnimEnd);
            if(OnAnimEnd != null){
                card.rectTransform.LeanMove(card.OriginParent.position, 0.25f).setEaseOutQuart().setOnComplete(delegate(){OnAnimEnd();});
            } else {
                card.rectTransform.LeanMove(card.OriginParent.position, 0.25f).setEaseOutQuart();
            }
        } else {
            card.rectTransform.anchoredPosition = card.OriginParent.position;
        }
        GM.PlayerAtPlay.AvailableCardSlots[card.HandIndex] = true;
        PlayingCard = card;
        CardObject = card.gameObject;
        if(card.mySpace != null){
            card.mySpace.Occupied = false;
        }
        card.mySpace = this;
        EventManager.OnBoardUpdate();
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
        // if(Owner == PlayerRole.Opponent){
        //     Debug.Log(CardSocketImage);
        //     CardSocketImage.Colorize = new Color(255,238,238,255);
        // }
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

    public void SetRowPositionData()
    {
        foreach (BoardRow row in Owner.MyBoardRows)
        {
            if (row.BoardSpaces.Contains(this))
            {
                myRow = row;
                for (int i = 0; i < row.BoardSpaces.Count; i++)
                {
                    if(row.BoardSpaces[i] == this)
                    {
                        myIndexInRow = i;
                    }
                }
            }
        }
    }
}

[System.Serializable]
public class BoardRow
{
    public List<CardSpace> BoardSpaces = new List<CardSpace>();
}

public enum CardLine {
    Defensive,
    Backline,
    Trap
}