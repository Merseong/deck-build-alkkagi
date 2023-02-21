using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriestStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if (calledByPacket)
        {
            if (!options.Equals(""))
            {
                StoneBehaviour stone = GameManager.Inst.FindStone(int.Parse(options));
                stone.AddProperty(new ShieldProperty(stone));
            }
        }
        else
        {
            float minDist = 10000f;
            StoneBehaviour minDistStone = null;
            foreach (StoneBehaviour stone in BelongingPlayer.Stones.Values)
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
                options = minDistStone.StoneId.ToString();
            }
        }

        base.OnEnter(calledByPacket, options);
    }
}
