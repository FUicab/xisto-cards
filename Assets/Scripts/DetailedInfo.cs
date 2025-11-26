using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static CardDisplay;
using static Card;

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
    
    void OnEnable(){
        EventManager.ClickCard += DisplayDetailedCardInfo;
    }

    void OnDisable(){
        EventManager.ClickCard -= DisplayDetailedCardInfo;
    }

    public void DisplayDetailedCardInfo(CardDisplay display){
        Card card = display.card;
        NameText.text = card.Name;
        CostText.text = card.Cost.ToString();
        ArtworkImage.sprite = card.Artwork;
        HPText.text = card.MaxHP.ToString();
        ArmorText.text = $"{card.Armor[0].ToString()}/{card.Armor[1].ToString()}/{card.Armor[2].ToString()}";
        AttackText.text = card.Attack.ToString();
        SubtypesText.text = card.Origin+" - "+SubtypesAsText(card.Subtypes);
        SkillInfoText.text = PrettifiedSkillText(display);
    }

    public string SubtypesAsText(List<UnitSubtype> subtypes){
        string SubtypeSymbols = "";
        foreach (var subtype in subtypes)
        {
            switch (subtype){
                case UnitSubtype.Defender:
                    SubtypeSymbols += "Df ";
                break;
                case UnitSubtype.Dual:
                    SubtypeSymbols += "Du ";
                break;
                case UnitSubtype.Mercenary:
                    SubtypeSymbols += "Mc ";
                break;
                case UnitSubtype.Assistant:
                    SubtypeSymbols += "At ";
                break;
                case UnitSubtype.Pacifist:
                    SubtypeSymbols += "Pc ";
                break;
                case UnitSubtype.Combo:
                    SubtypeSymbols += "Cb ";
                break;
                case UnitSubtype.Executioner:
                    SubtypeSymbols += "Ex ";
                break;
                case UnitSubtype.Noble:
                    SubtypeSymbols += "Nb ";
                break;
                case UnitSubtype.Solitary:
                    SubtypeSymbols += "Sl ";
                break;
                case UnitSubtype.Inheritor:
                    SubtypeSymbols += "In ";
                break;
            }   
        }
        return SubtypeSymbols;
    }

    public string PrettifiedSkillText(CardDisplay cardDisplay){
        string SkillText = "";
        List<string> actionList = new List<string>();
        CardActionMenu actionMenu = new CardActionMenu(cardDisplay);
        int index = 0;
        foreach (var action in actionMenu.actions)
        {
            actionList.Add("");
            foreach (var diceValue in action.diceValues)
            {
                actionList[index] += $"[{diceValue}]";
            }
            actionList[index] += " "+action.description;
            index += 1;
        }

        SkillText += String.Join('\n',actionList);

        return SkillText;
    }

}