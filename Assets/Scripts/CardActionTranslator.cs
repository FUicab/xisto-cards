using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Card;
using static CardDisplay;


public class CardActionMenu
{
    public List<CardActionObject> actions = new List<CardActionObject>();
    public CardDisplay sourceCard;

    public CardActionMenu(CardDisplay cardDisplay)
    {
        int index = 0;
        int actionIndex = 0;
        sourceCard = cardDisplay;
        foreach (var item in cardDisplay.card.CardActions)
        {
            if(item.actionType != ActionTypes.RepeatFromAbove)
            {
                if(index != 0){ actionIndex += 1; }
                actions.Add(new CardActionObject(item));
                actions[actionIndex].diceValues.Add(index+1);
            } else {
                actions[actionIndex].diceValues.Add(index+1);
            }
            index += 1;
        }
    }
}

[System.Serializable]
public class CardActionObject
{
    public List<int> diceValues = new List<int>();
    public CardAction action;
    public string description;
    public string shortDescription;

    public CardActionObject(CardAction theAction)
    {
        action = theAction;
        description = TranslateActionToText();
    }

    public string TranslateActionToText(){
        string text = "";
        int index = 0;
        int subIndex = 0;

        switch (action.actionType)
        {
            case ActionTypes.Attack:

                // Checks if all attacks are the same. This is to list each one separately or just display the number of attacks.
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
                    text += TextFormat(action.attacks.Count.ToString(),null,action.attackCountCanBeAugmented) + " ";
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

                        // Lists all attack effects
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

                                // Lists the requirements for those attack effects
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
												text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
											} else {
												if(requirement.subtypeRequirement.Count-1 == index){
													text += " and ";
													text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
												} else {
													text += ", ";
													text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
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
												text += TextFormat(TargetFactionDescription(faction,true),"faction");
											} else {
												if(requirement.factionRequirement.Count-1 == index){
													text += " and ";
													text += TextFormat(TargetFactionDescription(faction,true),"faction");
												} else {
													text += ", ";
													text += TextFormat(TargetFactionDescription(faction,true),"faction");
												}
											}
											subIndex += 1;
										}
									}
                                    if(requirement.requirement == RequirementTypes.TargetHasAttackedThisRound)
                                    {
                                        text += " if the target has attacked during this round";
                                    }
								}

								subIndex += 1;
							}
                        }

                        // Lists the temporary buffs of an attack. These buffs only apply while performing the action.
						subIndex = 0;
                        if (attack.temporaryBuffs.Count > 0)
                        {
                            text += ", with ";
							foreach (var tempBuff in attack.temporaryBuffs)
							{
								if(subIndex == 0)
								{
									if(tempBuff.amount < 0){ text += TextFormat(tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; }
									text += TextFormat(BuffAttributeDescription(tempBuff.Attribute),tempBuff.Attribute);
								} else {
									if(attack.temporaryBuffs.Count-1 == subIndex)
									{
										text += " and ";
										if(tempBuff.amount < 0){ text += TextFormat(tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; }
										text += TextFormat(BuffAttributeDescription(tempBuff.Attribute),tempBuff.Attribute);
									} else {
										text += ", ";
										if(tempBuff.amount < 0){ text += TextFormat(tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+tempBuff.amount.ToString(),null,tempBuff.amountCanBeAugmented)+" "; }
										text += TextFormat(BuffAttributeDescription(tempBuff.Attribute),tempBuff.Attribute);
									}
								}

                                // Lists the requirements for the temporary buffs on an attack.
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
												text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
											} else {
												if(requirement.subtypeRequirement.Count-1 == index){
													text += " and ";
													text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
												} else {
													text += ", ";
													text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
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
												text += TextFormat(TargetFactionDescription(faction,true),"faction");
											} else {
												if(requirement.factionRequirement.Count-1 == index){
													text += " and ";
													text += TextFormat(TargetFactionDescription(faction,true),"faction");
												} else {
													text += ", ";
													text += TextFormat(TargetFactionDescription(faction,true),"faction");
												}
											}
											subIndex += 1;
										}
									}
                                    if(requirement.requirement == RequirementTypes.TargetHasAttackedThisRound)
                                    {
                                        text += " if the target has attacked during this round";
                                    }
                                }

								subIndex += 1;
							}
                        }

                        // Lists the requirements of an attack.
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
                                        text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
                                    } else {
                                        if(requirement.subtypeRequirement.Count-1 == index){
                                            text += " and ";
                                            text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
                                        } else {
                                            text += ", ";
                                            text += TextFormat(TargetSubtypeDescription(subtype,true),"subtype");
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
                                        text += TextFormat(TargetFactionDescription(faction,true),"faction");
                                    } else {
                                        if(requirement.factionRequirement.Count-1 == index){
                                            text += " and ";
                                            text += TextFormat(TargetFactionDescription(faction,true),"faction");
                                        } else {
                                            text += ", ";
                                            text += TextFormat(TargetFactionDescription(faction,true),"faction");
                                        }
                                    }
                                    subIndex += 1;
                                }
                            }
                            if(requirement.requirement == RequirementTypes.TargetHasAttackedThisRound)
                            {
                                text += " if the target has attacked during this round";
                            }
                        }

                        index += 1;
                    }
                }
            break;
            case ActionTypes.Buff:
                index = 0;

                // Checks if all the buffs apply to the same target. This is to ensure that each buff describes who gets it.
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
							text += TextFormat("+"+buff.amount,null,buff.amountCanBeAugmented)+" ";
							text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
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
								text += TextFormat("+"+buff.amount,null,buff.amountCanBeAugmented)+" ";
								text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
								text += " ";
								text += TargetTypeDescription(buff.target);
							}
							text += BuffEffectDescription(buff);
                        } else {
                            text += ", ";
							if(buff.amount != 0)
                        	{
                            	text += TextFormat("+"+buff.amount,null,buff.amountCanBeAugmented)+" ";
                            	text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
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

    // Could not find a proper structure for this. May remove later.
    public string BuildListOfItems(List<object> items, string prefix = "", int value = 0, string content = "", string suffix = "")
    {
        string text = "";
        return text;
    }

    public string DamageTypeDescription(DamageTypes damageType)
    {
        string text = "";
        switch (damageType){
            case DamageTypes.Melee:
                text += "⚔️ Melee";
            break;
            case DamageTypes.Ranged:
                text += "🎯 Ranged";
            break;
            case DamageTypes.Energy:
                text += "✨ Energy";
            break;
            case DamageTypes.MeleeOrRanged:
                text += "⚔️ Melee or 🎯 ranged";
            break;
            case DamageTypes.RangedOrEnergy:
                text += "🎯 Ranged or ✨ energy";
            break;
            case DamageTypes.MeleeOrEnergy:
                text += "⚔️ Melee or ✨ energy";
            break;
            case DamageTypes.MeleeOrRangedOrEnergy:
                text += "⚔️ Melee, 🎯 ranged or ✨ energy";
            break;
        }
        return text;
    }

    public string TextFormat(string textToFormat = "", object colorTag = null, bool canBeAugmented = false)
    {
        string text = "<b><color=#";
        if (canBeAugmented)
        {
            text += "15c";
        } else
        {
            switch (colorTag)
            {
                case Attributes.Health: text += "583"; break;
                case Attributes.MaxHealth: text += "583"; break;
                case Attributes.Attack: text += "900"; break;
                case Attributes.Defense: text += "760"; break;
                case Attributes.DefenseMelee: text += "760"; break;
                case Attributes.DefenseRanged: text += "760"; break;
                case Attributes.DefenseEnergy: text += "760"; break;
                case Attributes.DamageReductionBeforeArmor: text += "760"; break;
                case Attributes.DamageReductionAfterArmor: text += "760"; break;
                case Attributes.ArmorPierce: text += "b50"; break;
                case "faction": text += "555"; break;
                case "subtype": text += "714"; break;
                case "self-damage": text += "c42"; break;
                default: text += "222"; break;
            }
        }
        text += $">{textToFormat}</color></b>";
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
                text += "to allies next to me";
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
				if(effect.value < 0){ text += TextFormat(effect.value.ToString(),null,effect.valueCanBeAugmented)+" "; } else { text += TextFormat("+"+effect.value.ToString(),null,effect.valueCanBeAugmented)+" "; }
                text += "damage to enemies next to the target";
            break;
			case AttackEffects.SelfDamage:
				text += "inflicts ";
				if(effect.value < 0){ text += TextFormat(effect.value.ToString(),null,effect.valueCanBeAugmented)+" "; } else { text += TextFormat("+"+effect.value.ToString(),null,effect.valueCanBeAugmented)+" "; }
                text += TextFormat("self-damage","self-damage");
            break;
			case AttackEffects.ApplyDebuff:
				index = 0;
				foreach (var buff in effect.buffs)
                {
                    if(index == 0){
						text += "removes ";
                        if(buff.amount < 0){ text += TextFormat(buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; }
                        text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
						switch (buff.target)
						{
							case TargetTypes.SameTarget: text += " from the target"; break;
						}
                    } else {
                        if(effect.buffs.Count-1 == index){
                            text += " and ";
                            if(buff.amount < 0){ text += TextFormat(buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; }
                            text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
                            text += " ";
                        } else {
                            text += ", ";
                            if(buff.amount < 0){ text += TextFormat(buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; } else { text += TextFormat("+"+buff.amount.ToString(),null,buff.amountCanBeAugmented)+" "; }
                            text += TextFormat(BuffAttributeDescription(buff.Attribute),buff.Attribute);
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