using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Card;
using static CardDisplay;


public class CardActionMenu
{
	public List<CardActionObject> actions = new List<CardActionObject>();
	public CardDisplay sourceCard;

	public CardActionMenu(List<CardAction> cardActions)
	{
		SetUpTheMenu(cardActions);
	}
	public CardActionMenu(CardDisplay cardDisplay)
	{
		sourceCard = cardDisplay;
		SetUpTheMenu(cardDisplay.cardActions);
	}

	private void SetUpTheMenu(List<CardAction> cardActions)
	{
		int index = 0;
		int actionIndex = 0;
		foreach (var item in cardActions)
		{
			if (item.actionType != ActionTypes.RepeatFromAbove)
			{
				if (index != 0) { actionIndex += 1; }
				actions.Add(new CardActionObject(item, sourceCard));
				actions[actionIndex].diceValues.Add(index + 1);
			}
			else
			{
				actions[actionIndex].diceValues.Add(index + 1);
			}
			index += 1;
		}
		foreach (var action in actions)
		{
			if (action.diceValues.Count == 1)
			{
				action.diceAverageValue = action.diceValues[0];
			}
			else if (action.diceValues.Count % 2 == 0)
			{
				action.diceAverageValue = action.diceValues[action.diceValues.Count / 2];
			}
			else
			{
				action.diceAverageValue = action.diceValues[(action.diceValues.Count - 1) / 2];
			}
			if (action.HasMatchingDice() && sourceCard.CanActThisTurn && (action.action.actionType != ActionTypes.Attack || !sourceCard.IsDisarmed))
			{
				action.canBeUsed = true;
			}
			else
			{
				action.canBeUsed = false;
			}
		}
	}
}

[System.Serializable]
public class CardSkillObject
{
	public List<int> diceValues = new List<int>();
	public int diceAverageValue = 0;
	public string description;
	public string shortDescription;
	public bool isAction;
	public CardDisplay sourceCard;

	public CardSkillObject(CardDisplay theCard)
	{
		sourceCard = theCard;
	}

	// Could not find a proper structure for this. May remove later.
	public string BuildListOfItems(List<object> items, string prefix = "", int value = 0, string content = "", string suffix = "")
	{
		string text = "";
		return text;
	}
}

public static class CardTranslator
{
	public static string GenerateSkillAttackText(CardAction skill)
	{
		return GenerateAttacksDescription(skill.attacks, skill.attackCountCanBeAugmented);
	}

	public static string GenerateAttacksDescription(List<AttackAction> attacks, bool attackCountCanBeAugmented = false)
	{
        string text = "";
        int index = 0;
        int subIndex = 0;
        /* Checks if all attacks are the same. This is to list each one separately or just display the number of attacks.
         * It also checks if all of those attacks share the same relevant data, like effects and requirements.
         */
        bool allSameAttackTypes = attacks.Count > 1;
        foreach (var attack in attacks)
        {
            if (attack.damageType != attacks[0].damageType || attack.damageMultiplier != attacks[0].damageMultiplier || attack.requirements.Count != attacks[0].requirements.Count || attack.attackEffect.Count != attacks[0].attackEffect.Count)
            {
                allSameAttackTypes = false;
            }
        }
        index = 0;
        foreach (var attack in attacks)
        {
			if (allSameAttackTypes && attacks.Count > 1 && index == 0)
			{
				text += TextFormat(attacks.Count.ToString(), null, attackCountCanBeAugmented) + " ";
				text += DamageTypeDescription(attack.damageType);
				text += " attacks";
				if (attack.requirements.Count > 0)
				{
					text += RequirementDescription(attack.requirements, attack.target, attack.source?.card, "effectOrTempBuff");
				}
			} else if (!allSameAttackTypes) {
				if (index == 0)
				{
					if (attacks.Count > 1)
					{
						text += "A ";
					}
					text += DamageTypeDescription(attack.damageType);
					text += " attack";
				}
				else
				{
					if (attacks.Count - 1 == index)
					{
						text += " and a ";
						text += DamageTypeDescription(attack.damageType);
						text += " attack";
					}
					else
					{
						text += ", a";
						text += DamageTypeDescription(attack.damageType);
						text += " attack";
					}
				}
			}

            if (attack.damageMultiplier != 1f)
            {
				if(attack.flatDamageOverwrite != 0)
				{
					if (attacks.Count > 1) { text += " and each "; } else { text += " which "; }
                    text += $"deals {TextFormat(NumberWithSign(attack.flatDamageOverwrite), null, attack.damageMultiplierCanBeAugmented)} {TextFormat("damage", Attributes.Attack)}";
                } else if (attack.damageMultiplier % 1 == 0) {
                    text += $" with {TextFormat("×" + attack.damageMultiplier.ToString(), null, attack.damageMultiplierCanBeAugmented)} {TextFormat("damage",Attributes.Attack)}";
				} else {
					text += $" with {TextFormat( (attack.damageMultiplier * 100)+"%" , null, attack.damageMultiplierCanBeAugmented )} effectivity";
				}
            }

            // Lists all attack effects
            subIndex = 0;
            if (attack.attackEffect.Count > 0)
            {
                text += " and ";
                foreach (var effect in attack.attackEffect)
                {
                    if (subIndex == 0)
                    {
                        text += AttackEffectDescription(effect);
                    }
                    else
                    {
                        if (attack.temporaryBuffs.Count - 1 == subIndex)
                        {
                            text += " and ";
                            text += AttackEffectDescription(effect);
                        }
                        else
                        {
                            text += ", ";
                            text += AttackEffectDescription(effect);
                        }
                    }

                    // Lists the requirements for those attack effects
                    if (effect.requirements.Count > 0)
                    {
                        text += RequirementDescription(effect.requirements, attack.target, attack.source?.card, "effectOrTempBuff");
                    }

                    subIndex += 1;
                }
            }

            // Lists the temporary buffs of an attack. These buffs only apply while performing the skill.
            //subIndex = 0;
            if (attack.temporaryBuffs.Count > 0)
            {
				List<BuffAction> tempBuffs = attack.temporaryBuffs.Where(x => x.Attribute != Attributes.Health).ToList();
                List<BuffAction> healingBuffs = attack.temporaryBuffs.Where(x => x.Attribute == Attributes.Health).ToList();
                if(tempBuffs.Count > 0){ text += ", with "; }
				for (int i = 0; i < tempBuffs.Count; i++)
				{
					BuffAction tempBuff = tempBuffs[i];
                    text += AttributeAndValue(tempBuff);

                    // Lists the requirements for the temporary buffs on an attack.
                    if (tempBuff.requirements.Count > 0)
                    {
                        text += RequirementDescription(tempBuff.requirements, tempBuff.target, attack.source?.card, "effectOrTempBuff");
                    }
                    if (i < tempBuffs.Count - 2)
                    {
                        text += ", ";
                    }
                    else if (i == tempBuffs.Count - 2)
                    {
                        text += " and ";
                    }
                }
                if (tempBuffs.Count > 0 && healingBuffs.Count > 0) { text += ","; }
                if (healingBuffs.Count > 0) { text += " and heals "; }
                for (int i = 0; i < healingBuffs.Count; i++)
                {
                    BuffAction healingBuff = healingBuffs[i];
                    text += AttributeAndValue(healingBuff)+" from ";
					text += TargetTypeDescription(healingBuff.target);

                    // Lists the requirements for the temporary buffs on an attack.
                    if (healingBuff.requirements.Count > 0)
                    {
                        text += RequirementDescription(healingBuff.requirements, healingBuff.target, attack.source?.card, "effectOrTempBuff");
                    }
                    if (i < tempBuffs.Count - 2)
                    {
                        text += ", ";
                    }
                    else if (i == tempBuffs.Count - 2)
                    {
                        text += " and ";
                    }
                }
            }

            // Lists the requirements of an attack.
            if (attack.requirements.Count > 0)
            {
                text += RequirementDescription(attack.requirements, attack.target, attack.source?.card, "attack");
            }
			if (allSameAttackTypes)
			{
				break;
			}
            index += 1;
        }
        return text;
    }

