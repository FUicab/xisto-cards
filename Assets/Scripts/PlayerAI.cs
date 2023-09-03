using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerProfile;
using static CardDisplay;
using static CardSpace;
using static CardLine;
using static UnitType;
using static UnitSubtype;
using static ActionType;

[System.Serializable]
public class PlayerAI{
    public PlayerProfile Profile;
    
    /* --- AI personality properties --------------------------------------------- */
    public bool PrioritizesDefendersForDefendingOnly = true; // Will place defenders only in defending positions if possible
    public bool PrioritizesDefendedBackline = true; // Will try to leave no undefended backline card
    public bool PrefersToSaveAtLeast2GoldPerTurn = true; // Would prioritize to have some gold saved for emergencies
    public bool WouldNotLetOpponentOverwhelm = true; // Tries to have as many active cards as its opponent
    public bool StartsWithBasicTrio = true; // On its first move will place at least a backline card with a defender
    public bool UnprotectedTargetsFirst = true; // Will try to attack undefended cards if possible
    public AIActionStrategy ChosenStrategy = AIActionStrategy.SaveGold;
    [SerializeField] public List<AITurnActions> MyActions = new List<AITurnActions>();
    public int currentActionIndex = 0;

    /* --- AI card management --------------------------------------------- */
    public List<CardSpace> MyCardSpaces = new List<CardSpace>();
    public List<CardSpace> MyBackline = new List<CardSpace>();
    public List<CardSpace> MyTrapline = new List<CardSpace>();
    public List<CardSpace> MyDefline = new List<CardSpace>();
    public List<CardSpace> OpponentSpaces = new List<CardSpace>();
    public List<CardDisplay> MyCards = new List<CardDisplay>();
    public List<CardDisplay> ReservedCards = new List<CardDisplay>(); // These cards are currently being used in other actions within this same turn and will not be considered for other actions

    /* --- Other variables --------------------------------------------- */
    public GameManager GM; // GM is designated during instatiation
    public int TurnCount = 1;

    void Start(){
        // GM = FindObjectOfType<GameManager>();
    }
    
    public void StartAI(){
        if(Profile == null){ return; }
        if(!Profile.useAI){ return; }

        MyActions.Clear();
        for (int i = 0; i < GM.ActionPoints; i++){
            MyActions.Add(new AITurnActions());
        }

        LookAtSpaces();
        if(StartsWithBasicTrio && TurnCount <= 1){
            SetActionsForStartingComp();
            StartActions();
        } else {
            
            RandomizeStrategy();
            GenerateActions();
            // Debug.Log(ChosenStrategy);
            switch (ChosenStrategy){
                case AIActionStrategy.PlaceCards:
                    StartActions();
                break;

                case AIActionStrategy.Aggresive:
                    PlayAggresive();
                break;

                case AIActionStrategy.Defensive:
                break;

                case AIActionStrategy.SaveGold:
                    // By saving gold the AI does nothing and ends its turn
                break;
            }

        }
        // GM.TurnEnd();
        TurnCount ++;
    }

    public void RandomizeStrategy(){
        switch (Random.Range(0,2)){
            case 0: ChosenStrategy = AIActionStrategy.PlaceCards; break;
            case 1: ChosenStrategy = AIActionStrategy.Aggresive; break;
            case 2: ChosenStrategy = AIActionStrategy.Defensive; break;
            case 3: ChosenStrategy = AIActionStrategy.SaveGold; break;
        }
    }

    public void StartActions(){
        currentActionIndex = 0;
        PerformAction();
    }

    public void GenerateActions(){
        switch (ChosenStrategy){
            case AIActionStrategy.PlaceCards:
                foreach (var action in MyActions){
                    action.Action = ActionType.CardPurchase;
                    action.DestinationSlot = PickRandomAvailableSpace();
                    CardDisplay ChosenCard = PickAValidCardForSpace(action.DestinationSlot);
                    ReservedCards.Add(ChosenCard);
                    action.BoughtCard = ChosenCard;
                }
            break;
        }
    }
    public void PerformNextAction(){
        currentActionIndex ++;
        PerformAction();
    }
    public void PerformAction(){
        bool Success = true;
        if(GM.ActionPoints <= 0 || currentActionIndex >= GM.InitialActionPoints){
            GM.TurnEnd();
            return;
        }
        AITurnActions CurrentAction = MyActions[currentActionIndex];
        switch (CurrentAction.Action){
            case ActionType.CardPurchase:
                if(CurrentAction.BoughtCard != null){
                    CurrentAction.DestinationSlot.AttemptToPlaceCard(CurrentAction.BoughtCard, PerformNextAction);
                } else {
                    Success = false;
                }
            break;
        }
        if(!Success){ PerformNextAction(); }
    }

