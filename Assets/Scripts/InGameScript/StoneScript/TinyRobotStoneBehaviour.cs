using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TinyRobotStoneBehaviour: StoneBehaviour
{
    public override void OnExit(bool calledByPacket = false)
    {
        base.OnExit(calledByPacket);

        GameManager.Inst.players[(int)BelongingPlayer].DrawCards(1);
    }
}
