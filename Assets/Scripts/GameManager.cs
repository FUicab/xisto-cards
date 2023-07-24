using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CardSpace;

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
        ConfirmButtonObject.SetActive(false);
        MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
        // CardDeck = JsonUtility.FromJson<CardList>(CardsJSON.text);
        Host.Role = PlayerRole.Host;
        Host.Gold = 8;
        GoldText.text = Host.Gold.ToString();
        Opponent.Role = PlayerRole.Opponent;
        Opponent.Gold = 50;
        Players.Add(Host);
        Players.Add(Opponent);
    }

    private void Update(){
        DeckSizeText.text = Deck.Count.ToString();
        DiscardPileSizeText.text = DiscardPile.Count.ToString();
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
        if(attacker != null && attacker.mySpace.Owner == PlayerRole.Opponent){
            return;
        }
        if(Attacker != null){
            Attacker.SetOutline();
            Attacker.SetLine();
        }

        Attacker = attacker;
        
        if(Attacker != null){
            Attacker.SetOutline("orange");
            PerformingAttack = true;
        } else {
            PerformingAttack = false;
            if(AttackTarget != null){
                AttackTarget.SetOutline();
                AttackTarget = null;
                ConfirmButtonObject.SetActive(false);
            }
        }
    }
    public void SetAttackTarget(CardDisplay attackTarget){
        if((attackTarget != null && attackTarget.mySpace.Owner == PlayerRole.Host) || !PerformingAttack){
            return;
        }
        if(AttackTarget != null){
            AttackTarget.SetOutline();
        }

        AttackTarget = attackTarget;

        if(AttackTarget != null){
            AttackTarget.SetOutline("red");
            Attacker.SetLine(AttackTarget);
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
            GameObject MessageObject = Instantiate(FloatingMessageObject);
            MessageObject.GetComponent<FloatingMessage>().SetMessage(damageDealt.ToString());
            Debug.Log(AttackTarget.transform.position);
            Debug.Log(MainUI.scaleFactor);
            Debug.Log(AttackTarget.SlotGroup.localScale);
            MessageObject.transform.Find("Canvas").GetComponent<RectTransform>().anchoredPosition = AttackTarget.transform.position;

            ConfirmButtonObject.SetActive(false);
            AttackTarget.SetOutline();
            AttackTarget = null;
            Attacker.SetOutline();
            Attacker.SetLine();
            Attacker = null;
            PerformingAttack = false;
        }
    }

    /* --- Turn management functions --------------------------------------------- */
    public bool BuyCard(CardDisplay card){
        bool CardCanBeBought = false;
        if(Host.Gold >= card.cost){
            Host.Gold -= card.cost;
            CardCanBeBought = true;
        } else {
            CardCanBeBought = false;
        }
        GoldText.text = Host.Gold.ToString();
        return CardCanBeBought;
    }
    public void TurnEnd(){

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
    public CardDisplay Attacker;
    public CardDisplay AttackTarget;
}

[System.Serializable]
public class PlayerProfile{
    public PlayerRole Role;
    public int Gold = 0;
}