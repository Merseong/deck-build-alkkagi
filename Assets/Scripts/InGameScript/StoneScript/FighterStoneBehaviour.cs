using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class FighterStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnShootExit += Punch;

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

            if (!min.IsGhost())
            {
                LocalPlayerBehaviour local = GameManager.Inst.LocalPlayer as LocalPlayerBehaviour;
                bool isRotated = (BelongingPlayerEnum == GameManager.PlayerEnum.LOCAL) == local.IsLocalRotated;
                min.Shoot(transform.position, isRotated);
            }
        }
    }
}