    public void RandomlyPlaceCardsInHand(){
        foreach (var cardInHand in Profile.Hand){
            CardDisplay card = cardInHand.gameObject.GetComponentInChildren<CardDisplay>();
        }

        int RandomIndex = 0;
        int RandomSlot = 0;
        for (int i = 0; i < 3; i++)
        {
            RandomIndex = Random.Range(0,Profile.Hand.Length);
            do {
                RandomSlot = Random.Range(0,MyCardSpaces.Count);
            } while (MyCardSpaces[RandomSlot].Occupied);
            MyCardSpaces[RandomSlot].AttemptToPlaceCard(PickAValidCardForSpace(MyCardSpaces[RandomSlot]));
        }
        GM.TurnEnd();
    }
    public void PlaceRandomCardFromHand(){
        if(GM.ActionPoints <= 0){
            return;
        }
        int RandomSlot = 0;
        do {
            RandomSlot = Random.Range(0,MyCardSpaces.Count);
        } while (MyCardSpaces[RandomSlot].Occupied);
        MyCardSpaces[RandomSlot].AttemptToPlaceCard(PickAValidCardForSpace(MyCardSpaces[RandomSlot]), PlaceRandomCardFromHand);
    }

    public CardSpace PickRandomAvailableSpace(){
        int RandomSlot = 0;
        do {
            RandomSlot = Random.Range(0,MyCardSpaces.Count);
        } while (MyCardSpaces[RandomSlot].Occupied);
        return MyCardSpaces[RandomSlot];
    }

    public CardDisplay PickAValidCardForSpace(CardSpace space, bool reserve = false){
        List<CardDisplay> ValidCards = new List<CardDisplay>();
        // Debug.Log("Picking...");
        if(PrioritizesDefendersForDefendingOnly){
            for (int i = 0; i < Profile.Hand.Length; i++){
                CardDisplay card = Profile.Hand[i].gameObject.GetComponentInChildren<CardDisplay>();
                if(card != null && !ReservedCards.Contains(card)){
                    if(space.Line == CardLine.Defensive && card.card.Subtypes.Contains(UnitSubtype.Defender)){
                        ValidCards.Add(card); }
                    if(space.Line == CardLine.Backline && !card.card.Subtypes.Contains(UnitSubtype.Defender)){
                        ValidCards.Add(card); }
                    if(space.Line == CardLine.Trap && card.card.Type == UnitType.Trap){
                        ValidCards.Add(card); }
                }
            }
        }
        if(ValidCards.Count == 0 || !PrioritizesDefendersForDefendingOnly){
            for (int i = 0; i < Profile.Hand.Length; i++){
                CardDisplay card = Profile.Hand[i].gameObject.GetComponentInChildren<CardDisplay>();
                if(card != null && !ReservedCards.Contains(card)){
                    if(space.Line == CardLine.Trap && card.card.Type == UnitType.Trap){
                        ValidCards.Add(card);
                    }
                    if(space.Line != CardLine.Trap && card.card.Type != UnitType.Trap){
                        ValidCards.Add(card);
                    }
                }
            }
        }
        if(ValidCards.Count == 0){
            return null;
        } else {
            return ValidCards[Random.Range(0,ValidCards.Count)];
        }
    }

    public List<CardDisplay> PickBestAttackers(){
        List<CardDisplay> AttackerList = new List<CardDisplay>();
        AttackerList.Sort((a,b) => {
            return b.attack.CompareTo(a.attack);
        });
        foreach (var card in MyCards){
            if(card.attack > 0 && AttackerList.Count < GM.ActionPoints){
                AttackerList.Add(card);
                // Debug.Log(card.card.Name);
            }
        }
        return AttackerList;
    }

