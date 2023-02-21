using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClayPuppetStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnShootExit += CardAbility;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootExit -= CardAbility;

        base.OnExit(calledByPacket, options);
    }

    private void CardAbility()
    {
        BelongingPlayer.GetCost(1);
    }
}
