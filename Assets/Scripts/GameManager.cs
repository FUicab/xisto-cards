using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.Linq;
//using UnityEngine.UIElements;
//using UnityEngine.UI;

public enum TurnMovementType{
	CardPurchase,
	PerformAction,
	MoveCard,
	Pass
}

public class GameManager : MonoBehaviour
{

	public GameObject CardObject;
	public TextMeshProUGUI GoldText;
	public TextMeshProUGUI OpponentGoldText;
	public TextMeshProUGUI ActionPointsText;
	public TextMeshProUGUI OpponentActionPointsText;
	public TextMeshProUGUI MainTooltip;
	public Dice Dice1UI_Host;
	public Dice Dice2UI_Host;
	public Dice Dice3UI_Host;
	public Dice Dice1UI_Opponent;
	public Dice Dice2UI_Opponent;
	public Dice Dice3UI_Opponent;
	public GameObject FloatingMessageObject;
	public GameObject DeckUI;
	public TextMeshProUGUI DebugText;
	public TextMeshProUGUI SmallTurnEndText;

	public Transform[] Hand;
	// public bool[] AvailableCardSlots;

	// public List<CardSlot> PlayingCards;
	public CardSpace[] CardSpaces;
 
	/* >>>> The list of the individual cards that will be available */
	public List<Card> CardList = new List<Card>();
	public List<Card> Deck = new List<Card>();

	/* >>>> Player turns and combat management */
	public List<PlayerProfile> Players;
	public List<TurnAction> RoundActions; /* The list of actions performed during the current round */
    public List<TurnAction> TurnActions; /* The list of actions performed during the current turn */
    public TurnAction CurrentAction = new TurnAction(); /* The action being executed right now */
	public int ActionPoints = 3;
	public int InitialActionPoints = 3;
	public PlayerProfile Host = new PlayerProfile();
	public PlayerProfile Opponent = new PlayerProfile();
	public PlayerProfile PlayerAtPlay;
	//public List<Dice> Dices;
	public TurnStatus turnStatus = TurnStatus.Idle;

	public GameObject TurnConfirmationButton;
	public Image ConfirmationButtonImage;
	public Button ConfirmationButton;
	public TextMeshProUGUI ConfirmationButtonText;
	public TextMeshProUGUI ConfirmationButtonSmallText;
	public string EndTurnText = "End turn";
	public string EndRoundText = "End round";
	public string GoldOnPassTip = "Passing on a turn rewards <b>+1 Gold</b> at the end of the round.";
	public string WaitingForOpponentText = "Waiting for opponent turn...";
	public int availableActionsForThisTurn = 0;
	public int turnIndex = 0;
	public int roundIndex = 0;

	[SerializeField] public PlayerAI OpponentAI = new PlayerAI();


	[SerializeField] public Canvas MainUI;

	public void DrawCards(PlayerProfile player){

		if(Deck.Count>=1){
			for(int i = 0; i < player.AvailableCardSlots.Count; i++){
				if(player.AvailableCardSlots[i] == true){
					Card RandomCard = Deck[Random.Range(0, Deck.Count)];
					GameObject CardInstance = Instantiate(CardObject,player.Hand[i].transform);
					CardInstance.GetComponent<CardDisplay>().card = RandomCard;
					//CardInstance.GetComponent<CardDisplay>().HasBeenPlayed = false;
					CardInstance.GetComponent<CardDisplay>().HandIndex = i;
                    CardInstance.GetComponent<CardDisplay>().Owner = player;
                    player.AvailableCardSlots[i] = false;
					Deck.Remove(RandomCard);
				}
			}
		}

	}

	void Awake()
	{
		MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
		ConfirmationButton = TurnConfirmationButton.GetComponent<Button>();
		ConfirmationButtonImage = TurnConfirmationButton.GetComponent<Image>();
		ConfirmationButtonText = GameObject.Find(ConfirmationButton.name + "/Main text").GetComponent<TextMeshProUGUI>();
		ConfirmationButtonSmallText = GameObject.Find(ConfirmationButton.name + "/Small text").GetComponent<TextMeshProUGUI>();
		CardSpaces = GameObject.FindObjectsOfType<CardSpace>();
	}

