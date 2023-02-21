using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightStoneBehaviour : StoneBehaviour
{
    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        base.OnExit(calledByPacket, options);

        if(BelongingPlayer == GameManager.PlayerEnum.LOCAL)
        {
            GameManager.Inst.knightEnterCount += 1;
            (GameManager.Inst.LocalPlayer as LocalPlayerBehaviour).SetKnightCommanderCost();
        }
    }
}