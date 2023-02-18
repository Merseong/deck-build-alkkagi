using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TinyRobotStoneBehaviour: StoneBehaviour
{
    public override void OnExit()
    {
        GameManager.Inst.players[(int)BelongingPlayer].DrawCards(1);
    }
}
