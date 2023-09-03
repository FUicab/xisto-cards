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
        ArmorText.text = card.Armor.ToString();
        AttackText.text = card.Attack.ToString();
        SubtypesText.text = SubtypesAsText(card.Subtypes);
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
}
