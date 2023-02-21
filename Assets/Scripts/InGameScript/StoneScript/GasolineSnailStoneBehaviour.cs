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

    private void Grease(StoneBehaviour other)
    {
        if (other.BelongingPlayerEnum != BelongingPlayerEnum)
        {
            other.AddProperty(new GreasedProperty(other));
        }
    }
}