	public static string GenerateSkillBuffText(List<BuffAction> buffs, PassiveSkill passiveSkill = null, bool startWithUppercase = true)
	{
		string text = "";
		int index = 0;

		// Checks if all the buffs apply to the same target. This is to ensure that each buff describes who gets it.
		bool allSameTarget = true;
		foreach (var buff in buffs)
		{
			if(buff.target != buffs[0].target)
			{
				allSameTarget = false;
			}
		}
		bool allSameTargetButCanHaveExtraWithRequirements = allSameTarget && buffs.Count > 1;
		if(allSameTarget && buffs[0].requirements.Count == 0)
		{
            for (int i = 1; i < buffs.Count; i++)
            {
				if (buffs[i].requirements.Count == 0) allSameTargetButCanHaveExtraWithRequirements = false;
            }
        } else
		{
			allSameTargetButCanHaveExtraWithRequirements = false;
		}

        bool allSameRequirements = true;
		if(buffs.Count > 0)
		if (buffs[0].requirements.Count > 0)
        foreach (var buff in buffs)
        {
			if(buff.requirements.Count > 0)
			{
				for (int i = 0; i < buff.requirements.Count; i++)
				{
					if (buff.requirements[i]?.requirement != buffs[0].requirements[i]?.requirement)
					{
						allSameRequirements = false;
					}
				}
			}
            else
            {
                allSameRequirements = false;
            }
        }

        foreach (var buff in buffs)
		{
			if(index == 0 && startWithUppercase){
				if (passiveSkill != null && (passiveSkill.trigger == TriggerTypes.OnAttack || buff.activatesOnHit))
				{
					text += "When ";
					switch (buff.target)
					{
						case TargetTypes.Self:
							text += "I";
							break;
						case TargetTypes.AlliesInSameLine:
							text += "my allies on the same line";
							break;
						case TargetTypes.SingleEnemy:
						case TargetTypes.LineOfEnemies:
						case TargetTypes.AllEnemies:
							text += "the enemies";
							break;
						case TargetTypes.SingleAlly:
						case TargetTypes.AllAllies:
							text += "my allies";
							break;
						case TargetTypes.AlliesNextToMe:
							text += "the allies next to me";
							break;
						case TargetTypes.AlliesInLineInFrontOfMe:
							text += "the allies in the line in front of me";
							break;
                        case TargetTypes.AlliesInLineBehind:
							text += "the allies in the line behind me";
							break;
                        default:
							text += "they";
							break;
					}
					if (buff.requirements.Count > 0)
						text += RequirementDescription(buff.requirements, buff.target, buff.source?.card);
					text += " <b>attack 💥</b> ";

					switch (buff.specialEffect)
					{
						case BuffSpecialEffects.TriggerExtraAttack:
							text += ", ";
							switch (buff.target)
							{
								case TargetTypes.Self:
									text += "I";
									break;
								default:
									text += "they";
									break;
							}
							text += " perform ";
							if (buff.extraAttacks.Count == 1)
							{
								text += "a ";
							}
							text += GenerateAttacksDescription(buff.extraAttacks);
							if (buff.extraAttacks.Count == 1)
							{
								text += " as an extra attack";
							}
							else
							{
								text += " as extra attacks";
							}
							break;
                        case BuffSpecialEffects.GrantAttackEffect:
                            text += ", this attack ";
							text += $"{AttackEffectDescription(buff.attackEffect[0])}";
                            break;
                        default:
							switch (buff.Attribute)
							{
								case Attributes.Health: text += "heals "; break;
								default:
									switch (buff.target)
									{
										case TargetTypes.Self:
											text += "I get ";
											break;
										default:
											text += "they get ";
											break;
									}
									break;
							}
							break;
					}
				}
				else if (buff.specialEffect == BuffSpecialEffects.TriggerExtraAttack)
				{
					text += "Make";
					switch (buff.target)
					{
						case TargetTypes.Self:
							text += "s me";
							break;
						case TargetTypes.AlliesInSameLine:
							text += " my allies on the same line";
							break;
						case TargetTypes.SingleEnemy:
						case TargetTypes.LineOfEnemies:
						case TargetTypes.AllEnemies:
							text += " the enemies";
							break;
						case TargetTypes.SingleAlly:
						case TargetTypes.AllAllies:
							text += " all my allies";
							break;
						case TargetTypes.AlliesNextToMe:
							text += "s the allies next to me";
							break;
                        case TargetTypes.AlliesInLineInFrontOfMe:
                            text += "s the allies in the line in front of me";
                            break;
                        case TargetTypes.AlliesInLineBehind:
                            text += "s the allies in the line behind me";
                            break;
                        default:
							text += "s them";
							break;
					}
					text += " perform ";
					if (buff.extraAttacks.Count == 1)
					{
						text += "a ";
					}
					text += GenerateAttacksDescription(buff.extraAttacks);
					if (buff.extraAttacks.Count == 1)
					{
						text += " as an extra attack";
					}
					else
					{
						text += " as extra attacks";
					}
				}
				else if (buff.specialEffect == BuffSpecialEffects.EnableGuardingPose)
				{
					switch (buff.target)
					{
						case TargetTypes.Self:
							text += "I";
							break;
						case TargetTypes.AlliesInSameLine:
							text += "My allies on the same line";
							break;
						case TargetTypes.SingleEnemy:
						case TargetTypes.LineOfEnemies:
						case TargetTypes.AllEnemies:
							text += "The enemies";
							break;
						case TargetTypes.SingleAlly:
						case TargetTypes.AllAllies:
							text += "My allies";
							break;
						case TargetTypes.AlliesNextToMe:
							text += "The allies next to me";
							break;
                        case TargetTypes.AlliesInLineInFrontOfMe:
                            text += "The allies in the line in front of me";
                            break;
                        default:
							text += "They";
							break;
					}
					text += $" take {TextFormat("guarding pose", "guarding")}";
				}
				else if (buff.specialEffect == BuffSpecialEffects.Disarm || buff.specialEffect == BuffSpecialEffects.Stun || buff.specialEffect == BuffSpecialEffects.Disrupt)
				{
					switch (buff.specialEffect)
					{
						case BuffSpecialEffects.Stun:
							text += TextFormat("Stun" + (buff.isTargetPlural ? "" : "s"), "statusEffect");
							break;
						case BuffSpecialEffects.Disarm:
							text += TextFormat("Disarm" + (buff.isTargetPlural ? "" : "s"), "statusEffect");
							break;
						case BuffSpecialEffects.Disrupt:
							text += TextFormat("Disrupt" + (buff.isTargetPlural ? "" : "s"), "statusEffect");
							break;
					}
					text += " " + TargetTypeDescription(buff.target);
				}
				else if (buff.specialEffect == BuffSpecialEffects.GrantAttackEffect)
				{
                    switch (buff.target)
                    {
                        case TargetTypes.Self:
                            text += "My attack ";
                            break;
                        case TargetTypes.AlliesInSameLine:
                            text += "Attack from allies in the same line ";
                            break;
                        case TargetTypes.SingleEnemy:
                        case TargetTypes.LineOfEnemies:
                        case TargetTypes.AllEnemies:
                            text += "Enemy attack ";
                            break;
                        case TargetTypes.SingleAlly:
                        case TargetTypes.AllAllies:
                            text += "Ally attack ";
                            break;
                        case TargetTypes.AlliesNextToMe:
                            text += "Attack from allies next to me ";
                            break;
                        case TargetTypes.AlliesInLineInFrontOfMe:
                            text += "Attack from allies in the line in front of me";
                            break;
                        case TargetTypes.AlliesInLineBehind:
                            text += "Attack from allies in the line behind me";
                            break;
                        default:
                            text += "Their attack ";
                            break;
                    }
                    text += $" {AttackEffectDescription(buff.attackEffect[0])}";
                }
                else if (buff.specialEffect == BuffSpecialEffects.AllowGuardingPoseRespondToRangedAttacks)
                {
                    switch (buff.target)
                    {
                        case TargetTypes.Self:
                            text += "I";
                            break;
                        case TargetTypes.AlliesInSameLine:
                            text += "My allies on the same line";
                            break;
                        case TargetTypes.SingleEnemy:
                        case TargetTypes.LineOfEnemies:
                        case TargetTypes.AllEnemies:
                            text += "The enemies";
                            break;
                        case TargetTypes.SingleAlly:
                        case TargetTypes.AllAllies:
                            text += "My allies";
                            break;
                        case TargetTypes.AlliesNextToMe:
                            text += "The allies next to me";
                            break;
                        case TargetTypes.AlliesInLineInFrontOfMe:
                            text += "The allies in the line in front of me";
                            break;
                        default:
                            text += "They";
                            break;
                    }
                    text += $" can {TextFormat("fight back", Attributes.Attack)} {TextFormat("against ranged attacks",Attributes.Defense)} when {TextFormat("guarding", "guarding")}";
                }
                else
				{
					switch (buff.Attribute)
					{
						case Attributes.Health: text += "Heals "; break;
						default: text += "Grants "; break;
					}
				}

				if(buff.amount != 0)
				{
					text += AttributeAndValue(buff);
					if ((!allSameTarget || buffs.Count == 1) && !buff.activatesOnHit)
					{
						text += " to ";
						text += TargetTypeDescription(buff.target);
					}
					if (passiveSkill != null && (passiveSkill.trigger == TriggerTypes.OnAttack || buff.activatesOnHit) )
					{ text += " <i>(for that attack)</i>"; }
					else if (passiveSkill != null && (passiveSkill.trigger == TriggerTypes.OnAttack || buff.activatesOnHit) )
					{ text += " <i>(until the end of the round)</i>"; }
					text += BuffEffectDescription(buff);
				} else
				{
					if(buff.specialEffect == BuffSpecialEffects.GrantSubtypes)
					{
                        for (int i = 0; i < buff.grantedSubtypes.Count; i++)
                        {
							text += TextFormat(TargetSubtypeDescription(buff.grantedSubtypes[i], true), "subtype");

                            if (i < buff.grantedSubtypes.Count - 2)
							{
								text += ", ";
							} else if (i == buff.grantedSubtypes.Count - 2)
							{
								text += " and ";
							}
                        }
                        text += " to ";
                        text += TargetTypeDescription(buff.target);
                    }
				}
			} else {
				if(buffs.Count-1 == index){
                    if (allSameTargetButCanHaveExtraWithRequirements) { text += " to "+TargetTypeDescription(buff.target);}
                    if (index != 0) text += " and ";
					if(buff.Attribute == Attributes.Health)
					{
						text += TextFormat("heals ", Attributes.Health);
					}
					if(buff.amount != 0 && !buff.addDamageDealtAsValue)
					{
						if (allSameTargetButCanHaveExtraWithRequirements) { text += " an additional "; }
						text += AttributeAndValue(buff);
                        if (allSameTargetButCanHaveExtraWithRequirements) { text += RequirementDescription(buff.requirements, buff.target, buff.source?.card); }
						if (!allSameTargetButCanHaveExtraWithRequirements) {
							text += " to ";
							text += TargetTypeDescription(buff.target);
						}
                    }

                    if (buff.addDamageDealtAsValue) {
						switch (buff.Attribute)
						{
							case Attributes.Health:
								text += TargetTypeDescription(buff.target);
								text += " for";
								if (buff.amount != 0){
									text += $"{AttributeAndValue(buff)} and";
								}
								text += " the amount of damage dealt";
							break;
						}
					}

					text += BuffEffectDescription(buff);
				} else {
					text += ", ";
					if(buff.amount != 0)
					{
						text += AttributeAndValue(buff);
						if (!allSameTarget)
						{
							text += " to ";
							text += TargetTypeDescription(buff.target);
						}
					}
					text += BuffEffectDescription(buff);
				}
			}

			if (buff.multiplyThisBuff) {
				text += $", for each{UnitDefinitionDescription(buff.multiplyForEach, buff.source?.card)}";
			}

			if(buff.requirements.Count > 0 && !buff.activatesOnHit && (index == buffs.Count-1 || !allSameRequirements) && !allSameTargetButCanHaveExtraWithRequirements )
			{
				text += RequirementDescription(buff.requirements, buff.target, buff.source?.card);
			}

			if (buff.onHitRequirements.Count > 0 && (index == buffs.Count - 1 || !allSameRequirements) && !allSameTargetButCanHaveExtraWithRequirements)
			{
				text += RequirementDescription(buff.onHitRequirements, buff.target, buff.source?.card, "onHit");
			}
			index += 1;
		}

		return text;
	}

