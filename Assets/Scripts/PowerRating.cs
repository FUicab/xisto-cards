using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.UI.Image;

[Serializable]
public class PowerRating
{
	private readonly float HP_scaling = 1f;
	private readonly float[] armor_scaling = { 1.2f, 0.9f, 2f };
	private readonly float[] dicePowerMultipliers_scaling = { 1f, 0.95f, 0.9f, 0.85f, 0.8f, 0.75f };
	private readonly float augmentationBonus_scaling = 0.1f;


	public Card card;
	public float HPBonus { get { return card.MaxHP * HP_scaling; } }
	public float[] armorBonus { get { return new float[]{ card.Armor[0] * armor_scaling[0], card.Armor[1] * armor_scaling[1], card.Armor[2] * armor_scaling[2] }; } }
	public float baseBonus { get { return HPBonus + armorBonus.Sum() / 3; } }
	public float subTypeBonus{
		get
		{
			float theBonus = 0f;
			foreach (UnitSubtype subtype in card.Subtypes)
			{
				theBonus += GetSubtypeBonus(subtype);
			}
			return theBonus;
		}
	}
	//public float[] dicePowerMultipliers = { 1f, 0.95f, 0.9f, 0.85f, 0.8f, 0.75f };
	public List<float> bonusPerAction = new();
	public float actionOutputBonus { get { return bonusPerAction.Sum(); } }
	public List<float> bonusPerPassive = new();
	public float passiveBonus { get { return bonusPerPassive.Sum(); } }
	public float total { get { return baseBonus + actionOutputBonus + passiveBonus + subTypeBonus; } }
	public string passiveDescription = "";
	public string passiveBuffDescriptions = "";
	public string actionsDescription = "";

	public PowerRating(Card card)
	{
		this.card = card;
		SetActionBonus();
		SetPassiveBonus();
		List<BuffAction> applicableBuffs = new List<BuffAction>();
        foreach (List<BuffAction> buffs in card.Passives.Select(x => x.buffs).ToList())
        {
			applicableBuffs.AddRange(buffs);
        }
        passiveBuffDescriptions = CardTranslator.AppliedBuffDescription(applicableBuffs);
	}

	public void SetActionBonus()
	{
		actionsDescription = "";
		CardActionMenu actionMenu = new CardActionMenu(card.CardActions);
		foreach (CardActionObject actionObj in actionMenu.actions)
		{
			float actionPower = 0f;
			float dicePower = 0f;
			for (int i = 0; i < actionObj.diceValues.Count; i++)
			{
				dicePower += dicePowerMultipliers_scaling[actionObj.diceValues[i] - 1];
			}

			if (actionObj.action.actionType != ActionTypes.DoNothing)
			{
				switch (actionObj.action.actionType)
				{
					case ActionTypes.Attack:
						actionPower += GetPowerOfAttackAction(actionObj);
						break;
					case ActionTypes.Buff:
					case ActionTypes.ApplyDebuff:
						actionPower += GetBuffPowerBonus(actionObj.action.buffs);
						break;
				}
			}
			bonusPerAction.Add((actionPower * dicePower) / 6);
			actionsDescription += actionObj.description+"\n";
		}
	}

	public float GetSubtypeBonus(UnitSubtype subtype)
	{
		float bonus = 0f;

        switch (subtype)
        {
            case UnitSubtype.Defender:
                bonus += baseBonus / 2;
                break;
            case UnitSubtype.Dual:
                bonus += card.Attack * 1.25f; // This unit subtype is no longer at use but we'll see what this could be used for
                break;
            case UnitSubtype.Mercenary:
                bonus += card.Attack * 0.25f;
                break;
            case UnitSubtype.Assistant:
                bonus += 1; // This unit subtype is no longer at use but we'll see what this could be used for
                break;
            case UnitSubtype.Pacifist:
                bonus -= card.Attack * 1.25f;
                break;
            case UnitSubtype.Combo:
                bonus += card.Attack;
                break;
            case UnitSubtype.Executioner:
                bonus += card.Attack;
                break;
            case UnitSubtype.Noble:
                bonus += 2;
                break;
            case UnitSubtype.Solitary:
                bonus += 1.5f;
                break;
            case UnitSubtype.Inheritor:
                bonus += (armorBonus.Sum() + HPBonus) * 0.66f;
                break;
            case UnitSubtype.Opportunist:
                bonus += (armorBonus.Sum() + HPBonus) * (0.15f * card.Origin.Count);
                break;
            case UnitSubtype.Yatza:
            case UnitSubtype.Doragon:
                bonus += (baseBonus + card.Attack) / 2;
                break;
        }

        return bonus;
	}

