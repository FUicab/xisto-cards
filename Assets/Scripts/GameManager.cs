using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CardSpace;

public enum ActionType{
    CardPurchase,
    PerformAttack,
    MoveCard
}

public class GameManager : MonoBehaviour
{
    // public List<CardTest> Deck = new List<CardTest>();
    // public List<CardTest> DiscardPile = new List<CardTest>();
    // public TextAsset CardsJSON;
    // public CardList CardDeck;

    // public Text DeckSizeText;
    // public Text DiscardPileSizeText;

    public GameObject CardObject;
    public TextMeshProUGUI DeckSizeText;
    public TextMeshProUGUI DiscardPileSizeText;
    public GameObject ConfirmButtonObject;
    public TextMeshProUGUI GoldText;
    public TextMeshProUGUI ActionPointsText;
    public GameObject FloatingMessageObject;

    public Transform[] Hand;
    public bool[] AvailableCardSlots;

    public List<CardSlot> PlayingCards;
    public CardSpace[] CardSpaces;
 
    /* >>>> The list of the individual cards that will be available */
    public List<Card> CardList = new List<Card>();
    public List<Card> Deck = new List<Card>();
    public List<Card> DiscardPile = new List<Card>();

    /* >>>> Player turns and combat management */
    public List<PlayerProfile> Players;
    public CardDisplay Attacker;
    public CardDisplay AttackTarget;
    public bool PerformingAttack = false;
    public List<TurnAction> TurnActions;
    public TurnAction CurrentAction = new TurnAction();
    public int ActionPoints = 3;
    public PlayerProfile Host;
    public PlayerProfile Opponent;


    [SerializeField] private Canvas MainUI;

    public void DrawCard(){

        // if(Deck.Count>=1){
        //     CardTest RandomCard = Deck[Random.Range(0, Deck.Count)];
        //     for(int i = 0; i < AvailableCardSlots.Length; i++){
        //         if(AvailableCardSlots[i] == true){
        //             RandomCard.gameObject.SetActive(true);
        //             RandomCard.HandIndex = i;
        //             RandomCard.transform.position = CardSlots[i].position;
        //             RandomCard.HasBeenPlayed = false;
        //             AvailableCardSlots[i] = false;
        //             Deck.Remove(RandomCard);
        //             return;
        //         }
        //     }
        // }
        if(Deck.Count>=1){
            Card RandomCard = Deck[Random.Range(0, Deck.Count)];
            for(int i = 0; i < AvailableCardSlots.Length; i++){
                if(AvailableCardSlots[i] == true){
                    // RandomCard.gameObject.SetActive(true);
                    // RandomCard.HandIndex = i;
                    // RandomCard.transform.position = CardSlots[i].position;
                    // RandomCard.HasBeenPlayed = false;
                    GameObject CardInstance = Instantiate(CardObject,Hand[i].transform);
                    CardInstance.GetComponent<CardDisplay>().card = RandomCard;
                    CardInstance.GetComponent<CardDisplay>().HasBeenPlayed = false;
                    CardInstance.GetComponent<CardDisplay>().HandIndex = i;
                    AvailableCardSlots[i] = false;
                    Deck.Remove(RandomCard);
                    return;
                }
            }
        }

    }

    public void DrawCards(){

        if(Deck.Count>=1){
            for(int i = 0; i < AvailableCardSlots.Length; i++){
                if(AvailableCardSlots[i] == true){
                    Card RandomCard = Deck[Random.Range(0, Deck.Count)];
                    GameObject CardInstance = Instantiate(CardObject,Hand[i].transform);
                    CardInstance.GetComponent<CardDisplay>().card = RandomCard;
                    CardInstance.GetComponent<CardDisplay>().HasBeenPlayed = false;
                    CardInstance.GetComponent<CardDisplay>().HandIndex = i;
                    AvailableCardSlots[i] = false;
                    Deck.Remove(RandomCard);
                }
            }
        }

    }