	public static string NumberWithSign(BuffAction buff)
	{
		string text = "";
		if(buff.Attribute == Attributes.DamageMultiplier)
		{
			text += "×" + (buff.amount + (buff.originAttack != null? buff.originAttack.damageMultiplier : 0));
		} else
		{
			text += NumberWithSign(buff.amount);
		}
		return text;
	}

	public static string NumberWithSign(int number)
	{
		string text = "";
        if (number > 0) { text += $"+{number}"; }
        if (number < 0) { text += $"{number}"; }
		return text;
    }

	public static string AttributeAndValue(BuffAction buff)
	{
		string text = "";
		text += TextFormat(NumberWithSign(buff), null, buff.amountCanBeAugmented) + " ";
		text += TextFormat(BuffAttributeDescription(buff.Attribute), buff.Attribute);
		return text;
	}

	public static string TextFormat(string textToFormat = "", object colorTag = null, bool canBeAugmented = false)
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
                case Attributes.DamageMultiplier: text += "900"; break;
                case Attributes.Cost: case "gold": text += "b90"; break;
                case "faction": text += "555"; break;
				case "subtype": text += "714"; break;
				case "self-damage": text += "c42"; break;
                case "statusEffect": text += "90f"; break;
                case "guarding": text += "775"; break;
                case "actionPoints": text += "395"; break;
                default: text += "222"; break;
			}
		}
		text += $">{textToFormat}</color></b>";
		return text;
	}

	public static string FactionOrSubtypeRequirementDescription(Requirements requirement)
	{
		string text = "";
		for (int i = 0; i < requirement.subtypeRequirement.Count; i++)
		{
			text += TextFormat(TargetSubtypeDescription(requirement.subtypeRequirement[i], true), "subtype");
			if (i == requirement.subtypeRequirement.Count - 2) { text += " or "; }
			if (i < requirement.subtypeRequirement.Count - 2) { text += ", "; }
		}
		if (requirement.subtypeRequirement.Count > 0 && requirement.factionRequirement.Count > 0) { text += " or "; }
		for (int i = 0; i < requirement.factionRequirement.Count; i++)
		{
			text += TextFormat(TargetFactionDescription(requirement.factionRequirement[i], true), "faction");
			if (i == requirement.factionRequirement.Count - 2) { text += " or "; }
			if (i < requirement.factionRequirement.Count - 2) { text += ", "; }
		}
		return text;
	}

	public static string RequirementDescription(List<Requirements> requirements, TargetTypes providedTarget, Card card = null, string formattingFor = "buff")
	{
		string text = "";
		foreach (var requirement in requirements)
		{
			TargetTypes target;
			BuffAction originBuff = requirement.originAction as BuffAction;
			AttackAction originAttack = requirement.originAction as AttackAction;
			//Debug.Log($"{card?.Name} -> {providedTarget} . Origin buff: {(originBuff != null? "✅": "❌")} | Attack in buff: {(originBuff?.originAttack != null ? "✅" : "❌")} | Origin attack: {(originAttack != null ? "✅" : "❌")}");

			if(formattingFor == "onHit")
			{
				target = TargetTypes.SingleEnemy;
			} else if (originBuff != null)
			{
				if (originBuff.activatesOnHit)
				{
					target = TargetTypes.SingleEnemy;
				} else if (originBuff.originAttack != null && requirement.targetOfRequirementIsTargetOfAttack)
				{
					target = providedTarget;
				}
				else
				{
					target = originBuff?.originAttack?.target ?? TargetTypes.SingleEnemy;
				}
			}
			else if (originAttack != null && requirement.targetOfRequirementIsTargetOfAttack && originAttack.target != TargetTypes.Self)
			{
				target = originAttack.target;
			}
			else
			{
				target = providedTarget;
			}

			switch (target)
				{
					case TargetTypes.Self:
						switch (requirement.requirement)
						{
							case RequirementTypes.TargetHasSubtypesOrFactions:
								text += " if I'm ";
								text += FactionOrSubtypeRequirementDescription(requirement);
								break;
							case RequirementTypes.TargetIsNextTo:
								text += " if I'm next to";
								text += UnitDefinitionDescription(requirement.targetIs, card);
								break;
							case RequirementTypes.TargetHasAttackedThisRound:
								text += " if I have attacked before during this round";
								break;
							case RequirementTypes.TargetAttributeIs:
								text += " if my ";
								text += TextFormat(BuffAttributeDescription(requirement.attribute), requirement.attribute);
								text += " is ";
								text += $"{ComparisonDescription(requirement.comparison)} <b>{requirement.attributeValue}</b>";
								break;
							case RequirementTypes.TargetIsInRowInFrontOf:
								text += " if I'm in the row in front of ";
								text += UnitDefinitionDescription(requirement.targetIs, card);
                            break;
							case RequirementTypes.TargetHasAffectedUnitDefinition:
								text += " if I have performed an action that affects ";
								text += UnitDefinitionDescription(requirement.targetIs, card);
                            break;
							case RequirementTypes.TargetIsStunned:
								text += $" if I'm {TextFormat("stunned","statusEffect")}";
                            break;
							case RequirementTypes.TargetIsDisarmed:
								text += $" if I'm {TextFormat("disarmed","statusEffect")}";
								break;
							case RequirementTypes.TargetIsDisrupted:
								text += $" if I'm {TextFormat("disrupted","statusEffect")}";
								break;
							case RequirementTypes.TargetIsGuarding:
								text += $" if I'm {TextFormat("guarding", "guarding")}";
								break;
                    }
                    break;
					case TargetTypes.SameTarget:
					case TargetTypes.SingleEnemy:
					case TargetTypes.SingleAlly:
					
						switch (requirement.requirement)
						{
							case RequirementTypes.TargetHasSubtypesOrFactions:
								switch (formattingFor)
								{
									case "attack":
										text += ", but can only target ";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when targetting ";
										break;
									case "buff":
										text += " if they're ";
										break;
								}
								text += FactionOrSubtypeRequirementDescription(requirement);
								break;
							case RequirementTypes.TargetIsNextTo:
							case RequirementTypes.TargetIsInRowInFrontOf:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target must be ";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when target is ";
										break;
									case "buff":
										text += " if they're ";
										break;
								}
								switch (requirement.requirement)
								{
									case RequirementTypes.TargetIsNextTo:
										text += "next to ";
										break;
									case RequirementTypes.TargetIsInRowInFrontOf:
										text += "in the row in front of ";
										break;
								}
								text += UnitDefinitionDescription(requirement.targetIs, card);
								break;
							case RequirementTypes.TargetHasAttackedThisRound:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target must have had ";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when the target has ";
										break;
									case "buff":
										text += " if they have ";
										break;
								}
								text += "attacked before during this round";
								break;
							case RequirementTypes.TargetAttributeIs:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target's ";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when the target's ";
										break;
									case "buff":
										text += " if target's ";
										break;
								}
								text += TextFormat(BuffAttributeDescription(requirement.attribute), requirement.attribute);
								switch (formattingFor)
								{
									case "attack":
										text += " must be ";
										break;
									case "effectOrTempBuff":
									case "onHit":
									case "buff":
										text += " is ";
										break;
								}
								text += $"{ComparisonDescription(requirement.comparison)} <b>{requirement.attributeValue}</b>";
							break;
							case RequirementTypes.TargetHasAffectedUnitDefinition:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target must have had";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when the target has";
										break;
									case "buff":
										text += " if the target has";
										break;
								}
								text += " performed an action that affects ";
								text += UnitDefinitionDescription(requirement.targetIs, card);
							break;
							case RequirementTypes.TargetIsStunned:
							case RequirementTypes.TargetIsDisarmed:
							case RequirementTypes.TargetIsDisrupted:
							case RequirementTypes.TargetIsGuarding:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target must be";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when the target is";
										break;
									case "buff":
										text += " if the target is";
										break;
								}
								switch (requirement.requirement)
								{
									case RequirementTypes.TargetIsStunned:
										text += $" {TextFormat("stunned", "statusEffect")}";
									break;
									case RequirementTypes.TargetIsDisarmed:
										text += $" {TextFormat("disarmed", "statusEffect")}";
									break;
									case RequirementTypes.TargetIsDisrupted:
										text += $" {TextFormat("disrupted", "statusEffect")}";
									break;
									case RequirementTypes.TargetIsGuarding:
										text += $" {TextFormat("guarding", "guarding")}";
                                    break;
                            }
							break;
                    }
					break;
					case TargetTypes.LineOfEnemies:
					case TargetTypes.AlliesInSameLine:
					case TargetTypes.AllAllies:
					case TargetTypes.AllEnemies:
					case TargetTypes.AlliesNextToMe:
					case TargetTypes.AlliesInLineInFrontOfMe:
					case TargetTypes.AlliesInLineBehind:
						switch (requirement.requirement)
						{
							case RequirementTypes.TargetHasSubtypesOrFactions:
								switch (formattingFor)
								{
									case "attack":
										text += ", but only hits ";
										break;
									case "effectOrTempBuff":
										text += " when they're ";
										break;
									case "buff":
										text += " who are ";
										break;
									case "onHit":
										text += " when targetting ";
										break;
								}
								text += FactionOrSubtypeRequirementDescription(requirement);
								break;
							case RequirementTypes.TargetIsNextTo:
							case RequirementTypes.TargetIsInRowInFrontOf:
								switch (formattingFor)
								{
									case "attack":
										text += ", but only hits if they're ";
										break;
									case "effectOrTempBuff":
										text += " when they're ";
										break;
									case "buff":
										text += " who are ";
										break;
									case "onHit":
										text += " when the target is ";
										break;
								}
								switch (requirement.requirement)
								{
									case RequirementTypes.TargetIsNextTo:
										text += "next to ";
										break;
									case RequirementTypes.TargetIsInRowInFrontOf:
										text += "in the row in front of ";
										break;
								}
								text += UnitDefinitionDescription(requirement.targetIs, card);
                            break;
							case RequirementTypes.TargetHasAttackedThisRound:
								switch (formattingFor)
								{
									case "attack":
										text += ", but hits only those who have ";
										break;
									case "effectOrTempBuff":
										text += " who have ";
										break;
									case "buff":
										text += " who have ";
										break;
									case "onHit":
										text += " when the target has ";
										break;
								}
								text += "attacked before during this round";
								break;
							case RequirementTypes.TargetAttributeIs:
								switch (formattingFor)
								{
									case "attack":
										text += ", but hits only those whose ";
										break;
									case "effectOrTempBuff":
									case "buff":
										text += " whose ";
										break;
									case "onHit":
										text += " when target's ";
										break;
								}
								text += TextFormat(BuffAttributeDescription(requirement.attribute), requirement.attribute);
								text += " is ";
								text += $"{ComparisonDescription(requirement.comparison)} <b>{requirement.attributeValue}</b>";
								break;
							case RequirementTypes.TargetHasAffectedUnitDefinition:
								switch (formattingFor)
								{
									case "attack":
										text += ", but hits only those who have";
										break;
									case "effectOrTempBuff":
									case "onHit":
									case "buff":
										text += " when they have";
										break;
								}
								text += " performed an action that affects ";
								text += UnitDefinitionDescription(requirement.targetIs, card);
								break;
							case RequirementTypes.TargetIsStunned:
							case RequirementTypes.TargetIsDisarmed:
							case RequirementTypes.TargetIsDisrupted:
							case RequirementTypes.TargetIsGuarding:
								switch (formattingFor)
								{
									case "attack":
										text += ", but target must be";
										break;
									case "effectOrTempBuff":
									case "onHit":
										text += " when they're";
										break;
									case "buff":
										text += " if they're";
										break;
								}
								switch (requirement.requirement)
								{
									case RequirementTypes.TargetIsStunned:
										text += $" {TextFormat("stunned", "statusEffect")}";
										break;
									case RequirementTypes.TargetIsDisarmed:
										text += $" {TextFormat("disarmed", "statusEffect")}";
										break;
									case RequirementTypes.TargetIsDisrupted:
										text += $" {TextFormat("disrupted", "statusEffect")}";
										break;
									case RequirementTypes.TargetIsGuarding:
										text += $" {TextFormat("guarding", "guarding")}";
										break;
								}
								break;
						}
					break;
				}
		}
		return text;
	}

	public static string DamageTypeDescription(DamageTypes damageType)
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

	public static string BuffAttributeDescription(Attributes attribute)
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
            case Attributes.Cost:
                text += "cost";
            break;
            case Attributes.DamageMultiplier:
                text += "damage";
            break;
        }
		return text;
	}

	public static string TargetTypeDescription(TargetTypes targetType)
	{
		string text = "";
		switch (targetType){
			case TargetTypes.Self:
				text += "me";
			break;
			case TargetTypes.AlliesInSameLine:
				text += "all allies in the same row";
			break;
			case TargetTypes.SingleEnemy:
				text += "an enemy";
			break;
			case TargetTypes.LineOfEnemies:
				text += "the front-most row of enemies";
			break;
			case TargetTypes.AllAllies:
				text += "all allies";
			break;
			case TargetTypes.SameTarget:
				text += "";
			break;
			case TargetTypes.AlliesNextToMe:
				text += "allies next to me";
			break;
            case TargetTypes.SingleAlly:
                text += "an ally";
            break;
            case TargetTypes.MostHarmedAlly:
                text += "the most harmed ally <i>(prioritizing the front row)</i>";
            break;
            case TargetTypes.AlliesInLineInFrontOfMe:
                text += "allies in the line in front of me";
            break;
            case TargetTypes.AllEnemies:
				text += "all enemies";
			break;
            case TargetTypes.AlliesInLineBehind:
                text += "allies in the line behind me";
            break;
        }
		return text;
	}

	public static string TargetFactionDescription(Faction faction, bool plural = false)
	{
		string text = "";
		switch (faction)
		{
			case Faction.Protectors:
				if (!plural) { text += "Protector"; } else { text += "Protectors"; }
				break;
			case Faction.Saggists:
				if (!plural) { text += "Saggist"; } else { text += "Saggists"; }
				break;
			case Faction.Keraneans:
				if (!plural) { text += "Keranean"; } else { text += "Keraneans"; }
				break;
			case Faction.Voucari:
				if (!plural) { text += "Voucarian"; } else { text += "Voucarians"; }
				break;
			case Faction.Auro:
				if (!plural) { text += "Auroran"; } else { text += "Aurorans"; }
				break;
			case Faction.Independent:
				if (!plural) { text += "Independent unit"; } else { text += "Independent units"; }
				break;
			case Faction.Fennraign:
				if (!plural) { text += "Fennraigner"; } else { text += "Fennraigners"; }
				break;
			case Faction.Zikin:
				if (!plural) { text += "Zikinite"; } else { text += "Zikinites"; }
				break;
			case Faction.Tekvault:
				if (!plural) { text += "Tekvault associate"; } else { text += "Tekvault associates"; }
				break;
			default:
				if (!plural) { text += "extradimensional being"; } else { text += "extradimensional beings"; }
				break;
		}
		return text;
	}

	public static string TargetSubtypeDescription(UnitSubtype subtype, bool plural = false)
	{
		string text = "";
		switch (subtype)
		{
			case UnitSubtype.Defender:
				if(!plural){ text += "⚓ defender"; }else{ text += "⚓ defenders"; }
			break;
			case UnitSubtype.Mercenary:
				if(!plural){ text += "🏴 mercenary"; }else{ text += "🏴 mercenaries"; }
			break;
			case UnitSubtype.Pacifist:
				if(!plural){ text += "🕊 pacifist"; }else{ text += "🕊 pacifists"; }
			break;
			case UnitSubtype.Combo:
				text += "⛓ combo";
			break;
			case UnitSubtype.Executioner:
				if(!plural){ text += "💀 executioner"; }else{ text += "💀 executioners"; }
			break;
			case UnitSubtype.Noble:
				if(!plural){ text += "👑 noble"; }else{ text += "👑 nobles"; }
			break;
			case UnitSubtype.Solitary:
				if(!plural){ text += "🕯 solitary"; }else{ text += "🕯 solitaries"; }
			break;
			case UnitSubtype.Inheritor:
				if(!plural){ text += "🧬 inheritor"; }else{ text += "🧬 inheritors"; }
			break;
			case UnitSubtype.Opportunist:
				if (!plural) { text += "📜 opportunist"; } else { text += "📜 opportunists"; }
			break;
			case UnitSubtype.Doragon:
				if (!plural) { text += "🌌 Yatza"; } else { text += "🌌 Yatza"; }
			break;
			case UnitSubtype.Yatza:
				if (!plural) { text += "⭐ Doragon"; } else { text += "⭐ Doragon"; }
			break;
		}
		return text;
	}

	public static string AttackEffectDescription(AttackEffect effect)
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
						text += AttributeAndValue(buff);
						switch (buff.target)
						{
							case TargetTypes.SameTarget: text += " from the target"; break;
						}
					} else {
						if(effect.buffs.Count-1 == index){
							text += " and ";
							text += AttributeAndValue(buff);
							text += " ";
						} else {
							text += ", ";
							text += AttributeAndValue(buff);
						}
					}
					index += 1;
				}
			break;
            case AttackEffects.ApplyBuff:
				text += GenerateSkillBuffText(effect.buffs, null, false);
            break;
            case AttackEffects.Execute:
                text += $"{TextFormat("executes",Attributes.Attack)}{RequirementDescription(effect.requirements, effect.originAttack?.target ?? TargetTypes.SingleEnemy, effect.originAttack?.source?.card, "onHit")}{(effect.effectChecksAfterAttack? " after the attack": "")}";
			break;
        }
		return text;
	}

	public static string BuffEffectDescription(BuffAction buffAction)
	{
		string text = "";
		switch (buffAction.specialEffect){
			case BuffSpecialEffects.RedirectAttacksTowardsMe:
				text += "I absorb attacks targetted towards ";
				text += TargetTypeDescription(buffAction.target);
			break;
		}
		return text;
	}

	public static string AppliedBuffDescription(List<BuffAction> buffs)
	{
		string text = "";

		for (int i = 0; i < buffs.Count; i++)
		{
			BuffAction buff = buffs[i];
			if(buff.specialEffect != BuffSpecialEffects.RedirectAttacksTowardsMe && buff.specialEffect != BuffSpecialEffects.Stun && buff.specialEffect != BuffSpecialEffects.Disarm && buff.specialEffect != BuffSpecialEffects.Disrupt)
            {
				if (buff.activatesOnHit)
				{
					text += "<b>💥 On hit</b>: ";
				}
				if(buff.specialEffect == BuffSpecialEffects.GrantSubtypes)
				{
					text += "I have acquired ";
                    for (int j = 0; j < buff.grantedSubtypes.Count; j++)
                    {
                        text += TextFormat(TargetSubtypeDescription(buff.grantedSubtypes[j], true), "subtype");

                        if (j < buff.grantedSubtypes.Count - 2)
                        {
                            text += ", ";
                        }
                        else if (j == buff.grantedSubtypes.Count - 2)
                        {
                            text += " and ";
                        }
                    }
                } else if (buff.specialEffect == BuffSpecialEffects.TriggerExtraAttack)
                {
                    text += "Perform ";
					if(buff.extraAttacks.Count > 0)
					{
						text += "a ";
					}
					text += GenerateAttacksDescription(buff.extraAttacks);
					if(buff.extraAttacks.Count == 1)
					{
						text += " as an extra attack";
					} else
					{
						text += " as extra attacks ";
					}
                } else if (buff.specialEffect == BuffSpecialEffects.AllowGuardingPoseRespondToRangedAttacks)
                {
                    text += $"When {TextFormat("guarding", "guarding")}, I can {TextFormat("fight back", Attributes.Attack)} {TextFormat("against ranged attacks", Attributes.Defense)}, ";
                }
                else if (buff.specialEffect == BuffSpecialEffects.GrantAttackEffect)
                {
					foreach (AttackEffect attackEffect in buff.attackEffect)
					{
						switch (attackEffect.effectType)
						{
							case AttackEffects.SplashDamage:
                                text += TextFormat(NumberWithSign(attackEffect.value), null, attackEffect.valueCanBeAugmented);
								text += " splash damage";
                                break;
							case AttackEffects.SelfDamage:
                                text += TextFormat(NumberWithSign(attackEffect.value), null, attackEffect.valueCanBeAugmented);
								text += TextFormat(" self damage", "self-damage");
                                break;
							case AttackEffects.ApplyDebuff:
							case AttackEffects.Execute:
							default:
								break;
						}
					}
                }
                else {
					text += AttributeAndValue(buff);
				}
				if (buff.activatesOnHit)
				{
					text += RequirementDescription(buff.onHitRequirements, buff.target, buff.source?.card, "onHit")+",";
				}
				text += " from ";
				if (buff.originPassive.title == "")
				{
					if(buff.source == buff.receiver)
					{
						text += "myself";
					} else {
						text += $"<b>{buff.source?.card.Name ?? buff.originAttack?.source?.card.Name}</b>";
					}
				} else {
					if (buff.source == buff.receiver)
					{
						text += $"my passive <b>{buff.originPassive.title}</b>";
					}
					else
					{
						text += $"<b>{buff.source?.card.Name}</b>'s <b>{buff.originPassive?.title}</b>";
					}
				}
			} else {
				text += BuffEffectDescriptionAsTarget(buff);
			}

			text += "\n";
		}
		return text;
	}

	public static string BuffEffectDescriptionAsTarget(BuffAction buffAction)
	{
		string text = "";
		switch (buffAction.specialEffect){
			case BuffSpecialEffects.RedirectAttacksTowardsMe:
				text += "Redirect all attacks I receive towards <b>"+buffAction.source.card.Name+"</b>";
			break;
			case BuffSpecialEffects.Stun:
				text += $"{TextFormat("Stunned","statusEffect")} by {buffAction.source.card.Name}";
			break;
            case BuffSpecialEffects.Disarm:
                text += $"{TextFormat("Disarmed", "statusEffect")} by {buffAction.source.card.Name}";
            break;
            case BuffSpecialEffects.Disrupt:
                text += $"{TextFormat("Disrupted", "statusEffect")} by {buffAction.source.card.Name}";
            break;
        }
		return text;
	}

	public static string ComparisonDescription(Comparison comparison)
	{
		string text = "";
		switch (comparison)
		{
			case Comparison.LessThan:
				text += "less than";
				break;
			case Comparison.LessThanOrEqual:
				text += "less than or equal to";
				break;
			case Comparison.Equal:
				text += "equal to";
				break;
			case Comparison.MoreThan:
				text += "more than";
				break;
			case Comparison.MoreThanOrEqual:
				text += "more than or equal to";
				break;
			case Comparison.Not:
				text += "not";
				break;
		}
		return text;
	}

	public static string UnitDefinitionDescription(List<TargetUnitDefinition> unitDefinitions, Card card = null)
	{
        string text = "";
		for (int i = 0; i < unitDefinitions.Count; i++)
		{
            text += UnitDefinitionDescription(unitDefinitions[i], card);
            if (i < unitDefinitions.Count - 2)
            {
                text += ", ";
            }
            else if (i == unitDefinitions.Count - 2)
            {
                text += " or ";
            }
        }
		return text;
    }
    public static string UnitDefinitionDescription(TargetUnitDefinition unitDefinition, Card card = null)
	{
        string text = "";
        switch (unitDefinition)
        {
            case TargetUnitDefinition.SameAsMyself:
                text += $" any <b>{(card?.Name ?? "card of my kind")}</b>";
                break;
            case TargetUnitDefinition.TheLeader:
                text += $" the {TextFormat("leader", "subtype")}";
                break;
            case TargetUnitDefinition.BenefitedYatzasAndDoragons:
                text += $" benefited ";
				if (card != null && ( card.Subtypes.Contains(UnitSubtype.Yatza) || card.Subtypes.Contains(UnitSubtype.Doragon) ) ) {
					if (card.Subtypes.Contains(UnitSubtype.Yatza))
						text += $"{TextFormat("Doragon", "subtype")}";
					if (card.Subtypes.Contains(UnitSubtype.Doragon))
						text += $"{TextFormat("Yatza", "subtype")}";
                } else
				{
					text += $"{TextFormat("Doragon", "subtype")} or {TextFormat("Yatza", "subtype")}";
				}
                break;
        }
        return text;
    }


    public static string GeneratePlayerSkillBuffText(List<PlayerBuffs> playerBuffs, PassiveSkill passiveSkill = null)
	{
		string text = "";
        foreach (PlayerBuffs pBuff in playerBuffs)
        {
			switch (pBuff.buffType)
			{
				case PlayerBuffTypes.AddGold:
					text += $"Add {TextFormat(NumberWithSign(pBuff.amount),null,pBuff.canBeAugmented)} {TextFormat("gold","gold")} to your ";
					if (pBuff.target == PlayerTarget.OtherPlayer)
						text += "opponent's ";
					text += "treasury";
					break;
				case PlayerBuffTypes.RemoveGold:
                    text += $"Remove {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} {TextFormat("gold", "gold")} from your ";
                    if (pBuff.target == PlayerTarget.OtherPlayer)
                        text += "opponent's ";
                    text += "treasury";
                    break;
				case PlayerBuffTypes.StealGold:
					if (pBuff.target == PlayerTarget.OtherPlayer) { text += $"Steal {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} {TextFormat("gold", "gold")} from your opponnent's treasury"; }
					else { text += $"Donate {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} {TextFormat("gold", "gold")} to your opponnent's treasury"; }
                    break;
				case PlayerBuffTypes.ExecutionerThresholdModifier:
                    if (pBuff.target == PlayerTarget.OtherPlayer) { text += "Opponent's "; } else { text += "Your "; }
                    text += $"{TextFormat("executors","subtype")} receive {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} to their {TextFormat("execution threshold",Attributes.Attack)}";
					break;
				case PlayerBuffTypes.MercenaryKillGoldReward:
                    if (pBuff.target == PlayerTarget.OtherPlayer) { text += "Opponent's "; } else { text += "Your "; }
                    text += $"{TextFormat("mercenaries", "subtype")} gain {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} {TextFormat("gold", "gold")} ";
                    if (pBuff.target == PlayerTarget.OtherPlayer) { text += " for their treasury "; } else { text += " for your treasury "; }
					text += "when they score a kill";
                    break;
                case PlayerBuffTypes.FreeAttackActions:
                    if (pBuff.target == PlayerTarget.OtherPlayer) { text += "Opponent's "; } else { text += "Your "; }
                    text += $"cards gain {TextFormat(NumberWithSign(pBuff.amount), null, pBuff.canBeAugmented)} {TextFormat("free attack action"+(pBuff.amount>1?"s":""),"actionPoints")} which also {(pBuff.amount > 1 ? "don't use their" : "doesn't use its")} dice";
                    break;
            }
		}
		if(passiveSkill != null)
		{
			switch (passiveSkill.trigger)
			{
				case TriggerTypes.OnAssistingAKill:
					text += ", when I assist on a kill";
					break;
				case TriggerTypes.OnScoringAKill:
                    text += ", when I score a kill";
                    break;
			}
		}
        return text;
	}

}

