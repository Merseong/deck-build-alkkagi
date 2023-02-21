using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlchemistStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnStart += CardAbility;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnEnd -= CardAbility;

        base.OnExit(calledByPacket, options);
    }

    private void CardAbility()
    {
        LocalPlayerBehaviour local = GameManager.Inst.LocalPlayer as LocalPlayerBehaviour;
        bool isRotated = (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL) == local.IsLocalRotated;
        if (isRotated ? (transform.position.z > 0) : (transform.position.z < 0))
        {
            BelongingPlayer.GetCost(2);
        }
    }
}
