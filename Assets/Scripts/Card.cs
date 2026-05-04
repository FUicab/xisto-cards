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
    public PowerRating powerRating;

    //private GameManager GM;

    void OnEnable(){
        powerRating = new PowerRating(this);
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