using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class FighterStoneBehaviour : StoneBehaviour
{
    private float punchForce = 100.0f;

    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        if (!calledByPacket)
        {
            OnShootExit += Punch;
        }

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnShootExit -= Punch;

        base.OnExit(calledByPacket, options);
    }

    private void Punch()
    {
        StoneBehaviour min = null;
        float minDist = 1.0f;

        PlayerBehaviour oppo = GameManager.Inst.GetOppoPlayer(BelongingPlayerEnum);
        foreach (StoneBehaviour oppoStone in oppo.Stones.Values)
        {
            float dist = Vector3.Distance(transform.position, oppoStone.transform.position) - GetComponent<AkgRigidbody>().circleRadius - oppoStone.GetComponent<AkgRigidbody>().circleRadius;
            if (dist < minDist)
            {
                min = oppoStone;
                minDist = dist;
            }
        }

        if (min != null)
        {
            // Punch animation

            if (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL && !min.IsGhost())
            {
                LocalPlayerBehaviour local = BelongingPlayer as LocalPlayerBehaviour;
                min.Shoot(Vector3.Normalize(min.transform.position - transform.position) * punchForce, local.IsLocalRotated, true, false);
            }
        }
    }
}
