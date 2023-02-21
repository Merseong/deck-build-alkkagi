using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClayShamanStoneBehaviour : StoneBehaviour
{
    public override void OnExit(bool calledByPacket = false)
    {
        base.OnExit(calledByPacket);

        GameManager.Inst.players[(int)BelongingPlayer].CardToHand(Util.GetCardDataFromID(13, GameManager.Inst.CardDatas), 3);
    }
}