	private void Start(){
		foreach(Card card in CardList){
			for(int i = 0; i < card.CardCount; i++){
				Deck.Add(card);
			}
		}
		foreach(CardSpace slot in CardSpaces){
			if(slot.OwnerRole == PlayerRole.Host){
				slot.Owner = Host;
			} else if(slot.OwnerRole == PlayerRole.Opponent){
				slot.Owner = Opponent;
			}
			slot.SetRowPositionData();
		}
		EventManager.OnDeckReady();
		SetupStartingRound();
	}

	private void Update(){
		// DeckSizeText.text = Deck.Count.ToString();
		// DiscardPileSizeText.text = DiscardPile.Count.ToString();
		UpdateDebugInfo();
	}

	public void UpdateDisplayGoldValues(){
		GoldText.text = Host.Gold.ToString();
		OpponentGoldText.text = Opponent.Gold.ToString();
		ActionPointsText.text = Host.actionPoints.ToString();
		OpponentActionPointsText.text = Opponent.actionPoints.ToString();
	}

	public TurnAction RegisterCurrentAction(){
        //RoundActions.Add(new TurnAction(CurrentAction));
        if (PlayerAtPlay.selectedDice != null) PlayerAtPlay.selectedDice.Use();
        TurnActions.Add(new TurnAction(CurrentAction) { Owner = PlayerAtPlay });
        CurrentAction.Clean();
		availableActionsForThisTurn -= 1;
		SetConfirmationButton("");
		return TurnActions[TurnActions.Count - 1];
	}

	public void SaveTurnActions(List<TurnAction> turnActions)
	{
		RoundActions.AddRange(turnActions);
		TurnActions.Clear();
	}

	public void RemoveAction(TurnAction Action){
		RoundActions.Remove(Action);
		PlayerAtPlay.actionPoints += 1;
	}

	public void ClearActionPoints(){
		RoundActions.Clear();
	}