	public void SetPassiveBonus()
	{
		passiveDescription = "";
		foreach (PassiveSkill passiveSkill in card.Passives)
		{
			float passivePower = GetBuffPowerBonus(passiveSkill.buffs);
			if (passiveSkill.canBeShared) { passivePower *= 1.05f; }
			if (passiveSkill.oncePerTurn) { passivePower *= 0.66f; }
			if (passiveSkill.requiresElementalExchange) { passivePower *= 0.66f; }
			bonusPerPassive.Add(passivePower);
			CardPassiveSkillObject cardPassiveSkillObject = new CardPassiveSkillObject(passiveSkill, null);
			passiveDescription += cardPassiveSkillObject.description+"\n";
		}
	}

	public float GetAttackTypeBonus(DamageTypes damageType)
	{
		float attackTypeMultiplier = 0.6f;
		switch (damageType)
		{
			case DamageTypes.Melee:
				attackTypeMultiplier += 0.125f;
				break;
			case DamageTypes.Ranged:
				attackTypeMultiplier += 0.25f;
				break;
			case DamageTypes.Energy:
				attackTypeMultiplier += 0.5f;
				break;
			case DamageTypes.MeleeOrRanged:
				attackTypeMultiplier += 0.3f;
				break;
			case DamageTypes.RangedOrEnergy:
				attackTypeMultiplier += 0.6f;
				break;
			case DamageTypes.MeleeOrEnergy:
				attackTypeMultiplier += 0.55f;
				break;
			case DamageTypes.MeleeOrRangedOrEnergy:
				attackTypeMultiplier += 0.7f;
				break;
		}
		return attackTypeMultiplier;
	}
	public float GetRequirementsNerf(List<Requirements> requirements)
	{
		float requirementsPowerNerf = 1f;
		if (requirements.Count > 0)
		{
			requirementsPowerNerf /= requirements.Count + 1;
			foreach (Requirements requirement in requirements)
			{
				if (requirement.factionRequirement.Count > 1)
				{
					requirementsPowerNerf *= 1f + ((requirement.factionRequirement.Count - 1) * 0.1f);
				}
				if (requirement.subtypeRequirement.Count > 1)
				{
					requirementsPowerNerf *= 1f + ((requirement.subtypeRequirement.Count - 1) * 0.1f);
				}
				if (requirement.targetIs.Count > 1)
				{
					requirementsPowerNerf *= 1f + ((requirement.targetIs.Count - 1) * 0.1f);
				}
			}
		}
		return requirementsPowerNerf;
	}

