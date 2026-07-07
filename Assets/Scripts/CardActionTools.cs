using JetBrains.Annotations;
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
        Debug.Log($"{attacker.card.Name} attacks {target.card.Name}. Extra: {attack.isExtra} . Damage: {attack.damageMultiplier*100}%|+{attack.flatDamageOverwrite}");
        //CardDisplay actualTarget = GetActualTarget(target);
        List<CardDisplay> actualTargets = new();
        if (attack.isTargetImplicit)
        {
            actualTargets.AddRange(attack.GetImplicitTargetsOfAction());
        } else
        {
            actualTargets.Add(target);
        }

        foreach (CardDisplay theTarget in actualTargets)
        {
            CardDisplay actualTarget = theTarget;
            actualTarget.ReceiveDamageFromAttack(attack);
		    if (attack.attackActionOutput.resultsInDeath)
		    {
			    actualTarget = GetActualTarget(actualTarget);
		    }


            if (actualTarget != null && !attack.isExtra)
		    {
                List<BuffAction> buffsFromPlayerEvents = new();
                foreach (PlayerBuffs pBuff in attacker.Owner.BuffsForNextAttackingAlly)
                {
                    if (!pBuff.hasBeenUsed)
                    {
                        buffsFromPlayerEvents.AddRange(pBuff.buffs);
                        pBuff.hasBeenUsed = true;
                    }
                }
                foreach (BuffAction buff in attacker.appliedBuffs.Concat(buffsFromPlayerEvents).Where(x => x.specialEffect == BuffSpecialEffects.TriggerExtraAttack && x.extraAttacks.Count > 0))
                {

                    Debug.Log($"Found a buff with effect {buff.specialEffect}.");

				    if(buff.TargetMeetsOnHitRequirements(actualTarget))
				    foreach (AttackAction extraAttack in buff.extraAttacks)
					    {
                            AttackAction extraAttackAction = new(extraAttack) { source = attacker };
                            Debug.Log($"Found an extra attack from {attacker.card.Name} towards {actualTarget.card.Name}. Damage: {extraAttackAction.damageMultiplier * 100}%|+{extraAttackAction.flatDamageOverwrite}");
                            if(extraAttackAction.target == TargetTypes.SameTarget)
                            {
						        if (extraAttackAction.TargetMeetsRequirements(actualTarget))
						        {
							        actualTarget.ReceiveDamageFromAttack(extraAttackAction);
						        }
                            } else {
                                foreach (CardDisplay implicitTarget in extraAttackAction.GetImplicitTargetsOfAction())
                                {
                                    implicitTarget.ReceiveDamageFromAttack(extraAttackAction);
                                }
                            }
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

        List<AttackEffect> attackEffectsFromBuffs = attacker.appliedBuffs.Where(buff => buff.specialEffect == BuffSpecialEffects.GrantAttackEffect && buff.attackEffect.Count > 0 && !buff.activatesOnHit ).SelectMany(x => x.attackEffect).ToList();

        foreach (AttackEffect effect in attack.attackEffect.Concat(attackEffectsFromBuffs).ToList())
		{
			switch (effect.effectType)
			{
				case AttackEffects.SplashDamage:
					List<CardDisplay> affectedTargets = new List<CardDisplay>();
					AttackAction splashAttack = new AttackAction(attack)
					{
						requirements = new List<Requirements>(),
						attackEffect = new List<AttackEffect>(),
                        isExtra = true
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

	public static void PerformPlayerBuffAction(PlayerBuffs playerBuff)
	{
		PlayerProfile playerTarget;
        if (playerBuff.target == PlayerTarget.OwnerOfCard) { playerTarget = playerBuff.source.Owner; }
        else { playerTarget = playerBuff.source.Owner.otherPlayer; }
        switch (playerBuff.buffType)
		{
			case PlayerBuffTypes.AddGold:
				playerTarget.Gold += playerBuff.amount;
                break;
			case PlayerBuffTypes.RemoveGold:
                playerTarget.Gold -= playerBuff.amount;
                break;
			case PlayerBuffTypes.StealGold:
				int bounty = 0;
                if (playerTarget.Gold >= playerBuff.amount) {
					bounty = playerBuff.amount;
				} else {
					bounty = playerTarget.Gold;
				}
				playerTarget.Gold -= bounty;
				playerTarget.otherPlayer.Gold += bounty;
                break;
			case PlayerBuffTypes.ExecutionerThresholdModifier:
			case PlayerBuffTypes.MercenaryKillGoldReward:
            case PlayerBuffTypes.FreeAttackActions:
            case PlayerBuffTypes.BuffForNextAttackingUnit:
                playerTarget.ReceiveActiveBuff(playerBuff);
				break;
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
                    PerformBuffAction(buff, ActionData.targets[i]);
					//List<CardDisplay> targets = new();
					//if (buff.isTargetImplicit) {
					//	targets.AddRange(buff.GetImplicitTargetsOfAction().Where(x => buff.TargetMeetsRequirements(x)));
					//} else if (ActionData.targets[i] != null) {
					//	if (buff.TargetMeetsRequirements(ActionData.targets[i])) { targets.Add(ActionData.targets[i]); }
     //               }
     //               foreach (CardDisplay cardDisplay in targets)
     //               {
					//	cardDisplay.ReceiveActiveBuff(buff);
     //               }
				}
				break;
			case ActionTypes.PlayerBuff:
				for (int i = 0; i < ActionData.actionObject.action.playerBuffs.Count; i++)
				{
					PlayerBuffs pBuff = ActionData.actionObject.action.playerBuffs[i];
					PerformPlayerBuffAction(pBuff);
				}
               break;
		}
	}

    public static void PerformBuffAction(BuffAction buffAction, CardDisplay target = null, AttackActionOutput attackOutput = null)
    {
        BuffAction buff = new(buffAction);
        List<CardDisplay> realTargets = new();

        if (buff.isTargetImplicit)
        {
            realTargets.AddRange(buff.GetImplicitTargetsOfAction().Where(x => buff.TargetMeetsRequirements(x)));
        }
        else if (target != null)
        {
            if (buff.TargetMeetsRequirements(target)) { realTargets.Add(target); }
        }

        if(attackOutput != null)
        {
            if(buff.addDamageDealtAsValue) { buff.amount += attackOutput.damage; }
        }

        foreach (CardDisplay cardDisplay in realTargets)
        {
            cardDisplay.ReceiveActiveBuff(buff);
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
                    foreach (CardSpace cardSpace in row.BoardSpaces.Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                    {
						targets.Add(cardSpace.PlayingCard);
                    }
                }
                break;
            case TargetTypes.AllEnemies:
                foreach (BoardRow row in action.source.mySpace.Owner.otherPlayer.MyBoardRows)
                {
                    foreach (CardSpace cardSpace in row.BoardSpaces.Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                    {
                        targets.Add(cardSpace.PlayingCard);
                    }
                }
                break;
            case TargetTypes.AlliesInSameLine:
                foreach (CardSpace cardSpace in action.source.mySpace.myRow.BoardSpaces.Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                {
                    targets.Add(cardSpace.PlayingCard);
                }
                break;
            case TargetTypes.AlliesNextToMe:
                foreach (CardSpace cardSpace in action.source.mySpace.SpacesNextToMe().Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                {
					targets.Add(cardSpace.PlayingCard);
                }
                break;
            case TargetTypes.AlliesInLineInFrontOfMe:
                foreach (CardSpace cardSpace in action.source.mySpace.SpacesInLineInFrontOfMe().Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                {
                    targets.Add(cardSpace.PlayingCard);
                }
                break;
            case TargetTypes.AlliesInLineBehind:
                foreach (CardSpace cardSpace in action.source.mySpace.SpacesInLineInBehindMe().Where(x => x.HasCard && action.TargetMeetsRequirements(x.PlayingCard)))
                {
                    targets.Add(cardSpace.PlayingCard);
                }
                break;
            case TargetTypes.MostHarmedAlly:
                List<List<CardDisplay>> rows = new();
                CardDisplay chosenTarget = action.source; /* If no ally was found to be harmed then it will heal themselves. */
                bool found = false;

                /* We should prioritize allies in the front-most rows */
                Debug.Log($"Source: {action.source}");
                Debug.Log($"Source of action: {action.source.card.Name}");
                foreach (BoardRow row in action.source.Owner.MyBoardRows )
                {
                    rows.Add(new());
                    foreach (CardSpace cardSpace in row.BoardSpaces.Where(x=>x.HasCard).OrderBy(x => x.PlayingCard.hp / x.PlayingCard.maxHP ))
                    {
                        if(cardSpace.PlayingCard.hp / cardSpace.PlayingCard.maxHP != 1) { /* 1 means unharmed */
                            rows[cardSpace.myRowIndex].Add(cardSpace.PlayingCard);
                        }
                    }
                }

                foreach (List<CardDisplay> row in rows)
                {
                    if(row.Count > 0)
                    {
                        chosenTarget = row[0];
                        found = true;
                    }
                    if (found) { break; }
                }

                targets.Add(chosenTarget);
                break;
        }

    return targets;
}

public static bool TargetMeetsRequirementsOfAction(CardDisplay target, ActiveAction action)
	{
		bool itDoes = false;
		//AttackAction attackAction = action as AttackAction;
		bool isMyself = action.target == TargetTypes.Self;
		bool isFromMyTeam = (action.targetIsFromMyTeam && target.Owner?.Role == action.source?.Owner?.Role);
		bool isFromOtherTeam = (!action.targetIsFromMyTeam && target.Owner?.Role != action.source?.Owner?.Role);

        if ( ( isFromMyTeam || isMyself) || (isFromOtherTeam && action.TargetCanBeReached(target)) )
		{
			itDoes = true;
		}
		//Debug.Log($"<b>{action.source?.card.Name}</b>: Does target meet all requirements? -> 1: {itDoes} . 2:{TargetMeetsRequirements(target, action.requirements)}");
        return itDoes && TargetMeetsRequirements(target, action.requirements);
	}
	public static bool TargetMeetsRequirements(CardDisplay actionTarget, List<Requirements> requirements)
	{
		return StatsMeetRequirements(new(actionTarget), requirements);
	}

    public static bool StatsMeetRequirements(StatList selectedTargetStats, List<Requirements> requirements)
	{
		bool itDoes = true;

        if (requirements.Count > 0) // Check attack requirements
        {
            //Debug.Log($"<b>{requirements[0].originAction?.source?.card.Name}</b>: <color=red>There is indeed some requirements for this action.</color>");
            itDoes = false;

            foreach (Requirements requirement in requirements)
            {
                StatList targetStats;

                BuffAction originBuff = requirement.originAction as BuffAction;
                AttackAction originAttack = requirement.originAction as AttackAction;
                if (selectedTargetStats.source == requirement.originAction?.source && requirement.originAction?.target == TargetTypes.Self)
                {
                    targetStats = selectedTargetStats;
                }
                else if (originBuff != null)
                {
                    if (!originBuff.activatesOnHit || (originBuff.originAttack != null && requirement.targetOfRequirementIsTargetOfAttack))
                    {
                        targetStats = selectedTargetStats;
                    }
                    else
                    {
                        targetStats = new(requirement.originAction.source);
                    }
                }
                else
                {
                    targetStats = selectedTargetStats;
                }

                //Debug.Log($"{target.card.Name} seems to be the target of this action.");
                //Debug.Log($"Origin buff: {originBuff != null}. Origin passive: {originBuff != null && originBuff.originPassive != null}");

                if ((originBuff != null && originBuff.originPassive != null && originBuff.originPassive.CanBeUsedThisRound) || (originBuff != null && originBuff.originPassive == null) || originBuff == null)
                    switch (requirement.requirement)
                    {
                        case RequirementTypes.TargetHasSubtypesOrFactions:
                            foreach (UnitSubtype subtype in targetStats.subtypes)
                            {
                                if (requirement.subtypeRequirement.Contains(subtype))
                                {
                                    itDoes = true;
                                }
                            }
                            foreach (Faction faction in targetStats.origin)
                            {
                                if (requirement.factionRequirement.Contains(faction))
                                {
                                    itDoes = true;
                                }
                            }
                            break;
                        case RequirementTypes.TargetIsNextTo:
                            foreach (CardSpace neighborSpace in targetStats.source.mySpace?.SpacesNextToMe())
                            {
                                if (neighborSpace.PlayingCard != null)
                                    foreach (TargetUnitDefinition neighborType in requirement.targetIs)
                                    {
                                        switch (neighborType)
                                        {
                                            case TargetUnitDefinition.SameAsMyself:
                                                if (neighborSpace.PlayingCard.card.Name == targetStats.source.card.Name)
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
                            if (targetStats.source.mySpace.Defendeds.Count > 0)
                                foreach (CardSpace spaceBehind in targetStats.source.mySpace.Defendeds[0].myRow.BoardSpaces)
                                {
                                    if (spaceBehind.PlayingCard != null)
                                        foreach (TargetUnitDefinition neighborType in requirement.targetIs)
                                        {
                                            switch (neighborType)
                                            {
                                                case TargetUnitDefinition.SameAsMyself:
                                                    if (spaceBehind.PlayingCard.card.Name == targetStats.source.card.Name)
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
                            itDoes = targetStats.source.HasAttackedThisRound;
                            break;
                        case RequirementTypes.TargetAttributeIs:
                            int attributeValueInTarget = 0;
                            switch (requirement.attribute)
                            {
                                case Attributes.Attack:
                                    attributeValueInTarget = targetStats.attack;
                                    break;
                                case Attributes.Health:
                                    attributeValueInTarget = targetStats.hp;
                                    break;
                                case Attributes.DefenseMelee:
                                    attributeValueInTarget = targetStats.armor[0];
                                    break;
                                case Attributes.DefenseRanged:
                                    attributeValueInTarget = targetStats.armor[1];
                                    break;
                                case Attributes.DefenseEnergy:
                                    attributeValueInTarget = targetStats.armor[2];
                                    break;
                                case Attributes.ArmorPierce:
                                    attributeValueInTarget = targetStats.armorPierce;
                                    break;
                                case Attributes.DamageReductionBeforeArmor:
                                case Attributes.DamageReductionAfterArmor:
                                    attributeValueInTarget = targetStats.damageReduction;
                                    break;
                                case Attributes.MaxHealth:
                                    attributeValueInTarget = targetStats.maxHP;
                                    break;
                                case Attributes.GoldCost:
                                    attributeValueInTarget = targetStats.cost;
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
                                        itDoes = targetStats.source.MyActionsOfThisRound.Where(tuAc => tuAc.targets.Where(x => x.card.Name == requirement.originAction.source.card.Name).ToList().Count > 0).ToList().Count > 0;
                                        break;
                                    case TargetUnitDefinition.TheLeader:
                                        itDoes = targetStats.source.MyActionsOfThisRound.Where(tuAc => tuAc.targets.Where(x => x.card.Type == UnitType.Leader && x.Owner == requirement.originAction.source.Owner).ToList().Count > 0).ToList().Count > 0;
                                        break;
                                }
                            }
                            break;
                        case RequirementTypes.TargetExists:
                            foreach (TargetUnitDefinition targetUnitDefinition in requirement.targetIs)
                            {
                                switch (targetUnitDefinition)
                                {
                                    case TargetUnitDefinition.LastAllyWhoAttacked:
                                        itDoes = requirement.originAction.source.Owner.LastPerformedAttackByAlly != null;
                                        break;
                                    case TargetUnitDefinition.LastAllyWhoPerformedAnAction:
                                        itDoes = requirement.originAction.source.Owner.LastPerformedActionByAlly != null;
                                        break;
                                }
                            }
                            break;
                        case RequirementTypes.TargetIsStunned:
                            itDoes = targetStats.source.IsStunned;
                            break;
                        case RequirementTypes.TargetIsDisarmed:
                            itDoes = targetStats.source.IsDisarmed;
                            break;
                        case RequirementTypes.TargetIsDisrupted:
                            itDoes = targetStats.source.IsDisrupted;
                            break;
                        case RequirementTypes.TargetIsGuarding:
                            itDoes = targetStats.source.guardingPose;
                            break;
                    }

                if (requirement.not) { itDoes = !itDoes; }
            }
        }

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
				if(action.target == TargetTypes.Self || target.Owner.Role == action.source.Owner.Role)
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

    /* GetAttackActionOutput doesn't actually make any changes to the cards!
     * It only generates the outcome of an attack. This makes it useful for displaying the outcome of all possible interactions. */
	public static AttackActionOutput GetAttackActionOutput(CardDisplay target, AttackAction attackAction)
	{
		AttackActionOutput output = new AttackActionOutput();
        CardDisplay attacker = attackAction.source;
        List<BuffAction> onHitBuffs = new();
        output.attackerStats = new(attacker);
        output.targetStats = new(target);

        if (!attackAction.isExtra)
            onHitBuffs.AddRange(attackAction.source.appliedBuffs.Where(x => x.activatesOnHit));

        /* Calculation of temporary buffs and debuffs */
        TempModifiers attackerTempModifiers = output.attackerModifiers;
        TempModifiers targetTempModifiers = output.targetModifiers;

        /* Extract and organize attack effects */
        List<AttackEffect> effectsFromAttack = attackAction.attackEffect.ToList();
		List<AttackEffect> effectsFromBuffs = new();

        if (!attackAction.isExtra)
            effectsFromBuffs.AddRange(attacker.appliedBuffs.Where(buff => buff.specialEffect == BuffSpecialEffects.GrantAttackEffect && buff.attackEffect.Count > 0).SelectMany(buff => buff.attackEffect));

        List<AttackEffect> attackEffectsBeforeAttack = effectsFromAttack.Concat(effectsFromBuffs).Where(atkFx => !atkFx.effectChecksAfterAttack).Select(atkFx => new AttackEffect(atkFx, attackAction)).ToList();
        List<AttackEffect> attackEffectsAfterAttack = effectsFromAttack.Concat(effectsFromBuffs).Where(atkFx => atkFx.effectChecksAfterAttack).Select(atkFx => new AttackEffect(atkFx, attackAction)).ToList();

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
        foreach (AttackEffect atkFx in attackEffectsBeforeAttack)
        {
            if (atkFx.StatsMeetRequirements(output.targetStats)) attackerTempModifiers.SetModifiersFromAttackEffect(atkFx);
        }
        if (!attackAction.isExtra)
            foreach (PlayerBuffs pBuff in attacker.Owner.BuffsForNextAttackingAlly)
            {
                if (!pBuff.hasBeenUsed)
                {
                    foreach (BuffAction buff in pBuff.buffs)
                    {
                        if (buff.TargetMeetsRequirements(attacker))
                        {
                            attackerTempModifiers.SetModifiersFromBuff(buff);
                        }
                    }
                }
            }

        output.attackerStats.ApplyModifiers(attackerTempModifiers);
        output.targetStats.ApplyModifiers(targetTempModifiers);


        int targetArmor = 0;
        DamageTypes damageType = DamageTypes.Melee;
        switch (attackAction.damageType)
        {
            case DamageTypes.Melee: damageType = DamageTypes.Melee; break;
            case DamageTypes.Ranged: damageType = DamageTypes.Ranged; break;
            case DamageTypes.Energy: damageType = DamageTypes.Energy;  break;
            case DamageTypes.MeleeOrRanged:
                if (target.armor[0] < target.armor[1]) { damageType = DamageTypes.Melee; }
                else { damageType = DamageTypes.Ranged; }
                break;
            case DamageTypes.RangedOrEnergy:
                if (target.armor[2] < target.armor[1]) { damageType = DamageTypes.Energy; }
                else { damageType = DamageTypes.Ranged; }
                break;
            case DamageTypes.MeleeOrEnergy:
                if (target.armor[0] < target.armor[2]) { damageType = DamageTypes.Melee; }
                else { damageType = DamageTypes.Energy; }
                break;
            case DamageTypes.MeleeOrRangedOrEnergy:
                if (target.armor[0] < target.armor[1] && target.armor[0] < target.armor[2]) { damageType = DamageTypes.Melee; }
                else if (target.armor[1] < target.armor[2]) { damageType = DamageTypes.Ranged; }
                else { damageType = DamageTypes.Energy; }
                break;
        }
        switch (damageType)
        {
            case DamageTypes.Melee: targetArmor += output.targetStats.armor[0]; break;
            case DamageTypes.Ranged: targetArmor += output.targetStats.armor[1]; break;
            case DamageTypes.Energy: targetArmor += output.targetStats.armor[2]; break;
        }
        targetArmor -= output.attackerStats.armorPierce;
        if (targetArmor < 0) { targetArmor = 0; }
        if (attackAction.damageType == DamageTypes.SelfDamage || attackAction.ignoresDefense)
        {
            targetArmor = 0;
        }

        int dmg = Mathf.FloorToInt((output.attackerStats.attack - output.targetStats.damageReduction) * (attackAction.damageMultiplier + attackerTempModifiers.DamageMultiplier)) - targetArmor;
        if (attackAction.flatDamageOverwrite > 0)
        {
            dmg = attackAction.flatDamageOverwrite - targetArmor;
        }
        if (dmg < attackerTempModifiers.MinDamageCap)
        {
            dmg = attackerTempModifiers.MinDamageCap;
        }
        if (attackAction.isExtra && target.IsImmuneToExtraAttacks)
        {
            dmg = 0;
        }
		output.damage = dmg;
		output.damageType = damageType;
		output.targetStats.hp -= dmg;

        foreach (AttackEffect atkFx in attackEffectsAfterAttack)
        {
            if (atkFx.StatsMeetRequirements(output.targetStats)) {
                attackerTempModifiers.usedAttackEffects.Add(atkFx);
                if(atkFx.effectType == AttackEffects.ApplyDebuff)
                {
                    foreach (BuffAction debuff in atkFx.buffs.Where(x => x.target == TargetTypes.SameTarget && x.TargetMeetsRequirements(target)))
                    {
                        output.appliedDebuffs.Add(debuff);
                        //GetActualTarget(target).ReceiveActiveBuff(debuff);
                    }
                }
            }
        }

        //if (!attackAction.isExtra)
        //    attackerTempModifiers.PerformExtraAttacks(attacker, target);

		bool canExecute = (output.targetStats.hp <= 2 && output.attackerStats.subtypes.Contains(UnitSubtype.Executioner) || attackerTempModifiers.willExecute );
		output.deathByExecution = canExecute;

        if (output.targetStats.hp <= 0 || canExecute ) {
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
            case TargetTypes.AlliesInLineInFrontOfMe: isIt = true; break;
            case TargetTypes.AlliesInLineBehind: isIt = true; break;
            case TargetTypes.MostHarmedAlly: isIt = true; break;
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
            case TargetTypes.AlliesInLineInFrontOfMe:
            case TargetTypes.AlliesInLineBehind:
            case TargetTypes.SingleAlly:
            case TargetTypes.MostHarmedAlly:
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
            case TargetTypes.MostHarmedAlly:
				itIs = false;
				break;
			case TargetTypes.AlliesInSameLine:
			case TargetTypes.LineOfEnemies:
			case TargetTypes.AllAllies:
			case TargetTypes.AlliesNextToMe:
            case TargetTypes.AllEnemies:
            case TargetTypes.AlliesInLineInFrontOfMe:
            case TargetTypes.AlliesInLineBehind:
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
			case BuffSpecialEffects.Stun:
			case BuffSpecialEffects.Disarm:
			case BuffSpecialEffects.Disrupt:
            case BuffSpecialEffects.GrantAttackEffect:
                itIs = false;
				break;
			case BuffSpecialEffects.EnableGuardingPose:
			case BuffSpecialEffects.TriggerExtraAttack:
            case BuffSpecialEffects.RepeatLastAllyAction:
				itIs = true;
				break;
		}
		if(buff.Attribute == Attributes.Health)
		{
			itIs = true;
		}
		return itIs;
    }

	public static void ActivateOnKillTriggers(List<CardDisplay> cards) {
		for (int i = 0; i < cards.Count; i++)
		{
			CardDisplay card = cards[i];
			bool isTheKiller = (i == cards.Count - 1);
			foreach (PassiveSkill passiveSkill in card.cardPassives)
			{
				bool killTriggerValid = false;
                if ( !card.usedPassives.Contains(passiveSkill) && ( (passiveSkill.trigger == TriggerTypes.OnScoringAKill && isTheKiller) || passiveSkill.trigger == TriggerTypes.OnAssistingAKill ) )
				{
					Debug.Log($"{card.card.Name} participated in the killing of someone and has something to say about it.");
					killTriggerValid = true;
                }

				if (killTriggerValid)
				{
					passiveSkill.playerBuffs.ForEach(pBuff => PerformPlayerBuffAction(pBuff));
					if (passiveSkill.oncePerRound)
					{
						if (passiveSkill.sharedAcrossAllCardsOfSameKind)
						{
                            card.Owner.GetActiveCards().Where(x => x.card.Name == card.card.Name && x.Owner == card.Owner && x.cardPassives.Exists(passive => passive.title == passiveSkill.title)).ToList().ForEach(x => x.usedPassives.Add(passiveSkill));
                        } else {
                            card.usedPassives.Add(passiveSkill);
                        }
					}

				}

			}
		}
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
    public int MinDamageCap = 1;
    public List<UnitSubtype> subtypes = new();
	//public int HPAtEndOfAction = 0;
	public List<BuffAction> usedBuffs = new();
	public List<AttackEffect> usedAttackEffects = new();
	public bool willExecute {
		get { return (usedAttackEffects.Exists(x => x.effectType == AttackEffects.Execute)); }
	}

	public TempModifiers() {

	}

    public void ApplyAfterAttackBuffs(AttackActionOutput attackOutput)
    {
        foreach (AttackEffect atkFx in usedAttackEffects)
        {
            if( (atkFx.effectType == AttackEffects.ApplyBuff || atkFx.effectType == AttackEffects.ApplyDebuff) && atkFx.effectChecksAfterAttack )
            {
                foreach (BuffAction buff in atkFx.buffs)
                {
                    CardActionTools.PerformBuffAction(buff,null,attackOutput);
                }
            }
        }
    }

	public void SetModifiersFromBuff(BuffAction buff)
	{
		usedBuffs.Add(new(buff));
		//if(buff.specialEffect == BuffSpecialEffects.GrantAttackEffect && buff.attackEffect.Count > 0) usedAttackEffects.AddRange(buff.attackEffect);
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
            case Attributes.MinDamageCap: MinDamageCap += buff.amount; break;
        }
    }

    public void SetModifiersFromAttackEffect(AttackEffect effect)
    {
        usedAttackEffects.Add(effect);
        List<CardDisplay> targets = new();

        switch (effect.effectType)
        {
            case AttackEffects.GainAttributesAsTemporaryAttack:
                switch (effect.from) /* Migrate this into a card action tool. */
                {
                    case TargetUnitDefinition.LastAllyWhoAttacked:
                        CardDisplay lastAllyWhoAttacked = effect.originAttack.source.Owner.LastPerformedAttackByAlly.CardInAction;
                        if(lastAllyWhoAttacked != null) targets.Add(lastAllyWhoAttacked);
                        break;
                    default: break;
                }
                targets.ForEach(target => {
                    effect.attributes.ForEach(attribute => {
                        switch (attribute) {
                            case Attributes.Attack: Attack += target.attack; break;
                        }
                    });
                });
                break;
            case AttackEffects.GainTemporarySubtypes:
                switch (effect.from)
                {
                    case TargetUnitDefinition.LastAllyWhoAttacked:
                        CardDisplay lastAllyWhoAttacked = effect.originAttack.source.Owner.LastPerformedAttackByAlly.CardInAction;
                        if (lastAllyWhoAttacked != null) targets.Add(lastAllyWhoAttacked);
                        break;
                    default: break;
                }
                targets.ForEach(target => {
                    foreach(UnitSubtype subtype in target.acquiredSubtypes.Concat(target.card.Subtypes))
                    {
                        switch (subtype)
                        {
                            case UnitSubtype.Executioner: subtypes.Add(subtype); break;
                            case UnitSubtype.Mercenary: subtypes.Add(subtype); break;
                            case UnitSubtype.Solitary: subtypes.Add(subtype); break;
                        }
                    }
                });
                break;
        }
    }

    //public void PerformExtraAttacks(CardDisplay attacker, CardDisplay target)
    //{
    //    List<AttackAction> extraAttacks = usedBuffs.Where(x => x.specialEffect == BuffSpecialEffects.TriggerExtraAttack).SelectMany(x => x.extraAttacks).ToList();
    //    foreach (AttackAction extraAttack in extraAttacks)
    //    {
    //        AttackAction newExtraAttack = new(extraAttack) { source = attacker, isExtra = true, attackEffect = { } };
    //        CardActionTools.PerformAttackAction(target, attacker, newExtraAttack);
    //    }
    //}

	public bool CheckComparison(Comparison comparisonType, int what, int toWhat) {
		bool checks = false;
		switch (comparisonType)
		{
			case Comparison.LessThan:
				checks = what < toWhat;
				break;
			case Comparison.LessThanOrEqual:
                checks = what <= toWhat;
                break;
			case Comparison.Equal:
                checks = what == toWhat;
                break;
			case Comparison.MoreThan:
                checks = what > toWhat;
                break;
			case Comparison.MoreThanOrEqual:
                checks = what >= toWhat;
                break;
			case Comparison.Not:
                checks = what != toWhat;
                break;
		}
		return checks;
	}
}
[Serializable]
public class StatList
{
	public int hp = 0;
    public int maxHP = 0;
	public int[] armor = { 0, 0, 0 };
	public int attack = 0;
	public int armorPierce = 0;
    public int damageReduction = 0;
    public int cost = 0;
    public List<UnitSubtype> subtypes = new();
    public List<Faction> origin = new();
    public CardDisplay source;

	public StatList() { }
    public StatList(CardDisplay cardSource) {
        source = cardSource;
        hp = cardSource.hp;
        maxHP = cardSource.maxHP;
        armor = cardSource.armor;
        attack = cardSource.attack;
        armorPierce = cardSource.armorPierce;
        damageReduction = cardSource.damageReduction;
        cost = cardSource.cost;
        subtypes = cardSource.card.Subtypes.Concat(cardSource.acquiredSubtypes).ToList();
        origin.AddRange(cardSource.card.Origin);
    }

    public void ApplyModifiers(TempModifiers modifiers)
    {
        hp += modifiers.Health;
        maxHP += modifiers.MaxHealth;
        armor[0] += modifiers.Armor[0];
        armor[1] += modifiers.Armor[1];
        armor[2] += modifiers.Armor[2];
        attack += modifiers.Attack;
        armorPierce += modifiers.ArmorPierce;
        damageReduction += modifiers.DamageReductionBeforeArmor + modifiers.DamageReductionAfterArmor;
        subtypes = subtypes.Union(modifiers.subtypes).ToList();
    }
}

[Serializable]
public class AttackActionOutput
{
	public int damage = 0;
	public bool resultsInDeath = false;
	public bool deathByExecution = false;
	public DamageTypes damageType = DamageTypes.Melee;
	public TempModifiers attackerModifiers = new();
	public TempModifiers targetModifiers = new();
    public StatList attackerStats;
    public StatList targetStats;
    public List<BuffAction> appliedDebuffs = new();
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