	public void UpdateDebugInfo()
	{
		string info = $"Current player: <b>{PlayerAtPlay.Role}</b>\n" +
				$"Card in action: <b>{CurrentAction.CardInAction?.card?.Name}</b>\n" +
				$"Turn: {turnIndex+1} | Round: {roundIndex+1} \n\n" +
				$"<i>Registered actions</i>\n";
		foreach (TurnAction tuAc in RoundActions)
		{
			switch (tuAc.movementType)
			{
				case TurnMovementType.CardPurchase:
					if(tuAc.CardInAction != null)
					info += $"💲 <b>{tuAc.Owner.Role}</b> bought <b>{tuAc.CardInAction.card.name}</b> for <b>{tuAc.PurchasePrice}</b> gold.\n";
				break;

				case TurnMovementType.PerformAction:
					if(tuAc.CardInAction != null)
					switch (tuAc.actionObject.action.actionType)
					{
						case ActionTypes.Attack:
							info += $"💥 <b>{tuAc.Owner.Role}</b>'s <b>{tuAc.CardInAction.card.Name}</b> attacks: \n";
							for (int i = 0; i < tuAc.targets.Count; i++)
							{
								CardDisplay target = tuAc.targets[i];
                                AttackActionOutput attackOutput = tuAc.actionObject.action.attacks[i].attackActionOutput;
								info += $" <b>· {target.card.name}</b> <i>({attackOutput.damageTypeIcon} {attackOutput.damage}) {(attackOutput.resultsInDeath?"FATAL":"")}</i> \n";

								List<AttackAction> extraAttacks = tuAc.CardInAction.appliedBuffs.Where(x => x.specialEffect == BuffSpecialEffects.TriggerExtraAttack).Where(x => x.TargetMeetsOnHitRequirements(target)).SelectMany(x => x.extraAttacks).ToList();
								if(extraAttacks.Count > 0) {
									foreach (AttackAction extraAttack in extraAttacks)
									{
										AttackActionOutput extraAttackOutput = extraAttack.attackActionOutput;
										info += $"    <b>·</b> <i>({extraAttackOutput.damageTypeIcon} {extraAttackOutput.damage}) {(extraAttackOutput.resultsInDeath ? "FATAL" : "")}</i> \n";
									}

								}
							}
							//info += "\n";
						break;
						case ActionTypes.Buff:
							info += $"⏫ <b>{tuAc.Owner.Role}</b>'s <b>{tuAc.CardInAction.card.Name}</b> applies buffs: ";
							for (int i = 0; i < tuAc.targets.Count; i++)
							{
								if(tuAc.actionObject.action.buffs[i].amount > 0){ info += "+"; }
								info += tuAc.actionObject.action.buffs[i].amount+" "+CardTranslator.BuffAttributeDescription(tuAc.actionObject.action.buffs[i].Attribute);
								switch (tuAc.actionObject.action.buffs[i].target)
								{
									default: info += " "+CardTranslator.TargetTypeDescription(tuAc.actionObject.action.buffs[i].target); break;
								}
								// info += "<b>· "+tuAc.targets[i].card.name+"</b> "+CardTranslator.BuffAttributeDescription(tuAc.actionObject.action.buffs[i].Attribute)+" <i>("+tuAc.actionObject.action.buffs[i].amount+")</i> ";
								if(i+1 < tuAc.targets.Count){ info += ", "; }
							}
							info += "\n";
						break;
					}
				break;

                case TurnMovementType.Pass:
                    info += $"💰 <b>{tuAc.Owner.Role}</b> passed.\n";
                break;

                case TurnMovementType.MoveCard:
				break;
			}
		}
		DebugText.text = info;
	}

	/* --- Attack management functions --------------------------------------------- */
	public void ClearAttackActions(CardDisplay attacker){
		for (int i = RoundActions.Count-1; i >= 0; i--)
		{
			if(RoundActions[i].CardInAction == attacker && RoundActions[i].movementType == TurnMovementType.PerformAction){
				foreach (var target in RoundActions[i].targets)
				{
					if(target != null) { target.ClearAllDisplay(); }
				}
				if(RoundActions[i].CardInAction != null){
					RoundActions[i].CardInAction.ClearAllDisplay();
				}
				RemoveAction(RoundActions[i]);
			}
		}
	}

	/* --- Turn management and card action functions --------------------------------------------- */
	/** Starts an action event */
	public void StartAction(CardActionObject action)
	{
		if(!CheckAvailableActions() || !CheckAvailableTargetsForAction(action)){ return; }
		CurrentAction.movementType = TurnMovementType.PerformAction;
		switch (action.action.actionType)
		{
			case ActionTypes.Attack:
			case ActionTypes.Buff:
				CurrentAction.RegisterAction(action);
			break;
		}
		SetConfirmationButton(false);
		UpdateTargetSelectionStatus();
		EventManager.OnTurnActionChange(CurrentAction);
	}

	public void SelectCardAsTargetOfAction(CardDisplay card)
	{
		CurrentAction.SetCardAsTarget(card);
		UpdateTargetSelectionStatus();
		EventManager.OnTurnActionChange(CurrentAction);
	}