	public float GetTargetTypeBonus(TargetTypes targetType)
	{
		float bonus = 1f;
		switch (targetType)
		{
			case TargetTypes.Self:
			case TargetTypes.SingleAlly:
			case TargetTypes.SingleEnemy:
			case TargetTypes.SameTarget:
				bonus *= 1;
				break;
			case TargetTypes.AlliesInSameLine:
				bonus *= 4;
				break;
			case TargetTypes.LineOfEnemies:
				bonus *= 4;
				break;
			case TargetTypes.AllEnemies:
			case TargetTypes.AllAllies:
				bonus *= 8;
				break;
			case TargetTypes.AlliesNextToMe:
				bonus *= 1.5f;
				break;
		}
		return bonus;
	}
	public float GetBuffPowerBonus(BuffAction buff)
	{
		float augmentationBonus = 1f;
		float buffPower = 0f;
		augmentationBonus *= GetTargetTypeBonus(buff.target);
		if (buff.amountCanBeAugmented) { augmentationBonus *= 1.1f; }
		switch (buff.Attribute)
		{
			case Attributes.Attack:
			case Attributes.ArmorPierce:
			case Attributes.Health:
			case Attributes.Defense:
				buffPower += buff.amount * augmentationBonus;
				break;
			case Attributes.DefenseMelee:
				buffPower += (buff.amount * armor_scaling[0] * augmentationBonus) / 3;
				break;
			case Attributes.DefenseRanged:
				buffPower += (buff.amount * armor_scaling[1] * augmentationBonus) / 3;
				break;
			case Attributes.DefenseEnergy:
				buffPower += (buff.amount * armor_scaling[2] * augmentationBonus) / 3;
				break;
			case Attributes.DamageReductionBeforeArmor:
			case Attributes.DamageReductionAfterArmor:
			case Attributes.MaxHealth:
				buffPower += (buff.amount * augmentationBonus) * 1.25f;
				break;
		}
		switch (buff.specialEffect)
		{
			case BuffSpecialEffects.RedirectAttacksTowardsMe:
				buffPower += baseBonus / 2;
				break;
			case BuffSpecialEffects.GrantSubtypes:
                foreach (UnitSubtype subtype in buff.grantedSubtypes)
                {
					buffPower += GetSubtypeBonus(subtype);
                }
                break;
			case BuffSpecialEffects.TriggerExtraAttack:
				buffPower += GetPowerOfAttacks(buff.extraAttacks);
				break;
			case BuffSpecialEffects.EnableGuardingPose:
				buffPower += (baseBonus / 2) + card.Attack;
				break;
		}
		if (!buff.targetIsFromMyTeam) { buffPower *= -1f; }
		float temporaryBuffRequirementNerf = GetRequirementsNerf(buff.requirements);
		float onHitBuffRequirementNerf = GetRequirementsNerf(buff.onHitRequirements);
		return buffPower * temporaryBuffRequirementNerf * onHitBuffRequirementNerf;
	}
	public float GetBuffPowerBonus(List<BuffAction> buffs)
	{
		float powerBonus = 0f;
		foreach (BuffAction buff in buffs)
		{
			powerBonus += GetBuffPowerBonus(buff);
		}
		return powerBonus;
	}

	public float GetAttackEffectPowerBonus(AttackEffect attackEffect)
	{
		float baseValue = 0f;
		float effectPower = 0f;
		if (attackEffect.useAttackValue) { baseValue = card.Attack; } else { baseValue = attackEffect.value; }
		switch (attackEffect.effectType)
		{
			case AttackEffects.SplashDamage:
				effectPower += baseValue * 1.5f;
				break;
			case AttackEffects.SelfDamage:
				effectPower += baseValue * -1;
				break;
			case AttackEffects.ApplyDebuff:
				effectPower += GetBuffPowerBonus(attackEffect.buffs);
				break;
		}
		float attackEffectRequirementNerf = GetRequirementsNerf(attackEffect.requirements);
		return effectPower * attackEffectRequirementNerf;
	}
	public float GetAttackEffectPowerBonus(List<AttackEffect> attackEffects)
	{
		float powerBonus = 0f;
		foreach (AttackEffect attackEffect in attackEffects)
		{
			powerBonus += GetAttackEffectPowerBonus(attackEffect);
		}
		return powerBonus;
	}

	public float GetPowerOfAttackAction(CardActionObject actionObject)
	{
		float actionPower = 0f;
		float augmentationBonus = 1f;
		if (actionObject.action.attackCountCanBeAugmented)
		{
			augmentationBonus += card.Attack * augmentationBonus_scaling;
		}
		actionPower += GetPowerOfAttacks(actionObject.action.attacks, augmentationBonus);
		return actionPower;
	}

	public float GetPowerOfAttacks(List<AttackAction> attacks, float augmentationBonus = 1f)
	{
		float powerBonus = 0f;
        foreach (AttackAction attack in attacks)
        {
            float damagePower = card.Attack * attack.damageMultiplier;
            float attackTypeMultiplier = GetAttackTypeBonus(attack.damageType);
            float requirementsPowerNerf = GetRequirementsNerf(attack.requirements);
            powerBonus += GetBuffPowerBonus(attack.temporaryBuffs);
            powerBonus += GetAttackEffectPowerBonus(attack.attackEffect);
            powerBonus += damagePower * attackTypeMultiplier * requirementsPowerNerf * augmentationBonus;
        }
		return powerBonus;
    }
}