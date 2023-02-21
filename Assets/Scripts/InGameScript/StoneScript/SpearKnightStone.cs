using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpearKnightStone : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        base.OnEnter(calledByPacket, options);

        if(BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL)  // TODO
        {
            GameManager.Inst.knightEnterCount += 1;
            (GameManager.Inst.LocalPlayer as LocalPlayerBehaviour).SetKnightCommanderCost();
        }
    }
}