	public void UpdateTargetSelectionStatus()
	{
		string tooltipMessage = "";
		CardActionObject actionObject = CurrentAction.actionObject;
		if(CurrentAction.remainingTargets > 0)
		{
			turnStatus = TurnStatus.SelectingTargets;
			tooltipMessage += CurrentAction.remainingTargets+" target(s) to select.";
			//actionObject.action.attacks.Concat<ActiveAction>(actionObject.action.buffs).ToList()[CurrentAction.nextNullIndex].GetPotentialTargets().ForEach(x => x.isPotentialTargetForPerformingAction = true);
            switch (actionObject.action.actionType)
			{
				case ActionTypes.Attack:
					if(CurrentAction.nextNullIndex >= 0)
					{
                        actionObject.action.attacks[CurrentAction.nextNullIndex].WarnPotentialTargetsAboutThisAction();
                        switch (actionObject.action.attacks[CurrentAction.nextNullIndex].target)
						{
							case TargetTypes.SingleEnemy:
								tooltipMessage += " Select 1 enemy for a "+CardTranslator.DamageTypeDescription(actionObject.action.attacks[CurrentAction.nextNullIndex].damageType)+" attack.";
							break;
						}
					}
				break;
				case ActionTypes.Buff:
					if(CurrentAction.nextNullIndex >= 0)
					{
                        actionObject.action.buffs[CurrentAction.nextNullIndex].WarnPotentialTargetsAboutThisAction();
                        switch (actionObject.action.buffs[CurrentAction.nextNullIndex].target)
						{
							case TargetTypes.SingleEnemy:
								// SelectCardAsTargetOfAction(CurrentAction.CardInAction);
								// UpdateTargetSelectionStatus();
								// return;
								tooltipMessage += " Select 1 enemy to apply "+ CardTranslator.AttributeAndValue(actionObject.action.buffs[CurrentAction.nextNullIndex])+".";
							break;
						}
					}
				break;
				default: tooltipMessage += " Oh no, this action type doesn't seem to be supported yet."; break;
			}
			CurrentAction.CardInAction.SetLine(CurrentAction.targets);
			SetMainTooltipText(tooltipMessage);
		} else {
			RegisterCurrentAction();
			foreach (TurnAction tuAc in TurnActions)
			{
				tuAc.CardInAction?.SetLine(tuAc.targets);
			}
			turnStatus = TurnStatus.Waiting;
			SetMainTooltipText("");
		}
	}
	
	/** Searchs for a card and returns the first action performed with it */
	public TurnAction ActionOfCard(CardDisplay card){
		foreach (TurnAction action in RoundActions)
		{
			if(action.CardInAction == card){
				return action;
			}
		}
		return null;
	}

	/* --- Turn management functions --------------------------------------------- */
	public bool CanBuyCard(CardDisplay card){
		bool CardCanBeBought = false;
		if(CheckGold(card.cost) && CheckAvailableActions()){
			PlayerAtPlay.Gold -= card.cost;
			CardCanBeBought = true;
		}
		UpdateDisplayGoldValues();
		return CardCanBeBought;
	}
	public void RefundCard(TurnAction PurchaseAction){
		if(PurchaseAction != null){
			PlayerAtPlay.Gold += PurchaseAction.PurchasePrice;
			PlayerAtPlay.AvailableCardSlots[PurchaseAction.HandIndexOrigin] = false;
			if(CurrentAction.CardInAction != null){
				CurrentAction.CardInAction.ClearAllDisplay();
				CurrentAction.CardInAction = null;
			}
			ClearAttackActions(PurchaseAction.CardInAction);
			RemoveAction(PurchaseAction);
			UpdateDisplayGoldValues();
		}
	}
	public void UndoAction(){
		if(RoundActions.Count > 0){
			TurnAction Action = RoundActions[RoundActions.Count - 1];
			if(Action.movementType == TurnMovementType.PerformAction){
				Action.CardInAction.SetLine();
				Action.CardInAction.SetOutline();
			}
			RoundActions.RemoveAt(RoundActions.Count - 1);
		}
	}

	/* --- Round and Turn management functions --------------------------------------------- */