    private void Start(){
        foreach(Card card in CardList){
            for(int i = 0; i < card.CardCount; i++){
                Deck.Add(card);
            }
        }
        EventManager.OnDeckReady();
        // Debug.Log(GameObject.FindObjectsOfType<CardSpace>());
        CardSpaces = GameObject.FindObjectsOfType<CardSpace>();
        foreach(CardSpace slot in CardSpaces){
            CardSlot newSlot = new CardSlot();
            newSlot.Line = slot.Line;
            PlayingCards.Add(newSlot);
        }
        // ConfirmButtonObject.SetActive(false);
        MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
        // CardDeck = JsonUtility.FromJson<CardList>(CardsJSON.text);
        Host.Role = PlayerRole.Host;
        Host.Gold = 8;
        GoldText.text = Host.Gold.ToString();
        ActionPointsText.text = ActionPoints.ToString();
        Opponent.Role = PlayerRole.Opponent;
        Opponent.Gold = 50;
        Players.Add(Host);
        Players.Add(Opponent);
        DrawCards();
    }

    private void Update(){
        DeckSizeText.text = Deck.Count.ToString();
        DiscardPileSizeText.text = DiscardPile.Count.ToString();
    }

    public TurnAction RegisterCurrentAction(){
        TurnActions.Add(new TurnAction(CurrentAction));
        CurrentAction.Clean();
        ActionPoints -= 1;
        ActionPointsText.text = ActionPoints.ToString();
        return TurnActions[TurnActions.Count - 1];
    }

    public void RemoveAction(TurnAction Action){
        TurnActions.Remove(Action);
        ActionPoints += 1;
        ActionPointsText.text = ActionPoints.ToString();
    }

    public void ClearActionPoints(){
        TurnActions.Clear();
        ActionPoints = 3;
        ActionPointsText.text = ActionPoints.ToString();
    }

    public void Shuffle(){
        if(DiscardPile.Count >= 1){
            foreach(Card card in DiscardPile){
                Deck.Add(card);
            }
            DiscardPile.Clear();
        }
    }

    /* --- Attack management functions --------------------------------------------- */
    public void SetAttacker(CardDisplay attacker){
        
        // We check if this is a proper "attacker" overall
        if(attacker != null && attacker.mySpace.Owner == PlayerRole.Opponent){
            return;
        }

        /* We check if this attacker has already an action in queue.
           If that's the case we remove the action from the list.
        */
        TurnAction AttackerAction = ActionOfCard(attacker);
        if(AttackerAction != null){
            // if(AttackerAction.Attacker != null){
            //     AttackerAction.Attacker.SetOutline();
            //     AttackerAction.Attacker.SetLine();
            //     AttackerAction.AttackTarget.SetOutline();
            // }
            // RemoveAction(AttackerAction);
            ClearAttackActions(attacker);
            return;
        }

        // Are there Action Points left to perform this action?
        if(ActionPoints <= 0){ return; }

        // We procceed to give the attacker the proper display for its action
        if(CurrentAction.Attacker != null){
            CurrentAction.Attacker.SetOutline();
            CurrentAction.Attacker.SetLine();
            CurrentAction.Attacker = null;
            if(CurrentAction.AttackTarget != null){
                CurrentAction.AttackTarget.SetOutline();
                CurrentAction.AttackTarget = null;
                ConfirmButtonObject.SetActive(false);
            }
        } else {
            CurrentAction.Attacker = attacker;
            CurrentAction.Action = ActionType.PerformAttack;
            CurrentAction.Attacker.SetOutline("orange");
        }

        // CurrentAction.Attacker = attacker;
        // Attacker = attacker;
        
        if(CurrentAction.Attacker != null){
            // CurrentAction.Attacker.SetOutline("orange");
            // PerformingAttack = true;
        } else {
            // PerformingAttack = false;
            // if(CurrentAction.AttackTarget != null){
            //     CurrentAction.AttackTarget.SetOutline();
            //     CurrentAction.AttackTarget = null;
            //     ConfirmButtonObject.SetActive(false);
            // }
        }
    }
    public void SetAttackTarget(CardDisplay attackTarget){
        CurrentAction.Action = ActionType.PerformAttack;
        if((attackTarget != null && attackTarget.mySpace.Owner == PlayerRole.Host) || CurrentAction.Attacker == null || ActionPoints <= 0){
            return;
        }
        if(CurrentAction.AttackTarget != null){
            CurrentAction.AttackTarget.SetOutline();
            CurrentAction.AttackTarget = null;
        }

        CurrentAction.AttackTarget = attackTarget;
        // AttackTarget = attackTarget;

        if(CurrentAction.AttackTarget != null && CurrentAction.Attacker != null){
            // CurrentAction.Attacker = Attacker;
            // CurrentAction.AttackTarget = AttackTarget;
            CurrentAction.Attacker.SetLine(CurrentAction.AttackTarget);
            CurrentAction.AttackTarget.SetOutline("red");
            RegisterCurrentAction();
            // TurnActions.Add(new TurnAction(CurrentAction));
            // CurrentAction.Clean();
            ConfirmButtonObject.SetActive(true);
        }
    }
    public void ConfirmAttack(){
        int damageDealt;
        if(PerformingAttack && AttackTarget!=null && Attacker!=null){
            damageDealt = Attacker.attack - AttackTarget.armor;
            if(damageDealt <= 0){
                damageDealt = 1;
            }
            AttackTarget.ReceiveDamage(damageDealt);
            DisplayDamage(damageDealt, AttackTarget);

            ConfirmButtonObject.SetActive(false);
            AttackTarget.SetOutline();
            AttackTarget = null;
            Attacker.SetOutline();
            Attacker.SetLine();
            Attacker = null;
            PerformingAttack = false;
        }
    }
    public void ClearAttackActions(CardDisplay attacker){
        for (int i = TurnActions.Count-1; i >= 0; i--)
        {
            if(TurnActions[i].Attacker == attacker){
                if(TurnActions[i].AttackTarget != null){
                    TurnActions[i].AttackTarget.ClearAllDisplay();
                }
                attacker.ClearAllDisplay();
                RemoveAction(TurnActions[i]);
            }
        }
    }

