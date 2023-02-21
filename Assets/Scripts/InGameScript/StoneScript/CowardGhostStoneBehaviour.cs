using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CowardGhostStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        GameManager.Inst.players[(int)BelongingPlayer].OnStoneHit += AbilityActivated;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        GameManager.Inst.players[(int)BelongingPlayer].OnStoneHit -= AbilityActivated;

        base.OnExit(calledByPacket, options);
    }

    public void AbilityActivated(StoneBehaviour stone)
    {
        AddProperty(new GhostProperty(this, 1));
    }
}