	public void SetupStartingRound()
	{
		Host.Role = PlayerRole.Host;
		Host.Gold = 5;
		Host.GM = this;
		Host.Dices.Add(Dice1UI_Host);
		Host.Dices.Add(Dice2UI_Host);
		Host.Dices.Add(Dice3UI_Host);
		DrawCards(Host);

		Opponent.Role = PlayerRole.Opponent;
		Opponent.Gold = 5;
		Opponent.GM = this;
		Opponent.Dices.Add(Dice1UI_Opponent);
		Opponent.Dices.Add(Dice2UI_Opponent);
		Opponent.Dices.Add(Dice3UI_Opponent);
		DrawCards(Opponent);

		Opponent.otherPlayer = Host;
		Host.otherPlayer = Opponent;

		Players.Add(Host);
		Players.Add(Opponent);
		OpponentAI.GM = this;
		OpponentAI.MyProfile = Opponent;
		OpponentAI.OpponentProfile = Host;
		MainTooltip.text = "";

		PlayerAtPlay = Host;
		PlayerAtPlay.actionPoints -= 1;

		availableActionsForThisTurn = 1;
		UpdateDisplayGoldValues();
		RollAllDices();
		SetConfirmationButton(EndTurnText, GoldOnPassTip, true);
	}
	public async void TurnEnd(){
		List<TurnAction> turnActions = new List<TurnAction>();
		foreach (TurnAction action in TurnActions)
		{
			switch (action.movementType)
			{
				case TurnMovementType.CardPurchase:
					if(action.CardInAction != null)
					{
						action.CardInAction.DisableUndoPurchase();
					}
				break;

				case TurnMovementType.PerformAction:
					if (PlayerAtPlay.useAI)
					{
						await Task.Delay(400);
					}
					action.Perform();
					break;
			}
			turnActions.Add(action);
		}
		if(TurnActions.Count == 0)
		{
			turnActions.Add(new TurnAction() { movementType = TurnMovementType.Pass, Owner = PlayerAtPlay});
		}
		if(availableActionsForThisTurn > 0)
		{
			PlayerAtPlay.actionPointsTurnedToGold += availableActionsForThisTurn;
		}
		//PlayerAtPlay.Gold += ActionPoints;
		//PlayerAtPlay.Gold += 1;
		//DrawCards(PlayerAtPlay);
		SaveTurnActions(turnActions);
		UpdateDisplayGoldValues();
		//ClearActionPoints();
		SwitchTurns();
	}

	public void RoundEnd()
	{
		EventManager.OnRoundEnd();
		RoundRestart();
	}

	public async void RoundRestart()
	{
		RollAllDices();
		foreach (PlayerProfile player in Players)
		{
			player.Gold += 1;
			player.Gold += player.actionPointsTurnedToGold;
			player.actionPointsTurnedToGold = 0;
			player.actionPoints = player.maxActionPoints;
			DrawCards(player);
		}
		turnIndex = 0;
		PlayerAtPlay.actionPoints--;
		SetConfirmationButton(EndTurnText, GoldOnPassTip, true);
		roundIndex++;
		UpdateDisplayGoldValues();
		ClearActionPoints();
	}

	public void SwitchTurns(){
		bool newRound = false;
		if((PlayerAtPlay == Host || Host.actionPoints <= 0) && Opponent.actionPoints > 0){
			PlayerAtPlay = Opponent;
			SetConfirmationButton(EndTurnText, WaitingForOpponentText, false);
		} else if(Host.actionPoints > 0) {
			PlayerAtPlay = Host;
			SetConfirmationButton(EndTurnText, GoldOnPassTip, true);
		} else
		{
			newRound = true;
			RoundEnd();
		}
		//Debug.Log($"Host: {Host.actionPoints} | Opponent: {Opponent.actionPoints} | {PlayerAtPlay.Role} will play now.");
		if (!newRound)
		{
			PlayerAtPlay.actionPoints -= 1;
			turnIndex++;
			if(Host.actionPoints <= 0 && Opponent.actionPoints <= 0)
			{
				SetConfirmationButton(EndRoundText, GoldOnPassTip, true);
			}
		}
		availableActionsForThisTurn = 1;
		if(PlayerAtPlay.useAI){
			OpponentAI.StartAI();
		}
	}

