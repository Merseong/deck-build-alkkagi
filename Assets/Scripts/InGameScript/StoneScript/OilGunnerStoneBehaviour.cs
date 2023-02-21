using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OilGunnerStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        foreach (StoneBehaviour stone in GameManager.Inst.AllStones.Values)
        {
            if (stone != this)
                stone.AddProperty(new GreasedProperty(stone));
        }

        base.OnEnter(calledByPacket, options);
    }
}