[System.Serializable]
public class CardActionObject : CardSkillObject
{
	public CardAction action;
	public bool canBeUsed = false;

	public CardActionObject(CardAction theSkill, CardDisplay theCard) : base(theCard)
	{
		isAction = true;
		action = theSkill;
		sourceCard = theCard;
		description = TranslateActionToText();
	}

	public bool HasMatchingDice()
	{
		return sourceCard?.mySpace?.Owner.HasDiceForAction(this) ?? false;
	}

	public string TranslateActionToText(){
		string text = "";

		switch (action.actionType)
		{
			case ActionTypes.Attack:
				text += CardTranslator.GenerateSkillAttackText(action);
			break;
			case ActionTypes.Buff:
				text += CardTranslator.GenerateSkillBuffText(action.buffs);
			break;
            case ActionTypes.PlayerBuff:
                text += CardTranslator.GeneratePlayerSkillBuffText(action.playerBuffs);
                break;
            case ActionTypes.DoNothing:
				text += "Do nothing";
				break;

		}

		text += ".";

		return text;
	}
}

public class CardPassiveSkillObject : CardSkillObject
{
	public PassiveSkill skill;

	public CardPassiveSkillObject(PassiveSkill theSkill, CardDisplay theCard) : base(theCard)
	{
		skill = theSkill;
		List<BuffAction> buffs = new List<BuffAction>();
		foreach (BuffAction buff in skill.buffs)
		{
			buffs.Add(new BuffAction(buff){ source = theCard });
		}
		skill.buffs = buffs;
		sourceCard = theCard;
		description = TranslatePassiveSkillsToText();
	}
	public string TranslatePassiveSkillsToText()
	{
		string text = "";
		if (skill.oncePerRound) { text += "❶"; }
		if (skill.canBeShared) { text += "🔗"; }
		if (skill.requiresElementalExchange) { text += "💫"; }
		if (skill.oncePerRound || skill.canBeShared || skill.requiresElementalExchange) { text += " "; }
		text += $"<b>{skill.title}</b>";
		if(skill.buffs.Count > 0) { text += $": {CardTranslator.GenerateSkillBuffText(skill.buffs, skill)}"; }
		if(skill.playerBuffs.Count > 0) { text += $": {CardTranslator.GeneratePlayerSkillBuffText(skill.playerBuffs, skill)}"; }
        if (skill.oncePerRound && !skill.sharedAcrossAllCardsOfSameKind) { text += " <i>(once per round)</i>"; }
        if (skill.oncePerRound && skill.sharedAcrossAllCardsOfSameKind) { text += $" <i>(once per round for all <b>{sourceCard?.card.Name ?? "cards of my kind"}</b>)</i>"; }
        text += ".";

        return text;
	}
}