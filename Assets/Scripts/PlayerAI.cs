using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerProfile;
using static CardDisplay;
using static CardSpace;
using static CardLine;
using static UnitType;
using static UnitSubtype;
using static TurnMovementType;
using System.Threading.Tasks;
using System.Linq;

[System.Serializable]
public class PlayerAI{
	public PlayerProfile MyProfile;
    public PlayerProfile OpponentProfile;

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
	public bool allCardSpacesOccupied{
		get {
			bool allOccupied = true;
			foreach (CardSpace cardSpace in MyCardSpaces)
			{
				if (cardSpace.Occupied == false)
				{
					allOccupied = false;
				}
			}
			return allOccupied;
		}
	}

	/* --- AI card management --------------------------------------------- */
	public List<CardSpace> MyCardSpaces = new List<CardSpace>();
	public List<CardSpace> MyBackline = new List<CardSpace>();
	public List<CardSpace> MyDefline = new List<CardSpace>();
	public List<CardDisplay> ReservedCards = new List<CardDisplay>(); // These cards are currently being used in other actions within this same turn and will not be considered for other actions

	/* --- Other variables --------------------------------------------- */
	public GameManager GM; // GM is designated during instatiation
	public int TurnCount = 1;
	
	public async void StartAI(){
		if(MyProfile == null){ return; }
		if(!MyProfile.useAI){ return; }

		MyActions.Clear();
		for (int i = 0; i < GM.availableActionsForThisTurn; i++){
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
					PlayDefensive();
					break;

				case AIActionStrategy.SaveGold:
                    GM.TurnEnd(); // By saving gold the AI does nothing and ends its turn
                    break;

				default:
					GM.TurnEnd();
				break;
			}

		}
		// GM.TurnEnd();
		TurnCount ++;
	}

	public void RandomizeStrategy(){
		switch (Random.Range(0,4)){
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
				if (!allCardSpacesOccupied)
				{
					foreach (var action in MyActions){
						action.actionType = TurnMovementType.CardPurchase;
						action.DestinationSlot = PickRandomAvailableSpace();
						CardDisplay ChosenCard = PickAValidCardForSpace(action.DestinationSlot);
						ReservedCards.Add(ChosenCard);
						action.CardInAction = ChosenCard;
					}
				}
			break;
			case AIActionStrategy.Aggresive:
				
				break;
		}
	}
	public void PerformNextAction(){
		currentActionIndex ++;
		PerformAction();
	}
	public async void PerformAction(){
		bool Success = true;
		if(MyProfile.actionPoints <= 0 || GM.availableActionsForThisTurn <= 0 || currentActionIndex >= MyActions.Count){
			await Task.Delay(200);
			GM.TurnEnd();
			return;
		}
		AITurnActions CurrentAction = MyActions[currentActionIndex];
		switch (CurrentAction.actionType){
			case TurnMovementType.CardPurchase:
				if(CurrentAction.CardInAction != null){
					CurrentAction.DestinationSlot.AttemptToPlaceCard(CurrentAction.CardInAction, PerformNextAction);
				} else {
					Success = false;
				}
			break;
		}
		if(!Success){ PerformNextAction(); }
	}

	public void RandomlyPlaceCardsInHand(){
		foreach (var cardInHand in MyProfile.Hand){
			CardDisplay card = cardInHand.gameObject.GetComponentInChildren<CardDisplay>();
		}

		int RandomIndex = 0;
		int RandomSlot = 0;
		if (!allCardSpacesOccupied)
		{
			for (int i = 0; i < 3; i++)
			{
				RandomIndex = Random.Range(0,MyProfile.Hand.Count);
				do {
					RandomSlot = Random.Range(0,MyCardSpaces.Count);
				} while (MyCardSpaces[RandomSlot].Occupied);
				MyCardSpaces[RandomSlot].AttemptToPlaceCard(PickAValidCardForSpace(MyCardSpaces[RandomSlot]));
			}
		}
		GM.TurnEnd();
	}
	public void PlaceRandomCardFromHand(){
		if(GM.availableActionsForThisTurn <= 0 || allCardSpacesOccupied){
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
		if (!allCardSpacesOccupied)
		{
			do {
				RandomSlot = Random.Range(0,MyCardSpaces.Count);
			} while (MyCardSpaces[RandomSlot].Occupied);
		}
		return MyCardSpaces[RandomSlot];
	}

	public CardDisplay PickAValidCardForSpace(CardSpace space, bool reserve = false){
		List<CardDisplay> ValidCards = new List<CardDisplay>();
		// Debug.Log("Picking...");
		if(PrioritizesDefendersForDefendingOnly){
			for (int i = 0; i < MyProfile.Hand.Count; i++){
				CardDisplay card = MyProfile.Hand[i].gameObject.GetComponentInChildren<CardDisplay>();
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
			for (int i = 0; i < MyProfile.Hand.Count; i++){
				CardDisplay card = MyProfile.Hand[i].gameObject.GetComponentInChildren<CardDisplay>();
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

	//public List<CardDisplay> PickBestAttackers(){
	//	List<CardDisplay> AttackerList = new List<CardDisplay>();
	//	foreach (var card in MyCards){
	//		if(card.attack > 0 && AttackerList.Count < GM.availableActionsForThisTurn){
	//			AttackerList.Add(card);
	//			// Debug.Log(card.card.Name);
	//		}
	//	}
	//	AttackerList.Sort((a,b) => {
	//		return b.attack.CompareTo(a.attack);
	//	});
	//	return AttackerList;
	//}

	public async void PlayAggresive(){
		if(OpponentProfile.GetActiveCards().Count == 0) { GM.TurnEnd();  return; }

		List<CardActionObject> attackActions = GetUsableAttacksOfTopWarriors();
		if (attackActions.Count == 0) { GM.TurnEnd();  return; }

		CardActionObject chosenAction = attackActions[Random.Range(0,attackActions.Count)];
		GM.StartAction(chosenAction);
		await Task.Delay(500);
        foreach (AttackAction attack in chosenAction.action.attacks.Where(x => !x.isTargetImplicit).ToList())
        {
			List<CardDisplay> potentialTargets = GetMostVulnerableEnemies(attack);
			potentialTargets[Random.Range(0,potentialTargets.Count)].TriggerClickEvent();
			await Task.Delay(250);
        }
		GM.TurnEnd();
	}

	public async void PlayDefensive()
	{
        if (MyProfile.GetActiveCards().Count == 0) { GM.TurnEnd(); return; }

        List<CardActionObject> buffActions = GetUsableBuffsOfTopAllies();
		if(buffActions.Count == 0) { GM.TurnEnd(); return; }

		CardActionObject chosenAction = buffActions[Random.Range(0,buffActions.Count)];
        GM.StartAction(chosenAction);
        await Task.Delay(500);
        foreach (BuffAction buff in chosenAction.action.buffs.Where(x => !x.isTargetImplicit).ToList())
        {
			Debug.Log($"{GM.CurrentAction.CardInAction.card.Name} wants to give {CardTranslator.AttributeAndValue(buff)}");
			List<CardDisplay> potentialTargets = new();
			if (buff.targetIsFromMyTeam) {
                potentialTargets.AddRange(GetMostVulnerableAllies());
            } else {
                potentialTargets.AddRange(GetMostVulnerableEnemies(buff));
            }
            potentialTargets[Random.Range(0, potentialTargets.Count)].TriggerClickEvent();
            await Task.Delay(250);
        }
        GM.TurnEnd();
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
		foreach (CardSpace defSpace in ChosenSpace.Defenders){
			// Debug.Log(i);
			// CardDisplay ChosenCard = PickAValidCardForSpace(ChosenSpace);
			CardDisplay ChosenDefender = PickAValidCardForSpace(defSpace);
			ReservedCards.Add(ChosenDefender);
			MyActions[i].actionType = TurnMovementType.CardPurchase;
			MyActions[i].DestinationSlot = defSpace;
			MyActions[i].CardInAction = ChosenDefender;
			i++;
		}
		//MyActions[i].Action = TurnMovementType.CardPurchase;
		//MyActions[i].DestinationSlot = ChosenSpace;
		//MyActions[i].BoughtCard = ChosenCard;
	}

	public void LookAtSpaces(){
		MyCardSpaces = MyProfile.mySpaces;
		MyBackline.Clear();
		MyDefline.Clear();
		//MyTrapline.Clear();
		//MyCards.Clear();
		//MyCardSpaces.Clear();
		//OpponentSpaces.Clear();
		for (int i = 0; i < MyCardSpaces.Count; i++)
		{
			switch(MyCardSpaces[i].Line){
				case CardLine.Backline: MyBackline.Add(MyCardSpaces[i]); break;
				case CardLine.Defensive: MyDefline.Add(MyCardSpaces[i]); break;
				//case CardLine.Trap: MyTrapline.Add(MyCardSpaces[i]); break;
			}
		}
	}

    /* -------------------------------- AI analyzing tasks -------------------------------- */

	public List<CardDisplay> GetTopWarriors(int howMany = 3)
	{
		List<CardDisplay> theTop = new();
		List<CardDisplay> myCards = MyProfile.GetActiveCards().OrderByDescending(x => x.power).ToList();

        foreach (CardDisplay cardDisplay in myCards)
        {
            if(cardDisplay.card.Type == UnitType.Warrior && theTop.Count < howMany) {
				theTop.Add(cardDisplay);
			}
        }

        return theTop;
	}

    public List<CardActionObject> GetUsableActions(CardDisplay cardDisplay, ActionTypes actionType = ActionTypes.DoNothing)
	{
        List<CardActionObject> actions = new List<CardActionObject>();
        CardActionMenu actionMenu = new(cardDisplay);
        foreach (CardActionObject actionObj in actionMenu.actions)
        {
            if (actionObj.canBeUsed && (actionObj.action.actionType == actionType || actionType == ActionTypes.DoNothing ))
            {
                actions.Add(actionObj);
            }
        }
        return actions;
    }


    public List<CardActionObject> GetUsableAttackActions(CardDisplay cardDisplay) {
		return GetUsableActions(cardDisplay, ActionTypes.Attack);
	}

    public List<CardActionObject> GetUsableBuffActions(CardDisplay cardDisplay)
    {
        return GetUsableActions(cardDisplay, ActionTypes.Buff);
    }

	public List<CardActionObject> GetUsableAttacksOfTopWarriors()
	{
        List<CardActionObject> actions = GetTopWarriors().SelectMany(x => GetUsableAttackActions(x)).ToList();
        return actions;
    }

    public List<CardActionObject> GetUsableBuffsOfTopAllies()
    {
        List<CardActionObject> actions = MyProfile.GetActiveCards().SelectMany(x => GetUsableBuffActions(x)).ToList();
        return actions;
    }

    public List<CardDisplay> GetMostVulnerableEnemies(ActiveAction action)
	{
		if (action is AttackAction attackAction) {
			return OpponentProfile.GetActiveCards().Where(x => action.TargetCanBeReached(x)).ToList().OrderByDescending(x => CardActionTools.CalculateDamage(x, attackAction)).ToList();
		}
        else
        {
            return OpponentProfile.GetActiveCards().Where(x => action.TargetCanBeReached(x)).ToList().OrderByDescending(x => x.power).Reverse().ToList();
        }
    }

    public List<CardDisplay> GetMostVulnerableAllies()
    {
        return MyProfile.GetActiveCards().Where(x => !x.ProtectedByDefender).ToList().OrderByDescending(x => x.hp).Reverse().ToList();
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
	public TurnMovementType actionType;
	public CardDisplay AttackTarget;
	public CardDisplay CardInAction;
	public CardSpace DestinationSlot;
	public int PurchasePrice;
}