	public void SetConfirmationButton(string bigText = "", string smallText = "", bool enabled = true){
		ConfirmationButtonText.text = bigText;
		ConfirmationButtonSmallText.text = smallText;
		ConfirmationButton.interactable = enabled;
		switch (bigText) {
			case "End turn":
				ConfirmationButtonImage.color = new Color(1f, 0.8f, 0.74f);
			break;
			case "End round":
				ConfirmationButtonImage.color = new Color(0.97f, 0.64f, 0.98f);
			break;
			default:
				ConfirmationButtonImage.color = new Color(0.94f, 0.85f, 0.74f);
			break;
		}
	}
	public void SetConfirmationButton(bool enabled = true)
	{
		SetConfirmationButton(ConfirmationButtonText.text, ConfirmationButtonSmallText.text, enabled);
	}
	public void SetConfirmationButton(string smallText = "")
	{
		SetConfirmationButton(ConfirmationButtonText.text, smallText, true);
	}

	public void HealCardsOfPlayer(PlayerProfile player){
		CardSpace[] AllSpaces = Object.FindObjectsOfType<CardSpace>();
		for (int i = 0; i < AllSpaces.Length; i++){
			if(AllSpaces[i].Owner == player){
				if(AllSpaces[i].PlayingCard != null){
					AllSpaces[i].PlayingCard.ResetHP();
				}
			}
		}
	}

	public void RollAllDices()
	{
		foreach (PlayerProfile player in Players)
		{
			player.RollDices();
		}
	}

	/** Checks if the attacked target can be attacked */
	public bool CheckValidAttack(CardDisplay target, CardDisplay attacker = null){
		bool isOk = false;

		/* Check for defending status */
		bool isDefended = false;
		foreach (var defendingSpace in target.mySpace.Defenders){
			if(defendingSpace.PlayingCard != null){
				isDefended = true;
			}
		}
		if(isDefended){
			DisplayFloatingMessage("Can't attack defended cards", Camera.main.ScreenToWorldPoint(Input.mousePosition), "orange");
		} else if(target.card.Type == UnitType.Trap){
			DisplayFloatingMessage("Can't attack traps", Camera.main.ScreenToWorldPoint(Input.mousePosition), "orange");
		} else {
			isOk = true;
		}
		return isOk;
	}

	/* --- Values and resource checks --------------------------------------------- */
	public bool CheckGold(int requirement){
		bool isOk = false;
		if(PlayerAtPlay.Gold >= requirement){
			isOk = true;
		} else {
			DisplayFloatingMessage("Not enough gold", Camera.main.ScreenToWorldPoint(Input.mousePosition), "gold");
		}
		return isOk;
	}

	public bool CheckAvailableTargetsForAction(CardActionObject actionObject)
	{
		bool isOk = true;
        foreach (ActiveAction action in actionObject.action.attacks.Concat<ActiveAction>(actionObject.action.buffs).ToList())
        {
			if (action.GetPotentialTargets().Count == 0) {
				isOk = false;
			}
        }
		if (!isOk)
		{
            DisplayFloatingMessage("This action would not reach anyone", Camera.main.ScreenToWorldPoint(Input.mousePosition), "red");
        }
        return isOk;
	}

	public bool CheckAvailableActions(int requirement = 1){
		bool isOk = false;
		if(availableActionsForThisTurn >= requirement){
			isOk = true;
		} else {
			DisplayFloatingMessage("No more actions available\nEnd your turn to continue", Camera.main.ScreenToWorldPoint(Input.mousePosition), "green");
		}
		return isOk;
	}

