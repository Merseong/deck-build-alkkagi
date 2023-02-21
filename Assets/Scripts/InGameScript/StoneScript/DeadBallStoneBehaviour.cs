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

    private void KickCursed(StoneBehaviour other)
    {
        foreach (var property in other.Properties)
        {
            if (property is CursedProperty)
            {
                other.RemoveStoneFromGame();
                other.StartCoroutine(other.EIndirectExit(true));
            }
        }
    }
}
