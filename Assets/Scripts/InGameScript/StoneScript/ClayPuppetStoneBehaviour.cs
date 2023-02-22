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

    public override void ParseActionString(string actionStr)
    {
        base.ParseActionString(actionStr);
        
        if (actionStr.StartsWith("COST"))
        {
            BelongingPlayer.GetCost(1);
        }
    }

    private void CardAbility()
    {
        BelongingPlayer.GetCost(1);
        AkgPhysicsManager.Inst.rigidbodyRecorder.AppendEventRecord(StoneId, "COST");
    }
}
