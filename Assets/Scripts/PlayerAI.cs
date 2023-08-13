using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerProfile;

public class PlayerAI : MonoBehaviour
{
    public PlayerProfile Profile;
    
    /* --- AI personality properties --------------------------------------------- */
    public bool PrioritizesDefendersForDefendingOnly = true;
    public bool PrefersToSaveAtLeast2GoldPerTurn = true;
    public bool WouldNotLetOpponentOverwhelm = true;
    public GameManager GM;

    void Start(){
        GM = FindObjectOfType<GameManager>();
    }
    
    public void StartAIBehavior(){
        if(Profile == null){ return; }
        if(!Profile.useAI){ return; }
    }

}