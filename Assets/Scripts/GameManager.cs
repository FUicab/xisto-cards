using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CardSpace;
using static UnitType;
using static PlayerAI;
using System.Threading.Tasks;
//using UnityEngine.UIElements;
//using UnityEngine.UI;

public enum TurnMovementType{
    CardPurchase,
    PerformAction,
    MoveCard
}

public class GameManager : MonoBehaviour
{

    public GameObject CardObject;
    public TextMeshProUGUI GoldText;
    public TextMeshProUGUI OpponentGoldText;
    public TextMeshProUGUI ActionPointsText;
    public TextMeshProUGUI OpponentActionPointsText;
    public TextMeshProUGUI MainTooltip;
    public GameObject Dice1UI_Host;
    public GameObject Dice2UI_Host;
    public GameObject Dice3UI_Host;
    public GameObject Dice1UI_Opponent;
    public GameObject Dice2UI_Opponent;
    public GameObject Dice3UI_Opponent;
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
    public List<TurnAction> TurnActions;
    public TurnAction CurrentAction = new TurnAction();
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
                    CardInstance.GetComponent<CardDisplay>().HasBeenPlayed = false;
                    CardInstance.GetComponent<CardDisplay>().HandIndex = i;
                    player.AvailableCardSlots[i] = false;
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
        CardSpaces = GameObject.FindObjectsOfType<CardSpace>();
        foreach(CardSpace slot in CardSpaces){
            if(slot.OwnerRole == PlayerRole.Host){
                slot.Owner = Host;
            } else if(slot.OwnerRole == PlayerRole.Opponent){
                slot.Owner = Opponent;
            }
            slot.SetRowPositionData();
        }
        MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
        ConfirmationButton = TurnConfirmationButton.GetComponent<Button>();
        ConfirmationButtonImage = TurnConfirmationButton.GetComponent<Image>();
        ConfirmationButtonText = GameObject.Find(ConfirmationButton.name+"/Main text").GetComponent<TextMeshProUGUI>();
        ConfirmationButtonSmallText = GameObject.Find(ConfirmationButton.name+"/Small text").GetComponent<TextMeshProUGUI>();
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
        TurnActions.Add(new TurnAction(CurrentAction));
        CurrentAction.Clean();
        //PlayerAtPlay.actionPoints -= 1;
        availableActionsForThisTurn -= 1;
        //ActionPointsText.text = Host.actionPoints.ToString();
        //OpponentActionPointsText.text = Opponent.actionPoints.ToString();
        SetConfirmationButton("");
        return TurnActions[TurnActions.Count - 1];
    }

    public void RemoveAction(TurnAction Action){
        TurnActions.Remove(Action);
        PlayerAtPlay.actionPoints += 1;
        //ActionPointsText.text = Host.actionPoints.ToString();
        //OpponentActionPointsText.text = Opponent.actionPoints.ToString();
    }

    public void ClearActionPoints(){
        TurnActions.Clear();
        //ActionPoints = 3;
        //InitialActionPoints = ActionPoints; 
        //ActionPointsText.text = Host.actionPoints.ToString();
        //OpponentActionPointsText.text = Opponent.actionPoints.ToString();
    }

    public void UpdateDebugInfo()
    {
        string info = "";
        info += $"Current player: <b>{PlayerAtPlay.Role}</b>\n" +
                $"Card in action: <b>{CurrentAction.CardInAction?.card?.Name}</b>\n" +
                $"Turn: {turnIndex+1} | Round: {roundIndex+1} \n\n" +
                $"<i>Registered actions</i>\n";
        foreach (TurnAction tuAc in TurnActions)
        {
            if(tuAc.CardInAction != null)
            {
                switch (tuAc.movementType)
                {
                    case TurnMovementType.CardPurchase:
                        info += "Bought <b>"+tuAc.CardInAction.card.name+"</b> for <b>"+tuAc.PurchasePrice+"</b> gold.\n";
                    break;

                    case TurnMovementType.PerformAction:
                        switch (tuAc.actionObject.action.actionType)
                        {
                            case ActionTypes.Attack:
                                info += "<b>"+tuAc.CardInAction.card.Name+"</b> attacks ";
                                for (int i = 0; i < tuAc.targets.Count; i++)
                                {
                                    info += "<b>· "+tuAc.targets[i].card.name+"</b> <i>("+CalculateDamage(tuAc.targets[i], tuAc.actionObject.action.attacks[i])+")</i> ";
                                }
                                // foreach (var tgt in tuAc.targets)
                                // {
                                //     info += "<b>· "+tgt.card.name+"</b> <i>("+CalculateDamage()+")</i> ";
                                // }
                                info += "\n";
                            break;
                            case ActionTypes.Buff:
                                info += "<b>"+tuAc.CardInAction.card.Name+"</b> applies buffs: ";
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
                    
                    case TurnMovementType.MoveCard:
                    break;
                }
            }
        }
        DebugText.text = info;
    }

    /* --- Attack management functions --------------------------------------------- */
    public void ClearAttackActions(CardDisplay attacker){
        for (int i = TurnActions.Count-1; i >= 0; i--)
        {
            if(TurnActions[i].CardInAction == attacker && TurnActions[i].movementType == TurnMovementType.PerformAction){
                foreach (var target in TurnActions[i].targets)
                {
                    if(target != null) { target.ClearAllDisplay(); }
                }
                if(TurnActions[i].CardInAction != null){
                    TurnActions[i].CardInAction.ClearAllDisplay();
                }
                RemoveAction(TurnActions[i]);
            }
        }
    }

    public CardDisplay GetActualTarget(CardDisplay target)
    {
        // AttackAction attack = ActionData.actionObject.action.attacks[i];
        CardDisplay actualTarget;
        if(target.ProtectedByDefender)
        {
            actualTarget = target.mySpace.Defenders[0].PlayingCard;
        } else {
            if(target.attackSponge != null){
                actualTarget = target.attackSponge;
            } else {
                actualTarget = target;
            }
        }
        return actualTarget;
        // actualTarget.ReceiveDamage(CalculateDamage(ActionData.CardInAction, target, attack));
    }

    public void PerformAttackAction(CardDisplay target, TurnAction ActionData, int i  = 0)
    {
        AttackAction attack = ActionData.actionObject.action.attacks[i];
        GetActualTarget(target).ReceiveDamage(CalculateDamage(GetActualTarget(target), attack));

        foreach (AttackEffect effect in attack.attackEffect)
        {
            switch (effect.effectType)
            {
                case AttackEffects.SplashDamage:
                    List<CardDisplay> affectedTargets = new List<CardDisplay>();
                    AttackAction splashAttack = new AttackAction(attack){
                        requirements = new List<Requirements>(),
                        attackEffect = new List<AttackEffect>(),
                    };
                    if (!effect.useAttackValue){
                        splashAttack.flatDamageOverwrite = effect.value;
                    }
                    int myIndex = target.mySpace.myIndexInRow;
                    if(myIndex != 0)
                    {
                        affectedTargets.Add(target.mySpace.myRow.BoardSpaces[myIndex-1].PlayingCard);
                    }
                    if(myIndex < target.mySpace.myRow.BoardSpaces.Count-1)
                    {
                        affectedTargets.Add(target.mySpace.myRow.BoardSpaces[myIndex+1].PlayingCard);
                    }

                    foreach (CardDisplay affectedTarget in affectedTargets)
                    {
                        if (affectedTarget != null)
                        {
                            GetActualTarget(affectedTarget).ReceiveDamage(CalculateDamage(GetActualTarget(affectedTarget), splashAttack));
                        }
                    }
                break;
                case AttackEffects.SelfDamage:
                    AttackAction selfAttack = new AttackAction(attack){
                        requirements = new List<Requirements>(),
                        attackEffect = new List<AttackEffect>(),
                        damageType = DamageTypes.SelfDamage
                    };
                    if (!effect.useAttackValue){
                        selfAttack.flatDamageOverwrite = effect.value;
                    }
                    attack.source.ReceiveDamage(CalculateDamage(attack.source, selfAttack));
                break;
                case AttackEffects.ApplyDebuff:
                    foreach (BuffAction debuff in effect.buffs)
                    {
                        BuffAction debuffAction = new BuffAction(debuff){
                            requirements = new List<Requirements>()
                        };
                        GetActualTarget(target).ReceiveBuff(debuffAction);
                    }
                break;
            }
        }
    }

    public void PerformConfirmedAction(TurnAction ActionData){
        if(ActionData.CardInAction != null)
        {
            ActionData.CardInAction.SetOutline();
            ActionData.CardInAction.SetLine();
        }
        switch (ActionData.actionObject.action.actionType)
        {
            case ActionTypes.Attack:
                for (int i = 0; i < ActionData.targets.Count; i++)
                {
                    if(ActionData.targets[i] != null)
                    {
                        ActionData.targets[i].SetOutline();
                    }
                    if(ActionData.targets[i]!=null && ActionData.CardInAction!=null){
                        PerformAttackAction(ActionData.targets[i], ActionData, i);
                    }
                }
            break;
            case ActionTypes.Buff:
                for (int i = 0; i < ActionData.targets.Count; i++)
                {
                    // if(ActionData.targets[i]!=null && ActionData.CardInAction!=null){
                    //     ActionData.actionObject.action.buffs[i].source = ActionData.CardInAction;
                    // }
                    switch (ActionData.actionObject.action.buffs[i].target)
                    {
                        case TargetTypes.AllAllies:
                            foreach(BoardRow row in ActionData.targets[i].mySpace.Owner.MyBoardRows)
                            {
                                foreach (CardSpace cardSpace in row.BoardSpaces)
                                {
                                    cardSpace.PlayingCard?.ReceiveBuff(ActionData.actionObject.action.buffs[i], ActionData.CardInAction);
                                }
                            }
                        break;
                        case TargetTypes.AlliesInSameLine:
                            foreach(CardSpace cardSpace in ActionData.targets[i].mySpace.myRow.BoardSpaces)
                            {
                                cardSpace.PlayingCard?.ReceiveBuff(ActionData.actionObject.action.buffs[i], ActionData.CardInAction);
                            }
                        break;
                        case TargetTypes.AlliesNextToMe:
                            foreach(CardSpace cardSpace in ActionData.targets[i].mySpace.myRow.BoardSpaces)
                            {
                                if (cardSpace.PlayingCard != null && ActionData.targets[i].mySpace.IsNextToMe(cardSpace))
                                {
                                    cardSpace.PlayingCard.ReceiveBuff(ActionData.actionObject.action.buffs[i], ActionData.CardInAction);
                                }
                            }
                        break;
                        default:
                            ActionData.targets[i].ReceiveBuff(ActionData.actionObject.action.buffs[i], ActionData.CardInAction);
                        break;
                    }
                    if(ActionData.targets[i] != null)
                    {
                        ActionData.targets[i].SetOutline();
                    }
                }
            break;
        }
    }

    // public int CalculateDamage(CardDisplay attacker, CardDisplay target){
    //     int dmg = attacker.attack - target.armor[0];
    //     if(dmg <= 0){
    //         dmg = 1;
    //     }

    //     return dmg;
    // }

    public int CalculateDamage(CardDisplay target, AttackAction attackAction){
        
        CardDisplay attacker = attackAction.source;

        /* Calculation of temporary buffs and debuffs */
        var attackerTempModifiers = (
            Attack: 0,
            Health: 0,
            MaxHealth: 0,
            Defense: 0,
            Armor: new List<int>{0,0,0},
            ArmorPierce: 0,
            DamageReductionBeforeArmor: 0,
            DamageReductionAfterArmor: 0
        );
        var targetTempModifiers = (
            Attack: 0,
            Health: 0,
            MaxHealth: 0,
            Defense: 0,
            Armor: new List<int>{0,0,0},
            ArmorPierce: 0,
            DamageReductionBeforeArmor: 0,
            DamageReductionAfterArmor: 0
        );
        foreach (BuffAction modifier in attackAction.temporaryBuffs)
        {
            if(attackAction.target == TargetTypes.Self) /* The modifiers apply to myself */
            {
                switch (modifier.Attribute)
                {
                    case Attributes.Attack: attackerTempModifiers.Attack += modifier.amount; break;
                    case Attributes.Health: attackerTempModifiers.Health += modifier.amount; break;
                    case Attributes.MaxHealth: attackerTempModifiers.MaxHealth += modifier.amount; break;
                    case Attributes.Defense: attackerTempModifiers.Defense += modifier.amount; break;
                    case Attributes.DefenseMelee: attackerTempModifiers.Armor[0] += modifier.amount; break;
                    case Attributes.DefenseRanged: attackerTempModifiers.Armor[1] += modifier.amount; break;
                    case Attributes.DefenseEnergy: attackerTempModifiers.Armor[2] += modifier.amount; break;
                    case Attributes.ArmorPierce: attackerTempModifiers.ArmorPierce += modifier.amount; break;
                    case Attributes.DamageReductionBeforeArmor: attackerTempModifiers.DamageReductionBeforeArmor += modifier.amount; break;
                    case Attributes.DamageReductionAfterArmor: attackerTempModifiers.DamageReductionAfterArmor += modifier.amount; break;
                }
            } else if(attackAction.target == TargetTypes.SingleEnemy)
            {
                switch (modifier.Attribute)
                {
                    case Attributes.Attack: attackerTempModifiers.Attack += modifier.amount; break;
                    case Attributes.Health: attackerTempModifiers.Health += modifier.amount; break;
                    case Attributes.MaxHealth: attackerTempModifiers.MaxHealth += modifier.amount; break;
                    case Attributes.Defense: attackerTempModifiers.Defense += modifier.amount; break;
                    case Attributes.DefenseMelee: attackerTempModifiers.Armor[0] += modifier.amount; break;
                    case Attributes.DefenseRanged: attackerTempModifiers.Armor[1] += modifier.amount; break;
                    case Attributes.DefenseEnergy: attackerTempModifiers.Armor[2] += modifier.amount; break;
                    case Attributes.ArmorPierce: attackerTempModifiers.ArmorPierce += modifier.amount; break;
                    case Attributes.DamageReductionBeforeArmor: attackerTempModifiers.DamageReductionBeforeArmor += modifier.amount; break;
                    case Attributes.DamageReductionAfterArmor: attackerTempModifiers.DamageReductionAfterArmor += modifier.amount; break;
                }
            }
        }
        
        int targetArmor = 0;
        string damageType = "melee";
        switch (attackAction.damageType)
        {
            case DamageTypes.Melee: targetArmor = target.armor[0]; break;
            case DamageTypes.Ranged: targetArmor = target.armor[1]; break;
            case DamageTypes.Energy: targetArmor = target.armor[2]; break;
            case DamageTypes.MeleeOrRanged:
                if(target.armor[0] < target.armor[1])
                    { targetArmor = target.armor[0]; } else
                    { targetArmor = target.armor[1]; damageType = "ranged"; } break;
            case DamageTypes.RangedOrEnergy:
                if(target.armor[2] < target.armor[1])
                    { targetArmor = target.armor[2]; damageType = "energy"; } else
                    { targetArmor = target.armor[1]; damageType = "ranged"; } break;
            case DamageTypes.MeleeOrEnergy:
                if(target.armor[0] < target.armor[2])
                    { targetArmor = target.armor[0]; } else
                    { targetArmor = target.armor[2]; damageType = "energy"; } break;
            case DamageTypes.MeleeOrRangedOrEnergy:
                if(target.armor[0] < target.armor[1] && target.armor[0] < target.armor[2])
                    { targetArmor = target.armor[0]; } else if(target.armor[1] < target.armor[2])
                    { targetArmor = target.armor[1]; damageType = "ranged"; } else 
                    { targetArmor = target.armor[2]; damageType = "energy"; } break;
        }
        switch (damageType)
        {
            case "melee": targetArmor += targetTempModifiers.Armor[0]; break;
            case "ranged": targetArmor += targetTempModifiers.Armor[1]; break;
            case "energy": targetArmor += targetTempModifiers.Armor[2]; break;
        }
        targetArmor -= attacker.armorPierce + attackerTempModifiers.ArmorPierce;
        if(targetArmor < 0){ targetArmor = 0; }

        int dmg = Mathf.FloorToInt((attacker.attack + attackerTempModifiers.Attack) * attackAction.damageMultiplier) - targetArmor;
        if(attackAction.damageType == DamageTypes.SelfDamage)
        {
            targetArmor = 0;
        }
        if(attackAction.flatDamageOverwrite > 0)
        {
            dmg = attackAction.flatDamageOverwrite - targetArmor;
        }
        if(dmg <= 0){
            dmg = 1;
        }

        return dmg;
    }

    /* --- Turn management and card action functions --------------------------------------------- */
    /** Starts an action event */
    public void StartAction(CardActionObject action)
    {
        if(!CheckAvailableActions()){ return; }
        CurrentAction.movementType = TurnMovementType.PerformAction;
        switch (action.action.actionType)
        {
            case ActionTypes.Attack:
            case ActionTypes.Buff:
                CurrentAction.RegisterAction(action);
            break;
        }
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
            switch (actionObject.action.actionType)
            {
                case ActionTypes.Attack:
                    if(CurrentAction.nextNullIndex >= 0)
                    {
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
                        switch (actionObject.action.buffs[CurrentAction.nextNullIndex].target)
                        {
                            case TargetTypes.Self:
                                // SelectCardAsTargetOfAction(CurrentAction.CardInAction);
                                // UpdateTargetSelectionStatus();
                                // return;
                                // tooltipMessage += " Select 1 enemy for a "+actionObject.DamageTypeDescription(actionObject.action.attacks[CurrentAction.nextNullIndex].damageType)+" attack.";
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
        foreach (TurnAction action in TurnActions)
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
        if(TurnActions.Count > 0){
            TurnAction Action = TurnActions[TurnActions.Count - 1];
            if(Action.movementType == TurnMovementType.PerformAction){
                Action.CardInAction.SetLine();
                Action.CardInAction.SetOutline();
            }
            TurnActions.RemoveAt(TurnActions.Count - 1);
        }
    }

    /* --- Round and Turn management functions --------------------------------------------- */

    public void SetupStartingRound()
    {
        Host.Role = PlayerRole.Host;
        Host.Gold = 5;
        //ActionPointsText.text = Host.actionPoints.ToString();
        //OpponentActionPointsText.text = Opponent.actionPoints.ToString();
        Opponent.Role = PlayerRole.Opponent;
        Opponent.Gold = 5;
        UpdateDisplayGoldValues();
        Players.Add(Host);
        Players.Add(Opponent);
        OpponentAI.GM = this;
        OpponentAI.Profile = Opponent;
        DrawCards(Opponent);
        DrawCards(Host);
        MainTooltip.text = "";
        PlayerAtPlay = Host;
        Host.Dices.Add(new Dice(Dice1UI_Host));
        Host.Dices.Add(new Dice(Dice2UI_Host));
        Host.Dices.Add(new Dice(Dice3UI_Host));
        Opponent.Dices.Add(new Dice(Dice1UI_Opponent));
        Opponent.Dices.Add(new Dice(Dice2UI_Opponent));
        Opponent.Dices.Add(new Dice(Dice3UI_Opponent));
        PlayerAtPlay.actionPoints -= 1;
        availableActionsForThisTurn = 1;
        RollAllDices();
        SetConfirmationButton(EndTurnText, GoldOnPassTip, true);
    }
    public async void TurnEnd(){
        foreach (var action in TurnActions)
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
                    PerformConfirmedAction(action);
                break;
            }
        }
        if(availableActionsForThisTurn > 0)
        {
            PlayerAtPlay.actionPointsTurnedToGold += availableActionsForThisTurn;
        }
        //PlayerAtPlay.Gold += ActionPoints;
        //PlayerAtPlay.Gold += 1;
        //DrawCards(PlayerAtPlay);
        UpdateDisplayGoldValues();
        //ClearActionPoints();
        SwitchTurns();
    }

    public async void RoundEnd()
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
        Debug.Log($"Host: {Host.actionPoints} | Opponent: {Opponent.actionPoints} | {PlayerAtPlay.Role} will play now.");
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
        Vector3 position = new Vector3(Random.Range(-50,50)+target.transform.position.x, Random.Range(-50, 50) + target.transform.position.z, target.transform.position.z);
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

    public TurnAction(TurnAction Origin = null){
        if(Origin != null){
            movementType = Origin.movementType;
            CardInAction = Origin.CardInAction;
            //BoughtCard = Origin.BoughtCard;
            PurchasePrice = Origin.PurchasePrice;
            HandIndexOrigin = Origin.HandIndexOrigin;
            actionObject = Origin.actionObject;
            targets = new List<CardDisplay>(Origin.targets);
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
                    switch (attack.target)
                    {
                        case TargetTypes.SingleEnemy: targets.Add(null); break;
                        case TargetTypes.LineOfEnemies: targets.Add(null); break;
                        case TargetTypes.Self: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AllAllies: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AlliesNextToMe: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AlliesInSameLine: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.SingleAlly: targets.Add(null); break;
                        default: targets.Add(actionObject.sourceCard); break;
                    }
                }
            break;
            case ActionTypes.Buff:
                foreach (var buff in actionObject.action.buffs)
                {
                    switch (buff.target)
                    {
                        case TargetTypes.SingleEnemy: targets.Add(null); break;
                        case TargetTypes.LineOfEnemies: targets.Add(null); break;
                        case TargetTypes.Self: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AllAllies: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AlliesNextToMe: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.AlliesInSameLine: targets.Add(actionObject.sourceCard); break;
                        case TargetTypes.SingleAlly: targets.Add(null); break;
                        default: targets.Add(actionObject.sourceCard); break;
                    }
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

[System.Serializable]
public class PlayerProfile{
    public PlayerRole Role;
    public bool useAI = false;
    public int Gold = 0;
    [HideInInspector] public int maxActionPoints = 3;
    [HideInInspector] public int actionPoints = 3;
    [HideInInspector] public int actionPointsTurnedToGold = 0;
    public List<Transform> Hand = new List<Transform>();
    public List<bool> AvailableCardSlots = new List<bool>();
    public List<BoardRow> MyBoardRows = new List<BoardRow>();
    public List<BuffAction> buffs = new List<BuffAction>();
    public GameObject DiceUI_1;
    public GameObject DiceUI_2;
    public GameObject DiceUI_3;
    public List<Dice> Dices = new List<Dice>() {};
    public void RollDices()
    {
        foreach (var dice in Dices)
        {
            dice.Reset();
        }
        if (Dices[0].value == Dices[1].value && Dices[1].value == Dices[2].value)
        {
            Dices[0].MakeWild();
            Dices[1].MakeWild();
            Dices[2].MakeWild();
        }
    }
}

[System.Serializable]
public class Dice
{
    public int value;
    public bool used = false;
    public bool wild = false;
    public Color regularColor = new Color( 0.73f, 0.86f, 0.89f, 1.0f);
    public Color wildColor = new Color( 0.89f, 0.89f, 0.72f, 1.0f);
    public Color usedColor = new Color( 0.66f, 0.66f, 0.66f, 1.0f);
    public GameObject DiceDisplay;
    public Image DiceImage;
    public TextMeshProUGUI DiceText;
    public Dice(GameObject diceDisplay)
    {
        DiceDisplay = diceDisplay;
        DiceImage = DiceDisplay.GetComponent<Image>();
        DiceText = DiceDisplay.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Roll()
    {
        value = Random.Range(1,7);
        DiceText.text = value.ToString();
    }

    public void MakeWild()
    {
        wild = true;
        DiceImage.color = wildColor;
    }
    
    public void Use()
    {
        used = true;
        DiceImage.color = usedColor;
    }

    public void Reset()
    {
        Roll();
        DiceImage.color = regularColor;
        used = false;
        wild = false;
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