    public void DoAttackAction(TurnAction ActionData){
        int damageDealt;
        if(ActionData.AttackTarget!=null && ActionData.Attacker!=null){
            damageDealt = ActionData.Attacker.attack - ActionData.AttackTarget.armor;
            if(damageDealt <= 0){
                damageDealt = 1;
            }
            ActionData.AttackTarget.ReceiveDamage(damageDealt);
            DisplayDamage(damageDealt, ActionData.AttackTarget);

            ConfirmButtonObject.SetActive(false);
            ActionData.AttackTarget.SetOutline();
            ActionData.AttackTarget = null;
            ActionData.Attacker.SetOutline();
            ActionData.Attacker.SetLine();
            ActionData.Attacker = null;
        }
    }

    /* --- Turn management functions --------------------------------------------- */
    public bool CanBuyCard(CardDisplay card){
        bool CardCanBeBought = false;
        if(Host.Gold >= card.cost && ActionPoints > 0){
            Host.Gold -= card.cost;
            CardCanBeBought = true;
        }
        GoldText.text = Host.Gold.ToString();
        return CardCanBeBought;
    }
    public void RefundCard(TurnAction PurchaseAction){
        if(PurchaseAction != null){
            Host.Gold += PurchaseAction.PurchasePrice;
            // PurchaseAction.BoughtCard.HasBeenPlayed = false;
            // PurchaseAction.BoughtCard.rectTransform.anchoredPosition = PurchaseAction.BoughtCard.OriginPosition;
            AvailableCardSlots[PurchaseAction.HandIndexOrigin] = false;
            // PurchaseAction.BoughtCard.OriginParent = Hand[PurchaseAction.HandIndexOrigin];
            if(CurrentAction.Attacker != null){
                CurrentAction.Attacker.ClearAllDisplay();
                CurrentAction.Attacker = null;
            }
            ClearAttackActions(PurchaseAction.BoughtCard);
            RemoveAction(PurchaseAction);
            GoldText.text = Host.Gold.ToString();
        }
    }
    public void UndoAction(){
        if(TurnActions.Count > 0){
            TurnAction Action = TurnActions[TurnActions.Count - 1];
            if(Action.Action == ActionType.PerformAttack){
                Action.Attacker.SetLine();
                Action.Attacker.SetOutline();
                Action.AttackTarget.SetOutline();
            }
            TurnActions.RemoveAt(TurnActions.Count - 1);
        }
    }
    public void TurnEnd(){
        foreach (var action in TurnActions)
        {
            switch (action.Action)
            {
                case ActionType.CardPurchase:
                    action.BoughtCard.DisableUndoPurchase();
                break;

                case ActionType.PerformAttack:
                    DoAttackAction(action);
                break;
            }
        }
        ClearActionPoints();
        DrawCards();
    }

