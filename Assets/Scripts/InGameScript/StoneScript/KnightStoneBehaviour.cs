using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnightStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        if(BelongingPlayer == GameManager.PlayerEnum.LOCAL)
        {
            GameManager.Inst.knightEnterCount += 1;
            (GameManager.Inst.LocalPlayer as LocalPlayerBehaviour).SetKnightCommanderCost();
        }
    }
}