using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using static CardSpace;
using static TurnAction;
using static UnityEngine.GraphicsBuffer;

public class CardDisplay : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{

	public Card card;
	public float power = 0f;
	public bool HasBeenPlayed {
		get { return mySpace != null; }
	}
	public bool CanBeDragged
	{
		get { return !HasBeenPlayed && Owner.Role == PlayerRole.Host; }
	}
	public int HandIndex;
	public bool isPotentialTargetForPerformingAction = false;
	public PlayerProfile Owner;

	/* Card values calculated after all modifiers and other independent values */
	public int hp;
	public bool ProtectedByDefender = false;

	/* Some cards allow to redirect attacks towards them. This variable defines who's doing it for this card. This works differently from defenders in the sense that attacks can be redirected regardless of the position, and affected cards can still be targetted.*/
	public CardDisplay attackSponge = null;
	public List<BuffAction> activeBuffs = new List<BuffAction>();
	public List<BuffAction> passiveBuffs = new List<BuffAction>();
	public List<BuffAction> appliedBuffs{
		get {
			List<BuffAction> buffs = new List<BuffAction>();
			buffs.AddRange(passiveBuffs);
			buffs.AddRange(activeBuffs);
			return buffs;
		}
	}
	public List<CardAction> cardActions;
	public List<PassiveSkill> cardPassives;

