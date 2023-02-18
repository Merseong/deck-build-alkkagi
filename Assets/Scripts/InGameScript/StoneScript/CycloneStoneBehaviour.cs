using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycloneStoneBehaviour : StoneBehaviour
{
    public override void OnEnter()
    {
        ApplySprint();
        AkgPhysicsManager.Inst.rigidbodyRecorder.SendEventOnly(new EventRecord
        {
            eventEnum = EventEnum.POWER,
            stoneId = StoneId,
            eventMessage = "ENTER",
            time = Time.time,
        });
    }

    public override void ParseActionString(string actionStr)
    {
        base.ParseActionString(actionStr);
        if (actionStr.StartsWith("ENTER"))
        {
            ApplySprint();
        }
    }

    private void ApplySprint()
    {
        PlayerBehaviour player = GameManager.Inst.players[(int)BelongingPlayer];
        foreach (StoneBehaviour stone in player.Stones.Values)
        {
            if (SprintProperty.IsAvailable(stone))
                stone.AddProperty(new SprintProperty(stone));
        }
    }
}
