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

[System.Serializable]
public class AttackAction{
    public DamageTypes damageType;
    public float damageMultiplier = 1;
    public int flatDamageOverwrite = 0; //This overwrite will make attacks deal a given amount of damage without taking into account the attack value of the card nor any modifiers
    public TargetTypes target = TargetTypes.SingleEnemy;
    public List<AttackEffect> attackEffect;
    public List<BuffAction> temporaryBuffs; //Temporary buffs are applied during the attack
    public List<Requirements> requirements;
    public CardDisplay source;
    [HideInInspector] public CardDisplay receiver;

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
}

[System.Serializable]
public class AttackEffect{
    public AttackEffects effectType;
    public bool useAttackValue;
    public int value;
    public bool valueCanBeAugmented = false;
    public List<BuffAction> buffs;
    public List<BuffAction> debuffs;
    public List<Requirements> requirements;
}

[System.Serializable]
public class BuffAction{
    public TargetTypes target;
    public Attributes Attribute;
    public int amount;
    public bool amountCanBeAugmented = false;
    public BuffSpecialEffects specialEffect;
    public List<Requirements> requirements;
    public List<SpecialBehavior> specialBehavior;
    public CardDisplay source;
    public CardDisplay receiver;

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
    }
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
}

[System.Serializable]
public class PassiveSkill : CardSkill{
    public string title = "";
    public string description = "";
    public TriggerTypes trigger;
    public bool canBeShared = false;
    public bool oncePerTurn = false;
    // public bool oncePerGame = false;
    public List<BuffAction> buffs;
    public List<BuffAction> deBuffs;
}

// [System.Serializable]
// public class SkillEffect{
//     public TargetTypes target;
//     public TriggerTypes trigger;
//     public bool oncePerTurn;
//     public bool oncePerGame;
//     public List<Requirements> requirements;
//     public List<BuffAction> buffs;
//     public List<BuffAction> deBuffs;
// }

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
    SingleAlly
}
public enum SpecialBehavior{
    OnlyActivatesOnce
}
public enum RequirementTypes{
    TargetIsNextTo,
    TargetHasSubtypesOrFactions,
    TargetHasAttackedThisRound
}
public enum TargetUnitDefinition{
    SameAsMyself
}
public enum TriggerTypes{
    OnAttack,
    OnBoardChange
}