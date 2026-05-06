using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSkill
{
	
}

[System.Serializable]
public class CardAction : CardSkill
{
	public ActionTypes actionType;
	public List<AttackAction> attacks;
	public bool attackCountCanBeAugmented = false;
	public List<BuffAction> buffs;

	public CardAction(CardAction values)
	{
		actionType = values.actionType;
		attacks = values.attacks;
		attackCountCanBeAugmented = values.attackCountCanBeAugmented;
		buffs = values.buffs;
	}
}

public class ActiveAction
{
	public TargetTypes target;
    public List<Requirements> requirements;
    [HideInInspector] public CardDisplay source;
    [HideInInspector] public CardDisplay receiver;

    public bool isTargetImplicit { get { return CardActionTools.IsTargetImplicit(target); } }
	public List<CardDisplay> GetImplicitTargetsOfAction() { return CardActionTools.GetImplicitTargetsOfAction(this); }
	public bool TargetMeetsRequirements(CardDisplay target) { return CardActionTools.TargetMeetsRequirementsOfAction(target, this); }
	public bool targetIsFromMyTeam { get { return CardActionTools.TargetIsFromMyTeam(target); } }
	public bool isTargetPlural { get { return CardActionTools.IsTargetPlural(target); }  }
}

[System.Serializable]
public class AttackAction : ActiveAction{
	public DamageTypes damageType;
	public float damageMultiplier = 1;
	public int flatDamageOverwrite = 0; //This overwrite will make attacks deal a given amount of damage without taking into account the attack value of the card nor any modifiers
	public List<AttackEffect> attackEffect;
	public List<BuffAction> temporaryBuffs; //Temporary buffs are applied during the attack

	public AttackAction(AttackAction values)
	{
		damageType = values.damageType;
		damageMultiplier = values.damageMultiplier;
		target = values.target; 
		attackEffect = values.attackEffect;
		temporaryBuffs = values.temporaryBuffs;
		requirements = values.requirements;
		source = values.source;
		receiver = values.receiver;
	}

	public int CalculateDamage(CardDisplay target) { return CardActionTools.CalculateDamage(target, this); }
	public bool TargetCanBeReached(CardDisplay target) { return CardActionTools.TargetCanBeReachedByAttack(target, this); }
}

[System.Serializable]
public class BuffAction : ActiveAction{
    public Attributes Attribute;
	public int amount;
	public bool isDebuff = false; /* Check this if this is meant to be a negative effect for the one whoe receives it. */
	public bool amountCanBeAugmented = false;
	public bool activatesOnHit = false; /* Check this if the buff is meant to only activate during attacks. As if it was a temporary buff. */
	public List<Requirements> onHitRequirements; /* The buff will not activate its "on hit" benefits if these requirements are not met. */
	public BuffSpecialEffects specialEffect;
	public List<SpecialBehavior> specialBehavior;
	[HideInInspector] public PassiveSkill originPassive;

	public BuffAction(BuffAction values)
	{
		target = values.target;
		Attribute = values.Attribute;
		amount = values.amount;
		amountCanBeAugmented = values.amountCanBeAugmented;
		specialEffect = values.specialEffect;
		requirements = values.requirements;
		specialBehavior = values.specialBehavior;
		source = values.source;
		receiver = values.receiver;
		originPassive = values.originPassive;
		isDebuff = values.isDebuff;
		activatesOnHit = values.activatesOnHit;
		onHitRequirements = values.onHitRequirements;
	}
}

[System.Serializable]
public class AttackEffect
{
    public AttackEffects effectType;
    public bool useAttackValue;
    public int value;
    public bool valueCanBeAugmented = false;
    public List<BuffAction> buffs;
    //public List<BuffAction> debuffs;
    public List<Requirements> requirements;
}

public enum BuffSpecialEffects{
	None,
	RedirectAttacksTowardsMe
}

[System.Serializable]
public class Requirements{
	public RequirementTypes requirement;
	public List<UnitSubtype> subtypeRequirement;
	public List<Faction> factionRequirement;
	public List<TargetUnitDefinition> targetIs;
	public Attributes attribute;
	public Comparison comparison;
	public int attributeValue = 0;
	public bool targetOfRequirementIsTargetOfAttack;
}

[System.Serializable]
public class PassiveSkill : CardSkill{
	public string title = "";
	public string description = "";
	public TriggerTypes trigger;
	public bool canBeShared = false;
	public bool oncePerTurn = false;
	//public bool buffsAreTemporary = false;
	public bool requiresElementalExchange = false;
	public List<BuffAction> buffs;
	[HideInInspector] public CardDisplay source;

	public PassiveSkill(PassiveSkill passiveSkill) {
		title = passiveSkill.title;
		description = passiveSkill.description;
		trigger = passiveSkill.trigger;
		canBeShared = passiveSkill.canBeShared;
		oncePerTurn = passiveSkill.oncePerTurn;
		buffs = new List<BuffAction>();
		source = passiveSkill.source;
		requiresElementalExchange = passiveSkill.requiresElementalExchange;
	}
}

public enum Attributes{
	Attack,
	Health,
	Defense,
	DefenseMelee,
	DefenseRanged,
	DefenseEnergy,
	ArmorPierce,
	DamageReductionBeforeArmor,
	DamageReductionAfterArmor,
	MaxHealth,
	Cost
}
public enum ActionTypes{
	Attack,
	RepeatFromAbove,
	Buff,
	ApplyDebuff,
	DoNothing
}
public enum DamageTypes{
	Melee,
	Ranged,
	Energy,
	MeleeOrRanged,
	RangedOrEnergy,
	MeleeOrEnergy,
	MeleeOrRangedOrEnergy,
	SelfDamage
}
public enum AttackEffects{
	SplashDamage,
	SelfDamage,
	ApplyDebuff
}
public enum TargetTypes{
	Self,
	AlliesInSameLine,
	SingleEnemy,
	LineOfEnemies,
	AllAllies,
	SameTarget,
	AlliesNextToMe,
	SingleAlly,
	AllEnemies
}
public enum SpecialBehavior{
	OnlyActivatesOnce
}
public enum RequirementTypes{
	TargetIsNextTo,
	TargetHasSubtypesOrFactions,
	TargetHasAttackedThisRound,
	TargetAttributeIs
}
public enum Comparison
{
	LessThan,
	LessThanOrEqual,
	Equal,
	MoreThan,
	MoreThanOrEqual,
	Not
}
public enum TargetUnitDefinition{
	SameAsMyself
}
public enum TriggerTypes{
	OnAttack,
	OnBoardChange
}