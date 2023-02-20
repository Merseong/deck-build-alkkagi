using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClayPuppetStoneBehaviour : StoneBehaviour
{

    public override void OnEnter()
    {
        OnShootEnter += () =>{
            GameManager.Inst.players[(int)BelongingPlayer].GetCost(1);
        };
    }

}