using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CardDisplay;
using static Card;
using System.Threading.Tasks;

public class DetailedInfo : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI CostText;
    public Image ArtworkImage;
    public TextMeshProUGUI HPText;
    public TextMeshProUGUI ArmorText;
    public TextMeshProUGUI AttackText;
    public TextMeshProUGUI SubtypesText;
    public TextMeshProUGUI SkillInfoText;
    public TextMeshProUGUI BuffInfoText;
    public GameObject AbilityContainer;
    public GameObject ActionBoxPrefab;
    public List<GameObject> ActionBoxes = new List<GameObject>();
    private GameManager GM;

    void Awake()
    {
        GM = FindObjectOfType<GameManager>();
    }

    void OnEnable(){
        EventManager.ClickCard += DisplayDetailedCardInfo;
    }

    void OnDisable(){
        EventManager.ClickCard -= DisplayDetailedCardInfo;
    }

    public async void DisplayDetailedCardInfo(CardDisplay cardDisplay){
        Card card = cardDisplay.card;
        NameText.text = card.Name;
        CostText.text = card.Cost.ToString();
        ArtworkImage.sprite = card.Artwork;
        HPText.text = card.MaxHP.ToString();
        ArmorText.text = $"{card.Armor[0].ToString()}/{card.Armor[1].ToString()}/{card.Armor[2].ToString()}";
        AttackText.text = card.Attack.ToString();
        SubtypesText.text = "<b>"+card.Origin[0]+"</b> - "+SubtypesAsText(card.Subtypes);
        SkillInfoText.text = PrettifiedSkillText(cardDisplay);
        CardActionMenu actionMenu = new CardActionMenu(cardDisplay);
        foreach (var actionBox in ActionBoxes)
        {
            Destroy(actionBox);
        }
        ActionBoxes.Clear();

        int index = 0;
        await Task.Delay(1);
        foreach (CardActionObject action in actionMenu.actions)
        {
            ActionBoxes.Add(Instantiate(ActionBoxPrefab));
            ActionBoxes[index].transform.SetParent(AbilityContainer.transform, false);
            ActionBoxes[index].GetComponent<ActionBoxScript>().action = action;

            Image actionBoxImage = ActionBoxes[index].GetComponent<Image>();
            switch (action.diceAverageValue)
            {
                case 1: actionBoxImage.color = new Color( 0.93f, 0.93f, 0.93f, 0.65f); break;
                case 2: actionBoxImage.color = new Color( 0.86f, 0.95f, 0.86f, 0.65f); break;
                case 3: actionBoxImage.color = new Color( 0.86f, 0.89f, 0.95f, 0.65f); break;
                case 4: actionBoxImage.color = new Color( 0.89f, 0.86f, 0.95f, 0.65f); break;
                case 5: actionBoxImage.color = new Color( 0.95f, 0.86f, 0.86f, 0.65f); break;
                case 6: actionBoxImage.color = new Color( 0.95f, 0.94f, 0.86f, 0.65f); break;
            }

            Transform DiceNumbers = ActionBoxes[index].transform.Find("DiceNumbers");
            Transform SkillDetail = ActionBoxes[index].transform.Find("SkillDetail");
            Transform Overlay = ActionBoxes[index].transform.Find("Overlay");
            DiceNumbers.GetComponent<TextMeshProUGUI>().text = "";
            foreach (var diceValue in action.diceValues)
            {
                switch (diceValue)
                {
                    case 1: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "1️"; break;
                    case 2: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "2️"; break;
                    case 3: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "3️"; break;
                    case 4: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "4️"; break;
                    case 5: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "5️"; break;
                    case 6: DiceNumbers.GetComponent<TextMeshProUGUI>().text += "6️"; break;
                }
                if(action.HasMatchingDice() && cardDisplay.CanActThisTurn)
                {
                    action.canBeUsed = true;
                    Overlay.gameObject.SetActive(false);
                } else {
                    action.canBeUsed = false;
                    Overlay.gameObject.SetActive(true);
                }
            }
            SkillDetail.GetComponent<TextMeshProUGUI>().text = action.description;
            float largestHeight = 0;
            RectTransform DiceNumbersRect = DiceNumbers.GetComponent<RectTransform>();
            RectTransform SkillDetailRect = SkillDetail.GetComponent<RectTransform>();
            await Task.Delay(1);
            if(DiceNumbersRect.rect.height > SkillDetailRect.rect.height)
            {
                largestHeight = DiceNumbersRect.rect.height;
                SkillDetail.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                SkillDetailRect.sizeDelta = new Vector2(SkillDetailRect.sizeDelta.x,largestHeight);
            } else {
                largestHeight = SkillDetailRect.rect.height;
                DiceNumbers.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                DiceNumbersRect.sizeDelta = new Vector2(DiceNumbersRect.sizeDelta.x,largestHeight);
            }
            await Task.Delay(1);
            float stackedHeights = 0;
            for (int i = index-1; i >= 0; i--)
            {
                stackedHeights += ActionBoxes[i].GetComponent<RectTransform>().sizeDelta.y;
            }
            ActionBoxes[index].GetComponent<RectTransform>().anchoredPosition = new Vector2(5,-5-SkillInfoText.GetComponent<RectTransform>().rect.height-stackedHeights);
            // ActionBoxes[index].GetComponent<RectTransform>().sizeDelta = new Vector2(420,90);
            ActionBoxes[index].GetComponent<RectTransform>().sizeDelta = new Vector2(420,largestHeight);

            index ++;
        }

        BuffInfoText.text = CardTranslator.AppliedBuffDescription(cardDisplay.appliedBuffs);

    }

    public string SubtypesAsText(List<UnitSubtype> subtypes){
        string text = "";
        foreach (var subtype in subtypes)
        {
            switch (subtype){
                case UnitSubtype.Defender:
                    text += "⚓ Defender ";
                break;
                case UnitSubtype.Mercenary:
                    text += "🏴 Mercenary ";
                break;
                case UnitSubtype.Pacifist:
                    text += "🕊 Pacifist ";
                break;
                case UnitSubtype.Combo:
                    text += "⛓ Combo ";
                break;
                case UnitSubtype.Executioner:
                    text += "💀 Executioner ";
                break;
                case UnitSubtype.Noble:
                    text += "👑 Noble ";
                break;
                case UnitSubtype.Solitary:
                    text += "🕯 Solitary ";
                break;
                case UnitSubtype.Inheritor:
                    text += "🧬 Inheritor ";
                break;
            }   
        }
        return text;
    }

    public string PrettifiedSkillText(CardDisplay cardDisplay){
        string SkillText = "";
        List<string> actionList = new List<string>();
        
        List<CardPassiveSkillObject> passiveSkills = new List<CardPassiveSkillObject>();
        foreach (PassiveSkill passive in cardDisplay.card.Passives)
        {
            passiveSkills.Add(new CardPassiveSkillObject(passive, cardDisplay));
        }

        foreach (CardPassiveSkillObject passiveSkillObject in passiveSkills)
        {
            SkillText += passiveSkillObject.description+"\n";
        }
        SkillText += "\n\n";

        return SkillText;
    }

}