using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CardSkill
{
	
}

[System.Serializable]
public class CardAction : CardSkill
{
	public ActionTypes actionType = ActionTypes.DoNothing;
	public List<AttackAction> attacks = new();
	public bool attackCountCanBeAugmented = false;
    public List<BuffAction> buffs = new();
	public List<PlayerBuffs> playerBuffs = new();

	public CardAction(CardAction values)
	{
		actionType = values.actionType;
		attacks = values.attacks;
		attackCountCanBeAugmented = values.attackCountCanBeAugmented;
		buffs = values.buffs;
		playerBuffs = values.playerBuffs;
	}
}

public class ActiveAction
{
	public TargetTypes target = TargetTypes.SingleEnemy;
    public List<Requirements> requirements = new();
    [SerializeReference] public CardDisplay source;
    [SerializeReference] public CardDisplay receiver;

	public ActiveAction()
	{

	}

    public bool isTargetImplicit { get { return CardActionTools.IsTargetImplicit(target); } }
	public List<CardDisplay> GetImplicitTargetsOfAction() { return CardActionTools.GetImplicitTargetsOfAction(this); }
    public List<CardDisplay> GetPotentialTargets() { return CardActionTools.GetPotentialTargetsForAction(this); }
    public bool TargetMeetsRequirements(CardDisplay targetCard) { return CardActionTools.TargetMeetsRequirementsOfAction(targetCard, this); }
	public bool targetIsFromMyTeam { get { return CardActionTools.TargetIsFromMyTeam(target); } }
	public bool isTargetPlural { get { return CardActionTools.IsTargetPlural(target); }  }
    public bool TargetCanBeReached(CardDisplay target) { return CardActionTools.TargetCanBeReachedByAction(target, this); }
    public void WarnPotentialTargetsAboutThisAction() {
		List<CardDisplay> potentialTargets = new(CardActionTools.GetPotentialTargetsForAction(this));
		CardActionTools.AllPlayingCards.ForEach(x => x.isPotentialTargetForPerformingAction = potentialTargets.Contains(x));
	}
}

[System.Serializable]
public class AttackAction : ActiveAction{
	public DamageTypes damageType;
	public float damageMultiplier = 1;
	public bool damageMultiplierCanBeAugmented = false;
	public int flatDamageOverwrite = 0; //This overwrite will make attacks deal a given amount of damage without taking into account the attack value of the card nor any modifiers
    public List<AttackEffect> attackEffect = new();
    public List<BuffAction> temporaryBuffs = new(); //Temporary buffs are applied during the attack
    public AttackActionOutput attackActionOutput = new();
    [HideInInspector] public bool isExtra = false; /* Extra attacks do not trigger counter attacks and other "on hit" effects. Armor pierce and executions work as normal. */

	public AttackAction(AttackAction values)
	{
		damageType = values.damageType;
		damageMultiplier = values.damageMultiplier;
		damageMultiplierCanBeAugmented = values.damageMultiplierCanBeAugmented;
		flatDamageOverwrite = values.flatDamageOverwrite;
		target = values.target; 
		//attackEffect = values.attackEffect;
		//temporaryBuffs = values.temporaryBuffs;
		//requirements = values.requirements;
		source = values.source;
		receiver = values.receiver;
		isExtra = values.isExtra;
		attackActionOutput = values.attackActionOutput;
		values.requirements.ForEach(req => { requirements.Add(new Requirements(req, this) ); });
		values.attackEffect.ForEach(atkFx => { attackEffect.Add(new AttackEffect(atkFx, this));  } );
		values.temporaryBuffs.ForEach(tempBuff => { temporaryBuffs.Add(new BuffAction(tempBuff, this) { source = source }); });
	}
	public AttackAction(CardDisplay sourceCard) /* Will generate a single melee attack action. */
	{
		damageType = DamageTypes.Melee;
		damageMultiplier = 1;
		source = sourceCard;
		isExtra = true;
	}

	public int CalculateDamage(CardDisplay target) { return CardActionTools.CalculateDamage(target, this); }
    public AttackActionOutput GetAttackActionOutput(CardDisplay target) { return CardActionTools.GetAttackActionOutput(target, this); }
}

