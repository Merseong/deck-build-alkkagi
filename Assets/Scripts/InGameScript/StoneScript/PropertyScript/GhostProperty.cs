using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostProperty : StoneProperty
{
    public GhostProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        baseStone.GetComponent<AkgRigidbody>().layerMask |= AkgLayerMask.GHOST;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        baseStone.GetComponent<AkgRigidbody>().layerMask &= ~AkgLayerMask.GHOST;
    }

    public override bool IsGhost(bool value) { return value || (remainingTurn != 0); }
}
