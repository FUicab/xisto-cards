using System.Collections;
using System.Collections.Generic;
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
    public Faction Origin;
    public int MaxHP;
    private int HP;
    public int Armor;
    public int Attack;
    public List<Skill> SkillSet;

    private GameManager GM;

    void Start(){
        HP = MaxHP;
        // GM = FindObjectOfType<GameManager>();
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

[System.Serializable]
public class StatModifier{
    public string attribute = ""; // attack, armor, hp, armorPierce, cost, ...
    public string originSkill = ""; // The name of the skill that provides the modifier
    public string provider = ""; // The name of the card who gives this modifier
    public int value = 0;
}

[System.Serializable]
public class Skill{
    public bool shared = false;
    public bool pasive = false;
    public string title = "";
    public string description = "";
    public List<SkillEffect> skillEffects;
}

[System.Serializable]
public class SkillEffect{
    public enum SkillTarget{
        toSelf
    }
    public enum SkillTriggers{
        onAttack
    }

    public SkillTarget target;
    public SkillTriggers trigger;
    public string[] conditions;
    public string[] effects;
    public bool oncePerTurn;
    public bool oncePerGame;
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
    Independent
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
    Inheritor
}