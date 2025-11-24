using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardAction{

    public ActionTypes actionType;
    public List<AttackAction> attacks;
    public List<BuffAction> buffs;

}

[System.Serializable]
public class AttackAction{
    public DamageTypes damageType;
    public float damageMultiplier = 1;
    public TargetTypes target = TargetTypes.SingleEnemy;
    public List<AttackEffect> attackEffect;
    public List<BuffAction> temporaryBuffs; //Temporary buffs are applied during the attack
    public List<Requirements> requirements;
}

[System.Serializable]
public class AttackEffect{
    public AttackEffects effectType;
    public bool useAttackValue;
    public int value;
    public List<BuffAction> buffs;
    public List<BuffAction> debuffs;
    public List<Requirements> requirements;
}

[System.Serializable]
public class BuffAction{
    public TargetTypes target;
    public Attributes Attribute;
    public int amount;
    public BuffSpecialEffects specialEffect;
    public List<Requirements> requirements;
    public List<SpecialBehavior> specialBehavior;
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
public class Skill{
    public bool canBeShared = false;
    public string title = "";
    public string description = "";
    public List<SkillEffect> skillEffects;
}

[System.Serializable]
public class SkillEffect{
    public TargetTypes target;
    public TriggerTypes trigger;
    public bool oncePerTurn;
    public bool oncePerGame;
    public List<Requirements> requirements;
    public List<BuffAction> buffs;
    public List<BuffAction> deBuffs;
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
    MaxHealth
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
    AlliesNextToMe
}
public enum SpecialBehavior{
    OnlyActivatesOnce
}
public enum RequirementTypes{
    TargetHasSubtypes,
    TargetIsNextTo,
    TargetBelongsToFactions,
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