[System.Serializable]
public class BuffAction : ActiveAction{
    public Attributes Attribute;
	public int amount;
	public bool isDebuff = false; /* Check this if this is meant to be a negative effect for the one whoe receives it. */
	public bool amountCanBeAugmented = false;
	public bool activatesOnHit = false; /* Check this if the buff is meant to only activate during attacks. As if it was a temporary buff. */
    public List<Requirements> onHitRequirements = new(); /* The buff will not activate its "on hit" benefits if these requirements are not met. */
	public BuffSpecialEffects specialEffect;
    public List<AttackAction> extraAttacks = new(); /* Applying buffs with extra attacks will trigger the attack inmediatly at the moment of application and the buff effect will not stay. If OnHit is set then the buff stays and the extra attack performs on each hit of the target. */
	public List<UnitSubtype> grantedSubtypes = new();
    public List<AttackEffect> attackEffect = new();
    public List<SpecialBehavior> specialBehavior = new();
	public bool multiplyThisBuff = false;
	public TargetUnitDefinition multiplyForEach = TargetUnitDefinition.BenefitedYatzasAndDoragons; /* The buff re-applies its effects with each unit that matches the definition. If no matches are found the buff does not apply. */
    public AttackAction originAttack;
    public PassiveSkill originPassive;

	public BuffAction(BuffAction values, AttackAction sourceAttack = null)
	{
		target = values.target;
		Attribute = values.Attribute;
		amount = values.amount;
		amountCanBeAugmented = values.amountCanBeAugmented;
		specialEffect = values.specialEffect;
        //requirements = values.requirements;
		source = values.source;
		receiver = values.receiver;
		originPassive = values.originPassive;
		originAttack = sourceAttack ?? values.originAttack;
		isDebuff = values.isDebuff;
		activatesOnHit = values.activatesOnHit;
		grantedSubtypes = values.grantedSubtypes;
        specialBehavior = values.specialBehavior;
		multiplyThisBuff = values.multiplyThisBuff;
		multiplyForEach = values.multiplyForEach;
		values.requirements.ForEach(req => { requirements?.Add(new Requirements(req, (originAttack != null ? originAttack : this) ) ); });
        values.onHitRequirements.ForEach(req => { onHitRequirements?.Add(new Requirements(req, (originAttack != null ? originAttack : this) ) ); });
        values.extraAttacks.ForEach(atk => { extraAttacks?.Add(new AttackAction(atk) { source = values.source, isExtra = true } ); });
        values.attackEffect.ForEach(atkFx => { attackEffect.Add(new AttackEffect(atkFx, null)); });
        //onHitRequirements = values.onHitRequirements;
    }
    public bool TargetMeetsOnHitRequirements(CardDisplay target) { return CardActionTools.TargetMeetsRequirements(target, onHitRequirements); }
    public bool IsBuffEffectOfInstantEffect() { return CardActionTools.IsBuffEffectOfInstantEffect(this); }
}

[System.Serializable]
public class AttackEffect
{
    public AttackEffects effectType;
    public bool useAttackValue;
    public int value;
    public bool valueCanBeAugmented = false;
	public bool effectChecksAfterAttack = false;
    public List<BuffAction> buffs = new();
    //public List<BuffAction> debuffs;
    public List<Requirements> requirements = new();
	[SerializeReference] public AttackAction originAttack;

	public AttackEffect(AttackEffect values, AttackAction attackSource)
	{
		effectType = values.effectType;
		useAttackValue = values.useAttackValue;
		value = values.value;
		valueCanBeAugmented = values.valueCanBeAugmented;
		effectChecksAfterAttack = values.effectChecksAfterAttack;
		originAttack = attackSource ?? values.originAttack;
		values.buffs.ForEach(buff => { buffs.Add(new BuffAction(buff, originAttack){ source = attackSource.source }); });
        values.requirements.ForEach(req => { requirements?.Add(new Requirements(req, originAttack) ); });
    }

    public bool TargetMeetsRequirements(CardDisplay target) { return CardActionTools.TargetMeetsRequirements(target, requirements); }
    public bool StatsMeetRequirements(StatList stats) { return CardActionTools.StatsMeetRequirements(stats, requirements); }
}

[System.Serializable]
public class Requirements{
	public RequirementTypes requirement;
	public List<UnitSubtype> subtypeRequirement;
	public List<Faction> factionRequirement;
	public List<TargetUnitDefinition> targetIs;
	public Attributes attribute;
	public Comparison comparison;
	public bool compareToMyAttribute;
	public Attributes myAttribute;
	public int attributeValue = 0;
	public bool targetOfRequirementIsTargetOfAttack;
	[SerializeReference] public ActiveAction originAction;

