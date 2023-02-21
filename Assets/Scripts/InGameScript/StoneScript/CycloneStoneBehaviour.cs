using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycloneStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        foreach (StoneBehaviour stone in BelongingPlayer.Stones.Values)
        {
            if (stone != this)
                stone.AddProperty(new SprintProperty(stone));
        }

        base.OnEnter(calledByPacket, options);
    }
}
