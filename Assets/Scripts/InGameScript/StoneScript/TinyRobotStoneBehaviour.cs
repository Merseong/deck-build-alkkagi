using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TinyRobotStoneBehaviour: StoneBehaviour
{
    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.DrawCards(1);

        base.OnExit(calledByPacket, options);
    }
}