    /* --- Action check functions --------------------------------------------- */
    
    /** Searchs for a card and returns the first action performed with it */
    public TurnAction ActionOfCard(CardDisplay card){
        foreach (TurnAction action in TurnActions)
        {
            if(action.Attacker == card){
                return action;
            }
        }
        return null;
    }

    /* --- Values and resource checks --------------------------------------------- */
    public bool CheckGold(int requirement){
        
        return false;
    }

    /* --- Floating messages --------------------------------------------- */
    public void DisplayDamage(int damage, CardDisplay target){
        GameObject MessageObject = Instantiate(FloatingMessageObject);
        MessageObject.GetComponent<FloatingMessage>().SetMessage(damage.ToString());
        MessageObject.transform.Find("Canvas").GetComponent<RectTransform>().anchoredPosition = target.transform.position;
    }

    /**
     * This function is a BIG work in progress.
     * We seek to be able to "translate" pasive abilities into a computer-readable language.
     * While they make total sense to us, the computer needs very specific instructions to
       make them work and this little tool will help us with it. Or that's at least what we want.
    */
    public void PerformSkillAction(string[] parameters){
        /* "parameters" is an array of strings, each one works as a parameter for the correct interpretation of our ability
         * [0] The target of the ability. Who is going to benefit from the effects?
         * [1] The trigger event. When will this ability take effect? When should we check for it?
         * [2] The condition. Under which circunstances will this ability take effect? If left empty it will always happen.
         * [3] The effect. What will happen? Multiple effects are divided by commas.
         * [4] Other keywords. Any other parameter. Tipically used to limit an ability to work only once per turn.
        */
    }

    /**/
    public void GetCardsFromJSON(){

    }

    // [System.Serializable]
    // public class CardList{
    //     public List<Card> list;
    // }

}

[System.Serializable]
public class CardSlot{
    public bool Occupied = false;
    public CardLine Line = CardLine.Backline;
    // public bool IsDefensive = false;
    // public bool IsTrap = false;
    public CardDisplay PlayingCard;
    public Transform SlotObject;
}

[System.Serializable]
public class TurnAction{
    
    public ActionType Action;
    public CardDisplay Attacker;
    public CardDisplay AttackTarget;
    public CardDisplay BoughtCard;
    public int HandIndexOrigin;
    public int PurchasePrice;

    public TurnAction(TurnAction Origin = null){
        if(Origin != null){
            this.Action = Origin.Action;
            this.Attacker = Origin.Attacker;
            this.AttackTarget = Origin.AttackTarget;
            this.BoughtCard = Origin.BoughtCard;
            this.PurchasePrice = Origin.PurchasePrice;
            this.HandIndexOrigin = Origin.HandIndexOrigin;
        }
    }

    public void Clean(){
        if(Attacker != null){
            Attacker.ClearAllDisplay();
            Attacker = null;
        }
        if(AttackTarget != null){
            AttackTarget.ClearAllDisplay();
            AttackTarget = null;
        }
        BoughtCard = null;
        HandIndexOrigin = 0;
        PurchasePrice = 0;
    }
}

[System.Serializable]
public class PlayerProfile{
    public PlayerRole Role;
    public int Gold = 0;
}