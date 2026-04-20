using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using static CardSpace;
using static TurnAction;

public class CardDisplay : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{

    public Card card;
    public bool HasBeenPlayed;
    public int HandIndex;

    /* Card values calculated after all modifiers and other independent values */
    public int hp;
    private bool ProtectedByDefender = false;

    /* Some cards allow to redirect attacks towards them. This variable defines who's doing it for this card. This works differently from defenders in the sense that attacks can be redirected regardless of the position, and affected cards can still be targetted.*/
    public CardDisplay attackSponge = null;
    public List<BuffAction> appliedBuffs = new List<BuffAction>();

    /* Get functions. They apply buffs to properties and make the calculation before hand. These values cannot be changed by code and should only be manipulated by buffs or by modifying the card itself. */
    public int maxHP
    {
        get
        {
            int bonus = 0;
            foreach (BuffAction buff in appliedBuffs)
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
            foreach (BuffAction buff in appliedBuffs)
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
    public int attack
    {
        get
        {
            int bonus = 0;
            foreach (BuffAction buff in appliedBuffs)
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
            foreach (BuffAction buff in appliedBuffs)
            {
                if (buff.Attribute == Attributes.ArmorPierce)
                {
                    bonus += buff.amount;
                }
            }
            if (bonus < 0)
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
            foreach (BuffAction buff in appliedBuffs)
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


    void OnEnable()
    {
        EventManager.BoardUpdate += UpdateBuffStatus;
        EventManager.TurnActionChange += UpdateClickabilityStatus;
    }

    void OnDisable()
    {
        EventManager.BoardUpdate -= UpdateBuffStatus;
        EventManager.TurnActionChange -= UpdateClickabilityStatus;
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
            HasBeenPlayed = false;
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

    void Start()
    {
        GM = FindObjectOfType<GameManager>();
        canvasGroup = GetComponent<CanvasGroup>();
        MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
        SlotGroup = GameObject.Find("CardSlots").GetComponent<Transform>();
        // BuffListText = GameObject.Find("BuffListUIText").GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        outline = GetComponent<Outline>();
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        UndoButtonObject.SetActive(false);
        OriginParent = transform.parent;
        OriginPosition = rectTransform.anchoredPosition;

        NameText.text = card.Name;
        ArtworkImage.sprite = card.Artwork;
        hp = card.MaxHP;
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
        if (mySpace != null && mySpace.Owner.Role != PlayerRole.Host)
        {
            return;
        }
        // if(mySpace != null){
        //     mySpace.PlayingCard = null;
        //     mySpace.CardObject = null;
        // }
        if (HasBeenPlayed)
        {
            return;
        }
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.parent.parent.parent);
        // Debug.Log("OnBeginDrag");
    }

    private CardSpace LastHoveredSpace;
    public void OnDrag(PointerEventData eventData)
    {

        if ((mySpace != null && mySpace.Owner.Role != PlayerRole.Host) || HasBeenPlayed)
        {
            return;
        }

        if (eventData.hovered.Count > 0)
        {
            if (eventData.hovered[0].GetComponent<CardSpace>())
            {
                CardSpace space = eventData.hovered[0].GetComponent<CardSpace>();
                if (space.CanPlaceCard(card))
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Overlay.enabled) { return; }
        if (GM.turnStatus == TurnStatus.SelectingTargets)
        {
            GM.SelectCardAsTargetOfAction(this);
        }
        EventManager.OnClickCard(this);
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
            return;
        }
        GM.DisplayDamage(dmg, this);
        UpdateCardUI();
    }

    public void ReceiveBuff(BuffAction newBuff, CardDisplay source = null)
    {
        BuffAction buff = new BuffAction(newBuff)
        {
            source = source,
            receiver = this
        };
        switch (buff.Attribute)
        {
            case Attributes.Health: // Health buffs are actually healing effects
                hp += buff.amount;
                if (hp > maxHP) { hp = maxHP; }
            break;
            default:
                appliedBuffs.Add(buff);
            break;
        }
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
    public int GetDamageAgainstTarget(CardDisplay target)
    {
        return GM.CalculateDamage(this, target);
    }

    public void UpdateBuffStatus()
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
        if(appliedBuffs.Count > 0)
        {
            for (int i = appliedBuffs.Count-1; i >= 0; i--)
            {
                if(appliedBuffs[i].source == null)
                {
                    appliedBuffs.RemoveAt(i);
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

        // UpdateBuffListUI();
    }

    // public void UpdateBuffListUI()
    // {
    //     string buffText = "";
    //     string numberSign = "";

    //     for (int i = 0; i < appliedBuffs.Count; i++)
    //     {
    //         if(appliedBuffs[i].amount > 0){ numberSign = "+"; }
    //         buffText += CardTranslator.TextFormat(numberSign+appliedBuffs[i].amount,null,appliedBuffs[i].amountCanBeAugmented)+" "+CardTranslator.TextFormat(CardTranslator.BuffAttributeDescription(appliedBuffs[i].Attribute),appliedBuffs[i].Attribute);
    //         switch (appliedBuffs[i].target)
    //         {
    //             default: buffText += " "+CardTranslator.TargetTypeDescription(appliedBuffs[i].target)+" from <b>"+appliedBuffs[i].source.card.Name+"</b>"; break;
    //         }
    //         buffText += "\n";
    //     }
    //     BuffListText.text = buffText;
    // }

    public void UpdateClickabilityStatus(TurnAction currentTurn)
    {
        bool clickable = true;
        // if(!HasBeenPlayed && mySpace.Owner != GM.PlayerAtPlay){ clickable = false; }

        if (currentTurn.CardInAction != null && GM.turnStatus == TurnStatus.SelectingTargets)
        {
            if (mySpace == null || mySpace?.Owner.Role == GM.PlayerAtPlay.Role) { clickable = false; }
            if (ProtectedByDefender) { clickable = false; }
            if (!CanBeTargetOfAction(currentTurn)) { clickable = false; }
        }

        if (clickable)
        {
            Overlay.enabled = false;
        }
        else
        {
            Overlay.enabled = true;
        }
    }

    public bool CanBeTargetOfAction(TurnAction turnAction)
    {
        bool itCan = true;
        int i = turnAction.nextNullIndex;
        if (turnAction.movementType == TurnMovementType.PerformAction)
        {
            switch (turnAction.actionObject.action.actionType)
            {
                case ActionTypes.Attack:
                    if (turnAction.actionObject.action.attacks[i].requirements.Count > 0) // Check attack requirements
                    {
                        itCan = false;
                        foreach (Requirements requirement in turnAction.actionObject.action.attacks[i].requirements)
                        {
                            foreach (UnitSubtype subtype in card.Subtypes)
                            {
                                if (requirement.subtypeRequirement.Contains(subtype))
                                {
                                    itCan = true;
                                }
                            }
                            foreach (Faction faction in card.Origin)
                            {
                                if (requirement.factionRequirement.Contains(faction))
                                {
                                    itCan = true;
                                }
                            }
                        }
                    }

                    if (itCan)
                    {
                        bool targetIsDefended = false;
                        bool targetIsCovered = false;
                        if (mySpace != null)
                        {
                            foreach (CardSpace defenderSpace in mySpace.Defenders)
                            {
                                if (defenderSpace.PlayingCard != null)
                                {
                                    targetIsCovered = true;
                                    if (defenderSpace.PlayingCard.card.Subtypes.Contains(UnitSubtype.Defender))
                                    {
                                        targetIsDefended = true;
                                        Debug.Log(card.Name + " is covered AND defended.");
                                    }
                                    else
                                    {
                                        Debug.Log(card.Name + " is covered.");
                                    }
                                }
                            }
                            switch (turnAction.actionObject.action.attacks[i].damageType)
                            {
                                case DamageTypes.Melee: if (targetIsCovered) { itCan = false; } break;
                                case DamageTypes.Ranged: if (targetIsDefended) { itCan = false; } break;
                                case DamageTypes.Energy: if (targetIsCovered) { itCan = false; } break;
                                case DamageTypes.MeleeOrRanged: if (targetIsDefended) { itCan = false; } break;
                                case DamageTypes.RangedOrEnergy: if (targetIsDefended) { itCan = false; } break;
                                case DamageTypes.MeleeOrEnergy: if (targetIsCovered) { itCan = false; } break;
                                case DamageTypes.MeleeOrRangedOrEnergy: if (targetIsDefended) { itCan = false; } break;
                            }
                        }
                    }


                    break;
                case ActionTypes.Buff:

                break;
            }
        }

        return itCan;
    }

    public void OnDrawAnimationEnd()
    {
        // transform.SetParent(GM.PlayerAtPlay.Hand[HandIndex].transform);
    }

}