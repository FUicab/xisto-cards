using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public static class CardActionTools
{

	public static CardDisplay GetActualTarget(CardDisplay target)
	{
		CardDisplay actualTarget;
		if (target.ProtectedByDefender)
		{
			actualTarget = target.mySpace.Defenders[0].PlayingCard;
		}
		else
		{
			if (target.attackSponge != null)
			{
				actualTarget = target.attackSponge;
			}
			else
			{
				actualTarget = target;
			}
		}
		return actualTarget;
	}

	public static void PerformAttackAction(CardDisplay target, TurnAction ActionData, int i = 0)
	{
		AttackAction attack = ActionData.actionObject.action.attacks[i];
		GetActualTarget(target).ReceiveDamage(CalculateDamage(GetActualTarget(target), attack));

		foreach (AttackEffect effect in attack.attackEffect)
		{
			switch (effect.effectType)
			{
				case AttackEffects.SplashDamage:
					List<CardDisplay> affectedTargets = new List<CardDisplay>();
					AttackAction splashAttack = new AttackAction(attack)
					{
						requirements = new List<Requirements>(),
						attackEffect = new List<AttackEffect>(),
					};
					if (!effect.useAttackValue)
					{
						splashAttack.flatDamageOverwrite = effect.value;
					}
					int myIndex = target.mySpace.myIndexInRow;
					if (myIndex != 0)
					{
						affectedTargets.Add(target.mySpace.myRow.BoardSpaces[myIndex - 1].PlayingCard);
					}
					if (myIndex < target.mySpace.myRow.BoardSpaces.Count - 1)
					{
						affectedTargets.Add(target.mySpace.myRow.BoardSpaces[myIndex + 1].PlayingCard);
					}

					foreach (CardDisplay affectedTarget in affectedTargets)
					{
						if (affectedTarget != null)
						{
							GetActualTarget(affectedTarget).ReceiveDamage(CalculateDamage(GetActualTarget(affectedTarget), splashAttack));
						}
					}
					break;
				case AttackEffects.SelfDamage:
					AttackAction selfAttack = new AttackAction(attack)
					{
						requirements = new List<Requirements>(),
						attackEffect = new List<AttackEffect>(),
						damageType = DamageTypes.SelfDamage
					};
					if (!effect.useAttackValue)
					{
						selfAttack.flatDamageOverwrite = effect.value;
					}
					attack.source.ReceiveDamage(CalculateDamage(attack.source, selfAttack));
					break;
				case AttackEffects.ApplyDebuff:
					foreach (BuffAction debuff in effect.buffs)
					{
						BuffAction debuffAction = new BuffAction(debuff)
						{
							requirements = new List<Requirements>()
						};
						GetActualTarget(target).ReceiveActiveBuff(debuffAction);
					}
					break;
			}
		}
	}

	public static void PerformConfirmedAction(TurnAction ActionData)
	{
		if (ActionData.CardInAction != null)
		{
			ActionData.CardInAction.SetOutline();
			ActionData.CardInAction.SetLine();
		}
		switch (ActionData.actionObject.action.actionType)
		{
			case ActionTypes.Attack:
				for (int i = 0; i < ActionData.targets.Count; i++)
				{
					if (ActionData.targets[i] != null)
					{
						ActionData.targets[i].SetOutline();
					}
					if (ActionData.targets[i] != null && ActionData.CardInAction != null)
					{
						PerformAttackAction(ActionData.targets[i], ActionData, i);
					}
				}
				break;
			case ActionTypes.Buff:
				for (int i = 0; i < ActionData.targets.Count; i++)
				{
					BuffAction buff = ActionData.actionObject.action.buffs[i];
					List<CardDisplay> targets = new();
					if (buff.isTargetImplicit) {
						targets.AddRange(buff.GetImplicitTargetsOfAction().Where(x => buff.TargetMeetsRequirements(x)));
					} else if (ActionData.targets[i] != null) {
						if (buff.TargetMeetsRequirements(ActionData.targets[i])) { targets.Add(ActionData.targets[i]); }
                    }
                    foreach (CardDisplay cardDisplay in targets)
                    {
						cardDisplay.ReceiveActiveBuff(buff);
                    }
				}
				break;
		}
	}

	public static List<CardDisplay> GetImplicitTargetsOfAction(ActiveAction action)
	{
		if (!action.isTargetImplicit) {  return null; }
		List<CardDisplay> targets = new();
        switch (action.target)
        {
			case TargetTypes.Self:
                if(action.TargetMeetsRequirements(action.source)) targets.Add(action.source);
                break;
            case TargetTypes.AllAllies:
                foreach (BoardRow row in action.source.mySpace.Owner.MyBoardRows)
                {
                    foreach (CardSpace cardSpace in row.BoardSpaces.Where(x => x.PlayingCard != null && action.TargetMeetsRequirements(x.PlayingCard)))
                    {
						targets.Add(cardSpace.PlayingCard);
                    }
                }
                break;
            case TargetTypes.AllEnemies:
                foreach (BoardRow row in action.source.mySpace.Owner.otherPlayer.MyBoardRows)
                {
                    foreach (CardSpace cardSpace in row.BoardSpaces.Where(x => x.PlayingCard != null && action.TargetMeetsRequirements(x.PlayingCard)))
                    {
                        targets.Add(cardSpace.PlayingCard);
                    }
                }
                break;
            case TargetTypes.AlliesInSameLine:
                foreach (CardSpace cardSpace in action.source.mySpace.myRow.BoardSpaces.Where(x => x.PlayingCard != null && action.TargetMeetsRequirements(x.PlayingCard)))
                {
                    targets.Add(cardSpace.PlayingCard);
                }
                break;
            case TargetTypes.AlliesNextToMe:
                foreach (CardSpace cardSpace in action.source.mySpace.SpacesNextToMe().Where(x => x.PlayingCard != null && action.TargetMeetsRequirements(x.PlayingCard)))
                {
					targets.Add(cardSpace.PlayingCard);
                }
                break;
        }

        return targets;
	}

	public static bool TargetMeetsRequirementsOfAction(CardDisplay target, ActiveAction action)
	{
		return TargetMeetsRequirements(target, action.requirements);
	}
	public static bool TargetMeetsRequirements(CardDisplay actionTarget, List<Requirements> requirements)
	{
		bool itDoes = true;

        if (requirements.Count > 0) // Check attack requirements
        {
            itDoes = false;

            foreach (Requirements requirement in requirements)
            {
				CardDisplay target;

				BuffAction originBuff = requirement.originAction as BuffAction;
                AttackAction originAttack = requirement.originAction as AttackAction;
				if (originBuff != null)
				{
					if ( !originBuff.activatesOnHit || (originBuff.activatesOnHit && requirement.targetOfRequirementIsTargetOfAttack) )
					{
						target = actionTarget;
					} else {
						target = requirement.originAction.source;
					}
				} else {
					target = actionTarget;
				}

				//Debug.Log($"{target.card.Name} seems to be the target of this action.");

				switch (requirement.requirement)
                {
                    case RequirementTypes.TargetHasSubtypesOrFactions:
                        foreach (UnitSubtype subtype in target.card.Subtypes)
                        {
                            if (requirement.subtypeRequirement.Contains(subtype))
                            {
                                itDoes = true;
                            }
                        }
                        foreach (Faction faction in target.card.Origin)
                        {
                            if (requirement.factionRequirement.Contains(faction))
                            {
                                itDoes = true;
                            }
                        }
                        break;
                    case RequirementTypes.TargetIsNextTo:
                        foreach (CardSpace neighborSpace in target.mySpace.SpacesNextToMe())
                        {
                            if (neighborSpace.PlayingCard != null)
                                foreach (TargetUnitDefinition neighborType in requirement.targetIs)
                                {
                                    switch (neighborType)
                                    {
                                        case TargetUnitDefinition.SameAsMyself:
                                            if (neighborSpace.PlayingCard.card.Name == target.card.Name)
                                            {
                                                itDoes = true;
                                            }
                                            break;
                                    }
                                }
                        }
                        break;
					case RequirementTypes.TargetAttributeIs:
						int attributeValueInTarget = 0;
						switch (requirement.attribute)
						{
							case Attributes.Attack:
								attributeValueInTarget = target.attack;
								break;
							case Attributes.Health:
                                attributeValueInTarget = target.hp;
                                break;
							case Attributes.DefenseMelee:
                                attributeValueInTarget = target.armor[0];
                                break;
							case Attributes.DefenseRanged:
                                attributeValueInTarget = target.armor[1];
                                break;
							case Attributes.DefenseEnergy:
                                attributeValueInTarget = target.armor[2];
                                break;
							case Attributes.ArmorPierce:
                                attributeValueInTarget = target.armorPierce;
                                break;
							case Attributes.DamageReductionBeforeArmor:
							case Attributes.DamageReductionAfterArmor:
                                attributeValueInTarget = target.damageReduction;
                                break;
							case Attributes.MaxHealth:
                                attributeValueInTarget = target.maxHP;
                                break;
							case Attributes.Cost:
                                attributeValueInTarget = target.cost;
                                break;
						}
						switch (requirement.comparison)
						{
							case Comparison.LessThan:
								itDoes = attributeValueInTarget < requirement.attributeValue;
								break;
							case Comparison.LessThanOrEqual:
                                itDoes = attributeValueInTarget <= requirement.attributeValue;
                                break;
							case Comparison.Equal:
                                itDoes = attributeValueInTarget == requirement.attributeValue;
                                break;
							case Comparison.MoreThan:
                                itDoes = attributeValueInTarget > requirement.attributeValue;
                                break;
							case Comparison.MoreThanOrEqual:
                                itDoes = attributeValueInTarget >= requirement.attributeValue;
                                break;
							case Comparison.Not:
                                itDoes = attributeValueInTarget != requirement.attributeValue;
                                break;
						}
						break;
                }
            }
        }

        return itDoes;
	}

	public static bool TargetCanBeReachedByAttack(CardDisplay target, AttackAction attack)
	{
		bool itCan = true;
        bool targetIsDefended = false;
        bool targetIsCovered = false;
        if (target.mySpace != null)
        {
            foreach (CardSpace defenderSpace in target.mySpace.Defenders)
            {
                if (defenderSpace.PlayingCard != null)
                {
                    targetIsCovered = true;
                    if (defenderSpace.PlayingCard.card.Subtypes.Contains(UnitSubtype.Defender))
                    {
                        targetIsDefended = true;
                    }
                }
            }
            switch (attack.damageType)
            {
                case DamageTypes.Melee: if (targetIsCovered) { itCan = false; } break;
                case DamageTypes.Ranged: if (targetIsDefended) { itCan = false; } break;
                case DamageTypes.Energy: if (targetIsCovered) { itCan = false; } break;
                case DamageTypes.MeleeOrRanged: if (targetIsDefended) { itCan = false; } break;
                case DamageTypes.RangedOrEnergy: if (targetIsDefended) { itCan = false; } break;
                case DamageTypes.MeleeOrEnergy: if (targetIsCovered) { itCan = false; } break;
                case DamageTypes.MeleeOrRangedOrEnergy: if (targetIsDefended) { itCan = false; } break;
            }
        } else { return false; }

        return itCan;
	}

	public static int CalculateDamage(CardDisplay target, AttackAction attackAction)
	{

		CardDisplay attacker = attackAction.source;
		List<BuffAction> onHitBuffs = attackAction.source.appliedBuffs.Where(x => x.activatesOnHit).ToList();

		/* Calculation of temporary buffs and debuffs */
		var attackerTempModifiers = new TempModifiers();
		var targetTempModifiers = new TempModifiers();
		foreach (BuffAction buff in attackAction.temporaryBuffs)
		{
			if (attackAction.target == TargetTypes.Self) /* The modifiers apply to myself */
			{
				attackerTempModifiers.SetModifiersFromBuff(buff);
			}
			else if (attackAction.target == TargetTypes.SingleEnemy)
			{
				targetTempModifiers.SetModifiersFromBuff(buff);
			}
		}
        foreach (BuffAction buff in onHitBuffs)
        {
			if (buff.TargetMeetsOnHitRequirements(target))
			{
				attackerTempModifiers.SetModifiersFromBuff(buff);
			}
        }

        int targetArmor = 0;
		string damageType = "melee";
		switch (attackAction.damageType)
		{
			case DamageTypes.Melee: targetArmor = target.armor[0]; break;
			case DamageTypes.Ranged: targetArmor = target.armor[1]; break;
			case DamageTypes.Energy: targetArmor = target.armor[2]; break;
			case DamageTypes.MeleeOrRanged:
				if (target.armor[0] < target.armor[1])
				{ targetArmor = target.armor[0]; }
				else
				{ targetArmor = target.armor[1]; damageType = "ranged"; }
				break;
			case DamageTypes.RangedOrEnergy:
				if (target.armor[2] < target.armor[1])
				{ targetArmor = target.armor[2]; damageType = "energy"; }
				else
				{ targetArmor = target.armor[1]; damageType = "ranged"; }
				break;
			case DamageTypes.MeleeOrEnergy:
				if (target.armor[0] < target.armor[2])
				{ targetArmor = target.armor[0]; }
				else
				{ targetArmor = target.armor[2]; damageType = "energy"; }
				break;
			case DamageTypes.MeleeOrRangedOrEnergy:
				if (target.armor[0] < target.armor[1] && target.armor[0] < target.armor[2])
				{ targetArmor = target.armor[0]; }
				else if (target.armor[1] < target.armor[2])
				{ targetArmor = target.armor[1]; damageType = "ranged"; }
				else
				{ targetArmor = target.armor[2]; damageType = "energy"; }
				break;
		}
		switch (damageType)
		{
			case "melee": targetArmor += targetTempModifiers.Armor[0]; break;
			case "ranged": targetArmor += targetTempModifiers.Armor[1]; break;
			case "energy": targetArmor += targetTempModifiers.Armor[2]; break;
		}
		targetArmor -= attacker.armorPierce + attackerTempModifiers.ArmorPierce;
		if (targetArmor < 0) { targetArmor = 0; }

		int dmg = Mathf.FloorToInt((attacker.attack + attackerTempModifiers.Attack) * attackAction.damageMultiplier) - targetArmor;
		if (attackAction.damageType == DamageTypes.SelfDamage)
		{
			targetArmor = 0;
		}
		if (attackAction.flatDamageOverwrite > 0)
		{
			dmg = attackAction.flatDamageOverwrite - targetArmor;
		}
		if (dmg <= 0)
		{
			dmg = 1;
		}

		return dmg;
	}

	public static bool IsTargetImplicit(TargetTypes targetType)
	{
		bool isIt = false;
        switch (targetType)
        {
            case TargetTypes.SingleEnemy: isIt = false; break;
            case TargetTypes.LineOfEnemies: isIt = false; break;
            case TargetTypes.Self: isIt = true; break;
            case TargetTypes.AllAllies: isIt = true; break;
            case TargetTypes.AlliesNextToMe: isIt = true; break;
            case TargetTypes.AlliesInSameLine: isIt = true; break;
            case TargetTypes.SingleAlly: isIt = false; break;
            default: isIt = true; break;
        }
		return isIt;
    }

	public static bool TargetIsFromMyTeam(TargetTypes targetType)
	{
		bool itIs = false;
		switch (targetType)
		{
			case TargetTypes.Self:
			case TargetTypes.AlliesInSameLine:
			case TargetTypes.AllAllies:
			case TargetTypes.AlliesNextToMe:
			case TargetTypes.SingleAlly:
				itIs = true;
				break;
			case TargetTypes.SingleEnemy:
			case TargetTypes.LineOfEnemies:
			case TargetTypes.SameTarget:
				itIs = false;
				break;
		}
		return itIs;
	}

	public static bool IsTargetPlural(TargetTypes targetType)
	{
		bool itIs = false;
		switch (targetType)
		{
			case TargetTypes.Self:
			case TargetTypes.SingleEnemy:
			case TargetTypes.SameTarget:
			case TargetTypes.SingleAlly:
				itIs = false;
				break;
			case TargetTypes.AlliesInSameLine:
			case TargetTypes.LineOfEnemies:
			case TargetTypes.AllAllies:
			case TargetTypes.AlliesNextToMe:
            case TargetTypes.AllEnemies:
                itIs = true;
				break;
		}
		return itIs;
	}
}

class TempModifiers
{
    public int Attack = 0;
	public int Health = 0;
	public int MaxHealth = 0;
	public int Defense = 0;
	public List<int> Armor = new List<int> { 0, 0, 0 };
	public int ArmorPierce = 0;
	public int DamageReductionBeforeArmor = 0;
	public int DamageReductionAfterArmor = 0;

	public void SetModifiersFromBuff(BuffAction buff)
	{
        switch (buff.Attribute)
        {
            case Attributes.Attack: Attack += buff.amount; break;
            case Attributes.Health: Health += buff.amount; break;
            case Attributes.MaxHealth: MaxHealth += buff.amount; break;
            case Attributes.Defense: Defense += buff.amount; break;
            case Attributes.DefenseMelee: Armor[0] += buff.amount; break;
            case Attributes.DefenseRanged: Armor[1] += buff.amount; break;
            case Attributes.DefenseEnergy: Armor[2] += buff.amount; break;
            case Attributes.ArmorPierce: ArmorPierce += buff.amount; break;
            case Attributes.DamageReductionBeforeArmor: DamageReductionBeforeArmor += buff.amount; break;
            case Attributes.DamageReductionAfterArmor: DamageReductionAfterArmor += buff.amount; break;
        }
    }
}