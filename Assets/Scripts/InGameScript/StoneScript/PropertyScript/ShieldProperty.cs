using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldProperty : StoneProperty
{
    private int shieldCount;

    public ShieldProperty(StoneBehaviour stone, int shield = 1, int turn = -1) : base(stone, turn)
    {
        shieldCount = shield;
    }

    public override void OnAdded(bool isReplaced = false)
    {
        base.OnAdded(isReplaced);

        baseStone.OnHit += OnHit;

        baseStone.GetComponent<AkgRigidbody>().layerMask |= AkgPhysicsManager.AkgLayerMaskEnum.SHIELD;
    }

    public override void OnRemoved(bool isReplaced = false)
    {
        base.OnRemoved(isReplaced);

        baseStone.OnHit -= OnHit;

        baseStone.GetComponent<AkgRigidbody>().layerMask &= ~AkgPhysicsManager.AkgLayerMaskEnum.SHIELD;
    }

    public override int ShieldCount(int value) { return value + shieldCount; }

    private void OnHit(AkgRigidbody akg)
    {
        if (akg.layerMask.HasFlag(AkgPhysicsManager.AkgLayerMaskEnum.COLLIDED))
            DecreaseShieldCount();
    }

    private void DecreaseShieldCount()
    {
        shieldCount--;

        if (shieldCount <= 0)
            RemoveProperty();
    }
}
