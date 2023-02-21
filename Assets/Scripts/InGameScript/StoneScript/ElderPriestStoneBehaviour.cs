using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ElderPriestStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnHit += ApplyShield;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= ApplyShield;

        base.OnExit(calledByPacket, options);
    }

    private void ApplyShield(AkgRigidbody other)
    {
        if (other.layerMask.HasFlag(AkgLayerMask.STONE) && BelongingPlayer.StrikingStone == this)
        {
            StoneBehaviour stone = other.GetComponent<StoneBehaviour>();
            if (stone.BelongingPlayerEnum == BelongingPlayerEnum)
            {
                stone.AddProperty(new ShieldProperty(stone));
            }
        }
    }
}
