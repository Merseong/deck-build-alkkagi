using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriestStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        PlayerBehaviour player = GameManager.Inst.players[(int)BelongingPlayer];
        float minDist = 10000f;
        StoneBehaviour minDistStone = null;
        foreach (StoneBehaviour stone in player.Stones.Values)
        {
            if (stone != this)
            {
                float dist = Vector3.Distance(transform.position, stone.gameObject.transform.position);
                if (dist < minDist)
                {
                    minDistStone = stone;
                    minDist = dist;
                }
            }
        }
        if (minDistStone != null)
        {
            minDistStone.AddProperty(new ShieldProperty(minDistStone));
        }

        base.OnEnter(calledByPacket, options);
    }
}
