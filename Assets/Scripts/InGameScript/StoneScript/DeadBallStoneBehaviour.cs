using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadBallStoneBehaviour : StoneBehaviour
{
    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        OnHit += KickCursed;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        OnHit -= KickCursed;

        base.OnExit(calledByPacket, options);
    }

    private void KickCursed(AkgRigidbody other)
    {
        if (other.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            StoneBehaviour stone = other.GetComponent<StoneBehaviour>();
            foreach (var property in stone.Properties)
            {
                if (property is CursedProperty)
                {
                    stone.RemoveStoneFromGame();
                    stone.StartCoroutine(stone.EIndirectExit(true));
                }
            }
        }
    }
}