	/* Get functions. They apply buffs to properties and make the calculation before hand. These values cannot be changed by code and should only be manipulated by buffs or by modifying the card itself. */
	public List<UnitSubtype> acquiredSubtypes {
		get
		{
			List<UnitSubtype> subtypes = new();
            foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit && x.specialEffect == BuffSpecialEffects.GrantSubtypes))
            {
				subtypes.AddRange(buff.grantedSubtypes);
			}
			return subtypes;
        }
	}
	public int maxHP
	{
		get
		{
			int bonus = 0;
			foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
			{
				if (buff.Attribute == Attributes.MaxHealth)
				{
					bonus += buff.amount;
				}
			}
			if (card.MaxHP + bonus < 1)
			{
				return 1;
			}
			else
			{
				return card.MaxHP + bonus;
			}
		}
	}
	public int[] armor
	{
		get
		{
			int[] bonus = { 0, 0, 0 };
			foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
			{
				switch (buff.Attribute)
				{
					case Attributes.Defense:
						bonus[0] += buff.amount;
						bonus[1] += buff.amount;
						bonus[2] += buff.amount;
						break;
					case Attributes.DefenseMelee:
						bonus[0] += buff.amount;
						break;
					case Attributes.DefenseRanged:
						bonus[1] += buff.amount;
						break;
					case Attributes.DefenseEnergy:
						bonus[2] += buff.amount;
						break;
				}
			}
			int[] finalValues = { 0, 0, 0 };
			if (card.Armor[0] + bonus[0] > 0) { finalValues[0] = card.Armor[0] + bonus[0]; }
			if (card.Armor[1] + bonus[1] > 0) { finalValues[1] = card.Armor[1] + bonus[1]; }
			if (card.Armor[2] + bonus[2] > 0) { finalValues[2] = card.Armor[2] + bonus[2]; }

			return finalValues;
		}
	}
    public int damageReduction
    {
        get
        {
            int bonus = 0;
            foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
            {
                if (buff.Attribute == Attributes.DamageReductionAfterArmor || buff.Attribute == Attributes.DamageReductionBeforeArmor)
                {
                    bonus += buff.amount;
                }
            }
            if (bonus <= 0)
            {
                return 0;
            }
            else
            {
                return bonus;
            }
        }
    }
    public int attack
	{
		get
		{
			int bonus = 0;
			foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
			{
				if (buff.Attribute == Attributes.Attack)
				{
					bonus += buff.amount;
				}
			}
			if (card.Attack + bonus < 0)
			{
				return 0;
			}
			else
			{
				return card.Attack + bonus;
			}
		}
	}
	public int armorPierce
	{
		get
		{
			int bonus = 0;
			foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
			{
				if (buff.Attribute == Attributes.ArmorPierce)
				{
					bonus += buff.amount;
				}
			}
			if (bonus <= 0)
			{
				return 0;
			}
			else
			{
				return bonus;
			}
		}
	}
	public int cost
	{
		get
		{
			int bonus = 0;
			foreach (BuffAction buff in appliedBuffs.Where(x => !x.activatesOnHit))
			{
				if (buff.Attribute == Attributes.Cost)
				{
					bonus += buff.amount;
				}
			}
			if (card.Cost + bonus < 0)
			{
				return 0;
			}
			else
			{
				return card.Cost + bonus;
			}
		}
	}

	public TextMeshProUGUI NameText;
	public TextMeshProUGUI CostText;
	public Image ArtworkImage;
	public TextMeshProUGUI HPText;
	public TextMeshProUGUI ArmorText;
	public TextMeshProUGUI AttackText;
	public Image ArmorPierceImage;
    public Image DamageReductionImage;
    public Image PotentialDamageImage;
    public Image DefenderBarrier;
	// private TextMeshProUGUI BuffListText;

	private GameManager GM;
	public RectTransform rectTransform;
	[SerializeField] private Canvas MainUI;
	private CanvasGroup canvasGroup;
	private Outline outline;
	public Transform SlotGroup;
	public Vector3 OriginPosition;
	public Transform OriginParent;
	public CardSpace mySpace;
	public RawImage Overlay;
	private LineRenderer line;

	public GameObject UndoButtonObject;
	public TurnAction PurchaseAction;
	public bool HasActedThisRound
	{
		get { return GM.RoundActions.Exists(x => x.CardInAction == this && x.movementType == TurnMovementType.PerformAction ); }
	}
	public bool HasAttackedThisRound
	{
		get { return GM.RoundActions.Exists(x => x.CardInAction == this && x.movementType == TurnMovementType.PerformAction && x.actionObject.action.actionType == ActionTypes.Attack); }
	}
	public bool CanActThisTurn
	{
		get { return Owner.isMyTurnToPlay && HasBeenPlayed && (!HasActedThisRound || card.Subtypes.Concat(acquiredSubtypes).Contains(UnitSubtype.Combo) ); }
	}

	void OnEnable()
	{
		EventManager.BoardUpdate += UpdateActiveBuffStatus;
		EventManager.BoardUpdate += UpdatePassiveBuffStatus;
		EventManager.TurnActionChange += UpdateClickabilityStatus;
		EventManager.RoundEnd += RoundEndCleanUp;
	}

	void OnDisable()
	{
		EventManager.BoardUpdate -= UpdateActiveBuffStatus;
		EventManager.BoardUpdate -= UpdatePassiveBuffStatus;
		EventManager.TurnActionChange -= UpdateClickabilityStatus;
		EventManager.RoundEnd -= RoundEndCleanUp;
	}

	public void SetPurchaseAction(TurnAction Action)
	{
		PurchaseAction = Action;
		UndoButtonObject.SetActive(true);
	}
	public void UndoPurchaseAction()
	{
		if (PurchaseAction != null)
		{
			GM.RefundCard(PurchaseAction);
			mySpace.UndoPlaceCard();
			//HasBeenPlayed = false;
			HandIndex = PurchaseAction.HandIndexOrigin;
			OriginParent = GM.Hand[HandIndex];
			transform.SetParent(OriginParent);
			rectTransform.anchoredPosition = OriginPosition;
			rectTransform.rotation = OriginParent.rotation;
			PurchaseAction = null;
			UndoButtonObject.SetActive(false);
		}
	}
	public void DisableUndoPurchase()
	{
		PurchaseAction = null;
		UndoButtonObject.SetActive(false);
	}

	void Awake()
	{
		GM = FindObjectOfType<GameManager>();
		canvasGroup = GetComponent<CanvasGroup>();
		MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
		SlotGroup = GameObject.Find("CardSlots").GetComponent<Transform>();
		outline = GetComponent<Outline>();
		line = GetComponent<LineRenderer>();
	}

	void Start()
	{
		line.enabled = false;
		UndoButtonObject.SetActive(false);
		OriginParent = transform.parent;
		OriginPosition = rectTransform.anchoredPosition;
		power = card.powerRating.total;
		NameText.text = $"{power:0.00} · {card.Name}";
		ArtworkImage.sprite = card.Artwork;
		hp = card.MaxHP;
		PotentialDamageImage.gameObject.SetActive(false);

		for (int i = 0; i < card.CardActions.Count; i++)
		{
			cardActions.Add(new CardAction(card.CardActions[i]));
			List<AttackAction> newAttacks = new List<AttackAction>();
			List<BuffAction> newBuffs = new List<BuffAction>();
			foreach (AttackAction attack in cardActions[i].attacks)
			{
				newAttacks.Add(new AttackAction(attack) { source = this });
			}
			cardActions[i].attacks = newAttacks;
			foreach (BuffAction buff in cardActions[i].buffs)
			{
				newBuffs.Add(new BuffAction(buff) { source = this });
            }
			cardActions[i].buffs = newBuffs;
		}
		for (int i = 0; i < card.Passives.Count; i++)
		{
			cardPassives.Add(new PassiveSkill(card.Passives[i]) { source = this });
			List<BuffAction> newBuffs = new List<BuffAction>();
			foreach (BuffAction buff in card.Passives[i].buffs)
			{
				newBuffs.Add(new BuffAction(buff) { source = this, originPassive = cardPassives[i] });
			}
			cardPassives[i].buffs = newBuffs;
		}
		UpdateCardUI();

		rectTransform.anchoredPosition = GM.DeckUI.GetComponent<RectTransform>().position;
		rectTransform.LeanMove(OriginPosition, 0.5f).setEaseOutQuart().setOnComplete(OnDrawAnimationEnd);
	}

	void MoveToDiscardPile()
	{
		// GM.DiscardPile.Add(card);
		gameObject.SetActive(false);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!CanBeDragged)
		{
			return;
		}
		// if(mySpace != null){
		//     mySpace.PlayingCard = null;
		//     mySpace.CardObject = null;
		// }
		canvasGroup.alpha = 0.5f;
		canvasGroup.blocksRaycasts = false;
		transform.SetParent(transform.parent.parent.parent);
		// Debug.Log("OnBeginDrag");
	}

	private CardSpace LastHoveredSpace;
	public void OnDrag(PointerEventData eventData)
	{

		if (!CanBeDragged)
		{
			return;
		}

		if (eventData.hovered.Count > 0)
		{
			if (eventData.hovered[0].GetComponent<CardSpace>())
			{
				CardSpace space = eventData.hovered[0].GetComponent<CardSpace>();
				if (space.CanPlaceCard(this))
				{
					space.outline.enabled = true;
					LastHoveredSpace = space;
				}
			}
		}
		else
		{
			if (LastHoveredSpace != null)
			{
				LastHoveredSpace.outline.enabled = false;
				LastHoveredSpace = null;
			}
		}

		rectTransform.anchoredPosition += eventData.delta / MainUI.scaleFactor / SlotGroup.localScale;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		canvasGroup.alpha = 1f;
		canvasGroup.blocksRaycasts = true;
		transform.SetParent(OriginParent);
		rectTransform.anchoredPosition = OriginPosition;
		rectTransform.rotation = OriginParent.rotation;
		if (HasBeenPlayed && HandIndex != -1)
		{
			// GM.AvailableCardSlots[HandIndex] = true;
			HandIndex = -1;
		}
		if (LastHoveredSpace != null)
		{
			LastHoveredSpace.outline.enabled = false;
			LastHoveredSpace = null;
		}
	}

	public bool TriggerClickEvent(PointerEventData eventData = null) {
		bool success = true;
        if (Overlay.enabled) { return false; }
        if (GM.turnStatus == TurnStatus.SelectingTargets)
        {
            GM.SelectCardAsTargetOfAction(this);
        }
        EventManager.OnClickCard(this);
		return success;
    }

	public void OnPointerDown(PointerEventData eventData)
	{
		TriggerClickEvent(eventData);
	}

	/* --- Outline and UI functions --------------------------------------------- */
	private static int OutlineAlpha = 128;
	private Color orangeOutline = new Color(255, 128, 0, OutlineAlpha);
	private Color redOutline = new Color(255, 0, 0, OutlineAlpha);
	public void SetOutline(string color = "")
	{
		bool shouldActivate = true;
		switch (color)
		{
			case "orange":
				outline.effectColor = orangeOutline; break;
			case "red":
				outline.effectColor = redOutline; break;
			default:
				shouldActivate = false; break;
		}
		outline.enabled = shouldActivate;
	}
	public void ClearAllDisplay()
	{
		SetOutline();
		SetLine();
	}

	public void UpdateCardUI()
	{
		string costText = "<color=#";
		string hpText = "<color=#";
		string armorText = "<color=#";
		string attackText = "<color=#";
		string regularColor = "000";
		string betterColor = "0a3";
		string worseColor = "a03";

		if (cost < card.Cost) { costText += betterColor; } else if (cost > card.Cost) { costText += worseColor; } else { costText += regularColor; }
		costText += ">" + cost + "</color>";

		if (hp > maxHP) { hpText += betterColor; } else if (hp < maxHP) { hpText += worseColor; } else { hpText += regularColor; }
		hpText += ">" + hp + "</color>";

		if (attack > card.Attack) { attackText += betterColor; } else if (attack < card.Attack) { attackText += worseColor; } else { attackText += regularColor; }
		attackText += ">" + attack + "</color>";

		if (armor[0] > card.Armor[0]) { armorText += betterColor; } else if (armor[0] < card.Armor[0]) { armorText += worseColor; } else { armorText += regularColor; }
		armorText += ">" + armor[0] + "</color>/<color=#";
		if (armor[1] > card.Armor[1]) { armorText += betterColor; } else if (armor[1] < card.Armor[1]) { armorText += worseColor; } else { armorText += regularColor; }
		armorText += ">" + armor[1] + "</color>/<color=#";
		if (armor[2] > card.Armor[2]) { armorText += betterColor; } else if (armor[2] < card.Armor[2]) { armorText += worseColor; } else { armorText += regularColor; }
		armorText += ">" + armor[2] + "</color>";

		CostText.text = costText;
		HPText.text = hpText;
		ArmorText.text = armorText;
		AttackText.text = attackText;

        if (damageReduction <= 0)
        {
            DamageReductionImage.gameObject.SetActive(false);
        }
        else
        {
            DamageReductionImage.gameObject.SetActive(true);
            DamageReductionImage.GetComponentInChildren<TextMeshProUGUI>().text = damageReduction.ToString();
        }
        if (armorPierce <= 0)
        {
            ArmorPierceImage.gameObject.SetActive(false);
        }
        else
        {
            ArmorPierceImage.gameObject.SetActive(true);
            ArmorPierceImage.GetComponentInChildren<TextMeshProUGUI>().text = armorPierce.ToString();
        }
    }

	/* --- Combat functions --------------------------------------------- */
	public void ReceiveDamage(int dmg)
	{
		hp -= dmg;
		if (hp <= 0)
		{
			mySpace.FreeSpace();
			GM.Deck.Add(card);
			Destroy(gameObject);
			EventManager.OnTurnActionChange(GM.CurrentAction);
			RemovePassiveBuffsFromMe();
			return;
		}
		//await Task.Delay(200);
		GM.DisplayDamage(dmg, this);
		UpdateCardUI();
	}

    public void ReceiveDamageFromAttack(AttackAction attack)
    {
		attack.attackActionOutput = attack.GetAttackActionOutput(this);
		ReceiveDamage(attack.attackActionOutput.damage);
    }

    public void ReceiveActiveBuff(BuffAction newBuff)
	{
		BuffAction buff = new BuffAction(newBuff)
		{
			receiver = this
		};
		switch (buff.Attribute)
		{
			case Attributes.Health: // Health buffs are actually just healing effects
				hp += buff.amount;
				if (hp > maxHP) { hp = maxHP; }
			break;
			default:
				activeBuffs.Add(buff);
			break;
		}
		UpdateCardUI();
	}

	public void ReceivePassiveBuff(BuffAction newBuff)
	{
		BuffAction buff = new BuffAction(newBuff)
		{
			receiver = this
		};
		passiveBuffs.Add(buff);
		UpdateCardUI();
	}

	public void SetLine(List<CardDisplay> targets = null)
	{
		if (targets == null)
		{
			// Debug.Log("When drawing lines the targets were found to be null!");
			line.positionCount = 0;
			line.SetPositions(new Vector3[] { });
			line.enabled = false; return;
		}
		else
		{
			line.enabled = true;
			List<Vector3> points = new List<Vector3>();
			foreach (CardDisplay target in targets)
			{
				if (target != null)
				{
					points.Add(transform.position);
					points.Add(target.transform.position);
				}
			}
			// Vector3[] points = {transform.position,target.transform.position};
			line.positionCount = points.Count;
			line.SetPositions(points.ToArray());
		}
	}
	public void ResetHP()
	{
		hp = card.MaxHP;
		HPText.text = hp.ToString();
	}

	public void UpdateActiveBuffStatus()
	{
		/* Check for defending status */
		bool isDefended = false;
		if (HasBeenPlayed)
		{
			foreach (var defendingSpace in mySpace.Defenders)
			{
				if (defendingSpace.PlayingCard != null && defendingSpace.PlayingCard.card.Subtypes.Contains(UnitSubtype.Defender))
				{
					isDefended = true;
				}
			}
			if (isDefended)
			{
				ProtectedByDefender = true;
				DefenderBarrier.gameObject.SetActive(true);
			}
			else
			{
				ProtectedByDefender = false;
				DefenderBarrier.gameObject.SetActive(false);
			}
		}

		/* Check for buff source status */
		if(activeBuffs.Count > 0)
		{
			for (int i = activeBuffs.Count-1; i >= 0; i--)
			{
				if(activeBuffs[i].source == null)
				{
					activeBuffs.RemoveAt(i);
				}
			}
		}

		/* Check for buff special effects */
		attackSponge = null;
		foreach (BuffAction buff in appliedBuffs)
		{
			switch (buff.specialEffect)
			{
				case BuffSpecialEffects.RedirectAttacksTowardsMe:
					attackSponge = buff.source;
				break;
			}
		}

		UpdateCardUI();
	}

	public void UpdatePassiveBuffStatus()
	{
		if (!HasBeenPlayed) { return; }
		//passiveBuffs.Clear();
		foreach (PassiveSkill passive in cardPassives)
		{
			if(passive.trigger == TriggerTypes.OnBoardChange) /* These passives activate only by having the card there. */
			{
                foreach (BuffAction buff in passive.buffs)
				{
                    if (buff.isTargetImplicit) /* Passive skills can only provide buffs to implicit targets as none should be selected, otherwise the passive buff is invalid.*/
					{
                        foreach (CardDisplay target in buff.GetImplicitTargetsOfAction())
						{
							//Debug.Log($"{passive.title} from {passive.source.card.Name} is checking for validity on {target.card.Name}: {buff.TargetMeetsRequirements(target)}");
							if (!target.passiveBuffs.Exists(x => x.originPassive.title == passive.title && x.originPassive.source == passive.source && buff.Attribute == x.Attribute && buff.amount == x.amount)) {
                                //Debug.Log($"{passive.title} from {passive.source.card.Name} was successfully applied to {target.card.Name}");
                                target.ReceivePassiveBuff(buff);
							}
                        }
						foreach (CardSpace cardSpace in mySpace.Owner.mySpaces.Where(x => x.PlayingCard != null)) /* Check if my buffs change their status related to others */
						{
							for (int i = cardSpace.PlayingCard.passiveBuffs.Count-1; i >= 0; i--)
							{
								BuffAction theirBuff = cardSpace.PlayingCard.passiveBuffs[i];
                                //Debug.Log($"{theirBuff.originPassive.source.card.Name} applied {theirBuff.originPassive.title} to {passive.source.card.Name}. Can they keep it? : {theirBuff.TargetMeetsRequirements(cardSpace.PlayingCard)}");
                                if (theirBuff.originPassive.source == this && !theirBuff.TargetMeetsRequirements(cardSpace.PlayingCard))
								{
                                    //Debug.Log($"{passive.title} from {passive.source.card.Name} can no longer be applied to {cardSpace.PlayingCard.card.Name}");
                                    cardSpace.PlayingCard.passiveBuffs.RemoveAt(i);
								}
							}
						}
					}
				}
			}
		}
	}

	public void RemovePassiveBuffsFromMe()
	{
        foreach (CardSpace cardSpace in mySpace.Owner.mySpaces.Where(x => x.PlayingCard != null && x.PlayingCard.passiveBuffs.Count > 0))
        {
			//Debug.Log($"When removing buffs we found that {cardSpace.PlayingCard.card.Name} has buffs whose origin is a passive skill.");
            for (int i = cardSpace.PlayingCard.passiveBuffs.Count-1; i >= 0; i--)
            {
				BuffAction buff = cardSpace.PlayingCard.passiveBuffs[i];
                if (buff.originPassive.source == this)
				{
                    cardSpace.PlayingCard.passiveBuffs.RemoveAt(i);
                }
            }
        }
    }

	public void UpdateClickabilityStatus(TurnAction currentTurn)
	{
		bool clickable = true;

		if (currentTurn.CardInAction != null && GM.turnStatus == TurnStatus.SelectingTargets)
		{
			if (mySpace == null || mySpace?.Owner.Role == GM.PlayerAtPlay.Role) { clickable = false; }
			if (ProtectedByDefender) { clickable = false; }
			if (!isPotentialTargetForPerformingAction) {  clickable = false; }
		}

		if (clickable)
		{
			SetPotentialDamageDisplay(currentTurn);
			Overlay.enabled = false;
		}
		else
		{
            PotentialDamageImage.gameObject.SetActive(false);
            Overlay.enabled = true;
		}
	}

	public void SetPotentialDamageDisplay(TurnAction turnAction)
	{
		if (turnAction.movementType == TurnMovementType.PerformAction && turnAction.actionObject?.action?.actionType == ActionTypes.Attack && GM.turnStatus == TurnStatus.SelectingTargets) {
			PotentialDamageImage.gameObject.SetActive(true);
		} else
		{
            PotentialDamageImage.gameObject.SetActive(false);
            return;
		}
        int i = turnAction.nextNullIndex;
		TextMeshProUGUI potentialDamage = PotentialDamageImage.GetComponentInChildren<TextMeshProUGUI>();
        AttackActionOutput attackOutput = CardActionTools.GetAttackActionOutput(this, turnAction.actionObject.action.attacks[i]);
		if (attackOutput.resultsInDeath) {
            potentialDamage.text = $"☠️";
        } else
		{
			potentialDamage.text = $"{attackOutput.damageTypeIcon} {attackOutput.damage}";
		}
	}

	public void OnDrawAnimationEnd()
	{
		// transform.SetParent(GM.PlayerAtPlay.Hand[HandIndex].transform);
	}

	public void RoundEndCleanUp()
	{
		activeBuffs.Clear();
		UpdateActiveBuffStatus();
	}

}