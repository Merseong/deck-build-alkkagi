using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CowardGhostStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        GameManager.Inst.players[(int)BelongingPlayer].AddCowardGhost(this);

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        GameManager.Inst.players[(int)BelongingPlayer].RemoveCowardGhost(this);

        base.OnExit(calledByPacket, options);
    }

    public void AbilityActivated()
    {
        AddProperty(new GhostProperty(this, 1));

        // AkgPhysicsManager.Inst.rigidbodyRecorder.SendEventOnly(new EventRecord
        // {
        //     eventEnum = EventEnum.POWER,
        //     stoneId = StoneId,
        //     eventMessage = "ENTER",
        //     time = Time.time,
        // });
    }

    // public override void ParseActionString(string actionStr)
    // {
    //     base.ParseActionString(actionStr);
    //     if (actionStr.StartsWith("ENTER"))
    //     {
    //         AbilityActivated();
    //     }
    // }

}