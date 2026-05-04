using NUnit.Framework;
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
		bool itDoes = true;

        if (action.requirements.Count > 0) // Check attack requirements
        {
            itDoes = false;
            foreach (Requirements requirement in action.requirements)
            {
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
                        foreach (CardSpace neighborSpace in target.mySpace.SpacesNextToMe() )
                        {
							if(neighborSpace.PlayingCard != null)
							foreach (TargetUnitDefinition neighborType in requirement.targetIs)
							{
								switch (neighborType)
								{
									case TargetUnitDefinition.SameAsMyself:
										if(neighborSpace.PlayingCard.card.Name == target.card.Name)
											{
												itDoes = true;
											}
										break;
								}
							}
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

		/* Calculation of temporary buffs and debuffs */
		var attackerTempModifiers = (
			Attack: 0,
			Health: 0,
			MaxHealth: 0,
			Defense: 0,
			Armor: new List<int> { 0, 0, 0 },
			ArmorPierce: 0,
			DamageReductionBeforeArmor: 0,
			DamageReductionAfterArmor: 0
		);
		var targetTempModifiers = (
			Attack: 0,
			Health: 0,
			MaxHealth: 0,
			Defense: 0,
			Armor: new List<int> { 0, 0, 0 },
			ArmorPierce: 0,
			DamageReductionBeforeArmor: 0,
			DamageReductionAfterArmor: 0
		);
		foreach (BuffAction modifier in attackAction.temporaryBuffs)
		{
			if (attackAction.target == TargetTypes.Self) /* The modifiers apply to myself */
			{
				switch (modifier.Attribute)
				{
					case Attributes.Attack: attackerTempModifiers.Attack += modifier.amount; break;
					case Attributes.Health: attackerTempModifiers.Health += modifier.amount; break;
					case Attributes.MaxHealth: attackerTempModifiers.MaxHealth += modifier.amount; break;
					case Attributes.Defense: attackerTempModifiers.Defense += modifier.amount; break;
					case Attributes.DefenseMelee: attackerTempModifiers.Armor[0] += modifier.amount; break;
					case Attributes.DefenseRanged: attackerTempModifiers.Armor[1] += modifier.amount; break;
					case Attributes.DefenseEnergy: attackerTempModifiers.Armor[2] += modifier.amount; break;
					case Attributes.ArmorPierce: attackerTempModifiers.ArmorPierce += modifier.amount; break;
					case Attributes.DamageReductionBeforeArmor: attackerTempModifiers.DamageReductionBeforeArmor += modifier.amount; break;
					case Attributes.DamageReductionAfterArmor: attackerTempModifiers.DamageReductionAfterArmor += modifier.amount; break;
				}
			}
			else if (attackAction.target == TargetTypes.SingleEnemy)
			{
				switch (modifier.Attribute)
				{
					case Attributes.Attack: attackerTempModifiers.Attack += modifier.amount; break;
					case Attributes.Health: attackerTempModifiers.Health += modifier.amount; break;
					case Attributes.MaxHealth: attackerTempModifiers.MaxHealth += modifier.amount; break;
					case Attributes.Defense: attackerTempModifiers.Defense += modifier.amount; break;
					case Attributes.DefenseMelee: attackerTempModifiers.Armor[0] += modifier.amount; break;
					case Attributes.DefenseRanged: attackerTempModifiers.Armor[1] += modifier.amount; break;
					case Attributes.DefenseEnergy: attackerTempModifiers.Armor[2] += modifier.amount; break;
					case Attributes.ArmorPierce: attackerTempModifiers.ArmorPierce += modifier.amount; break;
					case Attributes.DamageReductionBeforeArmor: attackerTempModifiers.DamageReductionBeforeArmor += modifier.amount; break;
					case Attributes.DamageReductionAfterArmor: attackerTempModifiers.DamageReductionAfterArmor += modifier.amount; break;
				}
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
				itIs = true;
				break;
		}
		return itIs;
	}
}