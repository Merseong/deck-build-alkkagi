using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevengerStoneBehaviour : StoneBehaviour
{
    private bool isHit = false;
    private StoneBehaviour hitStone = null;

    public override void OnEnter(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnStart += ResetIsHit;
        OnHit += SetIsHit;

        base.OnEnter(calledByPacket, options);
    }

    public override void OnExit(bool calledByPacket = false, string options = "")
    {
        BelongingPlayer.OnTurnStart -= ResetIsHit;
        OnHit -= SetIsHit;
        if (hitStone != null)
            hitStone.OnShootExit -= StoneAbility;

        base.OnExit(calledByPacket, options);
    }

    private void SetIsHit(AkgRigidbody other)
    {
        if (other.layerMask.HasFlag(AkgLayerMask.STONE))
        {
            StoneBehaviour otherStone = other.GetComponent<StoneBehaviour>();
            if (BelongingPlayerEnum != otherStone.BelongingPlayerEnum)
            {
                isHit = true;
                hitStone = otherStone;

                hitStone.OnShootExit += StoneAbility;
            }
        }
    }

    private void ResetIsHit()
    {
        isHit = false;
        hitStone = null;
    }

    private void StoneAbility()
    {
        hitStone.AddProperty(new CursedProperty(hitStone));

        hitStone.OnShootExit -= StoneAbility;
        ResetIsHit();
    }
}