	public Requirements(Requirements requirements, ActiveAction originAction = null){
		requirement = requirements.requirement;
		subtypeRequirement = requirements.subtypeRequirement;
		factionRequirement = requirements.factionRequirement;
		targetIs = requirements.targetIs;
		attribute = requirements.attribute;
		comparison = requirements.comparison;
		attributeValue = requirements.attributeValue;
		targetOfRequirementIsTargetOfAttack = requirements.targetOfRequirementIsTargetOfAttack;
		this.originAction = originAction ?? requirements.originAction;
	}
}

[System.Serializable]
public class PassiveSkill : CardSkill{
	public string title = "";
	public string description = "";
	public TriggerTypes trigger;
	public bool canBeShared = false;
	public bool oncePerRound = false;
	public bool sharedAcrossAllCardsOfSameKind = false;
	//public bool buffsAreTemporary = false;
	public bool requiresElementalExchange = false;
	public List<BuffAction> buffs = new();
    public List<PlayerBuffs> playerBuffs = new();
	[HideInInspector] public CardDisplay source;
    [HideInInspector] private GameManager GM;

	public PassiveSkill(PassiveSkill passiveSkill) {
		title = passiveSkill.title;
		description = passiveSkill.description;
		trigger = passiveSkill.trigger;
		canBeShared = passiveSkill.canBeShared;
		oncePerRound = passiveSkill.oncePerRound;
		sharedAcrossAllCardsOfSameKind = passiveSkill.sharedAcrossAllCardsOfSameKind;
		//buffs = new List<BuffAction>();
		source = passiveSkill.source;
        passiveSkill.buffs.ForEach(buff => { buffs.Add(new BuffAction(buff, null) { source = source, originPassive = this }); });
        requiresElementalExchange = passiveSkill.requiresElementalExchange;
        GM = GameObject.FindObjectOfType<GameManager>();
    }

	/* Gets the list of performed attacks during this round that have used any buff provided by this passive, with distinction of each card instance. */
	public List<CardAction> GetAttackActionsWherePassiveHasBeenApplied(bool includeOthersOfSameKind = false)
	{
		List<CardAction> actions = new();
		actions.AddRange(GM.RoundActions.Select(tuAc => tuAc?.actionObject?.action).Where( (action) => {
			return action?.attacks?.Exists( (atk) => {
				return atk?.attackActionOutput?.attackerModifiers?.usedBuffs?.Exists( (buff) => {
					bool buffComesFromPassive = buff.originPassive != null;
					//if(buffComesFromPassive) Debug.Log($"Found a buff from <b>{buff.originPassive.source.card.Name}</b>'s passive");
					bool titlesMatch = buff.originPassive.title == title;
					//if(titlesMatch ) Debug.Log($"...coming from passive {buff.originPassive.title}");
					bool sourceOfPassiveMatch = (includeOthersOfSameKind ? buff.originPassive.source.card.Name == source.card.Name : buff.originPassive.source == source);
					//if (sourceOfPassiveMatch) Debug.Log($"...from {buff.originPassive.source.card.Name}");
					bool cardOwnerMatches = source.Owner == buff.originPassive.source.Owner;

                    bool itExists = buffComesFromPassive && titlesMatch && sourceOfPassiveMatch && cardOwnerMatches;
					//if(itExists) Debug.Log($"Found \"{title}\" in an action performed by {atk.source.card.Name}");
                    return itExists;
						}
					) ?? false;
				}) ?? false;
			}).ToList());
		return actions;
    }

	public bool HasBeenUsedThisRound {
		get { return (GetAttackActionsWherePassiveHasBeenApplied().Count > 0 || source.usedPassives.Exists(x => x.title == title) ); }
	}

    public bool HasBeenUsedThisRoundIncludingThoseOfMyKind
    {
        get { return (GetAttackActionsWherePassiveHasBeenApplied(true).Count > 0 || source.usedPassives.Exists(x => x.title == title) ); }
    }

