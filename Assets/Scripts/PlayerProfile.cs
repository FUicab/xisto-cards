using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class PlayerProfile
{
	public PlayerRole Role;
	public bool useAI = false;
	public int Gold = 0;
    public GameManager GM;
	[HideInInspector] public int maxActionPoints = 3;
	[HideInInspector] public int actionPoints = 3;
	[HideInInspector] public int actionPointsTurnedToGold = 0;
	public List<Transform> Hand = new List<Transform>();
	public List<bool> AvailableCardSlots = new List<bool>();
	public List<BoardRow> MyBoardRows = new List<BoardRow>();
	//public List<BuffAction> buffs = new List<BuffAction>();
	//public GameObject DiceUI_1;
	//public GameObject DiceUI_2;
	//public GameObject DiceUI_3;
	public List<Dice> Dices = new List<Dice>() { };
	public Dice selectedDice = null;
	public PlayerProfile otherPlayer;
	public List<PlayerBuffs> activeBuffs = new();
    public List<PlayerBuffs> passiveBuffs = new();
	public List<PlayerBuffs> appliedBuffs{ get { return activeBuffs.Concat(passiveBuffs).ToList(); } }
	public List<CardSpace> mySpaces {
		get
		{
			List<CardSpace> spaces = new();
			foreach (BoardRow row in MyBoardRows)
			{
				foreach (CardSpace space in row.BoardSpaces)
				{
					spaces.Add(space);
				}
			}
			return spaces;
		}
	}
	public bool isMyTurnToPlay { get { return GM?.PlayerAtPlay == this; } }
	public List<PlayerBuffs> FreeAttackBuffs {
		get { return appliedBuffs.Where(pBuff => pBuff.buffType == PlayerBuffTypes.FreeAttackActions && pBuff.usableAmount > 0).ToList(); }
	}
	public PlayerBuffs NextFreeAttackBuff
	{
        get { return FreeAttackBuffs.FirstOrDefault(pBuff => pBuff.usableAmount > 0); }
    }
	public int FreeAttackActions
	{
		get { return FreeAttackBuffs.Select(pBuff => pBuff.usableAmount).ToList().Sum(); }
	}

	public PlayerProfile()
	{
        EventManager.TurnActionChange += UpdateDiceSelectionStatus;
    }

    private void UpdateDiceSelectionStatus(TurnAction turnAction)
    {
		SelectDice(null);
        if(turnAction.CardInAction == null || GM?.PlayerAtPlay.Role != Role)
		{
			return;
		}
        foreach (Dice dice in Dices)
        {
			if (!dice.used && turnAction.actionObject.diceValues.Contains(dice.value)) {
				MakeDiceSelectable(dice);
			} else {
				dice.UpdateDiceSelectionStatus(false, false);
			}
        }
    }

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

	public void MakeDiceSelectable(Dice theDice)
	{
		if(selectedDice == null)
		{
			theDice.UpdateDiceSelectionStatus(true, true);
			selectedDice = theDice;
		} else {
            theDice.UpdateDiceSelectionStatus(true, false);
        }
    }

	public void SelectDice(Dice theDice)
	{
		foreach (Dice dice in Dices)
		{
			if(theDice == null)
			{
				dice.UpdateDiceSelectionStatus(false, false);
				selectedDice = null;
			} else if(dice == theDice) {
				dice.UpdateDiceSelectionStatus(true, true);
				selectedDice = dice;
			} else {
				dice.UpdateDiceSelectionStatus(dice.selectable, false);
			}
		}
	}

	public List<CardDisplay> GetActiveCards()
	{
		List<CardDisplay> activeCards = mySpaces.Where(x => x.HasCard).Select(x => x.PlayingCard).ToList();
        return activeCards;
	}

	public bool HasDiceForAction(CardActionObject action)
	{
		bool itHas = false;
        foreach (var dice in Dices)
        {
            if ((action.diceValues.Contains(dice.value) || dice.wild) && !dice.used)
            {
                itHas = true;
            }
        }
		return itHas;
    }

	public void ReceiveActiveBuff(PlayerBuffs playerBuff)
	{
		activeBuffs.Add(playerBuff);
	}

    public void ReceivePassiveBuff(PlayerBuffs playerBuff)
    {
        passiveBuffs.Add(playerBuff);
    }

	public void ResetBuffCounters()
	{
		appliedBuffs.ForEach(pBuff => pBuff.Reset());
	}
}