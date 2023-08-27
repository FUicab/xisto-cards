using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerProfile;
using static CardDisplay;
using static CardSpace;
using static CardLine;
using static UnitType;
using static UnitSubtype;

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

    /* --- AI card management --------------------------------------------- */
    public List<CardSpace> MyCardSpaces = new List<CardSpace>();
    public List<CardSpace> MyBackline = new List<CardSpace>();
    public List<CardSpace> MyTrapline = new List<CardSpace>();
    public List<CardSpace> MyDefline = new List<CardSpace>();
    public List<CardSpace> OpponentSpaces = new List<CardSpace>();
    public List<CardDisplay> MyCards = new List<CardDisplay>();

    /* --- Other variables --------------------------------------------- */
    public GameManager GM; // GM is designated during instatiation
    public int TurnCount = 1;

    void Start(){
        // GM = FindObjectOfType<GameManager>();
    }
    
    public void StartAI(){
        if(Profile == null){ return; }
        if(!Profile.useAI){ return; }

        LookAtSpaces();
        if(StartsWithBasicTrio && TurnCount <= 1){ PlaceStartingComp(); } else {
            
            RandomizeStrategy();

            Debug.Log(ChosenStrategy);
            switch (ChosenStrategy){
                case AIActionStrategy.PlaceCards:
                    RandomlyPlaceCardsInHand();
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
        GM.TurnEnd();
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

    public void RandomlyPlaceCardsInHand(){
        foreach (var cardInHand in Profile.Hand){
            CardDisplay card = cardInHand.gameObject.GetComponentInChildren<CardDisplay>();
        }

        int RandomIndex = 0;
        int RandomSlot = 0;
        for (int i = 0; i < 3; i++)
        {
            RandomIndex = Random.Range(0,Profile.Hand.Length);
            // CardDisplay RandomCardDisplay = Profile.Hand[RandomIndex].gameObject.GetComponentInChildren<CardDisplay>();
            do {
                RandomSlot = Random.Range(0,MyCardSpaces.Count-1);
            } while (MyCardSpaces[RandomSlot].Occupied);
            // Debug.Log(RandomCardDisplay.card.Name);
            // Debug.Log(MyCardSpaces[RandomSlot].gameObject.name);
            // Debug.Log(MyCardSpaces[RandomSlot].AttemptToPlaceCard(RandomCardDisplay));
            MyCardSpaces[RandomSlot].AttemptToPlaceCard(PickAValidCardForSpace(MyCardSpaces[RandomSlot]));
        }
    }

    public CardDisplay PickAValidCardForSpace(CardSpace space){
        List<CardDisplay> ValidCards = new List<CardDisplay>();
        if(PrioritizesDefendersForDefendingOnly){
            for (int i = 0; i < Profile.Hand.Length; i++){
                CardDisplay card = Profile.Hand[i].gameObject.GetComponentInChildren<CardDisplay>();
                if(card != null){
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
                if(card != null){
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
            return ValidCards[Random.Range(0,ValidCards.Count-1)];
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
                Debug.Log(card.card.Name);
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
        } else {
            RandomlyPlaceCardsInHand();
            return;
        }
    }

    public void PlaceStartingComp(){
        CardSpace ChosenSpace = MyBackline[Random.Range(0,MyBackline.Count-1)];
        // Debug.Log(ChosenSpace);
        CardDisplay ChosenCard = PickAValidCardForSpace(ChosenSpace);
        ChosenSpace.AttemptToPlaceCard(ChosenCard);
        foreach (var defSpace in ChosenSpace.Defenders){
            defSpace.AttemptToPlaceCard(PickAValidCardForSpace(defSpace));
        }
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