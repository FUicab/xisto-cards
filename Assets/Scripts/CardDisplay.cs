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
    public int armor;
    public int attack;
    public int armorPierce;
    public int cost;

    public TextMeshProUGUI NameText;
    public TextMeshProUGUI CostText;
    public Image ArtworkImage;
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI ArmorText;
    public TextMeshProUGUI AttackText;

    private GameManager GM;
    public RectTransform rectTransform;
    [SerializeField] private Canvas MainUI;
    private CanvasGroup canvasGroup;
    private Outline outline;
    public Transform SlotGroup;
    public Vector3 OriginPosition;
    public Transform OriginParent;
    public CardSpace mySpace;
    private LineRenderer line;
    // [SerializeField] private Canvas LocalCanvas;

    public GameObject UndoButtonObject;
    public TurnAction PurchaseAction;
    public void SetPurchaseAction(TurnAction Action){
        PurchaseAction = Action;
        UndoButtonObject.SetActive(true);
    }
    public void UndoPurchaseAction(){
        if(PurchaseAction != null){
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
    public void DisableUndoPurchase(){
        PurchaseAction = null;
        UndoButtonObject.SetActive(false);
    }

    void Start()
    {
        GM = FindObjectOfType<GameManager>();
        canvasGroup = GetComponent<CanvasGroup>();
        MainUI = GameObject.Find("MainUI").GetComponent<Canvas>();
        SlotGroup = GameObject.Find("CardSlots").GetComponent<Transform>();
        // LocalCanvas = GetComponent<Canvas>();
        rectTransform = GetComponent<RectTransform>();
        outline = GetComponent<Outline>();
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        UndoButtonObject.SetActive(false);
        OriginParent = transform.parent;
        OriginPosition = rectTransform.anchoredPosition;
        // LocalCanvas.worldCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        NameText.text = card.Name;
        CostText.text = card.Cost.ToString();
        ArtworkImage.sprite = card.Artwork;
        hp = card.MaxHP;
        armor = card.Armor;
        attack = card.Attack;
        cost = card.Cost;
        HPText.text = hp.ToString();
        ArmorText.text = armor.ToString();
        AttackText.text = attack.ToString();
    }

    void MoveToDiscardPile(){
        GM.DiscardPile.Add(card);
        gameObject.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData){
        if(mySpace != null && mySpace.Owner.Role != PlayerRole.Host){
            return;
        }
        // if(mySpace != null){
        //     mySpace.PlayingCard = null;
        //     mySpace.CardObject = null;
        // }
        if(HasBeenPlayed){
            return;
        }
        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.parent.parent.parent);
        // Debug.Log("OnBeginDrag");
    }

    private CardSpace LastHoveredSpace;
    public void OnDrag(PointerEventData eventData){

        if((mySpace!=null && mySpace.Owner.Role != PlayerRole.Host) || HasBeenPlayed){
            return;
        }

        if(eventData.hovered.Count > 0){
            if(eventData.hovered[0].GetComponent<CardSpace>()){
                CardSpace space = eventData.hovered[0].GetComponent<CardSpace>();
                if(space.CanPlaceCard(card)){
                    space.outline.enabled = true;
                    LastHoveredSpace = space;
                }
            }
        } else {
            if(LastHoveredSpace != null){
                LastHoveredSpace.outline.enabled = false;
                LastHoveredSpace = null;
            }
        }

        rectTransform.anchoredPosition += eventData.delta / MainUI.scaleFactor / SlotGroup.localScale;
    }

    public void OnEndDrag(PointerEventData eventData){
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(OriginParent);
        rectTransform.anchoredPosition = OriginPosition;
        rectTransform.rotation = OriginParent.rotation;
        if(HasBeenPlayed && HandIndex!=-1){
            // GM.AvailableCardSlots[HandIndex] = true;
            HandIndex = -1;
        }
        if(LastHoveredSpace != null){
            LastHoveredSpace.outline.enabled = false;
            LastHoveredSpace = null;
        }
    }

    public void OnPointerDown(PointerEventData eventData){
        if(HasBeenPlayed){
            // mySpace.Occupied = false;
            // GM.Deck.Add(card);
            // Destroy(gameObject);
            // if(GM.CurrentAction.Attacker == null || GM.CurrentAction.Attacker != this){
            //     GM.SetAttacker(this);
            // } else if(GM.CurrentAction.Attacker == this){
            //     GM.SetAttacker(null);
            // }
            if(mySpace.Owner.Role == PlayerRole.Host){
                GM.SetAttacker(this);
            }

            if(mySpace.Owner.Role == PlayerRole.Opponent){
                GM.SetAttackTarget(this);
            }

        }
        EventManager.OnClickCard(this);
    }

    /* --- Outline functions --------------------------------------------- */
    private static int OutlineAlpha = 128;
    private Color orangeOutline = new Color(255,128,0,OutlineAlpha);
    private Color redOutline = new Color(255,0,0,OutlineAlpha);
    public void SetOutline(string color = ""){
        bool shouldActivate = true;
        switch (color){
            case "orange":
            outline.effectColor = orangeOutline; break;
            case "red":
            outline.effectColor = redOutline; break;
            default:
            shouldActivate = false; break;
        }
        outline.enabled = shouldActivate;
    }
    public void ClearAllDisplay(){
        SetOutline();
        SetLine();
    }

    /* --- Combat functions --------------------------------------------- */
    public void ReceiveDamage(int dmg){
        hp -= dmg;
        if(hp<=0){
            mySpace.FreeSpace();
            GM.Deck.Add(card);
            Destroy(gameObject);
            return;
        }
        GM.DisplayDamage(dmg, this);
        HPText.text = hp.ToString();
        ArmorText.text = armor.ToString();
        AttackText.text = attack.ToString();
    }
    public void SetLine(CardDisplay target = null){
        if(target == null){ line.enabled = false; return; } else {
            Vector3[] points = {transform.position,target.transform.position};
            line.SetPositions(points);
            line.enabled = true;
        }
    }
    public void ResetHP(){
        hp = card.MaxHP;
        HPText.text = hp.ToString();
    }
    public int GetDamageAgainstTarget(CardDisplay target){
        return GM.GetDamage(this, target);
    }

}