    public void PlayAggresive(){
        List<CardDisplay> AttackableTargets = new List<CardDisplay>();
        List<CardDisplay> Attackers = PickBestAttackers();
        CardDisplay ChosenTarget = null;
        // List<CardDisplay> ChosenTargets = new List<CardDisplay>();
        foreach (var OSpace in OpponentSpaces){
            if(OSpace.PlayingCard != null){
                bool CanBeAttacked = true;
                foreach (var defSpace in OSpace.Defenders){
                    if(defSpace.PlayingCard != null){ CanBeAttacked = false; }
                }
                if(OSpace.Line == CardLine.Trap){ CanBeAttacked = false; }
                if(CanBeAttacked){ AttackableTargets.Add(OSpace.PlayingCard); }
            }
        }
        AttackableTargets.Sort((a,b) => a.hp.CompareTo(b.hp));
        foreach (var target in AttackableTargets){
            int health = target.hp;
            foreach (var attacker in Attackers){
                health -= attacker.GetDamageAgainstTarget(target);
            }
            if(health <= 0){
                ChosenTarget = target;
            }
        }
        if(ChosenTarget != null){
            foreach (var attacker in Attackers){
                // Debug.Log(attacker.card.Name);
                // Debug.Log(ChosenTarget.card.Name);
                GM.SetAttacker(attacker);
                GM.SetAttackTarget(ChosenTarget);
            }
            GM.TurnEnd();
        } else {
            ChosenStrategy = AIActionStrategy.PlaceCards;
            GenerateActions();
            StartActions();
            // RandomlyPlaceCardsInHand();
            return;
        }
    }

    public void PlaceStartingComp(){
        CardSpace ChosenSpace = MyBackline[Random.Range(0,MyBackline.Count)];
        // Debug.Log(ChosenSpace);
        CardDisplay ChosenCard = PickAValidCardForSpace(ChosenSpace);
        ChosenSpace.AttemptToPlaceCard(ChosenCard);
        foreach (var defSpace in ChosenSpace.Defenders){
            defSpace.AttemptToPlaceCard(PickAValidCardForSpace(defSpace));
        }
    }

    public void SetActionsForStartingComp(){
        CardSpace ChosenSpace = MyBackline[Random.Range(0,MyBackline.Count)];
        // Debug.Log(ChosenSpace);
        CardDisplay ChosenCard = PickAValidCardForSpace(ChosenSpace);
        ReservedCards.Add(ChosenCard);
        // ChosenSpace.AttemptToPlaceCard(ChosenCard);
        
        int i = 0;
        foreach (var defSpace in ChosenSpace.Defenders){
            // Debug.Log(i);
            // CardDisplay ChosenCard = PickAValidCardForSpace(ChosenSpace);
            CardDisplay ChosenDefender = PickAValidCardForSpace(defSpace);
            ReservedCards.Add(ChosenDefender);
            MyActions[i].Action = ActionType.CardPurchase;
            MyActions[i].DestinationSlot = defSpace;
            MyActions[i].BoughtCard = ChosenDefender;
            i++;
        }
        MyActions[i].Action = ActionType.CardPurchase;
        MyActions[i].DestinationSlot = ChosenSpace;
        MyActions[i].BoughtCard = ChosenCard;
    }

    public void LookAtSpaces(){
        CardSpace[] AllSpaces = Object.FindObjectsOfType<CardSpace>();
        MyBackline.Clear();
        MyDefline.Clear();
        MyTrapline.Clear();
        MyCards.Clear();
        MyCardSpaces.Clear();
        OpponentSpaces.Clear();
        for (int i = 0; i < AllSpaces.Length; i++)
        {
            if(AllSpaces[i].Owner == Profile){
                MyCardSpaces.Add(AllSpaces[i]);
                switch(AllSpaces[i].Line){
                    case CardLine.Backline: MyBackline.Add(AllSpaces[i]); break;
                    case CardLine.Defensive: MyDefline.Add(AllSpaces[i]); break;
                    case CardLine.Trap: MyTrapline.Add(AllSpaces[i]); break;
                }
                if(AllSpaces[i].PlayingCard != null){
                    MyCards.Add(AllSpaces[i].PlayingCard);
                }
            } else {
                OpponentSpaces.Add(AllSpaces[i]);
            }
        }
    }

}

public enum AIActionStrategy{
    PlaceCards,
    Aggresive,
    Defensive,
    SaveGold
}

/* This has to be different from regular Turn Actions because we don't want the AI to work around them */
[System.Serializable]
public class AITurnActions{
    public ActionType Action;
    public CardDisplay Attacker;
    public CardDisplay AttackTarget;
    public CardDisplay BoughtCard;
    public CardSpace DestinationSlot;
    public int PurchasePrice;
}