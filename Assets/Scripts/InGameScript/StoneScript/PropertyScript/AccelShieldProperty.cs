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

        GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart += ResetAccelShield;
        baseStone.OnShootEnter += UseAccelShield;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        GameManager.Inst.GetPlayer(baseStone.BelongingPlayer).OnTurnStart -= ResetAccelShield;
        baseStone.OnShootEnter -= UseAccelShield;
    }

    public override bool HasAccelShield(bool value) { return value || hasAccelShield; }

    private void ResetAccelShield()
    {
        hasAccelShield = true;
    }

    private void UseAccelShield()
    {
        hasAccelShield = false;
    }
}