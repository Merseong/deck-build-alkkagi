using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClayPuppetStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnShootEnter += () =>{
            BelongingPlayer.GetCost(1);
        };

        base.OnEnter(calledByPacket, options);
    }
}