	/* --- Floating messages --------------------------------------------- */
	public async void DisplayDamage(int damage, CardDisplay target){
		GameObject MessageObject = Instantiate(FloatingMessageObject);
		MessageObject.GetComponent<FloatingMessage>().SetMessage(damage.ToString());
		Vector3 position = new Vector3(Random.Range(-50f,50f)+target.transform.position.x, Random.Range(-50f, 50f) + target.transform.position.z, target.transform.position.z);
		MessageObject.transform.Find("Canvas").GetComponent<RectTransform>().anchoredPosition = position;
		await Task.Delay(200);
	}
	public void DisplayFloatingMessage(string message, Vector3 location, string colorName = ""){
		GameObject MessageObject = Instantiate(FloatingMessageObject);
		MessageObject.GetComponent<FloatingMessage>().SetFontSize(0.33f);
		MessageObject.GetComponent<FloatingMessage>().SetColor(colorName);
		MessageObject.GetComponent<FloatingMessage>().SetMessage(message);
		MessageObject.transform.Find("Canvas").GetComponent<RectTransform>().anchoredPosition = location;
	}

	/* --- Main Tooltip management --------------------------------------------- */
	public void SetMainTooltipText(string text)
	{
		MainTooltip.text = text;
	}
}

[System.Serializable]
public class CardSlot{
	public bool Occupied = false;
	public CardLine Line = CardLine.Backline;
	public CardDisplay PlayingCard;
	public Transform SlotObject;
}

[System.Serializable]
public class TurnAction{
	
	public TurnMovementType movementType;
	public CardDisplay CardInAction;
	//public CardDisplay BoughtCard;
	public CardActionObject actionObject;
	public List<CardDisplay> targets = new List<CardDisplay>();
	public int nextNullIndex = -1;
	public int remainingTargets = 0;
	public int HandIndexOrigin;
	public int PurchasePrice;
	public PlayerProfile Owner;

	public TurnAction(TurnAction Origin = null){
		if(Origin != null){
			movementType = Origin.movementType;
			CardInAction = Origin.CardInAction;
			//BoughtCard = Origin.BoughtCard;
			PurchasePrice = Origin.PurchasePrice;
			HandIndexOrigin = Origin.HandIndexOrigin;
			actionObject = Origin.actionObject;
			targets = new List<CardDisplay>(Origin.targets);
			Owner = Origin.Owner;
		}
		UpdateTargetCountAndIndex();
	}

	public void RegisterAction(CardActionObject actionObj)
	{
		Clean();
		actionObject = actionObj;
		CardInAction = actionObject.sourceCard;
		CardInAction.SetOutline("orange");
		switch (actionObject.action.actionType)
		{
			case ActionTypes.Attack:
				foreach (var attack in actionObject.action.attacks)
				{
					if (attack.isTargetImplicit) { targets.Add(actionObject.sourceCard); } else { targets.Add(null); }
				}
			break;
			case ActionTypes.Buff:
				foreach (var buff in actionObject.action.buffs)
				{
                    if (buff.isTargetImplicit) { targets.Add(actionObject.sourceCard); } else { targets.Add(null); }
                }
			break;
		}
		UpdateTargetCountAndIndex();
	}

	private void UpdateTargetCountAndIndex()
	{
		int index = 0;
		remainingTargets = 0;
		foreach (var target in targets)
		{
			if(target == null)
			{
				remainingTargets += 1;
				if(nextNullIndex < 0)
				{
					nextNullIndex = index;
				}
			}
			index ++;
		}
	}

	public void SetCardAsTarget(CardDisplay card)
	{
		targets[nextNullIndex] = card;
		remainingTargets -= 1;
		nextNullIndex = -1;

		int index = 0;
		foreach (var target in targets)
		{
			if(nextNullIndex < 0 && target == null)
			{
				nextNullIndex = index;
			}
			index ++;
		}
		if(remainingTargets <= 0)
		{
			nextNullIndex = -1;
		}
	}

	public void Perform()
	{
		CardActionTools.PerformConfirmedAction(this);
	}

	public void Clean(){
		CardInAction = null;
		//BoughtCard = null;
		HandIndexOrigin = 0;
		PurchasePrice = 0;
		remainingTargets = 0;
		nextNullIndex = -1;
		actionObject = null;
		targets.Clear();
	}
}

public enum PlayerRole {
	Host,
	Opponent
}

public enum TurnStatus
{
	Idle,
	Waiting,
	SelectingTargets
}