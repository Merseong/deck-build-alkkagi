using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasolineSnailStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnHit += Grease;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= Grease;

        base.OnExit(calledByPacket, options);
    }

    private void Grease(AkgRigidbody other)
    {
        if (other.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            StoneBehaviour stone = other.GetComponent<StoneBehaviour>();
            if (stone.BelongingPlayerEnum != BelongingPlayerEnum)
            {
                stone.AddProperty(new GreasedProperty(stone));
            }
        }
    }
}