	public bool CanBeUsedThisRound
	{
		get {
			//Debug.Log($"<b>{source.card.Name}</b>: Passive {title} has been used by me: {HasBeenUsedThisRound}");
   //         Debug.Log($"<b>{source.card.Name}</b>: Passive {title} has been used by someone of my kind: {HasBeenUsedThisRoundIncludingThoseOfMyKind}");
			bool canBeReusedInfinitely = !oncePerRound;
			bool canBeUsedOnceByMe = oncePerRound && !sharedAcrossAllCardsOfSameKind;
			bool canBeUsedOnceByAllCardsOfMyKind = oncePerRound && sharedAcrossAllCardsOfSameKind;
            bool canBeUsed = (canBeReusedInfinitely || ((canBeUsedOnceByMe && !HasBeenUsedThisRound) || (canBeUsedOnceByAllCardsOfMyKind && !HasBeenUsedThisRoundIncludingThoseOfMyKind)));
            //Debug.Log($"<b>{source.card.Name}</b>'s {title} -> Infinite uses: {canBeReusedInfinitely} | Once per turn: {canBeUsedOnceByMe} | Shared across all of my kind: {canBeUsedOnceByAllCardsOfMyKind} | <color=blue>Can be used this round?</color> <color={(canBeUsed ? "green" : "red")}>{canBeUsed}</color>");
            return canBeUsed;
		}
	}

}


[System.Serializable]
public class PlayerBuffs
{
	public PlayerTarget target = PlayerTarget.OwnerOfCard;
	public PlayerBuffTypes buffType = PlayerBuffTypes.AddGold;
	public int amount = 0;
	public bool canBeAugmented = false;
	[HideInInspector] public bool hasBeenUsed = false;
    [HideInInspector] public int usedAmount = 0;
    [HideInInspector] public int usableAmount { get {  return amount - usedAmount; }  }
    [HideInInspector] public CardDisplay source;
	[HideInInspector] public PassiveSkill originPassive;

	public PlayerBuffs(PlayerBuffs values)
	{
		target = values.target;
		buffType = values.buffType;
		amount = values.amount;
		canBeAugmented = values.canBeAugmented;
		hasBeenUsed = values.hasBeenUsed;
		usedAmount = values.usedAmount;
		source = values.source;
		originPassive = values.originPassive;
	}

	public bool IsOfInstantApplication
	{
		get
		{
			bool itIs = false;
			switch (buffType)
			{
				case PlayerBuffTypes.AddGold:
				case PlayerBuffTypes.RemoveGold:
				case PlayerBuffTypes.StealGold:
					itIs = true;
					break;
				case PlayerBuffTypes.ExecutionerThresholdModifier:
				case PlayerBuffTypes.MercenaryKillGoldReward:
					itIs = false;
					break;
			}
			return itIs;
		}
	}

	public void Reset()
	{
		hasBeenUsed = false;
		usedAmount = 0;
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
	Cost,
	DamageMultiplier,
	BaseAttack
}
public enum ActionTypes{
	Attack,
	RepeatFromAbove,
	Buff,
	ApplyDebuff,
	DoNothing,
	PlayerBuff
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
public enum TargetTypes{
	Self,
	AlliesInSameLine,
	SingleEnemy,
	LineOfEnemies,
	AllAllies,
	SameTarget,
	AlliesNextToMe,
	SingleAlly,
	AllEnemies,
	AlliesInLineInFrontOfMe,
	AlliesInLineBehind
}
public enum AttackEffects{
	SplashDamage,
	SelfDamage,
	ApplyDebuff,
	Execute
}
public enum BuffSpecialEffects
{
    None,
    RedirectAttacksTowardsMe,
    GrantSubtypes,
    TriggerExtraAttack,
    EnableGuardingPose,
	Stun,
	Disarm,
	Disrupt,
	GrantAttackEffect,
	AllowGuardingPoseRespondToRangedAttacks
}
public enum SpecialBehavior{
	OnlyActivatesOnce
}
public enum RequirementTypes{
	TargetIsNextTo,
	TargetHasSubtypesOrFactions,
	TargetHasAttackedThisRound,
	TargetAttributeIs,
	TargetIsInRowInFrontOf,
	TargetHasAffectedUnitDefinition,
	TargetIsStunned,
	TargetIsDisarmed,
	TargetIsDisrupted,
	TargetIsGuarding
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
	SameAsMyself,
	TheLeader,
	BenefitedYatzasAndDoragons
}
public enum TriggerTypes{
	OnAttack,
	OnBoardChange,
	OnAssistingAKill,
	OnScoringAKill
}
public enum PlayerTarget
{
	OwnerOfCard,
	OtherPlayer
}
public enum PlayerBuffTypes
{
	AddGold,
	RemoveGold,
	StealGold,
	ExecutionerThresholdModifier,
	MercenaryKillGoldReward,
	FreeAttackActions
}