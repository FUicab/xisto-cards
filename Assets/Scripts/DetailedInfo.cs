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
        SubtypesText.text = TargetFactionDescription(card.Origin)+" - "+SubtypesAsText(card.Subtypes);
        SkillInfoText.text = PrettifiedSkillText(card);
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

    public string PrettifiedSkillText(Card card){
        string SkillText = "";
        List<string> actionList = new List<string>();
        List<string> actionTexts = new List<string>();
        int index = 0;
        int lastActionListIndex = 0;
        foreach (var action in card.CardActions)
        {
            if(action.actionType != ActionTypes.RepeatFromAbove)
            {
                actionTexts.Add(TranslateActionToText(action));
                if(index != 0)
                {
                    lastActionListIndex += 1;
                }
            }
            if(lastActionListIndex == actionList.Count)
            {
                actionList.Add("");
            }

            switch (index)
            {
                case 0: 
                    actionList[lastActionListIndex] += "[1]";
                break;
                case 1: 
                    actionList[lastActionListIndex] += "[2]";
                break;
                case 2: 
                    actionList[lastActionListIndex] += "[3]";
                break;
                case 3: 
                    actionList[lastActionListIndex] += "[4]";
                break;
                case 4: 
                    actionList[lastActionListIndex] += "[5]";
                break;
                case 5: 
                    actionList[lastActionListIndex] += "[6]";
                break;
            }

            index += 1;
        }
        lastActionListIndex = 0;
        foreach (var actionText in actionTexts)
        {
            actionList[lastActionListIndex] += " "+actionText;
            lastActionListIndex += 1;
        }

        SkillText += String.Join('\n',actionList);

        return SkillText;
    }

    public string TranslateActionToText(CardAction action){
        string text = "";
        int index = 0;
        int subIndex = 0;

        switch (action.actionType)
        {
            case ActionTypes.Attack:
                bool allSameAttackTypes = true;
                foreach (var attack in action.attacks)
                {
                    if(attack.damageType != action.attacks[0].damageType)
                    {
                        allSameAttackTypes = false;
                    }
                }
                if (allSameAttackTypes && action.attacks.Count > 1) {
                    AttackAction attack = action.attacks[0];
                    text += action.attacks.Count.ToString() + " ";
                    text += DamageTypeDescription(attack.damageType);
                    text += " attacks";
                } else {
                    index = 0;
                    foreach (var attack in action.attacks)
                    {
                        if(index == 0)
                        {
                            if(action.attacks.Count > 1)
                            {
                                text += "A ";
                            }
                            text += DamageTypeDescription(attack.damageType);
                            text += " attack";
                        } else {
                            if(action.attacks.Count-1 == index)
                            {
                                text += " and a ";
                                text += DamageTypeDescription(attack.damageType);
                                text += " attack";
                            } else {
                                text += ", a";
                                text += DamageTypeDescription(attack.damageType);
                                text += " attack";
                            }
                        }

						subIndex = 0;
						if (attack.attackEffect.Count > 0)
                        {
                            text += " and ";
							foreach (var effect in attack.attackEffect)
							{
								if(subIndex == 0)
								{
									text += AttackEffectDescription(effect);
								} else {
									if(attack.temporaryBuffs.Count-1 == subIndex)
									{
										text += " and ";
										text += AttackEffectDescription(effect);
									} else {
										text += ", ";
										text += AttackEffectDescription(effect);
									}
								}

								if(effect.requirements.Count > 0)
								{
									Requirements requirement = effect.requirements[0];
									if(requirement.requirement == RequirementTypes.TargetHasSubtypes || requirement.requirement == RequirementTypes.TargetBelongsToFactions || requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
									{
										text += " when targetting ";
										subIndex = 0;
										foreach (var subtype in requirement.subtypeRequirement)
										{
											if(subIndex == 0)
											{
												text += TargetSubtypeDescription(subtype,true);
											} else {
												if(requirement.subtypeRequirement.Count-1 == index){
													text += " and ";
													text += TargetSubtypeDescription(subtype,true);
												} else {
													text += ", ";
													text += TargetSubtypeDescription(subtype,true);
												}
											}
											subIndex += 1;
										}
										if(requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
										{
											text += " and ";
										}
										subIndex = 0;
										foreach (var faction in requirement.factionRequirement)
										{
											if(subIndex == 0)
											{
												text += TargetFactionDescription(faction,true);
											} else {
												if(requirement.factionRequirement.Count-1 == index){
													text += " and ";
													text += TargetFactionDescription(faction,true);
												} else {
													text += ", ";
													text += TargetFactionDescription(faction,true);
												}
											}
											subIndex += 1;
										}
									}
								}

								subIndex += 1;
							}
                        }

						subIndex = 0;
                        if (attack.temporaryBuffs.Count > 0)
                        {
                            text += ", with ";
							foreach (var tempBuff in attack.temporaryBuffs)
							{
								if(subIndex == 0)
								{
									if(tempBuff.amount < 0){ text += tempBuff.amount+" "; } else { text += "+"+tempBuff.amount+" "; }
									text += BuffAttributeDescription(tempBuff.Attribute);
								} else {
									if(attack.temporaryBuffs.Count-1 == subIndex)
									{
										text += " and ";
										if(tempBuff.amount < 0){ text += tempBuff.amount+" "; } else { text += "+"+tempBuff.amount+" "; }
										text += BuffAttributeDescription(tempBuff.Attribute);
									} else {
										text += ", ";
										if(tempBuff.amount < 0){ text += tempBuff.amount+" "; } else { text += "+"+tempBuff.amount+" "; }
										text += BuffAttributeDescription(tempBuff.Attribute);
									}
								}

								if(tempBuff.requirements.Count > 0)
								{
									Requirements requirement = tempBuff.requirements[0];
									if(requirement.requirement == RequirementTypes.TargetHasSubtypes || requirement.requirement == RequirementTypes.TargetBelongsToFactions || requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
									{
										text += " when targetting ";
										subIndex = 0;
										foreach (var subtype in requirement.subtypeRequirement)
										{
											if(subIndex == 0)
											{
												text += TargetSubtypeDescription(subtype,true);
											} else {
												if(requirement.subtypeRequirement.Count-1 == index){
													text += " and ";
													text += TargetSubtypeDescription(subtype,true);
												} else {
													text += ", ";
													text += TargetSubtypeDescription(subtype,true);
												}
											}
											subIndex += 1;
										}
										if(requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
										{
											text += " and ";
										}
										subIndex = 0;
										foreach (var faction in requirement.factionRequirement)
										{
											if(subIndex == 0)
											{
												text += TargetFactionDescription(faction,true);
											} else {
												if(requirement.factionRequirement.Count-1 == index){
													text += " and ";
													text += TargetFactionDescription(faction,true);
												} else {
													text += ", ";
													text += TargetFactionDescription(faction,true);
												}
											}
											subIndex += 1;
										}
									}
								}

								subIndex += 1;
							}
                        }

                        if (attack.requirements.Count > 0)
                        {
                            Requirements requirement = attack.requirements[0];
                            if(requirement.requirement == RequirementTypes.TargetHasSubtypes || requirement.requirement == RequirementTypes.TargetBelongsToFactions || requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
                            {
                                text += ", but can only target ";
                                subIndex = 0;
                                foreach (var subtype in requirement.subtypeRequirement)
                                {
                                    if(subIndex == 0)
                                    {
                                        text += TargetSubtypeDescription(subtype,true);
                                    } else {
                                        if(requirement.subtypeRequirement.Count-1 == index){
                                            text += " and ";
                                            text += TargetSubtypeDescription(subtype,true);
                                        } else {
                                            text += ", ";
                                            text += TargetSubtypeDescription(subtype,true);
                                        }
                                    }
                                    subIndex += 1;
                                }
                                if(requirement.requirement == RequirementTypes.TargetHasSubtypesOrFactions)
                                {
                                    text += " and ";
                                }
                                subIndex = 0;
                                foreach (var faction in requirement.factionRequirement)
                                {
                                    if(subIndex == 0)
                                    {
                                        text += TargetFactionDescription(faction,true);
                                    } else {
                                        if(requirement.factionRequirement.Count-1 == index){
                                            text += " and ";
                                            text += TargetFactionDescription(faction,true);
                                        } else {
                                            text += ", ";
                                            text += TargetFactionDescription(faction,true);
                                        }
                                    }
                                    subIndex += 1;
                                }
                            }
                        }

                        index += 1;
                    }
                }
            break;
            case ActionTypes.Buff:
                index = 0;

                bool allSameTarget = true;
                foreach (var buff in action.buffs)
                {
                    if(buff.target != action.buffs[0].target)
                    {
                        allSameTarget = false;
                    }
                }
                
                foreach (var buff in action.buffs)
                {
                    if(index == 0){
						switch (buff.Attribute)
						{
							case Attributes.Health: text += "Heals "; break;
							default: text += "Grants "; break;
						}
						if(buff.amount != 0)
                        {
							text += "+"+buff.amount+" ";
							text += BuffAttributeDescription(buff.Attribute);
							if (!allSameTarget || action.buffs.Count == 1)
							{
								text += " ";
								text += TargetTypeDescription(buff.target);
							}
							text += BuffEffectDescription(buff);
                        }
                    } else {
                        if(action.buffs.Count-1 == index){
                            text += " and ";
                            if(buff.Attribute == Attributes.Health)
                            {
                                text += "heals ";
                            }
							if(buff.amount != 0)
                        	{
								text += "+"+buff.amount+" ";
								text += BuffAttributeDescription(buff.Attribute);
								text += " ";
								text += TargetTypeDescription(buff.target);
							}
							text += BuffEffectDescription(buff);
                        } else {
                            text += ", ";
							if(buff.amount != 0)
                        	{
                            	text += "+"+buff.amount+" ";
                            	text += BuffAttributeDescription(buff.Attribute);
								if (!allSameTarget)
								{
									text += " ";
									text += TargetTypeDescription(buff.target);
								}
							}
							text += BuffEffectDescription(buff);
                        }
                    }
                    index += 1;
                }
            break;
        }

        text += ".";

        return text;
    }

    public string DamageTypeDescription(DamageTypes damageType)
    {
        string text = "";
        switch (damageType){
            case DamageTypes.Melee:
                text += "Melee";
            break;
            case DamageTypes.Ranged:
                text += "Ranged";
            break;
            case DamageTypes.Energy:
                text += "Energy";
            break;
            case DamageTypes.MeleeOrRanged:
                text += "Melee or ranged";
            break;
            case DamageTypes.RangedOrEnergy:
                text += "Ranged or energy";
            break;
            case DamageTypes.MeleeOrEnergy:
                text += "Melee or energy";
            break;
            case DamageTypes.MeleeOrRangedOrEnergy:
                text += "Melee, ranged or energy";
            break;
        }
        return text;
    }

    public string BuffAttributeDescription(Attributes attribute)
    {
        string text = "";
        switch (attribute){
			case Attributes.Attack:
                text += "attack";
            break;
            case Attributes.Health:
                text += "HP";
            break;
            case Attributes.Defense:
                text += "defense";
            break;
            case Attributes.DefenseMelee:
                text += "melee defense";
            break;
            case Attributes.DefenseRanged:
                text += "ranged defense";
            break;
            case Attributes.DefenseEnergy:
                text += "energy defense";
            break;
            case Attributes.ArmorPierce:
                text += "armor pierce";
            break;
            case Attributes.DamageReductionBeforeArmor:
                text += "damage reduction";
            break;
            case Attributes.DamageReductionAfterArmor:
                text += "damage reduction (after armor)";
            break;
            case Attributes.MaxHealth:
                text += "max HP";
            break;
        }
        return text;
    }

    public string TargetTypeDescription(TargetTypes targetType)
    {
        string text = "";
        switch (targetType){
            case TargetTypes.Self:
                text += "to me";
            break;
            case TargetTypes.AlliesInSameLine:
                text += "to all allies in the same row";
            break;
            case TargetTypes.SingleEnemy:
                text += "to an enemy";
            break;
            case TargetTypes.LineOfEnemies:
                text += "to the front-most row of enemies";
            break;
            case TargetTypes.AllAllies:
                text += "to all allies";
            break;
            case TargetTypes.SameTarget:
                text += "";
            break;
            case TargetTypes.AlliesNextToMe:
                text += "to the allies next to me";
            break;
        }
        return text;
    }

    public string TargetFactionDescription(Faction faction, bool plural = false)
    {
        string text = "";
        switch (faction)
        {
            case Faction.Protectors:
                if(!plural){ text += "Protector"; }else{ text += "Protectors"; }
            break;
            case Faction.Saggists:
                if(!plural){ text += "Saggist"; }else{ text += "Saggists"; }
            break;
            case Faction.Keraneans:
                if(!plural){ text += "Keranean"; }else{ text += "Keraneans"; }
            break;
            case Faction.Voucari:
                if(!plural){ text += "Voucarian"; }else{ text += "Voucarians"; }
            break;
            case Faction.Auro:
                if(!plural){ text += "Auro"; }else{ text += "Auro"; }
            break;
            case Faction.Independent:
                if(!plural){ text += "Independent unit"; }else{ text += "Independent units"; }
            break;
        }
        return text;
    }

    public string TargetSubtypeDescription(UnitSubtype subtype, bool plural = false)
    {
        string text = "";
        switch (subtype)
        {
            case UnitSubtype.Defender:
                if(!plural){ text += "defender"; }else{ text += "defenders"; }
            break;
            case UnitSubtype.Mercenary:
                if(!plural){ text += "mercenary"; }else{ text += "mercenaries"; }
            break;
            case UnitSubtype.Pacifist:
                if(!plural){ text += "pacifist"; }else{ text += "pacifists"; }
            break;
            case UnitSubtype.Combo:
                text += "combo";
            break;
            case UnitSubtype.Executioner:
                if(!plural){ text += "executioner"; }else{ text += "executioners"; }
            break;
            case UnitSubtype.Noble:
                if(!plural){ text += "noble"; }else{ text += "nobles"; }
            break;
            case UnitSubtype.Solitary:
                if(!plural){ text += "solitary"; }else{ text += "solitaries"; }
            break;
            case UnitSubtype.Inheritor:
                if(!plural){ text += "inheritor"; }else{ text += "inheritors"; }
            break;
        }
        return text;
    }

	public string AttackEffectDescription(AttackEffect effect)
    {
        string text = "";
		int index = 0;
        switch (effect.effectType)
        {
            case AttackEffects.SplashDamage:
				text += "deals ";
				if(effect.value < 0){ text += effect.value+" "; } else { text += "+"+effect.value+" "; }
                text += "damage to enemies next to the target";
            break;
			case AttackEffects.SelfDamage:
				text += "inflicts ";
				if(effect.value < 0){ text += effect.value+" "; } else { text += "+"+effect.value+" "; }
                text += "self-damage";
            break;
			case AttackEffects.ApplyDebuff:
				index = 0;
				foreach (var buff in effect.buffs)
                {
                    if(index == 0){
						text += "removes ";
                        if(buff.amount < 0){ text += buff.amount+" "; } else { text += "+"+buff.amount+" "; }
                        text += BuffAttributeDescription(buff.Attribute);
						switch (buff.target)
						{
							case TargetTypes.SameTarget: text += " from the target"; break;
						}
                    } else {
                        if(effect.buffs.Count-1 == index){
                            text += " and ";
                            if(buff.amount < 0){ text += buff.amount+" "; } else { text += "+"+buff.amount+" "; }
                            text += BuffAttributeDescription(buff.Attribute);
                            text += " ";
                        } else {
                            text += ", ";
                            if(buff.amount < 0){ text += buff.amount+" "; } else { text += "+"+buff.amount+" "; }
                            text += BuffAttributeDescription(buff.Attribute);
                        }
                    }
                    index += 1;
                }
			break;
        }
        return text;
    }

	public string BuffEffectDescription(BuffAction buffAction)
    {
        string text = "";
        switch (buffAction.specialEffect){
			case BuffSpecialEffects.RedirectAttacksTowardsMe:
				text += "redirects all attacks ";
				text += TargetTypeDescription(buffAction.target);
                text += " towards me";
			break;
		}
		return text;
    }
}