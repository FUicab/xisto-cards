using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable][CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{

    // public bool HasBeenPlayed;
    // public int HandIndex;

    public string Name;
    public int CardCount;
    public UnitType Type;
    public List<UnitSubtype> Subtypes;
    public int Cost;
    public Sprite Artwork;
    public List<Faction> Origin;
    public int MaxHP;
    // [HideInInspector] public int HP;
    public int[] Armor = new int[3];
    public int Attack;
    //public List<PassiveSkill> SkillSet;
    public List<PassiveSkill> Passives;
    public List<CardAction> CardActions;
    public float PowerPoints = 0f; // A number that serves to reference the power level of different cards

    //private GameManager GM;

    void OnEnable(){
        CalculatePowerPoints();
    }

    void CalculatePowerPoints() {
        float basePower = MaxHP + Armor.Sum()/3;
        float subtypeBonus = 0f;
        float actionOutputBonus = 0f;
        float passiveBonus = 0f;

        foreach (UnitSubtype subtype in Subtypes)
        {
            switch (subtype)
            {
                case UnitSubtype.Defender:
                    subtypeBonus += MaxHP/2 + Armor.Sum()/2;
                    break;
                case UnitSubtype.Dual:
                    subtypeBonus += Attack * 1.25f; // This unit subtype is no longer at use but we'll see what this could be used for
                    break;
                case UnitSubtype.Mercenary:
                    subtypeBonus += Attack * 0.25f;
                    break;
                case UnitSubtype.Assistant:
                    subtypeBonus += 1; // This unit subtype is no longer at use but we'll see what this could be used for
                    break;
                case UnitSubtype.Pacifist:
                    subtypeBonus -= Attack * 1.25f;
                    break;
                case UnitSubtype.Combo:
                    subtypeBonus += Attack;
                    break;
                case UnitSubtype.Executioner:
                    subtypeBonus += Attack;
                    break;
                case UnitSubtype.Noble:
                    subtypeBonus += 2;
                    break;
                case UnitSubtype.Solitary:
                    subtypeBonus += 1.5f;
                    break;
                case UnitSubtype.Inheritor:
                    subtypeBonus += basePower * 0.66f;
                    break;
                case UnitSubtype.Opportunist:
                    subtypeBonus += basePower * (0.15f * Origin.Count);
                    break;
            }
        }

        CardActionMenu actionMenu = new CardActionMenu(CardActions);
        float[] dicePowerMultipliers = { 1f, 0.95f, 0.9f, 0.85f, 0.8f, 0.75f};

        foreach (CardActionObject actionObj in actionMenu.actions)
        {
            float actionPower = 0f;
            float dicePower = 0f;
            for (int i = 0; i < actionObj.diceValues.Count; i++)
            {
                dicePower += dicePowerMultipliers[actionObj.diceValues[i]-1];
            }

            if (actionObj.action.actionType != ActionTypes.DoNothing)
            {
                switch (actionObj.action.actionType)
                {
                    case ActionTypes.Attack:
                        if (actionObj.action.attackCountCanBeAugmented)
                        {
                            actionPower += Attack * 0.1f;
                        }
                        foreach (AttackAction attack in actionObj.action.attacks)
                        {
                            float damagePower = Attack * attack.damageMultiplier;
                            float attackTypeMultiplier = 0.6f;
                            float requirementsPowerNerf = 1f;
                            switch (attack.damageType)
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
                            if(attack.requirements.Count > 0)
                            {
                                requirementsPowerNerf /= attack.requirements.Count + 1;
                                foreach (Requirements requirement in attack.requirements)
                                {
                                    if(requirement.factionRequirement.Count > 1)
                                    {
                                        requirementsPowerNerf *= 1f + ((requirement.factionRequirement.Count-1) * 0.1f );
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
                            foreach (BuffAction buff in attack.temporaryBuffs)
                            {
                                float augmentationBonus = 1f;
                                float temporaryBuffPower = 0f;
                                if(buff.amountCanBeAugmented) { augmentationBonus *= 1.1f; }
                                switch (buff.Attribute)
                                {
                                    case Attributes.Attack:
                                    case Attributes.ArmorPierce:
                                    case Attributes.Health:
                                    case Attributes.Defense:
                                        temporaryBuffPower += buff.amount * augmentationBonus;
                                        break;
                                    case Attributes.DefenseMelee:
                                    case Attributes.DefenseRanged:
                                    case Attributes.DefenseEnergy:
                                        temporaryBuffPower += (buff.amount * augmentationBonus) / 3;
                                        break;
                                    case Attributes.DamageReductionBeforeArmor:
                                    case Attributes.DamageReductionAfterArmor:
                                    case Attributes.MaxHealth:
                                        temporaryBuffPower += (buff.amount * augmentationBonus ) * 1.25f;
                                        break;
                                }
                                float temporaryBuffRequirementNerf = 1f;
                                if(buff.requirements.Count > 0)
                                {
                                    temporaryBuffRequirementNerf /= buff.requirements.Count + 1;
                                    foreach (Requirements requirement in buff.requirements)
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
                                actionPower += temporaryBuffPower * temporaryBuffRequirementNerf;
                            }
                            foreach (AttackEffect attackEffect in attack.attackEffect)
                            {
                                float baseValue = 0f;
                                float effectPower = 0f;
                                if (attackEffect.useAttackValue) { baseValue = Attack; } else { baseValue = attackEffect.value; }
                                switch (attackEffect.effectType)
                                {
                                    case AttackEffects.SplashDamage:
                                        effectPower += baseValue * 1.5f;
                                        break;
                                    case AttackEffects.SelfDamage:
                                        effectPower += baseValue * -1;
                                        break;
                                    case AttackEffects.ApplyDebuff:
                                        foreach (BuffAction buff in attackEffect.buffs)
                                        {
                                            float augmentationBonus = 1f;
                                            float debuffPower = 0f;
                                            if (buff.amountCanBeAugmented) { augmentationBonus *= 1.1f; }
                                            switch (buff.Attribute)
                                            {
                                                case Attributes.Attack:
                                                case Attributes.ArmorPierce:
                                                case Attributes.Health:
                                                case Attributes.Defense:
                                                    debuffPower += buff.amount * augmentationBonus;
                                                    break;
                                                case Attributes.DefenseMelee:
                                                case Attributes.DefenseRanged:
                                                case Attributes.DefenseEnergy:
                                                    debuffPower += (buff.amount * augmentationBonus) / 3;
                                                    break;
                                                case Attributes.DamageReductionBeforeArmor:
                                                case Attributes.DamageReductionAfterArmor:
                                                case Attributes.MaxHealth:
                                                    debuffPower += (buff.amount * augmentationBonus) * 1.25f;
                                                    break;
                                            }
                                            float debuffRequirementNerf = 1f;
                                            if(buff.requirements.Count > 0)
                                            {
                                                debuffRequirementNerf /= buff.requirements.Count + 1;
                                                foreach (Requirements requirement in buff.requirements)
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
                                            actionPower += debuffPower * debuffRequirementNerf;
                                        }
                                        break;
                                }
                                float attackEffectRequirementNerf = 1f;
                                if(attack.requirements.Count > 1)
                                {
                                    attackEffectRequirementNerf /= attack.requirements.Count;
                                    foreach (Requirements requirement in attack.requirements)
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
                                actionPower += effectPower * attackEffectRequirementNerf;
                            }

                            actionPower += damagePower * attackTypeMultiplier * requirementsPowerNerf;
                        }
                        break;
                    case ActionTypes.Buff:
                    case ActionTypes.ApplyDebuff:
                        foreach (BuffAction buff in actionObj.action.buffs)
                        {
                            float augmentationBonus = 1f;
                            float buffPower = 0f;
                            float requirementsPowerNerf = 1f;
                            if (buff.amountCanBeAugmented) { augmentationBonus *= 1.1f; }
                            switch (buff.target)
                            {
                                case TargetTypes.Self:
                                case TargetTypes.SingleAlly:
                                case TargetTypes.SingleEnemy:
                                case TargetTypes.SameTarget:
                                    augmentationBonus *= 1;
                                    break;
                                case TargetTypes.AlliesInSameLine:
                                    augmentationBonus *= 4;
                                    break;
                                case TargetTypes.LineOfEnemies:
                                    augmentationBonus *= 4;
                                    break;
                                case TargetTypes.AllAllies:
                                    augmentationBonus *= 8;
                                    break;
                                case TargetTypes.AlliesNextToMe:
                                    augmentationBonus *= 1.5f;
                                    break;
                            }
                            switch (buff.Attribute)
                            {
                                case Attributes.Attack:
                                case Attributes.ArmorPierce:
                                case Attributes.Health:
                                case Attributes.Defense:
                                    buffPower += buff.amount * augmentationBonus;
                                    break;
                                case Attributes.DefenseMelee:
                                case Attributes.DefenseRanged:
                                case Attributes.DefenseEnergy:
                                    buffPower += (buff.amount * augmentationBonus) / 3;
                                    break;
                                case Attributes.DamageReductionBeforeArmor:
                                case Attributes.DamageReductionAfterArmor:
                                case Attributes.MaxHealth:
                                    buffPower += (buff.amount * augmentationBonus) * 1.25f;
                                    break;
                            }
                            if (buff.requirements.Count > 0)
                            {
                                requirementsPowerNerf /= buff.requirements.Count + 1;
                                foreach (Requirements requirement in buff.requirements)
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
                            switch (buff.specialEffect)
                            {
                                case BuffSpecialEffects.RedirectAttacksTowardsMe:
                                    buffPower = MaxHP + Armor.Sum() / 3;
                                    break;
                            }
                            actionPower += buffPower * requirementsPowerNerf;
                        }
                        break;
                }
            }
            actionOutputBonus += (actionPower * dicePower ) / 6;
        }

        foreach (PassiveSkill passiveSkill in Passives)
        {
            float passivePower = 0f;
            foreach (BuffAction buff in passiveSkill.buffs)
            {
                float augmentationBonus = 1f;
                float buffPower = 0f;
                float requirementsPowerNerf = 1f;
                if (buff.amountCanBeAugmented) { augmentationBonus *= 1.1f; }
                switch (buff.target)
                {
                    case TargetTypes.Self:
                    case TargetTypes.SingleAlly:
                    case TargetTypes.SingleEnemy:
                    case TargetTypes.SameTarget:
                        augmentationBonus *= 1;
                        break;
                    case TargetTypes.AlliesInSameLine:
                        augmentationBonus *= 4;
                        break;
                    case TargetTypes.LineOfEnemies:
                        augmentationBonus *= 4;
                        break;
                    case TargetTypes.AllAllies:
                        augmentationBonus *= 8;
                        break;
                    case TargetTypes.AlliesNextToMe:
                        augmentationBonus *= 1.5f;
                        break;
                }
                switch (buff.Attribute)
                {
                    case Attributes.Attack:
                    case Attributes.ArmorPierce:
                    case Attributes.Health:
                    case Attributes.Defense:
                        buffPower += buff.amount * augmentationBonus;
                        break;
                    case Attributes.DefenseMelee:
                    case Attributes.DefenseRanged:
                    case Attributes.DefenseEnergy:
                        buffPower += (buff.amount * augmentationBonus) / 3;
                        break;
                    case Attributes.DamageReductionBeforeArmor:
                    case Attributes.DamageReductionAfterArmor:
                    case Attributes.MaxHealth:
                        buffPower += (buff.amount * augmentationBonus) * 1.25f;
                        break;
                }
                if (buff.requirements.Count > 0)
                {
                    requirementsPowerNerf /= buff.requirements.Count + 1;
                    foreach (Requirements requirement in buff.requirements)
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
                switch (buff.specialEffect)
                {
                    case BuffSpecialEffects.RedirectAttacksTowardsMe:
                        buffPower = MaxHP + Armor.Sum() / 3;
                        break;
                }
                passivePower += buffPower * requirementsPowerNerf;
            }
            if (passiveSkill.canBeShared) { passivePower *= 1.05f; }
            passiveBonus += passivePower;
        }

        PowerPoints = (basePower + subtypeBonus + actionOutputBonus + passiveBonus);
    }

    private void OnMouseDown(){
        // if(!HasBeenPlayed){
            // transform.position += Vector3.up * 5;
            // HasBeenPlayed = true;
            // GM.AvailableCardSlots[HandIndex] = true;
            // Invoke("MoveToDiscardPile", 2f);
        // }
    }

    void MoveToDiscardPile(){
        // GM.DiscardPile.Add(this);
        // gameObject.SetActive(false);
    }

}

public enum UnitType {
    Warrior,
    Support,
    Machine,
    Leader,
    Trap
};
public enum Faction {
    Protectors,
    Saggists,
    Keraneans,
    Voucari,
    Auro,
    Independent,
    Fennraign,
    Zikin,
    Tekvault
};
public enum UnitSubtype {
    Defender,
    Dual,
    Mercenary,
    Assistant,
    Pacifist,
    Combo,
    Executioner,
    Noble,
    Solitary,
    Inheritor,
    Opportunist,
    Yatza,
    Doragon
}