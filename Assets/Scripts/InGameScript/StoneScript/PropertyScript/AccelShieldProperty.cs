using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelShieldProperty : StoneProperty
{
    private bool hasAccelShield = true;

    public AccelShieldProperty(StoneBehaviour stone, int turn = -1) : base(stone, turn) { }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        //GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart += ResetAccelShield;
        baseStone.OnShootEnter += ResetAccelShield;
        baseStone.OnShootExit += UseAccelShield;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        //GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart -= ResetAccelShield;
        baseStone.OnShootEnter -= ResetAccelShield;
        baseStone.OnShootExit -= UseAccelShield;
    }

    public override bool HasAccelShield(bool value) { return value || hasAccelShield; }

    private void ResetAccelShield()
    {
        hasAccelShield = true;

        baseStone.GetComponent<AkgRigidbody>().layerMask |= AkgLayerMask.SHIELD;
    }

    public void UseAccelShield()
    {
        hasAccelShield = false;

        if (!baseStone.HasAccelShield() && baseStone.ShieldCount() <= 0)
            baseStone.GetComponent<AkgRigidbody>().layerMask &= ~AkgLayerMask.SHIELD;
    }
}
