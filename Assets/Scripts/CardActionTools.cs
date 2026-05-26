using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public static class CardActionTools
{
	public static List<CardDisplay> AllPlayingCards {
		get {
			GameManager GM = GameObject.FindAnyObjectByType<GameManager>();
            return GM.Host.GetActiveCards().Concat(GM.Opponent.GetActiveCards()).ToList();
		}
	}

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

	public static List<CardDisplay> GetPotentialTargetsForAction(ActiveAction action)
	{
		List<CardDisplay> potentialTargets = new();
		//AttackAction attackAction = action as AttackAction;
		//if(action is BuffAction buffAction)
        //Debug.Log($"{action.source.card.Name} wants to target {action.target} to {CardTranslator.AttributeAndValue(action as BuffAction)}...");
		//if(action is AttackAction attackAction)
        //Debug.Log($"{action.source.card.Name} wants to target {action.target} for a {CardTranslator.DamageTypeDescription(attackAction.damageType)} attack...");

        foreach (CardDisplay playingCard in AllPlayingCards)
		{
			//Debug.Log($"Checking {playingCard.card.Name}...");
            if (action.TargetMeetsRequirements(playingCard)) 
            {
                potentialTargets.Add(playingCard);
				//Debug.Log($"{playingCard.card.Name} is potential target for this action.");
            }
        }

		return potentialTargets;
	}

	public static void PerformAttackAction(CardDisplay target, CardDisplay attacker, AttackAction attack)
	{
		CardDisplay actualTarget = GetActualTarget(target);
		actualTarget.ReceiveDamageFromAttack(attack);
		if (attack.attackActionOutput.resultsInDeath)
		{
			actualTarget = GetActualTarget(target);
		}
		if(actualTarget != null)
		{
            foreach (BuffAction buff in attacker.appliedBuffs.Where(x => x.specialEffect == BuffSpecialEffects.TriggerExtraAttack && x.extraAttacks.Count > 0))
            {
				if(buff.TargetMeetsOnHitRequirements(actualTarget))
				foreach (AttackAction extraAttack in attacker.appliedBuffs.Where(x => x.specialEffect == BuffSpecialEffects.TriggerExtraAttack).SelectMany(x => x.extraAttacks))
					{
						if (extraAttack.TargetMeetsRequirements(actualTarget))
						{
							actualTarget.ReceiveDamageFromAttack(extraAttack);
						}
					}
            }
        }

        foreach (BuffAction instantEffectBuff in attack.temporaryBuffs.Where(x => x.Attribute == Attributes.Health).Concat(attacker.appliedBuffs.Where(x => x.activatesOnHit && x.IsBuffEffectOfInstantEffect() )).ToList())
        {
			BuffAction buff = new(instantEffectBuff) { activatesOnHit = false, source = attacker };
            foreach (CardDisplay buffTarget in buff.GetImplicitTargetsOfAction())
            {
				buffTarget.ReceiveActiveBuff(buff);
            }
        }

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
						PerformAttackAction(ActionData.targets[i], ActionData.CardInAction, ActionData.actionObject.action.attacks[i]);
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
		List<CardDisplay> targets = new();
		if (!action.isTargetImplicit) {  return targets; }
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
		bool itDoes = false;
		//AttackAction attackAction = action as AttackAction;
		bool isFromMyTeam = (action.targetIsFromMyTeam && target.Owner?.Role == action.source?.Owner?.Role);
		bool isFromOtherTeam = (!action.targetIsFromMyTeam && target.Owner?.Role != action.source?.Owner?.Role);
		bool isMyself = action.target == TargetTypes.Self;

        if ( ( isFromMyTeam || isFromOtherTeam || isMyself) && (action.TargetCanBeReached(target)) )
		{
			itDoes = true;
		}
		//Debug.Log($"<b>{action.source?.card.Name}</b>: Does target of buff meet all requirements? -> {itDoes} { TargetMeetsRequirements(target, action.requirements)}");
		return itDoes && TargetMeetsRequirements(target, action.requirements);
	}
	public static bool TargetMeetsRequirements(CardDisplay actionTarget, List<Requirements> requirements)
	{
		bool itDoes = true;

        if (requirements.Count > 0) // Check attack requirements
        {
			//Debug.Log($"<b>{requirements[0].originAction?.source?.card.Name}</b>: <color=red>There is indeed some requirements for this action.</color>");
            itDoes = false;

            foreach (Requirements requirement in requirements)
            {
				CardDisplay target;

				BuffAction originBuff = requirement.originAction as BuffAction;
                AttackAction originAttack = requirement.originAction as AttackAction;
				if (originBuff != null)
				{
					if (!originBuff.activatesOnHit || (originBuff.originAttack != null && requirement.targetOfRequirementIsTargetOfAttack))
					{
						target = actionTarget;
					}
					else
					{
						target = requirement.originAction.source;
					}
				}
				else
				{
					target = actionTarget;
				}

				//Debug.Log($"{target.card.Name} seems to be the target of this action.");
                Debug.Log($"Origin buff: {originBuff != null}. Origin passive: {originBuff != null && originBuff.originPassive != null}");

				if ((originBuff != null && originBuff.originPassive != null && originBuff.originPassive.CanBeUsedThisRound) || (originBuff != null && originBuff.originPassive == null) || originBuff == null)
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
                        foreach (CardSpace neighborSpace in target.mySpace?.SpacesNextToMe())
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
										case TargetUnitDefinition.TheLeader:
                                            if (neighborSpace.PlayingCard.card.Type == UnitType.Leader)
                                            {
                                                itDoes = true;
                                            }
                                            break;
                                    }
                                }
                        }
                        break;
                    case RequirementTypes.TargetIsInRowInFrontOf:
						if(target.mySpace.Defendeds.Count > 0)
                        foreach (CardSpace spaceBehind in target.mySpace.Defendeds[0].myRow.BoardSpaces)
                        {
                            if (spaceBehind.PlayingCard != null)
                                foreach (TargetUnitDefinition neighborType in requirement.targetIs)
                                {
                                    switch (neighborType)
                                    {
                                        case TargetUnitDefinition.SameAsMyself:
                                            if (spaceBehind.PlayingCard.card.Name == target.card.Name)
                                            {
                                                itDoes = true;
                                            }
                                            break;
                                        case TargetUnitDefinition.TheLeader:
                                            if (spaceBehind.PlayingCard.card.Type == UnitType.Leader)
                                            {
                                                itDoes = true;
                                            }
                                            break;
                                    }
                                }
                        }
                        break;
                    case RequirementTypes.TargetHasAttackedThisRound:
						itDoes = target.HasAttackedThisRound;
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
					case RequirementTypes.TargetHasAffectedUnitDefinition:
                        foreach (TargetUnitDefinition targetUnitDefinition in requirement.targetIs)
                        {
							switch (targetUnitDefinition)
							{
								case TargetUnitDefinition.SameAsMyself:
									itDoes = target.MyActionsOfThisRound.Where(tuAc => tuAc.targets.Where(x => x.card.Name == requirement.originAction.source.card.Name).ToList().Count > 0).ToList().Count > 0;
									break;
								case TargetUnitDefinition.TheLeader:
                                    itDoes = target.MyActionsOfThisRound.Where(tuAc => tuAc.targets.Where(x => x.card.Type == UnitType.Leader && x.Owner == requirement.originAction.source.Owner).ToList().Count > 0).ToList().Count > 0;
                                    break;
							}
						}
                        break;
					case RequirementTypes.TargetIsStunned:
						itDoes = target.IsStunned;
						break;
                    case RequirementTypes.TargetIsDisarmed:
						itDoes = target.IsDisarmed;
                        break;
                    case RequirementTypes.TargetIsDisrupted:
						itDoes = target.IsDisrupted;
                        break;
                }
            }
        }

        //Debug.Log($"Trying to reach {actionTarget?.card?.Name}: {itDoes}");
		return itDoes;
	}

	public static bool TargetCanBeReachedByAction(CardDisplay target, ActiveAction action)
	{
		bool itCan = true;
        bool targetIsDefended = false;
        bool targetIsCovered = false;
        if (target.HasBeenPlayed)
        {
			if (action is AttackAction attackAction)
			{
				if(target.Owner.Role == action.source.Owner.Role)
				{
					itCan = true;
				} else  {
					foreach (CardSpace defenderSpace in target.mySpace.Defenders)
					{
						if (defenderSpace.HasCard)
						{
							targetIsCovered = true;
							if (defenderSpace.PlayingCard.card.Subtypes.Contains(UnitSubtype.Defender))
							{
								targetIsDefended = true;
							}
						}
					}
					switch (attackAction.damageType)
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
			else if (action is BuffAction buffAction) {
                if (target.Owner.Role == action.source?.Owner.Role)
                {
                    itCan = true;
                } else {
					foreach (CardSpace defenderSpace in target.mySpace.Defenders)
					{
						if (defenderSpace.HasCard)
						{
							targetIsCovered = true;
							itCan = false;
						}
					}
                }
            }
        } else { return false; }

        return itCan;
	}

	public static AttackActionOutput GetAttackActionOutput(CardDisplay target, AttackAction attackAction)
	{
		AttackActionOutput output = new AttackActionOutput();
        CardDisplay attacker = attackAction.source;
        List<BuffAction> onHitBuffs = attackAction.source.appliedBuffs.Where(x => x.activatesOnHit).ToList();

        /* Calculation of temporary buffs and debuffs */
        TempModifiers attackerTempModifiers = output.attackerModifiers;
        TempModifiers targetTempModifiers = output.targetModifiers;
        foreach (BuffAction buff in attackAction.temporaryBuffs)
        {
			//Debug.Log($"<b>{attackAction.source.card.Name}</b>: Checking buff to {buff.target} that gives {CardTranslator.AttributeAndValue(buff)} -> {buff.target == TargetTypes.Self} {buff.TargetMeetsRequirements(target)}");
            if (buff.target == TargetTypes.Self && buff.TargetMeetsRequirements(target)) /* The modifiers apply to myself */
            {
				Debug.Log($"I deserve {CardTranslator.AttributeAndValue(buff)}");
                attackerTempModifiers.SetModifiersFromBuff(buff);
            }
            else if ((buff.target == TargetTypes.SingleEnemy || buff.target == TargetTypes.SameTarget) && buff.TargetMeetsRequirements(target))
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
        DamageTypes damageType = DamageTypes.Melee;
        switch (attackAction.damageType)
        {
            case DamageTypes.Melee: targetArmor = target.armor[0]; break;
            case DamageTypes.Ranged: targetArmor = target.armor[1]; damageType = DamageTypes.Ranged; break;
            case DamageTypes.Energy: targetArmor = target.armor[2]; damageType = DamageTypes.Energy;  break;
            case DamageTypes.MeleeOrRanged:
                if (target.armor[0] < target.armor[1])
                { targetArmor = target.armor[0]; }
                else
                { targetArmor = target.armor[1]; damageType = DamageTypes.Ranged; }
                break;
            case DamageTypes.RangedOrEnergy:
                if (target.armor[2] < target.armor[1])
                { targetArmor = target.armor[2]; damageType = DamageTypes.Energy; }
                else
                { targetArmor = target.armor[1]; damageType = DamageTypes.Ranged; }
                break;
            case DamageTypes.MeleeOrEnergy:
                if (target.armor[0] < target.armor[2])
                { targetArmor = target.armor[0]; }
                else
                { targetArmor = target.armor[2]; damageType = DamageTypes.Energy; }
                break;
            case DamageTypes.MeleeOrRangedOrEnergy:
                if (target.armor[0] < target.armor[1] && target.armor[0] < target.armor[2])
                { targetArmor = target.armor[0]; }
                else if (target.armor[1] < target.armor[2])
                { targetArmor = target.armor[1]; damageType = DamageTypes.Ranged; }
                else
                { targetArmor = target.armor[2]; damageType = DamageTypes.Energy; }
                break;
        }
        switch (damageType)
        {
            case DamageTypes.Melee: targetArmor += targetTempModifiers.Armor[0]; break;
            case DamageTypes.Ranged: targetArmor += targetTempModifiers.Armor[1]; break;
            case DamageTypes.Energy: targetArmor += targetTempModifiers.Armor[2]; break;
        }
        targetArmor -= attacker.armorPierce + attackerTempModifiers.ArmorPierce;
        if (targetArmor < 0) { targetArmor = 0; }

        int dmg = Mathf.FloorToInt((attacker.attack + attackerTempModifiers.Attack - target.damageReduction) * (attackAction.damageMultiplier + attackerTempModifiers.DamageMultiplier)) - targetArmor;
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
		output.damage = dmg;
		output.damageType = damageType;
		if (target.hp - dmg <= 0 || (target.hp - dmg <= 2 && attacker.card.Subtypes.Contains(UnitSubtype.Executioner))) {
			output.resultsInDeath = true;
		}
        
        return output;
	}

	public static int CalculateDamage(CardDisplay target, AttackAction attackAction)
	{
		return GetAttackActionOutput(target, attackAction).damage;
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

    public static bool IsBuffEffectOfInstantEffect(BuffAction buff)
    {
        bool itIs = false;
		switch (buff.specialEffect)
		{
			case BuffSpecialEffects.None:
			case BuffSpecialEffects.RedirectAttacksTowardsMe:
			case BuffSpecialEffects.GrantSubtypes:
			case BuffSpecialEffects.EnableGuardingPose:
			case BuffSpecialEffects.Stun:
			case BuffSpecialEffects.Disarm:
			case BuffSpecialEffects.Disrupt:
				itIs = false;
				break;
			case BuffSpecialEffects.TriggerExtraAttack:
				itIs = true;
				break;
		}
		if(buff.Attribute == Attributes.Health)
		{
			itIs = true;
		}
		return itIs;
    }
}

[Serializable]
public class TempModifiers
{
    public int Attack = 0;
	public int Health = 0;
	public int MaxHealth = 0;
	public int Defense = 0;
	public List<int> Armor = new List<int> { 0, 0, 0 };
	public int ArmorPierce = 0;
	public int DamageReductionBeforeArmor = 0;
	public int DamageReductionAfterArmor = 0;
	public float DamageMultiplier = 0;
	public List<BuffAction> usedBuffs = new();

	public TempModifiers() {

	}

	public void SetModifiersFromBuff(BuffAction buff)
	{
		usedBuffs.Add(new(buff));
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
            case Attributes.DamageMultiplier: DamageMultiplier += buff.amount; break;
        }
    }
}

[Serializable]
public class AttackActionOutput
{
	public int damage = 0;
	public bool resultsInDeath = false;
	public DamageTypes damageType = DamageTypes.Melee;
	public TempModifiers attackerModifiers = new();
	public TempModifiers targetModifiers = new();
	public string damageTypeIcon {
		get {
			string character = "🥊";
			switch (damageType)
			{
				case DamageTypes.Melee:
					character = "⚔️";
                    break;
				case DamageTypes.Ranged:
                    character = "🎯";
                    break;
				case DamageTypes.Energy:
                    character = "✨";
                    break;
			}
			return character;
		}
	}
}